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
    public partial class ChannelProperties : Form
    {
        LocalDriverServer _myDriver;
        Channel _myChannel;
        string _myMode;
        bool _Click = false;

        public ChannelProperties()
        {
            InitializeComponent();
        }
        public ChannelProperties(LocalDriverServer d, Channel c, string Mode):this()
        {
            _myDriver = d; 
            _myChannel = c;
            _myMode = Mode;
        }
        private void ChannelProperties_Load(object sender, EventArgs e)
        {
            try
            {
                textBox1.Text = _myChannel.Name;
                textBox2.Text = _myChannel.DriverLocation;
                textBox3.Text = _myChannel.UpdateRate.ToString();
                textBox4.Text = _myChannel.Address;
                textBox5.Text = _myChannel.MaxWriteTime.ToString() ;

                this.FormClosing += ChannelProperties_FormClosing;
                this.KeyPress += iCheckKey;
            }
            catch { }
        }

        void ChannelProperties_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            { 
            if (_Click == false && _myMode == "Paste")
                    _myChannel.Name = "";
            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text != _myChannel.Name && _myDriver.GetChannel(textBox1.Text) != null)
                {
                    MessageBox.Show("This Channel Name is existed", "ATSCADA");
                    return;
                }

                if (_myChannel.Address != textBox4.Text ||
                    _myChannel.Name != textBox1.Text ||
                    _myChannel.UpdateRate.ToString() != textBox3.Text ||
                    _myChannel.MaxWriteTime.ToString() != textBox5.Text
                    )
                {
                    _myDriver.Saved = false;
                }

                _myChannel.Name = textBox1.Text;
                _myChannel.UpdateRate = Convert.ToDouble(textBox3.Text);
                _myChannel.MaxWriteTime = Convert.ToInt32(textBox5.Text);

                if (_myChannel.Address != textBox4.Text)
                {
                    //foreach (Channel c in _myDriver.ChannelList)
                    //{
                    //    if (textBox4.Text == c.Address)
                    //    {
                    //        MessageBox.Show("This Channel Address is existed", "ATSCADA");
                    //        return;
                    //    }
                    //}
                    string temp = _myChannel.Address;
                    _myChannel.Run = false;


                    _myChannel.Address = textBox4.Text;
                    //while (_myChannel.IsUpdating == true) { }

                    //_myChannel.DriverLoader.Driver.Disconnect();

                    //_myChannel.Address = textBox4.Text;
                    //_myChannel.DriverLoader.Driver.ChannelAddress = textBox4.Text;

                    //if (!_myChannel.DriverLoader.Driver.Connect())
                    //{
                    //    MessageBox.Show("Could not connect with these parameters", "ATSCADA");
                    //    _myChannel.Address = temp;
                    //    return;
                    //}

                    _myChannel.Run = true;
                }
                //else
                //{
                //    if (_myMode == "Paste")
                //    {
                //        MessageBox.Show("This Channel Address is existed", "ATSCADA");
                //        return;
                //    }
                //}

                //if (_myMode == "Paste")
                //{
                //    foreach (Channel c in _myDriver.ChannelList)
                //    {
                //        if (_myChannel.Address == c.Address)
                //        {
                //            MessageBox.Show("This Channel Address is existed", "ATSCADA");
                //            return;
                //        }
                //    }
                //}

                _Click = true;
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
