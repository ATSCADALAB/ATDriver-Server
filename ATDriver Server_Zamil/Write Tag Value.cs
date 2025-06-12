using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ATDriver_Server
{
    public partial class Write_Tag_Value : Form
    {
        LocalDriverServer _myServer;
        string _myChannelName;
        string _myDeviceName;
        string _myTagName;
        Timer _Timer = new Timer();  
        public Write_Tag_Value()
        {
            InitializeComponent();
        }

        public Write_Tag_Value(LocalDriverServer Server, string ChannelName, string DeviceName, string TagName):this()
        {
            try
            {
                _myServer = Server;
                _myChannelName = ChannelName;
                _myDeviceName = DeviceName;
                _myTagName = TagName;
            }
            catch { }
        }

        private void Write_Tag_Value_Load(object sender, EventArgs e)
        {
            try
            {
                this.KeyPress += iCheckKey;
                label2.Text = _myChannelName + "." + _myDeviceName + "." + _myTagName;
                label3.Text = _myServer.GetChannel(_myChannelName).GetDevice(_myDeviceName).GetTag(_myTagName).Value;

                if (_myServer.GetChannel(_myChannelName).GetDevice(_myDeviceName).GetTag(_myTagName).ClientAccess == "ReadOnly")
                {
                    textBox1.Enabled = false;
                    button1.Enabled = false;
                }
                else
                {
                    textBox1.Enabled = true;
                    button1.Enabled = true;
                }

                this.ActiveControl = textBox1;

                _Timer.Interval = 100;
                _Timer.Tick += _Timer_Tick;
                _Timer.Enabled = true;

                this.FormClosing += Write_Tag_Value_FormClosing;
            }
            catch { }
        }

        void Write_Tag_Value_FormClosing(object sender, FormClosingEventArgs e)
        {
            _Timer.Enabled = false;
            _Timer.Dispose();  
        }

        void _Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                label3.Text = _myServer.GetChannel(_myChannelName).GetDevice(_myDeviceName).GetTag(_myTagName).Value;
            }
            catch { }
        }

        //Write new Value
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {        
                if (textBox1.Text != "")
                    _myServer.GetChannel(_myChannelName).GetDevice(_myDeviceName).GetTag(_myTagName).ValuetoWrite = textBox1.Text;
            }
            catch { }
        }
        private void iCheckKey(object sender, System.Windows.Forms.KeyPressEventArgs e)
        {
            try
            {
                //If press enter key
                if (e.KeyChar == (char)13)
                {
                    object o = new object();
                    EventArgs e1 = new EventArgs();
                    button1_Click(o, e1);
                }
                //ESC
                else if (e.KeyChar == (char)27)
                {
                    this.Dispose();
                }
            }
            catch { }
        }
    }
}
