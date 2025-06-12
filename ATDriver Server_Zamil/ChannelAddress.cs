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
    public partial class ChannelAddress : Form
    {
        public Channel _myChannel;
        public ChannelAddress()
        {
            InitializeComponent();
        }
        public ChannelAddress(Channel C):this()
        {
            _myChannel = C;
        }
        private void ChannelAddress_Load(object sender, EventArgs e)
        {
            try
            {
                if (_myChannel.DriverLoader.Driver.ctlChannelAddress != null)
                {
                    //Fill connection user control 
                    this.Width = _myChannel.DriverLoader.Driver.ctlChannelAddress.Width;
                    _myChannel.DriverLoader.Driver.ctlChannelAddress.Dock = DockStyle.Fill;
                    this.Controls.Add(_myChannel.DriverLoader.Driver.ctlChannelAddress);
                    this.KeyPress += iCheckKey;
                }
                else
                {                    
                    this.Close();
                }                   
 
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
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
