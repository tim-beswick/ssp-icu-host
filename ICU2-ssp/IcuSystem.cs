using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSPComs;
using SSPDevice;
using System.Threading;


namespace ICU2_ssp
{

    public enum CommandResponse
    {
        OK = 0,
        PortError = 100,
        Timeout,
        GenericFail,

    }


    public class IcuSystem
    {

        public event EventHandler NewDeviceData;
        public event EventHandler RunError;
        public event EventHandler<LogDataEventArgs> LogData;

        private sspComs coms;
        private sspDevice device;
        private SSPCommand cmd;
        private bool SystemRunning;

        public string ComPort { get; set; }
        public string SerialNumber { get; set; }
        public string Version { get; set; }
        public string IPAddress { get; set; }



        public class LogDataEventArgs : EventArgs
        {
            public string packet { get; set; }

            public string timestamp { get; set; }
            public string direction { get; set; }

        }



        protected virtual void OnNewDeviceData(EventArgs e)
        {
            EventHandler handler = NewDeviceData;
            if (handler != null)
            {
                handler(this, e);
            }
        }


        protected virtual void OnRunError(EventArgs e)
        {
            EventHandler handler = RunError;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnLogData(LogDataEventArgs e)
        {
            EventHandler<LogDataEventArgs> handler = LogData;
            if (handler != null)
            {
                handler(this, e);
            }
        }


        public IcuSystem(string com, byte address)
        {
            
            ComPort = com;
            device = new sspDevice();
            device.SSPAddress = address;

            coms = device.SspComs;
            cmd = device.SspCommand;
            SystemRunning = false;

        }


        public void StartRun()
        {
            Thread thr = new Thread(RunThreadHandler);
            thr.Start();

        }



        public bool OpenPort()
        {

            if(ComPort == "")
            {
                return false;
            }

            return coms.OpenPort(ComPort);


        }

        public void ClosePort()
        {

            coms.ClosePort();
        }



        public CommandResponse Sync()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.SYNC;
            cmd.ESSPCommand = false;

            if(SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }
            
            if(cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }


            return CommandResponse.OK;

        }


        public CommandResponse RunInit()
        {

            CommandResponse rsp = Sync();
            if(rsp != CommandResponse.OK)
            {
                return rsp;
            }
            rsp = GetSerialNumber();
            if (rsp != CommandResponse.OK)
            {
                return rsp;
            }
            rsp = GetIPAddress();
            if (rsp != CommandResponse.OK)
            {
                return rsp;
            }
            rsp = GetFimrwareVersion();
            if (rsp != CommandResponse.OK)
            {
                return rsp;
            }

            return rsp;

        }


        


        private CommandResponse GetSerialNumber()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.GET_SERIAL_NUMBER;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }

            SerialNumber = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);


            return CommandResponse.OK;


        }



        private CommandResponse GetIPAddress()
        {

            cmd.CommandHeader = (byte)0x16;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }

            IPAddress = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);


            return CommandResponse.OK;


        }




        private CommandResponse GetFimrwareVersion()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.GET_FIRMWARE_VERSION;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }

            Version = Encoding.UTF8.GetString(cmd.ResponseData, 0, cmd.ResponseDataLength);


            return CommandResponse.OK;


        }




        private CommandResponse Enable()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.ENABLE;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }


            return CommandResponse.OK;


        }



        private CommandResponse Disable()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.DISABLE;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }


            return CommandResponse.OK;


        }


        private CommandResponse Poll()
        {

            cmd.CommandHeader = (byte)SSP_COMMAND.POLL;
            cmd.ParameterLength = 0;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }
            else
            {
                ParsePoll(cmd.ResponseData, cmd.ResponseDataLength);
            }




            return CommandResponse.OK;


        }


        private CommandResponse SendCommand(SSPCommand cmd)
        {

            if (coms.SendCommand(cmd) == false)
            {
                return CommandResponse.PortError;

            }
            LogDataEventArgs arg = new LogDataEventArgs()
            {
                direction = "TX",
                packet = coms.GetSSP().SSPLog.TxPacketPlain,
                timestamp = coms.GetSSP().SSPLog.TxTimeStamp,
            };
            OnLogData(arg);
            LogDataEventArgs arg_r = new LogDataEventArgs()
            {
                direction = "RX",
                packet = coms.GetSSP().SSPLog.RxPacketPlain,
                timestamp = coms.GetSSP().SSPLog.RxTimeStamp,
            };
            OnLogData(arg_r);

            return CommandResponse.OK;

        }



        private void RunThreadHandler()
        {

            if (!OpenPort())
            {
                OnRunError(EventArgs.Empty);
                return;
            }


            if(RunInit() != CommandResponse.OK)
            {
                OnRunError(EventArgs.Empty);
                return;
            }
            OnNewDeviceData(EventArgs.Empty);


            if(Enable() != CommandResponse.OK)
            {
                OnRunError(EventArgs.Empty);
                return;
            }
            while(SystemRunning)
            {

                if(Poll() != CommandResponse.OK)
                {
                    OnRunError(EventArgs.Empty);
                }

                // poll delay
                Thread.Sleep(200);

            }



            ClosePort();


        }


        private void ParsePoll(byte[] data, byte length)
        {

        }



    }
}
