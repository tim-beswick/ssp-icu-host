using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ICU2_ssp
{
    public partial class MDIform1 : Form
    {
        public MDIform1()
        {
            InitializeComponent();

            this.Text = "ICU2 SSP";

        }

        private void MDIform1_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {

            System.Environment.Exit(0);


        }

        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {

            Form1 frm = new Form1();
            frm.MdiParent = this;
            frm.WindowState = FormWindowState.Maximized;
            frm.Show();


        }
    }
}
