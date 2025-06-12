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
    public partial class TagProperties : Form
    {
        Channel _myChannel;
        
        public TagProperties()
        {
            InitializeComponent();
        }
        public TagProperties(Channel c):this()
        {
            _myChannel = c;
        }
        private void TagProperties_Load(object sender, EventArgs e)
        {
            try
            {
                //Fill connection user control 
                this.Width = _myChannel.DriverLoader.Driver.ctlTagDesign.Width;
                _myChannel.DriverLoader.Driver.ctlTagDesign.Dock = DockStyle.Fill;
                this.Controls.Add(_myChannel.DriverLoader.Driver.ctlTagDesign);
                this.FormClosing += TagProperties_FormClosing;
                this.KeyPress += iCheckKey;
            }
            catch { }
        }

        void TagProperties_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    _myChannel.DriverLoader.Driver.TagNameDesignMode = "";
                    _myChannel.DriverLoader.Driver.TagAddressDesignMode = "";
                    _myChannel.DriverLoader.Driver.TagTypeDesignMode = "";
                    _myChannel.DriverLoader.Driver.TagClientAccessDesignMode = "";
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
                    //object o = new object();
                    //EventArgs e1 = new EventArgs();
                    //button1_Click(o, e1);
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
