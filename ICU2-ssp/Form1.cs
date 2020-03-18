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

namespace ICU2_ssp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            label1.Text = "";

            sspDevice dev = new sspDevice();
            SSPCommand cmd = dev.SspCommand;

            dev.SSPAddress = 25;
            dev.OpenPort("COM12");

            sspComs coms = dev.SspComs;

            cmd.CommandHeader = (byte)SSP_COMMAND.SYNC;
            cmd.ParameterLength = 0;
            if (!coms.SendCommand(cmd) || cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                dev.CloseCom();
                return;
            }


            
            if(dev.DoDeviceKeyExchange() != SSP_COMMAND_RESPONSE.OK)
            {
                dev.CloseCom();
                return;
            }

            cmd.ESSPCommand = true;
            cmd.CommandHeader = 0x16;
            cmd.ParameterLength = 0;
            if (!coms.SendCommand(cmd) || cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                label1.Text = "failed 1";
                dev.CloseCom();
                return;
            }

            label1.Text = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);


            cmd.ESSPCommand = true;
            cmd.CommandHeader = 0x16;
            cmd.ParameterLength = 0;
            if (!coms.SendCommand(cmd) || cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                label1.Text = "failed 2";
                dev.CloseCom();
                return;
            }

            label1.Text = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);



            cmd.ESSPCommand = true;
            cmd.CommandHeader = 0x16;
            cmd.ParameterLength = 0;
            if (!coms.SendCommand(cmd) || cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                label1.Text = "failed 3";
                dev.CloseCom();
                return;
            }

            label1.Text = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);


            dev.CloseCom();
           


        }
    }
}
