using SSPComs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SSPDevice;
using ICU2_ssp;
using static ICU2_ssp.IcuSystem;

namespace ICU2_ssp
{
    public partial class Form1 : Form
    {
        private IcuSystem sys;

        public Form1()
        {
            InitializeComponent();

            sys = new IcuSystem("COM9", 25);
            sys.NewDeviceData += NewDeviceDataHandler;
            sys.RunError += Sys_RunError;
            sys.LogData += Sys_LogData;

            listView1.View = View.Details;
            listView1.Columns.Add("item");
            listView1.Columns.Add("value");
            listView1.HeaderStyle = ColumnHeaderStyle.None;


            listView2.View = View.Details;
            listView2.Columns.Add("time");
            listView2.Columns.Add("dir");
            listView2.Columns.Add("packet");
            listView2.HeaderStyle = ColumnHeaderStyle.None;




        }

        private void Sys_LogData(object sender, LogDataEventArgs e)
        {

            AddLogListItem(listView2, e.timestamp, e.direction, e.packet);

            if (listView2.InvokeRequired)
            {
                listView2.Invoke(new MethodInvoker(delegate
                {
                    foreach (ColumnHeader c in listView2.Columns)
                    {
                        c.Width = -2;
                    }
                }));
            }


        }

        private void Sys_RunError(object sender, EventArgs e)
        {




        }

        private void NewDeviceDataHandler(object sender, EventArgs e)
        {

            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    listView1.Items.Clear();
                }));
            }

            AddListItem(listView1, "Version", sys.Version);
            AddListItem(listView1, "Serial Number", sys.SerialNumber);
            AddListItem(listView1, "IP address", sys.IPAddress);


            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    foreach (ColumnHeader c in listView1.Columns)
                    {
                        c.Width = -2;
                    }
                }));
            }

        }


        private void AddListItem(ListView list, string v1, string v2)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = v1;
                    item.SubItems.Add(v2);
                    list.Items.Add(item);
                }));
            }
        }


        private void AddLogListItem(ListView list, string v1, string v2, string v3)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = v1;
                    item.SubItems.Add(v2);
                    item.SubItems.Add(v3);
                    list.Items.Add(item);
                }));
            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            sys.StartRun();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

    }
}
