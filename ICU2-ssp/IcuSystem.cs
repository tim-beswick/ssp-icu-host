using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SSPComs;
using SSPDevice;
using System.Threading;
using System.Diagnostics;

namespace ICU2_ssp
{

    public enum RunState
    {
        None,
        Discover,
        Secure,
        Info,
        Enable,
        Run
    }


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
        public event EventHandler<ErrorEventArgs> RunError;
        public event EventHandler<LogDataEventArgs> LogData;
        public event EventHandler<DeviceEventArgs> NewDeviceEvent;

        private sspComs coms;
        private sspDevice device;
        private SSPCommand cmd;
        private bool SystemRunning;
        private bool DisabledSeen;

        private RunState state;


        public string ComPort { get; set; }
        public string SerialNumber { get; set; }
        public string Version { get; set; }
        public string IPAddress { get; set; }

        public string []  CameraName {get;set;}



        public class LogDataEventArgs : EventArgs
        {
            public string packet { get; set; }

            public string encrypted_packet { get; set; }

            public string timestamp { get; set; }
            public string direction { get; set; }

        }


        public class ErrorEventArgs : EventArgs
        {
            public CommandResponse response { get; set; }
            public string message { get; set; }
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


        protected virtual void OnRunError(ErrorEventArgs e)
        {
            EventHandler<ErrorEventArgs> handler = RunError;
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


        public IcuSystem(byte address)
        {

            device = new sspDevice();
            device.SSPAddress = address;

            coms = device.SspComs;
            cmd = device.SspCommand;
            cmd.ESSPCommand = false;
           
            SystemRunning = false;
            DisabledSeen = false;

            CameraName = new string[3];
            state = RunState.None;

        }



        public IcuSystem(string com, byte address)
        {
            
            ComPort = com;
            device = new sspDevice();
            device.SSPAddress = address;
          
            coms = device.SspComs;
            cmd = device.SspCommand;
            cmd.ESSPCommand = false;
            SystemRunning = false;
            DisabledSeen = false;

            CameraName = new string[3];

            state = RunState.None;

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

                OnRunError(new ErrorEventArgs() { response = CommandResponse.PortError, message = "No Port selected" });
                return false;
            }

            if (coms.OpenPort(ComPort))
            {

                return true;
            }
            else
            {
                OnRunError(new ErrorEventArgs() { response = CommandResponse.PortError, message = "Unable to open port" });
                return false;
            }



        }

        public void ClosePort()
        {

            coms.ClosePort();
        }



        public CommandResponse Sync()
        {
            cmd.ParameterLength = 0;
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
            CommandResponse rsp;

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


        
        private CommandResponse KeyExchange()
        {
            if(device.DoDeviceKeyExchange() == SSP_COMMAND_RESPONSE.OK)
            {
                // success - set global key flag so for all future commands
                cmd.ESSPCommand = true; 
                return CommandResponse.OK;
            }
            else
            {
                return CommandResponse.GenericFail;
            }
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
                encrypted_packet = cmd.ESSPCommand ? coms.GetSSP().SSPLog.TxPacketEncrypted : "",
            };
            OnLogData(arg);
            LogDataEventArgs arg_r = new LogDataEventArgs()
            {
                direction = "RX",
                packet = coms.GetSSP().SSPLog.RxPacketPlain,
                timestamp = coms.GetSSP().SSPLog.RxTimeStamp,
                encrypted_packet = cmd.ESSPCommand ? coms.GetSSP().SSPLog.RxPacketEncrypted : "",

            };
            OnLogData(arg_r);

            return CommandResponse.OK;

        }



        private void RunThreadHandler()
        {

            if (!OpenPort())
            {
                ClosePort();
                return;
            }


            state = RunState.Discover;
            SystemRunning = true;
            while (SystemRunning)
            {

                switch (state)
                {
                    case RunState.Discover: // find a connected device
                        if(Sync() == CommandResponse.OK)
                        {
                            state = RunState.Secure;
                        }
                        else
                        {
                            OnRunError(new ErrorEventArgs() { response = CommandResponse.Timeout, message = "Connection timeout" });
                            Thread.Sleep(1000); // retry delay
                        }
                        break;
                    case RunState.Secure: // set up encrypted ssp
                        if(KeyExchange() == CommandResponse.OK)
                        {
                            state = RunState.Info;
                        }
                        else
                        {
                            OnRunError(new ErrorEventArgs() { response = CommandResponse.GenericFail, message = "Key Exchange fail" });
                            state = RunState.Discover;
                        }
                        break;
                    case RunState.Info:
                        if(RunInit() == CommandResponse.OK)
                        {
                            // signak new device discovered
                            OnNewDeviceData(EventArgs.Empty);
                            state = RunState.Enable;
                        }
                        else
                        {
                            OnRunError(new ErrorEventArgs() { response = CommandResponse.GenericFail, message = "Device Info failed" });
                            state = RunState.Discover;
                        }
                        break;
                    case RunState.Enable:
                        if (Enable() == CommandResponse.OK)
                        {
                            state = RunState.Run;
                        }
                        else
                        {
                            OnRunError(new ErrorEventArgs() { response = CommandResponse.GenericFail, message = "Enable faile" });
                            state = RunState.Discover;
                        }
                        break;
                    case RunState.Run:
                        if(Poll() == CommandResponse.OK)
                        {
                            Thread.Sleep(300);  // poll delay
                        }
                        else
                        {
                            OnRunError(new ErrorEventArgs() { response = CommandResponse.Timeout, message = "Poll timeout" });
                            state = RunState.Discover;
                        }
                        break;
                }


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


        public List<string> GetInterfacePort()
        {
            System.Management.ManagementObjectCollection moReturn;
            System.Management.ManagementObjectSearcher moSearch;

            List<string> mComFullName = new List<string>();

            int i, j;

            moSearch = new System.Management.ManagementObjectSearcher("root\\CIMV2", "Select * from Win32_PnPEntity");
            moReturn = moSearch.Get();

            foreach(System.Management.ManagementObject mo in moReturn)
            {
                if(mo.Properties["Name"].Value != null)
                {



                    if(
                        mo.Properties["Name"].Value.ToString().Contains("USB Serial Port") ||
                        mo.Properties["Name"].Value.ToString().Contains("Gadget") ||
                        mo.Properties["Name"].Value.ToString().Contains("ITL USB") ||
                        mo.Properties["Name"].Value.ToString().Contains("ITL BV") ||
                        mo.Properties["Name"].Value.ToString().Contains("USB Serial Device")
                        )
                    {

                        mComFullName.Add(mo.Properties["Name"].Value.ToString());

                    }


                }
            }


            return mComFullName;


        }


        public string GetPortFromName(string name)
        {

            int ind = name.IndexOf("COM");
            return name.Substring(ind, 5).Replace(")","");

        }





    }
}
