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
    public partial class AddChannel : Form
    {
        public Channel _myChannel;
        public AddChannel()
        {
            InitializeComponent();
        }
        public AddChannel(Channel c): this() 
        {
            _myChannel = c;
        }
        private void AddChannel_Load(object sender, EventArgs e)
        {
            this.KeyPress += iCheckKey;
        }

        //Browse for driver dll
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            // setup a dialog;
            dlg.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Drivers";
            dlg.Filter = "ATDriver (*.dll)|*.dll";
            dlg.Title = "Select Driver for Channel";
            try
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBox2.Text = dlg.FileName;
                    button2.Enabled = true;  
                }
            }
            catch { }
        }

        //Add infomation for channel
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                _myChannel.Name = textBox1.Text;
                _myChannel.DriverLocation = textBox2.Text;
                _myChannel.UpdateRate = Convert.ToDouble(textBox3.Text);
                //Load the driver dll
                _myChannel.LoadDriver();
                this.Close();
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
                    button2_Click(o, e1);
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
