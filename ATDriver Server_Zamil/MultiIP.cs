using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;


namespace ATDriver_Server
{
    public partial class MultiIP : Form
    {
        public MultiIP()
        {
            InitializeComponent();
        }
        public MultiIP(string[] TransString): this()
        {
            _Trans = TransString; 
        }
        string[] _Trans;
        private void MultiIP_Load(object sender, EventArgs e)
        {            
            string hostname = Dns.GetHostName();
            var ip = Dns.GetHostEntry(hostname);

            foreach (IPAddress ipa in ip.AddressList)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    ListViewItem li = new ListViewItem();
                    li.Text = ipa.ToString();
                    listView1.Items.Add(li);
                }                   
            }
            listView1.Items[0].Selected = true;  
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                _Trans[0] = listView1.SelectedItems[0].Text;
            }
            catch { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close(); 
        }
    }
}
