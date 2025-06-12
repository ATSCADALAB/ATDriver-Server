using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;
using Microsoft.Win32;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

namespace ATDriver_Server
{
    public partial class Mainfrm : Form
    {
        //Remote Driver
        private static ServiceHost ATDriverHost = null;
        private string _ipAddress = "";
        //Client for updating data from local to remote
        string endPointAddr = "";
        ATDriverWCFInterface ATDriverClient;

        //Local Driver
        LocalDriverServer myServer;
        string _ChannelName = "";
        string _DeviceName = "";
        string _TagName = "";
        //Synchronize Timer
        System.Timers.Timer SynTimer = new System.Timers.Timer();

        //Copy -Paste Facilities
        private Channel _CopyChannel = null;
        private Device _CopyDevice = null;
        private Tag _CopyTag = null;

        protected System.Windows.Forms.Timer _Timer = new System.Windows.Forms.Timer();
        protected int topItemIndex;
        protected int selectedIndex;
        protected int OldselectedIndex = 0;

        string _RemServerIP = "";
        string _FileLoc = "";
        bool _Exit = false;

        // Certificate

        private CertificateEngine certificateEngine = new CertificateEngine();

        public Mainfrm()
        {
            if (System.Diagnostics.
                Process.GetProcessesByName(System.IO.Path.
                GetFileNameWithoutExtension(System.Reflection.Assembly.
                GetEntryAssembly().Location)).Count() > 1)
                System.Diagnostics.Process.GetCurrentProcess().Kill();

            InitializeComponent();
        }

