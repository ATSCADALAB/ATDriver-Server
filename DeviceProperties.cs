using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace ATDriver_Server
{
    public partial class DeviceProperties : Form
    {
        Channel _myChannel;
        public DeviceProperties()
        {
            InitializeComponent();
        }
        public DeviceProperties(Channel c): this()
        {
            _myChannel = c;         
        }        

        private void DeviceProperties_Load(object sender, EventArgs e)
        {
            try
            {
                //Fill connection user control 
                this.Width = _myChannel.DriverLoader.Driver.ctlDeviceDesign.Width;
                _myChannel.DriverLoader.Driver.ctlDeviceDesign.Dock = DockStyle.Fill;
                this.Controls.Add(_myChannel.DriverLoader.Driver.ctlDeviceDesign);
                this.FormClosing +=DeviceProperties_FormClosing;
                this.KeyPress += iCheckKey;
            }
            catch { }
        }

        void DeviceProperties_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    _myChannel.DriverLoader.Driver.DeviceNameDesignMode = "";                    
                    _myChannel.DriverLoader.Driver.DeviceIDDesignMode = "";
                }
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
                //    object o = new object();
                //    EventArgs e1 = new EventArgs();
                //    button1_Click(o, e1);                   
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
