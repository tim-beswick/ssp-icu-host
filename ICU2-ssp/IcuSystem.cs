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


    public enum ICUEvents
    {
        DSIABLED = 0xE8,
        FACE_DETECTED = 0x14,
    }


    public class IcuSystem
    {

        public event EventHandler NewDeviceData;
        public event EventHandler RunError;
        public event EventHandler<LogDataEventArgs> LogData;
        public event EventHandler<DeviceEventArgs> NewDeviceEvent;

        private sspComs coms;
        private sspDevice device;
        private SSPCommand cmd;
        private bool SystemRunning;
        private bool DisabledSeen;

        public string ComPort { get; set; }
        public string SerialNumber { get; set; }
        public string Version { get; set; }
        public string IPAddress { get; set; }

        public string []  CameraName {get;set;}



        public class LogDataEventArgs : EventArgs
        {
            public string packet { get; set; }

            public string timestamp { get; set; }
            public string direction { get; set; }

        }


        public class DeviceEventArgs : EventArgs
        {
            public ICUEvents event_name { get;set; }

            public int camera_index { get; set; }
            public int age { get; set; }
            public bool id_found { get; set; }

            public string uid { get; set; }
            public string attribute { get; set; }

            public string camera_name { get; set; }

            public DateTime timestamp { get; set; }

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



        protected virtual void OnNewDeviceEvent(DeviceEventArgs e)
        {
            EventHandler<DeviceEventArgs> handler = NewDeviceEvent;
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
            DisabledSeen = false;

            CameraName = new string[3];

        }


        public void StartRun()
        {
            Thread thr = new Thread(RunThreadHandler);
            thr.Start();

        }


        public void StopRun()
        {

            SystemRunning = false;
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
            byte[] data = null;
            rsp = GetCameraName(0, ref data);
            if(rsp != CommandResponse.OK)
            {
                return rsp;

            }
            CameraName[0] = Encoding.UTF8.GetString(data);

            rsp = GetCameraName(1, ref data);
            if (rsp != CommandResponse.OK)
            {
                return rsp;
            }
            CameraName[1] = Encoding.UTF8.GetString(data);
            rsp = GetCameraName(2, ref data);
            if (rsp != CommandResponse.OK)
            {
                return rsp;
            }
            CameraName[2] = Encoding.UTF8.GetString(data);

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
            SystemRunning = true;
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


        private CommandResponse GetCameraName(byte camera_index, ref byte[] response )
        {


            cmd.CommandHeader = (byte)0x2F;     // control command
            cmd.ParameterData[0] = (byte)0x01;    // read
            cmd.ParameterData[1] = (byte)0x01;   // camera sub command
            cmd.ParameterData[2] = (byte)0x04;
            cmd.ParameterData[3] = (byte)camera_index;
            cmd.ParameterLength = 4;
            if (SendCommand(cmd) != CommandResponse.OK)
            {
                return CommandResponse.PortError;
            }

            if (cmd.GenericResponse != SSP_COMMAND_RESPONSE.OK)
            {
                return CommandResponse.GenericFail;
            }

            response = new byte[cmd.ResponseDataLength];

            for(int i = 0; i < cmd.ResponseDataLength; i++)
            {
                response[i] = cmd.ResponseData[i];
            }


            return CommandResponse.OK;

        }



        private void ParsePoll(byte[] data, byte length)
        {
            CommandResponse cmd_rsp;


            if (length == 0)
            {
                DisabledSeen = false;
            }

            for(int i = 0; i < length; i++)
            {

                switch(data[i])
                {
                    case (int)ICUEvents.DSIABLED:
                        if (!DisabledSeen)
                        {
                            DeviceEventArgs arg = new DeviceEventArgs();
                            arg.event_name = ICUEvents.DSIABLED;                           
                            OnNewDeviceEvent(arg);
                            DisabledSeen = true;
                        }
                        break;

                    case (int)ICUEvents.FACE_DETECTED:
                        // get the face info
                        byte cam_index = data[i + 1];
                        DeviceEventArgs arg1 = new DeviceEventArgs();
                        arg1.age = (int)data[i + 2];
                        arg1.timestamp = DateTime.Now;
                        arg1.camera_name = CameraName[cam_index];
                        if(data[i + 3] == 0)
                        {
                            arg1.id_found = false;
                        }
                        else
                        {
                            byte[] id_data = new byte[66];
                            for(int j = 0; j < 66; j++)
                            {
                                id_data[j] = data[i + 4 + j];
                            }
                            arg1.uid = Encoding.UTF8.GetString(id_data,0,36);
                            arg1.attribute = (Encoding.UTF8.GetString(id_data, 36, 30)).Trim();
                            arg1.id_found = true;
                        }
                        arg1.event_name = ICUEvents.FACE_DETECTED;
                        OnNewDeviceEvent(arg1);
                        i = i + 4;
                        DisabledSeen = false;
                        break;


                    

                }

            }



        }



    }
}