        #region MainForm
        private void Mainfrm_Load(object sender, EventArgs e)
        {
            myServer = new LocalDriverServer();

            //Load EventLogfile
            //Log into data files
            try
            {
                string _LogFile = AppDomain.CurrentDomain.BaseDirectory + "EventsLog.ate";

                if (File.Exists(_LogFile))
                {
                    string[] content = File.ReadAllLines(_LogFile);

                    foreach (string s in content)
                    {
                        Event ev = new Event(s.Split(new string[] { "   " }, StringSplitOptions.None)[0],
                                                s.Split(new string[] { "   " }, StringSplitOptions.None)[1],
                                                s.Split(new string[] { "   " }, StringSplitOptions.None)[3],
                                                s.Split(new string[] { "   " }, StringSplitOptions.None)[2]
                                                );
                        myServer.EventList.Add(ev);
                    }
                }
            }
            catch { }

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Project Files\\Init.dfi"))
            {
                try
                {

                    _FileLoc = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Project Files\\Init.dfi");
                    myServer.Deserialize(_FileLoc);

                    _RemServerIP = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Project Files\\StoredIP.dfi");

                    //Update Treeview
                    Tree_Update();
                }
                catch
                {
                    myServer = new LocalDriverServer();
                    myServer.ATDriverClient = ATDriverClient;

                    _FileLoc = "";
                    _RemServerIP = "";
                }

                myServer.Saved = true;

                treeView1.MouseDown += treeView1_MouseClick;
                treeView1.NodeMouseClick += treeView1_NodeMouseClick;

                this.Text = "ATDriver Server- Ver 4.0.0.5     " + _FileLoc;
            }

            _Timer.Interval = 300;
            _Timer.Tick += DeviceView;
            _Timer.Enabled = true;

            atListView1.MouseDown += atListView1_MouseDown;

            //Start Service for remote service
            StartService();

            //Create remote client for updating data for server
            ClientCreate();

            myServer.ATDriverClient = ATDriverClient;

            #region End of SynTimer

            //init Updating timer
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "Config.cf"))
            {
                try
                {
                    SynTimer.Interval = Convert.ToDouble(File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "Config.cf"));
                }
                catch
                {
                    SynTimer.Interval = 1000;
                }
            }
            else
            {
                SynTimer.Interval = 1000;
            }

            SynTimer.Elapsed += SynTimer_Tick;
            SynTimer.Enabled = true;
            #endregion

            _ChannelName = "";
            _DeviceName = "";
            _TagName = "";

            treeView1.AfterSelect += treeView1_AfterSelect;
            treeView1.DoubleClick += treeView1_DoubleClick;
            this.KeyPress += iCheckKey;

            atListView1.DoubleClick += atListView1_DoubleClick;
            //atListView1.Enter += atListView1_DoubleClick;

            this.FormClosing += Mainfrm_FormClosing;
            this.Resize += Mainfrm_Resize;

            this.WindowState = FormWindowState.Minimized;
            this.Hide();
        }

        private void iCheckKey(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            try
            {
                //If press enter key
                if (e.KeyChar == (char)13)
                {
                    Properties();
                }
            }
            catch { }
        }
        void atListView1_DoubleClick(object sender, EventArgs e)
        {
            Properties();
        }

        void treeView1_DoubleClick(object sender, EventArgs e)
        {
            Properties();
        }

        void Mainfrm_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        //Icon on system tray
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        //View - Icon right click
        private void viewToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }
        //Exit - Icon Right Click
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (myServer.Saved == false)
            {
                DialogResult dlr = new System.Windows.Forms.DialogResult();
                dlr = MessageBox.Show("The working project file is not saved. Do you want to save now?", "ATSCADA", MessageBoxButtons.YesNoCancel);
                if (dlr == System.Windows.Forms.DialogResult.Yes)
                {
                    saveToolStripMenuItem_Click(sender, e);
                }
            }

            DialogResult dl = new System.Windows.Forms.DialogResult();
            dl = MessageBox.Show("Quit ATDriver Server?", "ATSCADA", MessageBoxButtons.YesNo);
            if (dl == System.Windows.Forms.DialogResult.Yes)
            {
                //Log into data files
                try
                {
                    string content = "";
                    foreach (Event ev in myServer.EventList)
                    {
                        if (content != "")
                        {
                            content = content + Environment.NewLine + ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                        }
                        else
                        {
                            content = ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                        }
                    }

                    string _LogFile = AppDomain.CurrentDomain.BaseDirectory + "EventsLog.ate";

                    if (File.Exists(_LogFile))
                    {
                        File.Delete(_LogFile);
                    }

                    File.WriteAllText(_LogFile, content);

                    _Timer.Enabled = false;
                    timer1.Enabled = false;
                    SynTimer.Enabled = false;
                    _Exit = true;
                    foreach (Channel c in myServer.ChannelList)
                    {
                        foreach (Device d in c.DeviceList)
                        {
                            foreach (Tag t in d.TagList)
                            {
                                t.Dispose();
                            }

                            d.Dispose();
                        }

                        c.DriverLoader.Driver.Disconnect();
                        c.DriverLoader.Driver.Dispose();
                        c.Dispose();
                    }

                    this.Close();
                }
                catch { }
            }
        }
        void Mainfrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_FileLoc != "")
                File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "Project Files\\Init.dfi", _FileLoc);

            if (_Exit == false)
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                }
            }
        }

        #endregion

        #region For Service
        //Synchronize data between local and remote
        int exCount = 0;
        void SynTimer_Tick(object sender, EventArgs e)
        {
            SynTimer.Enabled = false;

            try
            {
                if (myServer != null && ATDriverClient != null)
                {
                    #region Delete Old
                    var channelList = ATDriverClient.ChannelNameList();
                    if (channelList != null)
                    {
                        foreach (string c in channelList)
                        {
                            //if deleted channel
                            if (myServer.GetChannel(c) == null)
                            {
                                ATDriverClient.RemoveChannel(c);
                            }
                            else //if it's existing channel
                            {
                                //Check for deleted device
                                var deviceList = ATDriverClient.DeviceNameList(c);
                                if (deviceList != null)
                                {
                                    foreach (string d in deviceList)
                                    {
                                        //deleted device
                                        if (myServer.GetChannel(c)?.GetDevice(d) == null)
                                        {
                                            ATDriverClient.RemoveDevice(c, d);
                                        }
                                        else//existing device
                                        {
                                            var tagList = ATDriverClient.TagNameList(c, d);
                                            if (tagList != null)
                                            {
                                                foreach (string t in tagList)
                                                {
                                                    //deleted tag
                                                    var tag = myServer.GetChannel(c)?.GetDevice(d)?.GetTag(t);
                                                    if (tag == null)
                                                    {
                                                        ATDriverClient.RemoveTag(c, d, t);
                                                    }
                                                    else//update Tag value
                                                    {
                                                        var address = $"{c}.{d}.{t}";
                                                        var tagClient = ATDriverClient.GetTag(address);
                                                        if (tagClient != null && !string.IsNullOrEmpty(tagClient.ValuetoWrite))
                                                        {
                                                            tag.ValuetoWrite = tagClient.ValuetoWrite;
                                                            ATDriverClient?.WritetoTag(address, "");
                                                        }
                                                        else
                                                            ATDriverClient.UpdateTag(c, d, t, tag.TimeStamp, tag.Status, tag.Value);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }

                    }

                    #endregion

                    exCount = 0;
                }
            }
            catch
            {
                // Returns a list of ipaddress configuration
                IPHostEntry ips = Dns.GetHostEntry(Dns.GetHostName());

                List<string> iplist = new List<string>();
                foreach (IPAddress ip in ips.AddressList)
                {
                    iplist.Add(ip.ToString());
                }
                if (!iplist.Contains(_ipAddress))
                {
                    MessageBox.Show("This Server IP is changed, Please restart ATDriver Server for updating", "ATSCADA");
                    return;
                }

                exCount++;

                if (exCount > 50)
                {
                    Thread.Sleep(1000);
                    ClientCreate();
                    exCount = 0;
                }//Recreate a new client 
            }

            SynTimer.Enabled = true;
        }

        //Start Service for remote connection
        private void StartService()
        {
            try
            {
                // Returns a list of ipaddress configuration
                string hostname = Dns.GetHostName();
                IPHostEntry ips = Dns.GetHostEntry(hostname);
                //IPHostEntry ips = Dns.GetHostEntry(Dns.GetHostName());

                if (ips.AddressList.Length > 1)
                {
                    //if Multi included RemServerIP                    
                    foreach (IPAddress ip in ips.AddressList)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                        {
                            if (_RemServerIP == ip.ToString())
                            {
                                _ipAddress = _RemServerIP;
                                break;
                            }
                        }
                    }

                    if (_ipAddress == "")
                    {
                        string[] _selectedIP = new string[1];
                        MultiIP mu = new MultiIP(_selectedIP);
                        mu.ShowDialog();
                        _ipAddress = _selectedIP[0];

                        //store
                        if (_ipAddress != "")
                            File.WriteAllText(AppDomain.CurrentDomain.BaseDirectory + "Project Files\\StoredIP.dfi", _ipAddress);
                    }
                }
                else
                    _ipAddress = ips.AddressList[0].ToString();

                //display
                toolStripStatusLabel2.Text = "Serving on: " + _ipAddress + "     ";


                #region CUSTOM BINDING

                // Tạo URL. Là địa chỉ của Service. Su dụng net.tcp protocol. Port mặc định là 8001
                // So với http, net.tcp là giao thức có thể duy trì kết nối theo phiên (Session) => Tính được số Client kết nối + tốc độ nhanh hơn (không cần phải kết nối liên tục)
                Uri tcpUrl = new Uri("net.tcp://" + _ipAddress + ":8000/ATDriverService");

                // Tạo Custom Binding. Xác thực Client (UserName, Password) trên tầng Transport
                var binding = new CustomNetTcpBinding();
                binding.OpenTimeout = TimeSpan.FromMinutes(2);
                binding.SendTimeout = TimeSpan.FromMinutes(2);
                binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                ATDriverHost = new ServiceHost(typeof(RemoteDriverServer), tcpUrl);
                ATDriverHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new CustomValidator(); // Mỗi lần request sẽ gọi phương thức Validate của CustomValidator để xác thực
                ATDriverHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                ATDriverHost.AddServiceEndpoint(typeof(ATDriverWCFInterface), binding, "");

                // Tạo Behavior. Cho phép gọi service qua action GET của http protocol.
                // Khi add serviceReference, sẽ nhập theo url (http://...:8001/ATDriverService) để tìm kiếm
                ServiceMetadataBehavior metadataBehavior;
                metadataBehavior = ATDriverHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                if (metadataBehavior == null)
                {

                    metadataBehavior = new ServiceMetadataBehavior();
                    metadataBehavior.HttpGetUrl = new Uri("http://" + _ipAddress.ToString() + ":8001/ATDriverService");
                    metadataBehavior.HttpGetEnabled = true;
                    metadataBehavior.ToString();

                    ATDriverHost.Description.Behaviors.Add(metadataBehavior);
                }
                ATDriverHost.Open();


                #endregion

                #region NOT Secured
                //// Create the url that is needed to specify where the service should be started
                //urlService = "net.tcp://" + _ipAddress + ":8000/ATDriverService";
                //// Instruct the ServiceHost that the type that is used is a ServiceLibrary.service1
                //ATDriverHost = new ServiceHost(typeof(RemoteDriverServer));

                //// The binding is where we can choose what transport layer we want to use. HTTP, TCP ect.
                //NetTcpBinding tcpBinding = new NetTcpBinding();
                //tcpBinding.TransactionFlow = false;

                //tcpBinding.MaxReceivedMessageSize = 2147483647;
                //tcpBinding.MaxBufferSize = 2147483647;

                //tcpBinding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                //tcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                //tcpBinding.Security.Mode = SecurityMode.None; // <- Very crucial                

                //// Add a endpoint
                //ATDriverHost.AddServiceEndpoint(typeof(ATDriverWCFInterface), tcpBinding, urlService);

                //ServiceMetadataBehavior metadataBehavior;
                //metadataBehavior = ATDriverHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                //if (metadataBehavior == null)
                //{
                //    // This is how I create the proxy object that is generated via the svcutil.exe tool
                //    metadataBehavior = new ServiceMetadataBehavior();
                //    metadataBehavior.HttpGetUrl = new Uri("http://" + _ipAddress.ToString() + ":8001/ATDriverService");
                //    metadataBehavior.HttpGetEnabled = true;
                //    metadataBehavior.ToString();

                //    ATDriverHost.Description.Behaviors.Add(metadataBehavior);
                //    urlMeta = metadataBehavior.HttpGetUrl.ToString();
                //}
                //ATDriverHost.Open();

                #endregion

                #region Good Test
                //// This is a address of our service
                //Uri httpUrl = new Uri("http://" + _ipAddress + ":8000/ATDriverService");
                ////Create ServiceHost
                //ATDriverHost = new ServiceHost(typeof(RemoteDriverServer), httpUrl);

                ///// Set behaviour of **binding**
                //BasicHttpBinding http = new BasicHttpBinding();
                //http.MaxReceivedMessageSize = 2147483647;
                //http.MaxBufferSize = 2147483647;

                ////1. set Mode TransportCredentialOnly = no httpS 
                //http.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                ////2. Transport security Basic = user id and password
                //http.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;


                /////** Set behaviour of **host**
                ////Add a service endpoint
                //ATDriverHost.AddServiceEndpoint(typeof(ATDriverWCFInterface), http, "");
                //ATDriverHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode =
                //    System.ServiceModel.Security.UserNamePasswordValidationMode.Custom;
                //ATDriverHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new MyCustomValidator();

                //// checking and publishing meta data
                //ServiceMetadataBehavior smb = ATDriverHost.Description.Behaviors.Find<ServiceMetadataBehavior>();
                //if (smb == null)
                //{
                //    smb = new ServiceMetadataBehavior();
                //    smb.HttpGetEnabled = true;
                //    ATDriverHost.Description.Behaviors.Add(smb);
                //}

                ////Start the Service
                //ATDriverHost.Open();

                #endregion

                #region CERTIFICATE

                //Uri tcpUrl = new Uri("net.tcp://" + _ipAddress + ":8000/ATDriverService");

                //var binding = new NetTcpBinding(SecurityMode.TransportWithMessageCredential, true);         
                //binding.MaxReceivedMessageSize = 2147483647;
                //binding.MaxBufferSize = 2147483647;
                //binding.MaxConnections = 10000;
                //binding.OpenTimeout = TimeSpan.FromMinutes(2);
                //binding.SendTimeout = TimeSpan.FromMinutes(2);
                //binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                //binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                //binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                //binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
                //ATDriverHost = new ServiceHost(typeof(RemoteDriverServer), tcpUrl);

                //ATDriverHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new MyCustomValidator();
                //ATDriverHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;
                //ATDriverHost.Credentials.ServiceCertificate.Certificate = this.certificateEngine.GetServerCertifcate();

                //ATDriverHost.AddServiceEndpoint(
                //    typeof(ATDriverWCFInterface),
                //    binding,
                //    "");

                //ServiceMetadataBehavior metadataBehavior;
                //metadataBehavior = ATDriverHost.Description.Behaviors.Find<ServiceMetadataBehavior>();

                //if (metadataBehavior == null)
                //{

                //    metadataBehavior = new ServiceMetadataBehavior();
                //    metadataBehavior.HttpGetUrl = new Uri("http://" + _ipAddress.ToString() + ":8001/ATDriverService");
                //    metadataBehavior.HttpGetEnabled = true;
                //    metadataBehavior.ToString();

                //    ATDriverHost.Description.Behaviors.Add(metadataBehavior);
                //}
                //ATDriverHost.Open();

                #endregion

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //Create a client of remote server
        private void ClientCreate()
        {
            try
            {
                #region CUSTOM BINDING
                // Dịa chỉ của Service
                endPointAddr = "net.tcp://" + _ipAddress + ":8000/ATDriverService";

                var binding = new CustomNetTcpBinding();
                binding.OpenTimeout = TimeSpan.FromMinutes(2);
                binding.SendTimeout = TimeSpan.FromMinutes(2);
                binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                var endpointAddress = new EndpointAddress(endPointAddr);
                var myChannelFactory = new ChannelFactory<ATDriverWCFInterface>(binding, endpointAddress);

                myChannelFactory.Credentials.UserName.UserName = Account.UserName; // Gán UserName cho Client
                myChannelFactory.Credentials.UserName.Password = Account.Password; // Gán Password cho Client

                ATDriverClient = myChannelFactory.CreateChannel();
                ATDriverClient.Open();

                // Gán InternalSession
                ServiceRepository.Instance.InternalSessionID = ATDriverClient.GetSessionID();

                #endregion

                #region CERTIFICATE

                //endPointAddr = "net.tcp://" + _ipAddress + ":8000/ATDriverService";

                //var binding = new NetTcpBinding(SecurityMode.TransportWithMessageCredential, true);               
                //binding.Security.Mode = SecurityMode.Message;
                //binding.MaxReceivedMessageSize = 2147483647;
                //binding.MaxBufferSize = 2147483647;
                //binding.MaxConnections = 10000;
                //binding.OpenTimeout = TimeSpan.FromMinutes(2);
                //binding.SendTimeout = TimeSpan.FromMinutes(2);
                //binding.ReceiveTimeout = TimeSpan.FromMinutes(10);

                //binding.Security.Transport.ProtectionLevel = System.Net.Security.ProtectionLevel.EncryptAndSign;
                //binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;
                //binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

                //EndpointAddress endpointAddress = new EndpointAddress(endPointAddr);

                //ChannelFactory<ATDriverWCFInterface> myChannelFactory =
                //    new ChannelFactory<ATDriverWCFInterface>(binding, endpointAddress);

                //myChannelFactory.Credentials.ClientCertificate.Certificate = this.certificateEngine.GetClientCertifcate();
                //myChannelFactory.Credentials.UserName.UserName = Account.UserName;
                //myChannelFactory.Credentials.UserName.Password = Account.Password;

                //ATDriverClient = myChannelFactory.CreateChannel();
                //ATDriverClient.Open();


                #endregion

                #region NOT Secured
                //endPointAddr = "net.tcp://" + _ipAddress + ":8000/ATDriverService";

                //NetTcpBinding tcpBinding = new NetTcpBinding();
                //tcpBinding.TransactionFlow = false;

                //tcpBinding.MaxReceivedMessageSize = 2147483647;
                //tcpBinding.MaxBufferSize = 2147483647;

                //tcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.Windows;
                //tcpBinding.Security.Mode = SecurityMode.None;

                //EndpointAddress endpointAddress = new EndpointAddress(endPointAddr);
                //ATDriverClient = ChannelFactory<ATDriverWCFInterface>.CreateChannel(tcpBinding, endpointAddress);
                #endregion

                #region Good Test
                //EndpointAddress Serviceaddress = new EndpointAddress("http://" + _ipAddress + ":8000/ATDriverService");

                ///// Set behaviour of **binding** Same setting as ##Server##
                //BasicHttpBinding httpBinding = new BasicHttpBinding();
                //httpBinding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
                //httpBinding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Basic;

                //ChannelFactory<ATDriverWCFInterface> myChannelFactory =
                //    new ChannelFactory<ATDriverWCFInterface>(httpBinding, Serviceaddress);
                //var defaultCredentials = myChannelFactory.Endpoint.Behaviors.Find<ClientCredentials>();

                ////#1 IF this dosen not work then try #2
                //myChannelFactory.Credentials.UserName.UserName = "VietnamSCADA";
                //myChannelFactory.Credentials.UserName.Password = "ATProCorp12345";

                /////#2
                ////ClientCredentials CC = new ClientCredentials();
                ////CC.UserName.UserName = "h";
                ////CC.UserName.Password = "p";
                //// myChannelFactory.Endpoint.Behaviors.Remove(defaultCredentials); //remove default ones
                //// myChannelFactory.Endpoint.Behaviors.Add(CC); //add required on

                //// Create a channel.
                //ATDriverClient = myChannelFactory.CreateChannel();

                #endregion

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region TreeView
        //For easy sliding by updown key
        void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                foreach (TreeNode n in treeView1.Nodes)
                {
                    n.ForeColor = Color.Black;
                    foreach (TreeNode nn in n.Nodes)
                        nn.ForeColor = Color.Black;
                }
                e.Node.ForeColor = Color.Blue;

                //Update Global Channel Name, DeviceName, TagName
                if (e.Node.Level == 0)//Add Device for upper Channel
                {
                    if (e.Node.Text != "Click to Add a Channel")
                    {
                        _ChannelName = e.Node.Text;
                        _DeviceName = "";
                        _TagName = "";
                        atListView1.Items.Clear();
                    }
                }
                else if (e.Node.Level == 1) //Add tag for the upper device
                {
                    _ChannelName = e.Node.Parent.Text;
                    _DeviceName = e.Node.Text;
                    _TagName = "";
                }
                atListView1.Items.Clear();
            }
            catch { }
        }

        //Click to add init channel
        void treeView1_New_Click(object sender, EventArgs e)
        {
            newChannelToolStripMenuItem_Click(sender, e);

            //Detect right click
            treeView1.MouseDown += treeView1_MouseClick;
            treeView1.NodeMouseClick += treeView1_NodeMouseClick;
        }
        //Treeview Right click to add channels or device
        void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                treeView1.SelectedNode = e.Node;

                foreach (TreeNode n in treeView1.Nodes)
                {
                    n.ForeColor = Color.Black;
                    foreach (TreeNode nn in n.Nodes)
                        nn.ForeColor = Color.Black;
                }
                e.Node.ForeColor = Color.Blue;

                //Update Global Channel Name, DeviceName, TagName
                if (e.Node.Level == 0)//Add Device for upper Channel
                {
                    if (e.Node.Text != "Click to Add a Channel")
                    {
                        _ChannelName = e.Node.Text;
                        _DeviceName = "";
                        _TagName = "";
                        atListView1.Items.Clear();
                    }
                }
                else if (e.Node.Level == 1) //Add tag for the upper device
                {
                    _ChannelName = e.Node.Parent.Text;
                    _DeviceName = e.Node.Text;
                    _TagName = "";
                    atListView1.Items.Clear();
                }

                //Right Click Event
                if (e.Button == MouseButtons.Right)
                {
                    e.Node.Checked = true;

                    contextMenuStrip1.Hide();

                    if (e.Node.Level == 0)//Add Device for upper Channel
                    {
                        _ChannelName = e.Node.Text;
                        _DeviceName = "";
                        _TagName = "";

                        Point pt = treeView1.PointToScreen(e.Location);
                        if (_CopyDevice == null)
                        {
                            contextMenuStrip2.Items[2].Enabled = false;
                        }
                        else
                        {
                            contextMenuStrip2.Items[2].Enabled = true;
                        }
                        contextMenuStrip2.Show(pt);
                    }
                    else if (e.Node.Level == 1) //Add tag for the upper device
                    {
                        _ChannelName = e.Node.Parent.Text;
                        _DeviceName = e.Node.Text;
                        _TagName = "";

                        Point pt = treeView1.PointToScreen(e.Location);
                        if (_CopyTag == null)
                        {
                            contextMenuStrip3.Items[3].Enabled = false;
                        }
                        else
                        {
                            contextMenuStrip3.Items[3].Enabled = true;
                        }
                        contextMenuStrip3.Show(pt);
                    }
                }
            }
            catch { }
        }
        //Detect right click for adding Channel
        void treeView1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    Point pt = treeView1.PointToScreen(e.Location);
                    if (_CopyChannel == null)
                    {
                        contextMenuStrip1.Items[1].Enabled = false;
                    }
                    else
                    {
                        contextMenuStrip1.Items[1].Enabled = true;
                    }
                    contextMenuStrip1.Show(pt);
                }
            }
            catch { }
        }
        public void Tree_Update()
        {
            try
            {
                treeView1.Nodes.Clear();
                foreach (Channel _C in myServer.ChannelList)
                {
                    TreeNode _tn = treeView1.Nodes.Add(_C.Name);
                    _tn.ImageIndex = 0;
                    foreach (Device _D in _C.DeviceList)
                    {
                        TreeNode _tn1 = _tn.Nodes.Add(_D.Name);
                        _tn1.ImageIndex = 1;
                    }
                }
                treeView1.ExpandAll();

            }
            catch { }
        }

        #endregion

        #region Listview

        //Realtime Device Update
        private void DeviceView(object o, EventArgs e)
        {
            _Timer.Enabled = false;
            try
            {
                if (ATDriverClient != null)
                {
                    var count = ATDriverClient.GetClientCounter() - 1;
                    toolStripStatusLabel1.Text = "Clients: " + (count > 0 ? count : 0).ToString();
                }                    
            }
            catch { }

            try
            {
                var tagList = myServer?.GetChannel(_ChannelName)?.GetDevice(_DeviceName)?.TagList;
                if (tagList != null)
                {
                    foreach (Tag _t in tagList)
                    {

                        if (_t.TimeStamp != null)
                        {
                            string[] str = new string[8];
                            str[0] = _t.Name;
                            str[1] = _t.Address;
                            str[2] = _t.DataType;
                            str[3] = _t.ClientAccess;
                            str[4] = _t.Value;
                            str[5] = _t.Status;
                            str[6] = _t.TimeStamp;
                            str[7] = _t.Description;

                            //If available -> update
                            bool upd = false;
                            foreach (ListViewItem i in atListView1.Items)
                            {
                                if (i.Text == str[0])
                                {
                                    if (i.SubItems[1].Text != str[1])
                                        i.SubItems[1].Text = str[1];
                                    if (i.SubItems[2].Text != str[2])
                                        i.SubItems[2].Text = str[2];
                                    if (i.SubItems[3].Text != str[3])
                                        i.SubItems[3].Text = str[3];
                                    if (i.SubItems[4].Text != str[4])
                                        i.SubItems[4].Text = str[4];
                                    if (i.SubItems[5].Text != str[5])
                                        i.SubItems[5].Text = str[5];
                                    if (i.SubItems[6].Text != str[6])
                                        i.SubItems[6].Text = str[6];
                                    if (i.SubItems[7].Text != str[7])
                                        i.SubItems[7].Text = str[7];
                                    upd = true;

                                    break;
                                }
                            }
                            //if not available -> add new
                            if (upd == false)
                            {
                                atListView1.Items.Add(new ListViewItem(str, 2));
                            }
                        }
                    }
                }

                //Delete in listview if deleted in Device
                foreach (ListViewItem i in atListView1.Items)
                {
                    bool ok = false;
                    foreach (Tag _t in myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).TagList)
                    {
                        if (_t.Name == i.SubItems[0].Text)
                        {
                            ok = true;
                            break;
                        }
                    }
                    if (ok == false)
                    {
                        atListView1.Items.Remove(i);
                    }
                }

                _Timer.Enabled = true;
            }
            catch (Exception ex) { _Timer.Enabled = true; }//MessageBox.Show(ex.ToString()); }

        }

        //Add tag for selected device
        void atListView1_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (_ChannelName != "" && _ChannelName != null && _DeviceName != "" && _DeviceName != null)
                    {

                        Point pt = atListView1.PointToScreen(e.Location);
                        if (_CopyTag == null)
                        {
                            contextMenuStrip4.Items[4].Enabled = false;
                        }
                        else
                        {
                            contextMenuStrip4.Items[4].Enabled = true;
                        }
                        contextMenuStrip4.Show(pt);
                    }
                }
            }
            catch { }

        }

        //ATListview - selected index changed
        private void atListView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                atListView1.Items[atListView1.SelectedItems[0].Index].ImageIndex = 3;
                selectedIndex = atListView1.SelectedItems[0].Index;

                _TagName = atListView1.SelectedItems[0].Text;
            }
            catch { atListView1.Items[selectedIndex].ImageIndex = 2; }
        }

        #endregion

        #region RightClick

        //Write Value
        private void writeTagValueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Write_Tag_Value wtv = new Write_Tag_Value(myServer, _ChannelName, _DeviceName, _TagName);
            wtv.ShowDialog();
        }

        //New Tag - ATListview Click
        private void newTagToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {

                Tag _myTag = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));

                //Init values
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode = "NewTag";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode = "";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode = "Default";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription = "";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode = "ReadWrite";

                AddTag ad = new AddTag(myServer.GetChannel(_ChannelName));
                ad.ShowDialog();

                _myTag.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode;
                //Get tag address from the designmode address
                _myTag.Address = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode;
                _myTag.DataType = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode;
                _myTag.Description = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription;
                _myTag.ClientAccess = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode;

                if (_myTag.Name != null && _myTag.Address != null && _myTag.Name != "" && _myTag.Address != "")
                {
                    //Add device to this channel
                    if (myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_myTag.Name) == null)
                    {
                        myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).AddTag(_myTag);
                        Tree_Update();
                    }
                    else
                    {
                        MessageBox.Show("This Tag name is existed in selected Device", "ATSCADA");
                        return;
                    }

                    myServer.Saved = false;
                }

            }
            catch { }
        }
        //New Tag - treeview right click
        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                Tag _myTag = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));

                //Init values
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode = "NewTag";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode = "";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode = "Default";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription = "";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode = "ReadWrite";

                //Addtag screen
                AddTag ad = new AddTag(myServer.GetChannel(_ChannelName));
                ad.ShowDialog();

                _myTag.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode;
                //Get tag address from the designmode address
                _myTag.Address = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode;
                _myTag.DataType = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode;
                _myTag.Description = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription;
                _myTag.ClientAccess = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode;


                if (_myTag.Name != null && _myTag.Address != null && _myTag.Name != "" && _myTag.Address != "")
                {
                    //Add device to this channel
                    if (myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_myTag.Name) == null)
                    {
                        myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).AddTag(_myTag);
                        Tree_Update();
                    }
                    else
                    {
                        MessageBox.Show("This Tag name is existed in selected Device", "ATSCADA");
                        return;
                    }

                    myServer.Saved = false;
                }
            }
            catch { }
        }

        //New channel - Treeview Right Click
        private void newChannelToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            newChannelToolStripMenuItem_Click(sender, e);
        }

        //Add Device
        private void newDeviceToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {

                Device _myDevice = new Device();

                _myDevice.Channel = myServer.GetChannel(_ChannelName);

                //Init values
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode = "NewDevice";
                myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode = "NewID";

                AddDevice ad = new AddDevice(myServer.GetChannel(_ChannelName));
                ad.ShowDialog();

                _myDevice.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode;
                _myDevice.ID = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode;

                if (_myDevice.Name != null && _myDevice.ID != null && _myDevice.Name != "" && _myDevice.ID != "")
                {
                    //Add device to this channel
                    if (myServer.GetChannel(_ChannelName).GetDevice(_myDevice.Name) == null)
                    {
                        myServer.GetChannel(_ChannelName).AddDevice(_myDevice);
                        Tree_Update();
                    }
                    else
                    {
                        MessageBox.Show("This Device name is existed in selected Channel", "ATSCADA");
                        return;
                    }
                    myServer.Saved = false;
                }

            }
            catch { }
        }

        #endregion

        #region Toolbar Facilities

        //New Project file
        private void selectDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (myServer.Saved == false)
                {
                    DialogResult dlr = new System.Windows.Forms.DialogResult();
                    dlr = MessageBox.Show("The working project file is not saved. Do you want to save now?", "ATSCADA", MessageBoxButtons.YesNoCancel);
                    if (dlr == System.Windows.Forms.DialogResult.Yes)
                    {
                        saveToolStripMenuItem_Click(sender, e);
                    }
                }

                //Close current project
                if (myServer != null)
                {
                    myServer.Dispose();
                }

                try
                {
                    treeView1.Click -= treeView1_New_Click;
                }
                catch { }

                //Create a new one
                myServer = new LocalDriverServer();
                myServer.ATDriverClient = ATDriverClient;

                atListView1.Items.Clear();

                treeView1.Nodes.Clear();
                treeView1.Nodes.Add("Click to Add a Channel");
                treeView1.Nodes[0].ForeColor = Color.Blue;
                this.Cursor = Cursors.Hand;

                treeView1.Click += treeView1_New_Click;

                _FileLoc = "";
                this.Text = "ATDriver Server - Ver 4.0.0.5      " + _FileLoc + " *";
                _ChannelName = "";
                _DeviceName = "";
                _TagName = "";
            }
            catch { }
        }
        //New channel - Menu
        private void newChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //get name and driver for channel 
                Channel _mychannel = new Channel(myServer);

                AddChannel ac = new AddChannel(_mychannel);
                ac.ShowDialog();

                //get channel address
                if (_mychannel.DriverLocation != "" && _mychannel.DriverLocation != null)
                {
                    treeView1.Click -= treeView1_New_Click;
                    this.Cursor = Cursors.Arrow;
                    if (treeView1.Nodes.Count > 0)
                        treeView1.Nodes[0].ForeColor = Color.Black;

                    ChannelAddress ca = new ChannelAddress(_mychannel);
                    ca.ShowDialog();

                    if (_mychannel.DriverLoader.Driver.Error == ErrorCodes.None)
                    {
                        _mychannel.Address = _mychannel.DriverLoader.Driver.ChannelAddress;


                        //check if this channel name is existing
                        if (myServer.GetChannel(_mychannel.Name) != null)
                        {
                            MessageBox.Show("This Channel name is existed", "ATSCADA");
                            _mychannel.Dispose();
                            return;
                        }

                        //foreach (Channel cc in myServer.ChannelList)
                        //{
                        //    if (_mychannel.Address == cc.Address)
                        //    {
                        //        MessageBox.Show("This Channel Address is existed", "ATSCADA");

                        //        _mychannel.Dispose();
                        //        _mychannel = null;
                        //        return;
                        //    }
                        //}

                        //Run Channel
                        _mychannel.Run = true;
                        myServer.AddChannel(_mychannel);
                        Tree_Update();

                        myServer.Saved = false;
                    }
                    else
                        MessageBox.Show("Could not add this channel", "ATSCADA");
                }

                //Detect right click on treeview
                treeView1.MouseDown += treeView1_MouseClick;
                treeView1.NodeMouseClick += treeView1_NodeMouseClick;

            }
            catch { }
        }
        //New Device -- Menu
        private void newDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            newDeviceToolStripMenuItem1_Click(sender, e);
        }
        //New Tag -- Menu
        private void newTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripMenuItem1_Click(sender, e);
        }
        //Exit Click
        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (myServer.Saved == false)
                {
                    DialogResult dlr = new System.Windows.Forms.DialogResult();
                    dlr = MessageBox.Show("The working project file is not saved. Do you want to save now?", "ATSCADA", MessageBoxButtons.YesNoCancel);
                    if (dlr == System.Windows.Forms.DialogResult.Yes)
                    {
                        saveToolStripMenuItem_Click(sender, e);
                    }
                }

                DialogResult dl = new System.Windows.Forms.DialogResult();
                dl = MessageBox.Show("Quit ATDriver Server?", "ATSCADA", MessageBoxButtons.YesNo);
                if (dl == System.Windows.Forms.DialogResult.Yes)
                {
                    //Log into data files
                    try
                    {
                        string content = "";
                        foreach (Event ev in myServer.EventList)
                        {
                            if (content != "")
                            {
                                content = content + Environment.NewLine + ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                            }
                            else
                            {
                                content = ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                            }
                        }
                        string _LogFile = AppDomain.CurrentDomain.BaseDirectory + "EventsLog.ate";

                        if (File.Exists(_LogFile))
                        {
                            File.Delete(_LogFile);
                        }

                        File.WriteAllText(_LogFile, content);
                    }
                    catch { }
                    _Timer.Enabled = false;
                    timer1.Enabled = false;
                    SynTimer.Enabled = false;

                    _Exit = true;
                    foreach (Channel c in myServer.ChannelList)
                    {
                        foreach (Device d in c.DeviceList)
                        {
                            foreach (Tag t in d.TagList)
                            {
                                t.Dispose();
                            }

                            d.Dispose();
                        }

                        c.DriverLoader.Driver.Disconnect();
                        c.DriverLoader.Driver.Dispose();
                        c.Dispose();
                    }

                    this.Close();
                }
            }
            catch { }
        }
        //Edit  Menu Click
        private void driverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_ChannelName == "")
                {
                    newChannelToolStripMenuItem.Enabled = true;
                    newDeviceToolStripMenuItem.Enabled = false;
                    newTagToolStripMenuItem.Enabled = false;

                    propertiesToolStripMenuItem3.Enabled = false;
                }
                else if (_ChannelName != "" && _DeviceName == "")
                {
                    newDeviceToolStripMenuItem.Enabled = true;
                    newTagToolStripMenuItem.Enabled = false;

                    propertiesToolStripMenuItem3.Enabled = true;
                }
                else if (_ChannelName != "" && _DeviceName != "")
                {
                    newDeviceToolStripMenuItem.Enabled = true;
                    newTagToolStripMenuItem.Enabled = true;

                    propertiesToolStripMenuItem3.Enabled = true;
                }

                if (_CopyChannel != null || _CopyDevice != null || _CopyTag != null)
                {
                    pasteToolStripMenuItem.Enabled = true;

                    if (_CopyDevice != null && _ChannelName == "")
                    {
                        pasteToolStripMenuItem.Enabled = false;
                    }

                    if (_CopyTag != null && _DeviceName == "")
                    {
                        pasteToolStripMenuItem.Enabled = false;
                    }
                }
                else
                    pasteToolStripMenuItem.Enabled = false;
            }
            catch { }
        }
        //Save As        
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open XML file           
            SaveFileDialog dlg = new SaveFileDialog();
            // setup a dialog;
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Project Files";
            dlg.Filter = "XML Files (*.xml)|*.xml";
            dlg.Title = "Save as ATDriverFile - ATSCADA";
            try
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    //Generate news                    
                    _FileLoc = dlg.FileName;

                    XmlTextWriter Output = new XmlTextWriter(_FileLoc, System.Text.Encoding.UTF8);
                    myServer.Serialize().WriteTo(Output);
                    Output.Close();

                    myServer.Saved = true;
                    this.Text = "ATDriver Server- Ver 4.0.0.5      " + _FileLoc;
                }

            }
            catch { }
        }
        //Save
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                // Determine whether the directory exists.
                if (File.Exists(_FileLoc))
                {
                    XmlTextWriter Output = new XmlTextWriter(_FileLoc, System.Text.Encoding.UTF8);
                    myServer.Serialize().WriteTo(Output);
                    Output.Close();

                    myServer.Saved = true;
                    this.Text = "ATDriver Server- Ver 4.0.0.5      " + _FileLoc;
                }
                else
                {
                    //Save as
                    saveAsToolStripMenuItem_Click(sender, e);
                }

            }
            catch { }


        }
        //Open Project file
        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {

                OpenFileDialog dlg = new OpenFileDialog();
                // setup a dialog;
                dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Project Files";
                dlg.Filter = "XML Files (*.xml)|*.xml";
                dlg.Title = "Select ATDriverFile - ATSCADA";
                //try
                //{
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    if (myServer.Saved == false)
                    {
                        DialogResult dlr = new System.Windows.Forms.DialogResult();
                        dlr = MessageBox.Show("The working project file is not saved. Do you want to save now?", "ATSCADA", MessageBoxButtons.YesNoCancel);
                        if (dlr == System.Windows.Forms.DialogResult.Yes)
                        {
                            saveToolStripMenuItem_Click(sender, e);
                        }
                    }

                    try
                    {
                        treeView1.Click -= treeView1_New_Click;
                    }
                    catch { }

                    this.Cursor = Cursors.Arrow;
                    if (treeView1.Nodes.Count > 0)
                        treeView1.Nodes[0].ForeColor = Color.Black;

                    if (myServer != null)
                    {
                        //Close existing Project
                        myServer.Dispose();
                    }

                    myServer = new LocalDriverServer();
                    myServer.ATDriverClient = ATDriverClient;

                    //Generate news                    
                    _FileLoc = dlg.FileName;

                    myServer.Deserialize(_FileLoc);

                    //Update Treeview
                    Tree_Update();

                    myServer.Saved = true;
                    this.Text = "ATDriver Server- Ver 4.0.0.5      " + _FileLoc;

                    treeView1.MouseDown += treeView1_MouseClick;
                    treeView1.NodeMouseClick += treeView1_NodeMouseClick;
                }
            }
            catch { }
        }
        //About
        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About ab = new About();
            ab.ShowDialog();
        }
        //Tool Click
        private void toolToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey rk = Registry.LocalMachine;
                RegistryKey StartupPath;
                StartupPath = rk.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);

                //if (StartupPath.GetValue("ATDriverServer") == null)
                //{
                //    quickClientToolStripMenuItem.Checked = false;
                //}
                //else
                //{
                //    quickClientToolStripMenuItem.Checked = true;
                //}
            }
            catch { }
        }

        //Save as Event Log
        private void saveAsTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open XML file           
            SaveFileDialog dlg = new SaveFileDialog();
            // setup a dialog;
            dlg.Filter = "txt Files (*.txt)|*.txt";
            dlg.Title = "Save as Text File - ATSCADA";
            try
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {

                    string _LogFile = dlg.FileName;

                    string content = "";
                    foreach (Event ev in myServer.EventList)
                    {
                        if (content != "")
                        {
                            content = content + Environment.NewLine + ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                        }
                        else
                        {
                            content = ev.Datetime + "   " + ev.Source + "   " + ev.Type + "   " + ev.EventString;
                        }
                    }

                    if (File.Exists(_LogFile))
                    {
                        File.Delete(_LogFile);
                    }

                    File.WriteAllText(_LogFile, content);
                }
            }
            catch { }
        }
        //Reset Event list
        private void resetToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult dl = new System.Windows.Forms.DialogResult();
                dl = MessageBox.Show("Reset Server Event Log?", "ATSCADA", MessageBoxButtons.YesNo);
                if (dl == System.Windows.Forms.DialogResult.Yes)
                {
                    myServer.EventList.Clear();
                }
            }
            catch { }
        }
        private void raiseS7EthernetServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("C:\\Program Files\\ATPro\\ATDriverServer\\S7EthernetServer.exe");
            }
            catch
            {

            }
        }
        #endregion

        #region Toolbar Statusbar
        private void toolBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (toolBarToolStripMenuItem.Checked == false)
                {
                    toolStrip1.Hide();
                    splitContainer2.Location = new Point(splitContainer2.Location.X, splitContainer2.Location.Y - 25);
                    splitContainer2.Height = splitContainer2.Height + 25;
                }
                else
                {
                    toolStrip1.Show();
                    splitContainer2.Location = new Point(splitContainer2.Location.X, splitContainer2.Location.Y + 25);
                    splitContainer2.Height = splitContainer2.Height - 25;
                }
            }
            catch { }
        }
        private void statusBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (statusBarToolStripMenuItem.Checked == false)
                {
                    statusStrip1.Hide();
                    splitContainer2.Height = splitContainer2.Height + 22;
                }
                else
                {
                    statusStrip1.Show();
                    splitContainer2.Height = splitContainer2.Height - 22;
                }
            }
            catch { }
        }
        #endregion

        #region CutCopyPasteDelete 

        private void Copy()
        {
            try
            {
                //Tag selected
                if (_TagName != "" && _DeviceName != "" && _ChannelName != "")
                {
                    _CopyTag = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));
                    _CopyTag.Name = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Name;
                    _CopyTag.Address = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Address;
                    _CopyTag.ClientAccess = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).ClientAccess;
                    _CopyTag.DataType = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).DataType;
                    _CopyTag.Description = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Description;
                    _CopyTag.Status = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Status;
                    _CopyTag.Value = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Value;
                }
                else if (_DeviceName != "" && _ChannelName != "")//Device selected
                {
                    _CopyDevice = new Device();
                    _CopyDevice.Name = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).Name;
                    _CopyDevice.ID = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).ID;

                    foreach (Tag t in myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).TagList)
                    {
                        Tag _ct = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));
                        _ct.Name = t.Name;
                        _ct.Address = t.Address;
                        _ct.ClientAccess = t.ClientAccess;
                        _ct.DataType = t.DataType;
                        _ct.Description = t.Description;
                        _ct.Status = t.Status;
                        _ct.Value = t.Value;

                        _CopyDevice.AddTag(_ct);
                    }

                    _OldChannelName = _ChannelName;
                    _OldDeviceName = _DeviceName;

                }
                else if (_ChannelName != "")
                {
                    _CopyChannel = new Channel(myServer);
                    _CopyChannel.Name = myServer.GetChannel(_ChannelName).Name;
                    _CopyChannel.Address = myServer.GetChannel(_ChannelName).Address;
                    _CopyChannel.DriverLocation = myServer.GetChannel(_ChannelName).DriverLocation;
                    _CopyChannel.UpdateRate = myServer.GetChannel(_ChannelName).UpdateRate;

                    foreach (Device d in myServer.GetChannel(_ChannelName).DeviceList)
                    {
                        Device _cd = new Device(myServer.GetChannel(_ChannelName), d.Name, d.ID);

                        foreach (Tag t in d.TagList)
                        {
                            Tag _ct = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));
                            _ct.Name = t.Name;
                            _ct.Address = t.Address;
                            _ct.ClientAccess = t.ClientAccess;
                            _ct.DataType = t.DataType;
                            _ct.Description = t.Description;
                            _ct.Status = t.Status;
                            _ct.Value = t.Value;

                            _cd.AddTag(_ct);
                        }
                        _CopyChannel.AddDevice(_cd);
                    }
                }
            }
            catch { MessageBox.Show("Something is wrong with Copy feature, Please restart the Server", "ATSCADA"); }

        }

        private void Cut()
        {
            try
            {
                myServer.Saved = false;

                //Tag selected
                if (_TagName != "" && _DeviceName != "" && _ChannelName != "")
                {
                    _CopyTag = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));
                    _CopyTag.Name = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Name;
                    _CopyTag.Address = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Address;
                    _CopyTag.ClientAccess = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).ClientAccess;
                    _CopyTag.DataType = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).DataType;
                    _CopyTag.Description = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Description;
                    _CopyTag.Status = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Status;
                    _CopyTag.Value = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName).Value;

                    myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).RemoveTag(_TagName);
                }
                else if (_DeviceName != "" && _ChannelName != "")//Device selected
                {
                    _CopyDevice = new Device();
                    _CopyDevice.Name = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).Name;
                    _CopyDevice.ID = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).ID;

                    foreach (Tag t in myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).TagList)
                    {
                        Tag _ct = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));
                        _ct.Name = t.Name;
                        _ct.Address = t.Address;
                        _ct.ClientAccess = t.ClientAccess;
                        _ct.DataType = t.DataType;
                        _ct.Description = t.Description;
                        _ct.Status = t.Status;
                        _ct.Value = t.Value;

                        _CopyDevice.AddTag(_ct);
                    }

                    myServer.GetChannel(_ChannelName).RemoveDevice(_DeviceName);

                    _OldChannelName = _ChannelName;
                    _OldDeviceName = _DeviceName;

                }

                Tree_Update();
            }
            catch { MessageBox.Show("Something is wrong with Cut feature, Please restart the Server", "ATSCADA"); }
        }

        //Paste Device
        string _OldDeviceName = "";
        string _OldChannelName = "";
        private void Paste()
        {
            try
            {

                if (_CopyTag != null)
                {

                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode = _CopyTag.Name;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode = _CopyTag.Address;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode = _CopyTag.ClientAccess;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode = _CopyTag.DataType;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription = _CopyTag.Description;

                    //Test for new Tag name
                    bool ok = false;
                    while (ok == false)
                    {
                        myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode =
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode + "1";

                        Tag t = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).TagList.Find(delegate (Tag _t) { return _t.Name == myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode; });
                        if (t == null)
                            ok = true;
                    }

                    TagProperties tp = new TagProperties(myServer.GetChannel(_ChannelName));
                    tp.ShowDialog();

                    _CopyTag.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode;
                    _CopyTag.Address = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode;
                    _CopyTag.DataType = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode;
                    _CopyTag.ClientAccess = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode;

                    if (_CopyTag.Name != "" && _CopyTag.Address != "" && _CopyTag.DataType != "" && _CopyTag.ClientAccess != "")
                    {
                        if (myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_CopyTag.Name) != null)
                        {
                            MessageBox.Show("This Tag is existed in the Device " + _CopyTag.Name, "ATSCADA");
                            _CopyTag.Dispose();
                            _CopyTag = null;
                        }
                        else
                        {
                            _CopyTag.Device = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName);
                            myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).AddTag(_CopyTag);
                            Tree_Update();
                            myServer.Saved = false;
                        }
                    }

                    //_TagName = "";

                    _CopyTag = null;
                }
                else if (_CopyDevice != null)
                {

                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode = _CopyDevice.Name;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode = _CopyDevice.ID;

                    //Test for new device
                    bool ok = false;
                    while (ok == false)
                    {
                        myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode =
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode + "1";

                        Device d = myServer.GetChannel(_ChannelName).DeviceList.Find(delegate (Device _d) { return _d.Name == myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode; });

                        if (d == null)
                            ok = true;
                    }

                    //This is maybe new channel 
                    DeviceProperties dp = new DeviceProperties(myServer.GetChannel(_ChannelName));
                    dp.ShowDialog();

                    _CopyDevice.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode;
                    _CopyDevice.ID = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode;

                    if (_CopyDevice.Name != "")
                    {
                        if (myServer.GetChannel(_ChannelName).GetDevice(_CopyDevice.Name) == null)
                        {
                            myServer.GetChannel(_ChannelName).AddDevice(_CopyDevice);

                            _CopyDevice.Channel = myServer.GetChannel(_ChannelName);

                            //Modify Device of each tag
                            foreach (Tag t in _CopyDevice.TagList)
                            {
                                t.Device = _CopyDevice;
                            }

                            Tree_Update();
                            myServer.Saved = false;
                        }
                        else
                        {
                            MessageBox.Show("This Device Name is existed in the Channel", "ATSCADA");
                        }
                    }

                    _CopyDevice = null;

                }
                else if (_CopyChannel != null)
                {
                    if (_CopyChannel.DriverLocation != "" && _CopyChannel.DriverLocation != null)
                    {
                        _CopyChannel.LoadDriver();

                        //Test for new channel
                        bool ok = false;
                        while (ok == false)
                        {
                            _CopyChannel.Name =
                               _CopyChannel.Name + "1";

                            Channel c = myServer.ChannelList.Find(delegate (Channel _c) { return _c.Name == _CopyChannel.Name; });
                            if (c == null)
                                ok = true;
                        }

                        ChannelProperties cp = new ChannelProperties(myServer, _CopyChannel, "Paste");
                        cp.ShowDialog();

                        if (_CopyChannel.Name != "")
                        {
                            //foreach (Channel cc in myServer.ChannelList)
                            //{
                            //    if (_CopyChannel.Address == cc.Address)
                            //    {
                            //        MessageBox.Show("This Channel Address is existed", "ATSCADA");

                            //        _CopyChannel.Dispose();
                            //        _CopyChannel = null;
                            //        return;
                            //    }
                            //}

                            if (myServer.GetChannel(_CopyChannel.Name) == null)
                            {
                                myServer.AddChannel(_CopyChannel);

                                //Update Channel for each Device
                                foreach (Device d in _CopyChannel.DeviceList)
                                {
                                    d.Channel = _CopyChannel;

                                    foreach (Tag t in d.TagList)
                                    {
                                        t.Device = d;
                                    }
                                }

                                _CopyChannel.Run = true;

                                Tree_Update();
                                myServer.Saved = false;
                            }
                            else
                            {
                                MessageBox.Show("This Channel Name is existed", "ATSCADA");
                            }
                        }
                    }
                    _CopyChannel = null;

                }
            }
            catch { MessageBox.Show("Something is wrong with Paste feature, Please restart the Server", "ATSCADA"); }
        }

        private void Delete()
        {
            try
            {
                if (_TagName != "")
                {
                    DialogResult dlr = MessageBox.Show("Delete Tag \"" + _TagName + "\"?", "ATSCADA", MessageBoxButtons.YesNo);

                    if (dlr == System.Windows.Forms.DialogResult.Yes)
                    {
                        myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).RemoveTag(_TagName);
                    }
                }
                else if (_ChannelName != "" && _ChannelName != null && _DeviceName != "" && _DeviceName != null)
                {

                    DialogResult dlr = MessageBox.Show("Delete Device \"" + _DeviceName + "\" and all associated Tags?", "ATSCADA", MessageBoxButtons.YesNo);
                    if (dlr == System.Windows.Forms.DialogResult.Yes)
                    {

                        Channel c = myServer.GetChannel(_ChannelName);

                        Device d = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName);
                        c.RemoveDevice(_DeviceName);

                        d.Dispose();

                        Tree_Update();

                        _ChannelName = "";
                        _DeviceName = "";
                    }

                }
                else if (_ChannelName != "" && _ChannelName != null)
                {
                    DialogResult dlr = MessageBox.Show("Delete Channel \"" + _ChannelName + "\" and all associated Devices?", "ATSCADA", MessageBoxButtons.YesNo);

                    if (dlr == System.Windows.Forms.DialogResult.Yes)
                    {

                        Channel c = myServer.GetChannel(_ChannelName);
                        myServer.RemoveChannel(_ChannelName);
                        c.Dispose();

                        Tree_Update();

                        if (myServer.ChannelList.Count == 0)
                        {
                            treeView1.Nodes.Add("Click to Add a Channel");
                            treeView1.Nodes[0].ForeColor = Color.Blue;
                            this.Cursor = Cursors.Hand;

                            treeView1.Click += treeView1_New_Click;
                        }

                        _ChannelName = "";
                        _DeviceName = "";
                    }
                }
                myServer.Saved = false;

            }
            catch { MessageBox.Show("Something is wrong with Paste feature, Please restart the Server", "ATSCADA"); }
        }

        private void Properties()
        {
            _Timer.Enabled = false;

            try
            {
                if (_TagName != "")
                {
                    Tag _t = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_TagName);

                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode = _t.Name;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode = _t.Address;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode = _t.DataType;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode = _t.ClientAccess;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagDescription = _t.Description;

                    TagProperties tp = new TagProperties(myServer.GetChannel(_ChannelName));
                    tp.ShowDialog();

                    if (myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode != "")
                    {
                        if (myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode != _t.Name ||
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode != _t.Address ||
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode != _t.DataType ||
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode != _t.ClientAccess
                            )
                        {

                            _t.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagNameDesignMode;
                            _t.Address = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagAddressDesignMode;
                            _t.DataType = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagTypeDesignMode;
                            _t.ClientAccess = myServer.GetChannel(_ChannelName).DriverLoader.Driver.TagClientAccessDesignMode;

                            myServer.Saved = false;
                        }
                    }
                }
                else if (_DeviceName != "")
                {
                    Device _d = myServer.GetChannel(_ChannelName).GetDevice(_DeviceName);

                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode = _d.Name;
                    myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode = _d.ID;

                    DeviceProperties dp = new DeviceProperties(myServer.GetChannel(_ChannelName));
                    dp.ShowDialog();

                    if (myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode != ""
                        && myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode != "")
                    {
                        if (myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode != _d.Name ||
                            myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode != _d.ID
                            )
                        {
                            _d.Name = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceNameDesignMode;
                            _d.ID = myServer.GetChannel(_ChannelName).DriverLoader.Driver.DeviceIDDesignMode;

                            myServer.Saved = false;
                        }
                    }

                    Tree_Update();
                    _DeviceName = _d.Name;
                }
                else if (_ChannelName != "")
                {
                    Channel _c = myServer.GetChannel(_ChannelName);
                    ChannelProperties cp = new ChannelProperties(myServer, _c, "Pro");
                    cp.ShowDialog();
                    Tree_Update();
                    _ChannelName = _c.Name;
                }
            }
            catch { MessageBox.Show("Something is wrong with Properties feature, Please restart the Server", "ATSCADA"); }
            _Timer.Enabled = true;
        }

        #endregion

        #region Basic Functions
        private void cutToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Cut();
        }
        private void cutToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Cut();
        }
        private void pasteChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void copyChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void deleteDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void propertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Properties();
        }

        private void copyDeviceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void propertiesToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Properties();
        }

        private void copyTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteTagToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void deleteTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void propertiesToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            Properties();
        }

        private void newToolStripButton_Click(object sender, EventArgs e)
        {
            selectDriverToolStripMenuItem_Click(sender, e);
        }

        private void openToolStripButton_Click(object sender, EventArgs e)
        {
            openToolStripMenuItem_Click(sender, e);
        }

        private void saveToolStripButton_Click(object sender, EventArgs e)
        {
            saveToolStripMenuItem_Click(sender, e);
        }

        private void cutToolStripButton_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void copyToolStripButton_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteToolStripButton_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void deleteToolStripbutton_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void NewChannel_Click(object sender, EventArgs e)
        {
            newChannelToolStripMenuItem_Click(sender, e);
        }

        private void NewDevice_Click(object sender, EventArgs e)
        {
            newDeviceToolStripMenuItem_Click(sender, e);
        }

        private void NewTag_Click(object sender, EventArgs e)
        {
            newTagToolStripMenuItem_Click(sender, e);
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Paste();
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void propertiesToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Properties();
        }
        private void helpContentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProcessStartInfo sInfo = new ProcessStartInfo("http://atscada.com/?s=ATDriver + Server");
            Process.Start(sInfo);
        }

        int EventCount = 0;

        string _ToolStriptempstatus = "";
        private void importCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                //Ask to open file
                OpenFileDialog dlg = new OpenFileDialog();
                // setup a dialog;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.Filter = "CSV Files (*.csv)|*.csv";
                dlg.Title = "Select CSV DeviceTagTemplate file to import";
                //try
                //{
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    //display on skin
                    _ToolStriptempstatus = toolStripStatusLabel2.Text;
                    toolStripStatusLabel2.Text = "CSV importing ...";

                    var reader = new StreamReader(File.OpenRead(dlg.FileName));

                    //read and reject first line because its title line
                    reader.ReadLine();

                    //read all content
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        Tag _myTag = new Tag(myServer.GetChannel(_ChannelName).GetDevice(_DeviceName));

                        removeSpecialChar(ref values[0], true);
                        _myTag.Name = values[0];

                        /* ---- 25/02/2022 - LeHoaiNam
                         * Not remove special address
                        */
                        //removeSpecialChar(ref values[1], false);


                        _myTag.Address = values[1];

                        //Data Type
                        removeSpecialChar(ref values[2], true);
                        if (values[2].Contains("Bool") || values[2].Contains("bool"))
                            _myTag.DataType = "Bool";
                        else if (values[2] == "Byte" || values[2] == "byte")
                            _myTag.DataType = "Byte";
                        else if (values[2] == "word" || values[2] == "Word")
                            _myTag.DataType = "Word";
                        else if (values[2].Contains("hort"))
                            _myTag.DataType = "Short";
                        else if (values[2] == "DWord" || values[2] == "dword" || values[2] == "Dword")
                            _myTag.DataType = "DWord";
                        else if (values[2].Contains("ong"))
                            _myTag.DataType = "Long";
                        else if (values[2].Contains("loat"))
                            _myTag.DataType = "Float";
                        else if (values[2] == "Double" || values[2] == "double")
                            _myTag.DataType = "Double";
                        else if (values[2] == "String" || values[2] == "string")
                            _myTag.DataType = "String";
                        else if (values[2] == "Default" || values[2] == "default")
                            _myTag.DataType = "Default";


                        //Client Access
                        if ((values[3].Contains("R") && values[3].Contains("W")) ||
                            (values[3].Contains("r") && values[3].Contains("w")) ||
                            (values[3].Contains("R") && values[3].Contains("w")) ||
                            (values[3].Contains("r") && values[3].Contains("W")))
                            _myTag.ClientAccess = "ReadWrite";
                        else if ((values[3].Contains("R") && values[3].Contains("O")) ||
                            (values[3].Contains("r") && values[3].Contains("o")) ||
                            (values[3].Contains("r") && values[3].Contains("O")) ||
                            (values[3].Contains("R") && values[3].Contains("o")))

                            _myTag.ClientAccess = "ReadOnly";

                        removeSpecialChar(ref values[4], false);
                        _myTag.Description = values[4];



                        if (_myTag.Name != null && _myTag.Address != null && _myTag.Name != "" && _myTag.Address != "")
                        {
                            //Add device to this channel
                            if (myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).GetTag(_myTag.Name) == null)
                            {
                                myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).AddTag(_myTag);
                            }
                            else
                            {
                                MessageBox.Show("The Tag named \"" + _myTag.Name + "\" is existed in selected Device. Kindly change to another name.", "ATSCADA");
                            }
                        }
                    }
                    toolStripStatusLabel2.Text = _ToolStriptempstatus;
                    Tree_Update();
                    myServer.Saved = false;
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        /// <summary>
        /// if Removepoint = true -> remove all point in s
        /// </summary>
        /// <param name="s"></param>
        /// <param name="RemovePoint"></param>
        private void removeSpecialChar(ref string s, bool RemovePoint)
        {
            s = s.Replace("!", "");
            s = s.Replace("\"", "");
            s = s.Replace("@", "");
            s = s.Replace("#", "");
            s = s.Replace("$", "");
            s = s.Replace("%", "");
            s = s.Replace("^", "");
            s = s.Replace("&", "");
            s = s.Replace("*", "");
            s = s.Replace("(", "");
            s = s.Replace(")", "");
            s = s.Replace("+", "");
            s = s.Replace("=", "");
            s = s.Replace("|", "");
            s = s.Replace("\\", "");
            s = s.Replace("/", "");
            s = s.Replace(",", "");
            s = s.Replace("'", "");
            s = s.Replace(":", "");
            s = s.Replace(";", "");
            s = s.Replace("{", "");
            s = s.Replace("}", "");
            s = s.Replace("[", "");
            s = s.Replace("]", "");
            s = s.Replace("-", "");
            s = s.Replace("<", "");
            s = s.Replace(">", "");
            s = s.Replace("?", "");
            s = s.Replace(" ", "");

            if (RemovePoint)
            {
                s = s.Replace(".", "");
            }
        }
        //Export CSV
        private void exportCSVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {//Open XML file           
                SaveFileDialog dlg = new SaveFileDialog();
                // setup a dialog;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.Filter = "CSV Files (*.csv)|*.csv";
                dlg.Title = "Export Device Tag Collection to CSV file";
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    //display on skin
                    _ToolStriptempstatus = toolStripStatusLabel2.Text;
                    toolStripStatusLabel2.Text = "CSV exporting ...";

                    //before your loop
                    var csv = new StringBuilder();

                    //title
                    var newLine0 = string.Format("{0},{1},{2},{3},{4}", "Tag Name", "Address", "Data Type", "Client Access", "Description");
                    csv.AppendLine(newLine0);

                    foreach (Tag t in myServer.GetChannel(_ChannelName).GetDevice(_DeviceName).TagList)
                    {
                        var newLine = string.Format("{0},{1},{2},{3},{4}", t.Name, t.Address, t.DataType, t.ClientAccess, t.Description);
                        csv.AppendLine(newLine);
                    }
                    //after your loop
                    File.WriteAllText(dlg.FileName, csv.ToString());

                    //display on skin
                    toolStripStatusLabel2.Text = _ToolStriptempstatus;
                }

            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }

        }

        //Save template
        private void getDeviceTagCSVTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                // setup a dialog;
                dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                dlg.Filter = "CSV Files (*.csv)|*.csv";
                dlg.Title = "Export Device Tag Collection to CSV file";
                if (dlg.ShowDialog() == DialogResult.OK)
                {

                    System.IO.File.Copy("C:\\Program Files\\ATPro\\ATDriverServer\\Project Files\\ATDeviceTagTemplate.csv",
                    dlg.FileName);
                }
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                if (myServer.Saved == false)
                    this.Text = "ATDriver Server- Ver 4.0.0.5      " + _FileLoc + " *";

                //Events display
                if (myServer.EventList.Count != EventCount)
                {
                    //Remove
                    if (myServer.EventList.Count > 1000)
                        myServer.EventList.RemoveAt(0);

                    EventCount = myServer.EventList.Count;

                    //Display onto listview2
                    atListView2.Items.Clear();
                    foreach (Event ev in myServer.EventList)
                    {
                        string[] s = new string[3] { ev.Datetime, ev.Source, ev.EventString };

                        ListViewItem li = new ListViewItem(s);
                        if (ev.Type == "Error")
                            li.ImageIndex = 0;
                        else
                            li.ImageIndex = 1;

                        atListView2.Items.Add(li);
                    }
                }
            }
            catch { }

            if (myServer.ChannelList.Count == 0)
            {

                saveAsToolStripMenuItem.Enabled = false;
                saveToolStripMenuItem.Enabled = false;
                cutToolStripMenuItem.Enabled = false;
                copyToolStripMenuItem.Enabled = false;
                pasteToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;

                saveToolStripButton.Enabled = false;
                cutToolStripButton.Enabled = false;
                copyToolStripButton.Enabled = false;
                deleteToolStripbutton.Enabled = false;

                NewChannel.Enabled = false;
                NewDevice.Enabled = false;
                NewTag.Enabled = false;
            }
            else
            {
                saveAsToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;

                saveToolStripButton.Enabled = true;
            }

            if (_ChannelName == "")
            {
                newChannelToolStripMenuItem.Enabled = true;
                newDeviceToolStripMenuItem.Enabled = false;
                newTagToolStripMenuItem.Enabled = false;

                NewChannel.Enabled = true;
                NewDevice.Enabled = false;
                NewTag.Enabled = false;

                copyToolStripMenuItem.Enabled = false;
                deleteToolStripMenuItem.Enabled = false;

                copyToolStripButton.Enabled = false;
                deleteToolStripbutton.Enabled = false;

            }
            else if (_ChannelName != "" && _DeviceName == "")
            {
                newDeviceToolStripMenuItem.Enabled = true;
                newTagToolStripMenuItem.Enabled = false;

                NewChannel.Enabled = true;
                NewDevice.Enabled = true;
                NewTag.Enabled = false;

                copyToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;

                copyToolStripButton.Enabled = true;
                deleteToolStripbutton.Enabled = true;
            }
            else if (_ChannelName != "" && _DeviceName != "")
            {
                newDeviceToolStripMenuItem.Enabled = true;
                newTagToolStripMenuItem.Enabled = true;

                NewChannel.Enabled = true;
                NewDevice.Enabled = true;
                NewTag.Enabled = true;

                copyToolStripMenuItem.Enabled = true;
                deleteToolStripMenuItem.Enabled = true;

                copyToolStripButton.Enabled = true;
                deleteToolStripbutton.Enabled = true;

            }

            if (_CopyChannel != null || _CopyDevice != null || _CopyTag != null)
            {
                pasteToolStripMenuItem.Enabled = true;

                pasteToolStripButton.Enabled = true;

                if (_CopyDevice != null && _ChannelName == "")
                {
                    pasteToolStripMenuItem.Enabled = false;

                    pasteToolStripButton.Enabled = false;
                }

                if (_CopyTag != null && _DeviceName == "")
                {
                    pasteToolStripMenuItem.Enabled = false;

                    pasteToolStripButton.Enabled = false;
                }
            }
            else
            {
                pasteToolStripMenuItem.Enabled = false;
                pasteToolStripButton.Enabled = false;
            }

            if (_DeviceName == "" && _TagName == "")
            {
                cutToolStripButton.Enabled = false;
                cutToolStripMenuItem.Enabled = false;
            }
            else
            {
                cutToolStripButton.Enabled = true;
                cutToolStripMenuItem.Enabled = true;
            }
        }
        #endregion

    }
    
}
