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
        private ListViewNF listView2;
        private ListViewNF listView3;

        public Form1()
        {
            InitializeComponent();

            this.Text = "ICU2 SSP";

            sys = new IcuSystem(25);

            List<String> ports = sys.GetInterfacePort();
            foreach (String s in ports)
            {
                comboBox1.Items.Add(s);
            }


            sys.NewDeviceData += NewDeviceDataHandler;
            sys.RunError += Sys_RunError;
            sys.LogData += Sys_LogData;
            sys.NewDeviceEvent += Sys_NewDeviceEvent;

            listView1.View = View.Details;
            listView1.Columns.Add("item");
            listView1.Columns.Add("value");
            listView1.HeaderStyle = ColumnHeaderStyle.None;


            listView2 = new ListViewNF();
            listView2.View = View.Details;
            listView2.Columns.Add("time", 100);
            listView2.Columns.Add("dir", 100);
            listView2.Columns.Add("Tx", 500);
            listView2.Columns.Add("enc", 800);
            listView2.HeaderStyle = ColumnHeaderStyle.None;
            splitContainer2.Panel2.Controls.Add(listView2);
            listView2.Dock = DockStyle.Fill;


            listView3 = new ListViewNF();
            listView3.View = View.Details;
            listView3.Columns.Add("DateTime", 150);
            listView3.Columns.Add("Event", 150);
            listView3.Columns.Add("Camera", 120);
            listView3.Columns.Add("Age", 60);
            listView3.Columns.Add("UID", 300);
            listView3.Columns.Add("Group", 150);
            splitContainer2.Panel1.Controls.Add(listView3);
            listView3.Dock = DockStyle.Fill;

            button1.Enabled = true;
            button2.Enabled = false;

        }

        private void Sys_NewDeviceEvent(object sender, DeviceEventArgs e)
        {

            AddEventListItem(listView3, e);

        }

        private void Sys_LogData(object sender, LogDataEventArgs e)
        {

            AddLogListItem(listView2, e.timestamp, e.direction, e.packet, e.encrypted_packet);

        }

        private void Sys_RunError(object sender, ErrorEventArgs e)
        {


            AddErrorListItem(listView3, DateTime.Now.ToString(), e.message);



        }

        private void NewDeviceDataHandler(object sender, EventArgs e)
        {

            if (listView1.InvokeRequired)
            {
                listView1.Invoke(new MethodInvoker(delegate
                {
                    listView1.Items.Clear();
                    button2.Enabled = true;
                    button1.Enabled = false;
                }));
            }

            AddListItem(listView1, "Version", sys.Version);
            AddListItem(listView1, "Serial Number", sys.SerialNumber);
            AddListItem(listView1, "IP address", sys.IPAddress);
            AddListItem(listView1, "Camera", sys.CameraName[0]);
            AddListItem(listView1, "Camera", sys.CameraName[1]);
            AddListItem(listView1, "Camera", sys.CameraName[2]);


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


            AddUIEventListItem(listView3, "New Device connection");

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

                    list.EnsureVisible(list.Items.Count - 1);

                }));
            }
        }


        private void AddErrorListItem(ListView list, string v1, string v2)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = v1;
                    item.SubItems.Add(v2);
                    list.Items.Add(item);

                    list.EnsureVisible(list.Items.Count - 1);

                }));
            }
        }


        private void AddLogListItem(ListView list, string v1, string v2, string v3, string v4)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = v1;
                    item.SubItems.Add(v2);
                    item.SubItems.Add(v3);
                    item.SubItems.Add(v4);
                    list.Items.Add(item);
                    list.EnsureVisible(list.Items.Count - 1);
                }));
            }
        }



        private void AddEventListItem(ListView list, DeviceEventArgs args)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = args.timestamp.ToString();
                    if (args.event_name == ICUEvents.FACE_DETECTED)
                    {
                        item.SubItems.Add(args.event_name.ToString());
                        item.SubItems.Add(args.camera_name);
                        item.SubItems.Add(args.age.ToString());
                        if (args.id_found)
                        {
                            item.SubItems.Add(args.uid);
                            item.SubItems.Add(args.attribute);
                        }
                    }
                    list.Items.Add(item);
                    list.EnsureVisible(list.Items.Count - 1);
                }));
            }
        }


        private void AddUIEventListItem(ListView list, string evt)
        {
            if (list.InvokeRequired)
            {
                list.Invoke(new MethodInvoker(delegate
                {
                    ListViewItem item = new ListViewItem();
                    item.Text = DateTime.Now.ToString();
                    item.SubItems.Add(evt);
                    list.Items.Add(item);
                    list.EnsureVisible(list.Items.Count - 1);
                }));
            }
            else
            {
                ListViewItem item = new ListViewItem();
                item.Text = DateTime.Now.ToString();
                item.SubItems.Add(evt);
                list.Items.Add(item);
                list.EnsureVisible(list.Items.Count - 1);


            }
        }



        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1)
            {
                MessageBox.Show("Select a serial comport");
                return;
            }

            button1.Enabled = false;
            AddUIEventListItem(listView3, "Start");
            sys.StartRun();
        }



        private void button2_Click(object sender, EventArgs e)
        {
            sys.StopRun();
            AddUIEventListItem(listView3, "Stop");
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (sys != null)
            {
                string port = sys.GetPortFromName(comboBox1.Text);
                if (port != "")
                {
                    sys.ComPort = port;
                }
            }

        }
    }
}
