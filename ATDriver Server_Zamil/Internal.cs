using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Collections.Concurrent;

namespace ATDriver_Server
{
    //Local Driver
    public class LocalDriverServer : IDisposable
    {
        protected string _Name;
        protected List<Channel> _ChannelList = new List<Channel>();
        public List<Event> EventList = new List<Event>();
        public bool Saved = false;
        public string NeedtoWriteTags = "";//format: Chanel1.Device1.tag1|Chanel2.Device2.tag2|Chanel3.Device3.tag3

        public ATDriverWCFInterface ATDriverClient;

        public string Servername
        {
            get { return _Name; }
            set { _Name = value; }

        }

        public List<Channel> ChannelList
        {
            get { return _ChannelList; }
            set { _ChannelList = value; }

        }

        public Channel GetChannel(string ChannelName)
        {
            Channel _Channel = _ChannelList.Find(delegate (Channel _C) { return _C.Name == ChannelName; });

            if (_Channel != null)
            {
                return _Channel;
            }
            else
            {
                return null;
            }
        }

        public void AddChannel(Channel C)
        {
            _ChannelList.Add(C);
        }

        public bool RemoveChannel(string ChannelName)
        {
            Channel _Channel = _ChannelList.Find(delegate (Channel _C) { return _C.Name == ChannelName; });

            if (_Channel != null)
            {
                _ChannelList.Remove(_Channel);
                _Channel.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Deserialize(string XMLLocation)
        {
            XmlReader Reader;
            Reader = new XmlTextReader(XMLLocation);

            Channel _myChannel = null;
            Device _myDevice = null;
            Tag _myTag = null;

            Reader.MoveToContent();

            while (Reader.Read())
            {
                switch (Reader.NodeType)
                {

                    //If node is the Start Node
                    case XmlNodeType.Element:
                        switch (Reader.Name)
                        {
                            //If this is a channel                                
                            case "Channel":
                                //Create new Task
                                _myChannel = new Channel(this);
                                break;
                            case "ChannelName":
                                Reader.MoveToFirstAttribute();
                                _myChannel.Name = Reader.Value;
                                break;
                            case "ChannelDriverLocation":
                                Reader.MoveToFirstAttribute();
                                _myChannel.DriverLocation = Reader.Value;
                                break;
                            case "ChannelRate":
                                Reader.MoveToFirstAttribute();
                                _myChannel.UpdateRate = Convert.ToDouble(Reader.Value);
                                _myChannel.LoadDriver();
                                break;
                            case "ChannelMaxWTimes":
                                Reader.MoveToFirstAttribute();
                                _myChannel.MaxWriteTime = Convert.ToInt16(Reader.Value);
                                break;
                            case "ChannelAddress":
                                Reader.MoveToFirstAttribute();
                                _myChannel.Address = Reader.Value;
                                _myChannel.DriverLoader.Driver.ChannelAddress = _myChannel.Address;
                                _myChannel.DriverLoader.Driver.Connect();
                                _myChannel.Run = true;
                                break;
                            case "Device":
                                _myDevice = new Device();
                                _myDevice.Channel = _myChannel;
                                break;
                            case "DeviceName":
                                Reader.MoveToFirstAttribute();
                                _myDevice.Name = Reader.Value;
                                break;
                            case "DeviceID":
                                Reader.MoveToFirstAttribute();
                                _myDevice.ID = Reader.Value;
                                break;
                            case "Tag":
                                //Add new Tag                                
                                _myTag = new Tag(_myDevice);

                                //Get Tag Name
                                Reader.MoveToFirstAttribute();
                                _myTag.Name = Reader.Value;

                                //Get Addrress
                                Reader.MoveToNextAttribute();
                                _myTag.Address = Reader.Value;

                                //Get Type
                                Reader.MoveToNextAttribute();
                                _myTag.DataType = Reader.Value;

                                //Get ClientAccess
                                Reader.MoveToNextAttribute();
                                _myTag.ClientAccess = Reader.Value;

                                //Get Description
                                Reader.MoveToNextAttribute();
                                _myTag.Description = Reader.Value;


                                //MessageBox.Show(_myTag.Name + _myTag.Address + _myTag.DataType + _myTag.ClientAccess);

                                //Add Tag to Device
                                _myDevice.AddTag(_myTag);

                                break;
                            default:
                                break;
                        }
                        break;
                    case XmlNodeType.EndElement:
                        if (Reader.Name == "Device")
                        {
                            //Add Device to Channel
                            _myChannel.AddDevice(_myDevice);
                        }
                        else if (Reader.Name == "Channel")
                        {
                            //Add Channel to DriverServer
                            this.AddChannel(_myChannel);
                        }
                        break;
                    case XmlNodeType.Text:
                        break;
                    default:
                        break;
                }
            }
            //end of XML Reading                
            Reader.Close();
            return true;

        }

        //Save configured project to file
        /// <summary>
        /// Serialize structure of the Driver to a Tag config file
        /// </summary>
        /// <returns>an Xml Document</returns>
        public XmlDocument Serialize()
        {
            XmlDocument Doc = new XmlDocument();
            XmlDeclaration Dec;
            XmlElement Root;
            XmlElement Child1;
            XmlElement Child2;
            XmlElement Child3;
            XmlAttribute Attr;

            try
            {
                Dec = Doc.CreateXmlDeclaration("1.0", "UTF-8", "yes");
                Doc.AppendChild(Dec);

                Root = Doc.CreateElement("Root");

                //Create Channel
                foreach (Channel C in _ChannelList)
                {
                    Child1 = Doc.CreateElement("Channel");

                    //ChannelName
                    Child2 = Doc.CreateElement("ChannelName");
                    Attr = Doc.CreateAttribute("Value");
                    Attr.Value = C.Name;
                    Child2.Attributes.Append(Attr);
                    Child1.AppendChild(Child2);

                    //DriverLocation
                    Child2 = Doc.CreateElement("ChannelDriverLocation");
                    Attr = Doc.CreateAttribute("Value");
                    Attr.Value = C.DriverLocation;
                    Child2.Attributes.Append(Attr);
                    Child1.AppendChild(Child2);

                    //ChannelRate
                    Child2 = Doc.CreateElement("ChannelRate");
                    Attr = Doc.CreateAttribute("Value");
                    Attr.Value = C.UpdateRate.ToString();
                    Child2.Attributes.Append(Attr);
                    Child1.AppendChild(Child2);


                    //Channel Max Write Times
                    Child2 = Doc.CreateElement("ChannelMaxWTimes");
                    Attr = Doc.CreateAttribute("Value");
                    Attr.Value = C.MaxWriteTime.ToString();
                    Child2.Attributes.Append(Attr);
                    Child1.AppendChild(Child2);

                    //Channel Address
                    Child2 = Doc.CreateElement("ChannelAddress");
                    Attr = Doc.CreateAttribute("Value");
                    Attr.Value = C.Address;
                    Child2.Attributes.Append(Attr);
                    Child1.AppendChild(Child2);

                    foreach (Device d in C.DeviceList)
                    {
                        //DEvice Collection
                        Child2 = Doc.CreateElement("Device");

                        //DeviceName
                        Child3 = Doc.CreateElement("DeviceName");
                        Attr = Doc.CreateAttribute("Value");
                        Attr.Value = d.Name;
                        Child3.Attributes.Append(Attr);
                        Child2.AppendChild(Child3);

                        //DeviceID
                        Child3 = Doc.CreateElement("DeviceID");
                        Attr = Doc.CreateAttribute("Value");
                        Attr.Value = d.ID;
                        Child3.Attributes.Append(Attr);
                        Child2.AppendChild(Child3);

                        foreach (Tag t in d.TagList)
                        {
                            //TaskName
                            Child3 = Doc.CreateElement("Tag");

                            Attr = Doc.CreateAttribute("Name");
                            Attr.Value = t.Name;
                            Child3.Attributes.Append(Attr);

                            Attr = Doc.CreateAttribute("Addrress");
                            Attr.Value = t.Address;
                            Child3.Attributes.Append(Attr);

                            Attr = Doc.CreateAttribute("Type");
                            Attr.Value = t.DataType;
                            Child3.Attributes.Append(Attr);

                            Attr = Doc.CreateAttribute("ClientAccess");
                            Attr.Value = t.ClientAccess;
                            Child3.Attributes.Append(Attr);

                            Attr = Doc.CreateAttribute("Description");
                            Attr.Value = t.Description;
                            Child3.Attributes.Append(Attr);

                            Child2.AppendChild(Child3);
                        }
                        Child1.AppendChild(Child2);
                    }
                    Root.AppendChild(Child1);

                }
                //End of Channel
                Doc.AppendChild(Root);

                return Doc;
            }
            catch { return null; }
        }

        public LocalDriverServer() { }

        public void Dispose()
        {
            foreach (Channel c in _ChannelList)
            {
                c.Dispose();
            }
        }
    }

    public class Channel : IDisposable
    {
        protected LocalDriverServer _Server;

        protected string _Name;
        protected string _Address;

        protected System.Timers.Timer _UpdateTimer;
        protected string _DriverLocation;
        protected ATDriverLoader _DriverLoader;
        protected List<Device> _DeviceList = new List<Device>();
        protected bool _IsUpdating = false;
        protected bool _Run = true;
        protected int _MaxWriteTime = 10;
        protected int _WriteTime = 0;

        private List<Event> _Events = new List<Event>();
        protected List<Tag> _WroteTags = new List<Tag>();

        public LocalDriverServer Server
        {
            get { return _Server; }
            set { _Server = value; }
        }
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        public double UpdateRate
        {
            get { return _UpdateTimer.Interval; }
            set { _UpdateTimer.Interval = value; }
        }
        public bool IsUpdating
        {
            get { return _IsUpdating; }
            set { _IsUpdating = value; }
        }
        public int MaxWriteTime
        {
            get { return _MaxWriteTime; }
            set { _MaxWriteTime = value; }
        }
        public string Address
        {
            get { return _Address; }
            set { _Address = value; }
        }
        public System.Timers.Timer UpdateTimer
        {
            get { return _UpdateTimer; }
            set { _UpdateTimer = value; }
        }
        public string DriverLocation
        {
            get { return _DriverLocation; }
            set { _DriverLocation = value; }
        }
        public bool Run
        {
            get
            {
                return _UpdateTimer.Enabled;
            }
            set
            {
                _Run = value;
                _UpdateTimer.Enabled = value;
            }
        }

        public ATDriverLoader DriverLoader
        {
            get { return _DriverLoader; }
            set { _DriverLoader = value; }
        }
        public List<Device> DeviceList
        {
            get { return _DeviceList; }
            set { _DeviceList = value; }
        }
        public Device GetDevice(string DeviceName)
        {
            Device _Device = _DeviceList.Find(delegate (Device _D) { return _D.Name == DeviceName; });

            if (_Device != null)
            {
                return _Device;
            }
            else
            {
                return null;
            }
        }
        public void AddDevice(Device D)
        {
            _DeviceList.Add(D);
        }
        public bool RemoveDevice(string DeviceName)
        {
            Device _Device = _DeviceList.Find(delegate (Device _D) { return _D.Name == DeviceName; });

            if (_Device != null)
            {
                _DeviceList.Remove(_Device);
                _Device.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }
        public void LoadDriver()
        {
            try
            {
                DriverLoader = new ATDriverLoader();
                DriverLoader.LoadDriver(_DriverLocation, _Name);
                Event myEvent = new Event(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), "Channel", "Driver " + _DriverLocation + " is loaded", "Info");
                _Events.Add(myEvent);
            }
            catch
            {
                Event myEvent = new Event(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), "Channel", "Driver " + _DriverLocation + " could not be loaded", "Error");
                _Events.Add(myEvent);
            }
        }

        private void _UpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                UpdateTimer.Enabled = false;

                _IsUpdating = true;
                ////////////////////////
                //REMOTE CLIENT PROCESS
                ///////////////////////                
                try
                {
                    //if new channel inserted
                    if (_Server.ATDriverClient != null && _Server.ATDriverClient.IsContainChannel(_Name) == false)
                    {
                        RemoteChannel rc = new RemoteChannel();
                        rc.Name = _Name;
                        rc.Address = _Address;
                        //empty channel
                        _Server.ATDriverClient.AddChannel(rc);

                        foreach (Device d in _DeviceList)
                        {
                            //REMOTE CLIENT PROCESS
                            RemoteDevice rd = new RemoteDevice();
                            rd.Name = d.Name;
                            rd.ID = d.ID;

                            //add empty device to empty channel
                            _Server.ATDriverClient.AddDevice(rc.Name, rd);

                            foreach (Tag t in d.TagList)
                            {
                                RemoteTag rt = new RemoteTag();

                                rt.Name = t.Name;
                                rt.Address = $"{_Name}.{d.Name}.{t.Name}";
                                rt.ClientAccess = t.ClientAccess;
                                rt.DataType = t.DataType;
                                rt.Description = t.Description;
                                rt.Status = t.Status;
                                rt.TimeStamp = t.TimeStamp;
                                rt.Value = t.Value;

                                //Add tag
                                _Server.ATDriverClient.AddTag(rc.Name, rd.Name, rt);
                            }
                        }
                    }
                }
                catch (Exception ex) { }

                DriverLoader.Driver.ChannelName = this.Name;
                DriverLoader.Driver.ChannelAddress = this.Address;

                foreach (Device d in _DeviceList)
                {
                    //REMOTE CLIENT PROCESS                 
                    try
                    {
                        //new device
                        if (_Server.ATDriverClient != null && _Server.ATDriverClient.IsContainDevice(_Name, d.Name) == false)
                        {
                            RemoteDevice rd = new RemoteDevice();
                            rd.Name = d.Name;
                            rd.ID = d.ID;

                            //Add empty device
                            _Server.ATDriverClient.AddDevice(_Name, rd);

                            foreach (Tag t in d.TagList)
                            {
                                RemoteTag rt = new RemoteTag();
                                rt.Name = t.Name;
                                rt.Address = $"{_Name}.{d.Name}.{t.Name}";
                                rt.ClientAccess = t.ClientAccess;
                                rt.DataType = t.DataType;
                                rt.Description = t.Description;
                                rt.Status = t.Status;
                                rt.TimeStamp = t.TimeStamp;
                                rt.Value = rt.Value;

                                _Server.ATDriverClient.AddTag(_Name, d.Name, rt);
                            }
                        }
                    }
                    catch { }

                    DriverLoader.Driver.DeviceName = d.Name;
                    DriverLoader.Driver.DeviceID = d.ID;

                    foreach (Tag t in d.TagList)
                    {
                        #region High priority for Write proccess
                        try
                        {
                            //Have tags need to write
                            if (_Server.NeedtoWriteTags != "" && _Server.NeedtoWriteTags != null)
                            {
                                string[] myNeedtoWriteArray = _Server.NeedtoWriteTags.Split('|');

                                foreach (string myString in myNeedtoWriteArray)
                                {
                                    if (myString.Split('.')[0] == this.Name)//Channel name
                                    {
                                        Tag tt = this.GetDevice(myString.Split('.')[1]).GetTag(myString.Split('.')[2]);
                                        //try
                                        {
                                            if (tt.ValuetoWrite != "")
                                            {
                                                SendPack sp = new SendPack();
                                                sp.ChannelAddress = this._Address;
                                                sp.DeviceID = tt.Device.ID;
                                                sp.TagAddress = tt.Address;
                                                sp.TagType = tt.DataType;
                                                sp.Value = tt.ValuetoWrite;

                                                //Thread.Sleep(200);

                                                while (DriverLoader.Driver.Write(sp) != "Good" && _WriteTime < _MaxWriteTime)
                                                {
                                                    Thread.Sleep(100);
                                                    _WriteTime++;
                                                }

                                                if (_WriteTime >= _MaxWriteTime)
                                                {
                                                    Event myEvent2 = new Event(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), "Tag", tt.Device.Channel.Name + "." + tt.Device.Name + "." + tt.Name + " Bad Tag Write", "Error");
                                                    _Events.Add(myEvent2);

                                                    //tt.Status = "Bad";
                                                }

                                                tt.ValuetoWrite = "";
                                                _WriteTime = 0;

                                                int _index = 0;
                                                _index = _Server.NeedtoWriteTags.IndexOf("|" + myString);
                                                if (_index > -1)
                                                {
                                                    _Server.NeedtoWriteTags = _Server.NeedtoWriteTags.Remove(_index, ("|" + myString).Length);
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                        }
                        catch (Exception ex) { _Server.NeedtoWriteTags = ""; }
                        //End tag write
                        #endregion

                        #region Tag read

                        DriverLoader.Driver.TagAddress = t.Address;
                        DriverLoader.Driver.TagType = t.DataType;

                        SendPack sb = null;

                        try
                        {
                            sb = DriverLoader.Driver.Read();

                            if (sb != null)
                            {
                                // MessageBox.Show($"{sb.ChannelAddress}--{sb.DeviceID}--{sb.TagAddress}");
                                if (sb.ChannelAddress == this.Address && sb.DeviceID == d.ID && sb.TagAddress == t.Address)
                                {
                                    t.Value = sb.Value;
                                    t.Status = "Good";
                                    //Update for Zamil
                                    t.BadDelayCount = 0;
                                }
                            }
                            else
                            {
                                //Update for Zamil
                                t.BadProcess();
                            }
                        }
                        catch
                        {
                            //Update for Zamil
                            t.BadProcess();
                        }

                        t.TimeStamp = DateTime.Now.ToString("HH:mm:ss");

                        #endregion

                        //REMOTE CLIENT PROCESS                        
                        try
                        {
                            if (_Server.ATDriverClient != null)
                            {
                                //New tag
                                if (_Server.ATDriverClient.IsContainTag(_Name, d.Name, t.Name) == false)
                                {
                                    RemoteTag rt = new RemoteTag();
                                    rt.Name = t.Name;
                                    rt.Address = $"{_Name}.{d.Name}.{t.Name}";
                                    rt.ClientAccess = t.ClientAccess;
                                    rt.DataType = t.DataType;
                                    rt.Description = t.Description;
                                    rt.Status = t.Status;
                                    rt.TimeStamp = t.TimeStamp;
                                    rt.Value = t.Value;

                                    _Server.ATDriverClient.AddTag(_Name, d.Name, rt);
                                }
                                else//update Tag value
                                {
                                    var address = $"{_Name}.{d.Name}.{t.Name}";
                                    var tagUpdate = _Server.ATDriverClient.GetTag(address);
                                    if (tagUpdate is null) continue;

                                    if (tagUpdate.ValuetoWrite != "")
                                    {
                                        t.ValuetoWrite = tagUpdate.ValuetoWrite;
                                        _Server.ATDriverClient.WritetoTag(address, "");
                                    }
                                    else
                                    {
                                        if (tagUpdate.Value != t.Value || tagUpdate.Status != t.Status)
                                        {
                                            _Server.ATDriverClient.UpdateTag(_Name, d.Name, t.Name, t.TimeStamp, t.Status, t.Value);
                                        }
                                    }

                                }
                            }
                        }
                        catch { }
                    }
                }

                if (_Run == true)
                    UpdateTimer.Enabled = true;

                _IsUpdating = false;

            }
            catch
            {
                if (_Run == true)
                    UpdateTimer.Enabled = true;
            }
        }

        public Channel(LocalDriverServer Server)
        {
            _UpdateTimer = new System.Timers.Timer();
            _UpdateTimer.Interval = 100;
            _UpdateTimer.Elapsed += _UpdateTimer_Elapsed;
            _UpdateTimer.Enabled = false;

            _Server = Server;

            _Events = Server.EventList;

        }

        public void Dispose()
        {
            foreach (Device d in _DeviceList)
            {
                d.Dispose();
            }
            DriverLoader.ClosePlugin();
        }
    }

    public class Device : IDisposable
    {
        protected Channel _Channel;
        protected string _Name;
        protected string _ID;
        protected List<Tag> _TagList = new List<Tag>();

        public Channel Channel
        {
            get { return _Channel; }
            set { _Channel = value; }
        }
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }
        public List<Tag> TagList
        {
            get { return _TagList; }
            set { _TagList = value; }
        }

        public Tag GetTag(string TagName)
        {
            Tag _Tag = _TagList.Find(delegate (Tag _T) { return _T.Name == TagName; });

            if (_Tag != null)
            {
                return _Tag;
            }
            else
            {
                return null;
            }
        }
        public void AddTag(Tag T)
        {
            _TagList.Add(T);
        }
        public bool RemoveTag(string TagName)
        {
            Tag _Tag = _TagList.Find(delegate (Tag _T) { return _T.Name == TagName; });

            if (_Tag != null)
            {
                _TagList.Remove(_Tag);
                _Tag.Dispose();
                return true;
            }
            else
            {
                return false;
            }
        }
        public Device() { }
        public Device(Channel MyChannel, string DeviceName, string DeviceID)
        {
            _Channel = MyChannel;
            _Name = DeviceName;
            _ID = DeviceID;
        }
        public void Dispose()
        {
            foreach (Tag t in TagList)
            {
                t.Dispose();
            }
        }
    }

    public class Tag : IDisposable
    {
        protected Device _Device;
        protected string _TagName;
        protected string _TagAddress;
        protected string _DataType;
        protected string _ClientAccess;
        protected string _TagStatus;
        protected string _Description;
        protected string _TimeStamp;
        protected string _TagValue="";
        protected string _TagValuetoWrite = "";
       
        public Device Device
        {
            get { return _Device; }
            set { _Device = value; }
        }

        public string Name
        {
            get { return _TagName; }
            set { _TagName = value; }
        }
        public string Address
        {
            get { return _TagAddress; }
            set { _TagAddress = value; }
        }
        public string DataType
        {
            get { return _DataType; }
            set { _DataType = value; }
        }
        public string ClientAccess
        {
            get { return _ClientAccess; }
            set { _ClientAccess = value; }
        }
        public string Status
        {
            get { return _TagStatus; }
            set { _TagStatus = value; }
        }
        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }
        public string TimeStamp
        {
            get { return _TimeStamp; }
            set { _TimeStamp = value; }
        }
        public string Value
        {
            get { return _TagValue; }
            set { _TagValue = value; }
        }
        public string ValuetoWrite
        {
            get { return _TagValuetoWrite; }
            set
            {
                _TagValuetoWrite = value;

                if (!_Device.Channel.Server.NeedtoWriteTags.Contains("|" + this.Device.Channel.Name + "." + this.Device.Name + "." + this.Name))
                    _Device.Channel.Server.NeedtoWriteTags = _Device.Channel.Server.NeedtoWriteTags + "|" + this.Device.Channel.Name + "." + this.Device.Name + "." + this.Name;
            }
        }

        /// <summary>
        /// SPECIAL UPDATE FOR ZAMIL 19/2/2025
        /// </summary>
        protected int _BadDelayCount = 0;//counter
        protected int _BadDelayMAX = 30;//Max value to reach
        protected int _CountMAX = 0;//All time MAX
        
        public int BadDelayCount
        {
            get { return _BadDelayCount; }
            set { _BadDelayCount = value;}
        }
        public Tag(Device MyDevice)
        {
            _Device = MyDevice;          
        }


        public void BadProcess()
        {
            try
            {

                //get _BadDelayMAX
                if (!int.TryParse(_Description.Split('|')[0].Trim().Split(':')[1].Trim(), out _BadDelayMAX))
                {
                    _BadDelayMAX = 30;
                }

                if (_TagValue != "" && _TagValue != null)
                {                   
                    _BadDelayCount++;

                    if (_BadDelayCount >= _BadDelayMAX )
                    {
                        _BadDelayCount = _BadDelayMAX;
                        _TagStatus = "Bad";

                    }
                    else
                    {
                        _TagStatus = "Good";                        
                    }

                    if (_BadDelayCount > _CountMAX)//get All time MAX
                        _CountMAX = _BadDelayCount;

                    _Description = "CM." + _CountMAX.ToString() + ".BC." + _BadDelayCount.ToString() + ".BM:" + _BadDelayMAX.ToString() +
                            "|Word, Short, Dword, Long, Float, Double Types are for Holding Register memory addresses. Default type is Word. This is Read - Write Tag";


                }
                else// case of the first load time
                    _TagStatus = "Bad";
            }catch (Exception ex)
            {
                _Description = "Check the BM!";
            }
        }
        //END UPDATE
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }

    public class Event : IDisposable
    {
        public string Datetime;
        public string Source;
        public string EventString;
        public string Type;//There are two types: "Info" & "Error"

        public Event(string myDateTime, string mySource, string myEvent, string myType)
        {
            Datetime = myDateTime;
            Source = mySource;
            EventString = myEvent;
            Type = myType;
        }

        public void Dispose() { }

    }

    
    // PerSession: Mỗi client kết nối tới sẽ tạo 1 phiên (Session khác nhau). Tạo mới 1 đối tượng RemoteDriverServer
    [ServiceBehavior(UseSynchronizationContext = false, InstanceContextMode = InstanceContextMode.PerSession)]
    public class RemoteDriverServer : ATDriverWCFInterface, IDisposable
    {
        protected string _Name = "RemoteATDriverServer";

        protected static ulong _ClientCounter = 0;

        protected static List<RemoteChannel> _ChannelList = new List<RemoteChannel>();

        protected static List<string> _ChannelNameList = new List<string>();

        private static readonly object keyLock = new object();

        #region CONTROL

        // Phương thức bắt đầu BẮT BUỘC cho mỗi phiên (Session). Khi client kết nói với Service, BẮT BUỘC gọi phương thức này đầu tiên
        // Khi kết nối sẽ lấy SessionID của phiên. Add vào danh sách quản lý session
        public void Open()
        {
            lock (keyLock)
            {
                var sessionId = GetSessionID();
                if (!ServiceRepository.Instance.SessionIDs.Contains(sessionId))
                    ServiceRepository.Instance.SessionIDs.Add(sessionId);
            }
        }

        // Phương thức kết thúc BẮT BUỘC của phiên. Gọi khi client Close, Abort, Dispose
        // Lấy SessionID của phiên.Xóa session khỏi danh sách quản lý
        public void Close()
        {
            lock (keyLock)
            {
                var sessionId = GetSessionID();
                if (ServiceRepository.Instance.SessionIDs.Contains(sessionId))
                {
                    ServiceRepository.Instance.SessionIDs.Remove(sessionId);
                    foreach (RemoteChannel c in _ChannelList)
                    {
                        c.Dispose();
                    }
                }
            }
        }

        // Số lượng Client đang duy trì kết nối.
        public UInt64 GetClientCounter()
        {
            return (UInt64)ServiceRepository.Instance.SessionIDs.Count;
        }
        // Lấy sessionID của mỗi Request
        public string GetSessionID()
        {
            return OperationContext.Current?.SessionId;
        }
        //  Kiểm tra có phải request của InternalClient hay không
        private bool IsInternal()
        {
            return ServiceRepository.Instance.InternalSessionID == OperationContext.Current?.SessionId;
        }

        public void Dispose()
        {
        }

        #endregion

        // Tất cả phương thức: Tham số đầu vào (nếu có) sẽ được giải mã. Dữ liệu trả về (nếu có) sẽ được mã hóa.
        // Dữ liệu trên đường truyền đều được mã hóa
        // * Note: Với InternalClient thì không cần mã hóa/ giải mã. Kiểm tra qua phương thức IsInternal()

        #region

        public string GetServername()
        {
            return IsInternal() ? _Name : string.Empty;
        }
        public void SetServername(string ServerName)
        {
            if (!IsInternal()) return;
            _Name = ServerName;
        }

        public List<string> ChannelNameList()
        {
            if (IsInternal())
                return _ChannelNameList;
            else
                return _ChannelNameList.Select(x => x.EncryptAddress()).ToList();
        }

        public List<string> DeviceNameList(string ChannelName)
        {
            try
            {
                var isInternal = IsInternal();
                var channel = _ChannelList
                    .Find(x => x.Name == (isInternal ? ChannelName : ChannelName.DecryptAddress()));
                if (channel is null) return null;

                if (isInternal)
                    return channel.DeviceNameList;
                else
                    return channel.DeviceNameList.Select(x => x.EncryptAddress()).ToList();
            }
            catch { return null; }
        }

        public List<string> TagNameList(string ChannelName, string DeviceName)
        {
            try
            {
                var isInternal = IsInternal();
                var device = _ChannelList
                    .Find(x => x.Name == (isInternal ? ChannelName : ChannelName.DecryptAddress()))?
                    .GetDevice(isInternal ? DeviceName : DeviceName.DecryptAddress());
                if (device is null) return null;

                if (isInternal)
                    return device.TagNameList;
                else
                    return device.TagNameList.Select(x => x.EncryptAddress()).ToList();
            }
            catch { return null; }
        }
    
        public bool IsContainChannel(string ChannelName)
        {
            if (!IsInternal()) return false;
            return _ChannelNameList.Contains(ChannelName);
        }

        public bool IsContainDevice(string ChannelName, string DeviceName)
        {
            try
            {
                if (!IsInternal()) return false;
                if (_ChannelNameList.Contains(ChannelName))
                {
                    return GetChannel(ChannelName).DeviceNameList.Contains(DeviceName);
                }
                else
                    return false;
            }
            catch { return false; }
        }

        public bool IsContainTag(string ChannelName, string DeviceName, string TagName)
        {
            try
            {
                if (!IsInternal()) return false;
                if (_ChannelNameList.Contains(ChannelName))
                {
                    if (GetChannel(ChannelName).DeviceNameList.Contains(DeviceName))
                        return GetChannel(ChannelName).GetDevice(DeviceName).TagNameList.Contains(TagName);
                    else
                        return false;
                }
                else
                    return false;
            }
            catch { return false; }
        }

        public void AddChannel(RemoteChannel C)
        {
            try
            {
                if (!IsInternal()) return;
                if (!IsContainChannel(C.Name))
                {
                    _ChannelList.Add(C);
                    _ChannelNameList.Add(C.Name);
                }
            }
            catch { }
        }

        public void AddDevice(string ChannelName, RemoteDevice EmptyDevice)
        {
            try
            {
                if (!IsInternal()) return;
                RemoteChannel _Channel = _ChannelList.Find(delegate (RemoteChannel _C) { return _C.Name == ChannelName; });

                if (_Channel != null)
                {
                    if (!IsContainDevice(ChannelName, EmptyDevice.Name))
                    {
                        _Channel.AddDevice(EmptyDevice);
                    }
                }
            }
            catch { }
        }

        public void AddTag(string ChannelName, string DeviceName, RemoteTag Tag)
        {
            try
            {
                if (!IsInternal()) return;
                RemoteChannel _Channel = _ChannelList.Find(delegate (RemoteChannel _C) { return _C.Name == ChannelName; });

                if (_Channel != null)
                {
                    if (!IsContainTag(ChannelName, DeviceName, Tag.Name))
                    {
                        var device = _Channel.GetDevice(DeviceName);
                        if (device != null)
                            _Channel.GetDevice(DeviceName).AddTag(Tag);
                    }
                }
            }
            catch { }
        }

        public bool RemoveChannel(string ChannelName)
        {
            try
            {
                if (!IsInternal()) return false;
                RemoteChannel _Channel = _ChannelList.Find(delegate (RemoteChannel _C) { return _C.Name == ChannelName; });

                if (_Channel != null)
                {
                    _ChannelList.Remove(_Channel);
                    _ChannelNameList.Remove(_Channel.Name);
                    _Channel.Dispose();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        public bool RemoveDevice(string ChannelName, string DeviceName)
        {
            try
            {
                if (!IsInternal()) return false;
                RemoteChannel _Channel = _ChannelList.Find(delegate (RemoteChannel _C) { return _C.Name == ChannelName; });

                if (_Channel != null)
                {
                    _Channel.RemoveDevice(DeviceName);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        public bool RemoveTag(string ChannelName, string DeviceName, string TagName)
        {
            try
            {
                if (!IsInternal()) return false;
                RemoteChannel _Channel = _ChannelList.Find(delegate (RemoteChannel _C) { return _C.Name == ChannelName; });

                if (_Channel != null)
                {
                    var device = _Channel.GetDevice(DeviceName);
                    if (device != null)
                    {
                        device.RemoveTag(TagName);
                        return true;
                    }
                    else
                        return false;
                }
                else
                {
                    return false;
                }
            }
            catch { return false; }
        }

        public RemoteChannel GetChannel(string ChannelName)
        {
            try
            {
                if (!IsInternal()) return null;
                return _ChannelList.Find(x => x.Name == ChannelName);
            }
            catch { return null; }
        }

        public RemoteDevice GetDevice(string ChannelName, string DeviceName)
        {
            try
            {
                if (!IsInternal()) return null;
                return _ChannelList.Find(x => x.Name == ChannelName)?.GetDevice(DeviceName);
            }
            catch { return null; }
        }

        public RemoteTag GetTag(string address)
        {
            try
            {
                var remoteTagAddress = address;
                if (!IsInternal()) remoteTagAddress = address.DecryptAddress();

                var addressSplit = remoteTagAddress.Split('.');
                if (addressSplit.Length != 3) return null;

                var remoteTag = _ChannelList.Find(x => x.Name == addressSplit[0])?.GetDevice(addressSplit[1])?.GetTag(addressSplit[2]);
                if (IsInternal()) return remoteTag;

                return new RemoteTag()
                {
                    Address = address,
                    Status = remoteTag.Status,
                    DataType = remoteTag.DataType,
                    ClientAccess = remoteTag.ClientAccess,
                    Value = remoteTag.Value.EncryptValue()
                };
            }
            catch
            {
                return null;
            }
        }

        public void UpdateTag(string ChannelName, string DeviceName, string TagName, string newTimeStamp, string newStatus, string newValue)
        {
            try
            {
                if (!IsInternal()) return;
                var remoteTag = _ChannelList.Find(x => x.Name == ChannelName)?.GetDevice(DeviceName)?.GetTag(TagName);
                if (remoteTag != null)
                {
                    remoteTag.TimeStamp = newTimeStamp;
                    remoteTag.Value = newValue;
                    remoteTag.Status = newStatus;
                }
            }
            catch { }
        }

        public void WritetoTag(string address, string valueToWrite)
        {
            try
            {
                if (!IsInternal())
                {
                    address = address.DecryptAddress();
                    valueToWrite = valueToWrite.DecryptValue();
                }

                var addressSplit = address.Split('.');
                if (addressSplit.Length != 3) return;

                var remoteTag = _ChannelList.Find(x => x.Name == addressSplit[0])?.GetDevice(addressSplit[1])?.GetTag(addressSplit[2]);
                if (remoteTag is null) return;

                remoteTag.ValuetoWrite = valueToWrite;
            }
            catch { }
        }

        #endregion

    }
}