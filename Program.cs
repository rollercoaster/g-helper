using HidLibrary;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32.TaskScheduler;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

public class Startup
{

    static string taskName = "GHelper";

    public Startup()
    {

    }

    public bool IsScheduled()
    {
        TaskService taskService = new TaskService();
        return (taskService.RootFolder.AllTasks.Any(t => t.Name == taskName));
    }

    public void Schedule()
    {
        TaskService taskService = new TaskService();

        string strExeFilePath = Application.ExecutablePath;

        if (strExeFilePath is null) return;

        var userId = WindowsIdentity.GetCurrent().Name;

        Debug.WriteLine(strExeFilePath);
        TaskDefinition td = TaskService.Instance.NewTask();
        td.RegistrationInfo.Description = "GHelper Auto Start";
        td.Triggers.Add(new LogonTrigger { UserId = userId, });
        td.Actions.Add(strExeFilePath);

        td.Settings.StopIfGoingOnBatteries = false;
        td.Settings.DisallowStartIfOnBatteries = false;

        TaskService.Instance.RootFolder.RegisterTaskDefinition(taskName, td);
    }

    public void UnSchedule()
    {
        TaskService taskService = new TaskService();
        taskService.RootFolder.DeleteTask(taskName);
    }
}


public class AppConfig
{

    string appPath;
    string configFile;

    public Dictionary<string, object> config = new Dictionary<string, object>();

    public AppConfig()
    {

        appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\GHelper";
        configFile = appPath + "\\config.json";

        if (!System.IO.Directory.Exists(appPath))
            System.IO.Directory.CreateDirectory(appPath);

        if (File.Exists(configFile))
        {
            string text = File.ReadAllText(configFile);
            try
            {
                config = JsonSerializer.Deserialize<Dictionary<string, object>>(text);
            }
            catch
            {
                initConfig();
            }
        }
        else
        {
            initConfig();
        }

    }

    private void initConfig()
    {
        config = new Dictionary<string, object>();
        config["performance_mode"] = 0;
        string jsonString = JsonSerializer.Serialize(config);
        File.WriteAllText(configFile, jsonString);
    }

    public int getConfig(string name)
    {
        if (config.ContainsKey(name))
            return int.Parse(config[name].ToString());
        else return -1;
    }

    public void setConfig(string name, int value)
    {
        config[name] = value;
        string jsonString = JsonSerializer.Serialize(config);
        File.WriteAllText(configFile, jsonString);
    }



}


public class NativeMethods
{

    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerWriteDCValueIndex(IntPtr RootPowerKey,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
        int AcValueIndex);

    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerWriteACValueIndex(IntPtr RootPowerKey,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
        int AcValueIndex);

    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerReadACValueIndex(IntPtr RootPowerKey,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
        out IntPtr AcValueIndex
        );

    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerReadDCValueIndex(IntPtr RootPowerKey,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SubGroupOfPowerSettingsGuid,
        [MarshalAs(UnmanagedType.LPStruct)] Guid PowerSettingGuid,
        out IntPtr AcValueIndex
        );



    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerSetActiveScheme(IntPtr RootPowerKey,
        [MarshalAs(UnmanagedType.LPStruct)] Guid SchemeGuid);

    [DllImport("PowrProf.dll", CharSet = CharSet.Unicode)]
    static extern UInt32 PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);

    static readonly Guid GUID_CPU = new Guid("54533251-82be-4824-96c1-47b60b740d00");
    static readonly Guid GUID_BOOST = new Guid("be337238-0d82-4146-a960-4f3749d470c7");

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;

        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;

        public short dmLogPixels;
        public short dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    };

    [Flags()]
    public enum DisplaySettingsFlags : int
    {
        CDS_UPDATEREGISTRY = 1,
        CDS_TEST = 2,
        CDS_FULLSCREEN = 4,
        CDS_GLOBAL = 8,
        CDS_SET_PRIMARY = 0x10,
        CDS_RESET = 0x40000000,
        CDS_NORESET = 0x10000000
    }

    // PInvoke declaration for EnumDisplaySettings Win32 API
    [DllImport("user32.dll")]
    public static extern int EnumDisplaySettingsEx(
         string lpszDeviceName,
         int iModeNum,
         ref DEVMODE lpDevMode);

    // PInvoke declaration for ChangeDisplaySettings Win32 API
    [DllImport("user32.dll")]
    public static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName, ref DEVMODE lpDevMode, IntPtr hwnd,
            DisplaySettingsFlags dwflags, IntPtr lParam);


    public const int ENUM_CURRENT_SETTINGS = -1;

    public const string laptopScreenName = "\\\\.\\DISPLAY1";

    public static DEVMODE CreateDevmode()
    {
        DEVMODE dm = new DEVMODE();
        dm.dmDeviceName = new String(new char[32]);
        dm.dmFormName = new String(new char[32]);
        dm.dmSize = (short)Marshal.SizeOf(dm);
        return dm;
    }

    public static Screen FindLaptopScreen()
    {
        var screens = Screen.AllScreens;
        Screen laptopScreen = null;

        foreach (var screen in screens)
        {
            if (screen.DeviceName == laptopScreenName)
            {
                laptopScreen = screen;
            }
        }

        if (laptopScreen is null) return null;
        else return laptopScreen;
    }

    public static int GetRefreshRate()
    {
        DEVMODE dm = CreateDevmode();

        Screen laptopScreen = FindLaptopScreen();
        int frequency = -1;

        if (laptopScreen is null)
            return -1;

        if (0 != NativeMethods.EnumDisplaySettingsEx(laptopScreen.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
        {
            frequency = dm.dmDisplayFrequency;
        }

        return frequency;
    }

    public static int SetRefreshRate(int frequency = 120)
    {
        DEVMODE dm = CreateDevmode();
        Screen laptopScreen = FindLaptopScreen();

        if (laptopScreen is null)
            return -1;

        if (0 != NativeMethods.EnumDisplaySettingsEx(laptopScreen.DeviceName, NativeMethods.ENUM_CURRENT_SETTINGS, ref dm))
        {
            dm.dmDisplayFrequency = frequency;
            int iRet = NativeMethods.ChangeDisplaySettingsEx(laptopScreen.DeviceName, ref dm, IntPtr.Zero, DisplaySettingsFlags.CDS_UPDATEREGISTRY, IntPtr.Zero);
            return iRet;
        }

        return 0;

    }

    static Guid GetActiveScheme()
    {
        IntPtr pActiveSchemeGuid;
        var hr = PowerGetActiveScheme(IntPtr.Zero, out pActiveSchemeGuid);
        Guid activeSchemeGuid = (Guid)Marshal.PtrToStructure(pActiveSchemeGuid, typeof(Guid));
        return activeSchemeGuid;
    }

    public static int GetCPUBoost()
    {
        IntPtr AcValueIndex;
        Guid activeSchemeGuid = GetActiveScheme();

        UInt32 value = PowerReadACValueIndex(IntPtr.Zero,
             activeSchemeGuid,
             GUID_CPU,
             GUID_BOOST, out AcValueIndex);

        return AcValueIndex.ToInt32();

    }

    public static void SetCPUBoost(int boost = 0)
    {
        Guid activeSchemeGuid = GetActiveScheme();

        var hrAC = PowerWriteACValueIndex(
             IntPtr.Zero,
             activeSchemeGuid,
             GUID_CPU,
             GUID_BOOST,
             boost);

        PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);

        var hrDC = PowerWriteDCValueIndex(
             IntPtr.Zero,
             activeSchemeGuid,
             GUID_CPU,
             GUID_BOOST,
             boost);

        PowerSetActiveScheme(IntPtr.Zero, activeSchemeGuid);


    }
}


public class Aura
{

    static byte[] MESSAGE_SET = { 0x5d, 0xb5 };
    static byte[] MESSAGE_APPLY = { 0x5d, 0xb4 };

    public const int Static = 0;
    public const int Breathe = 1;
    public const int Strobe = 2;
    public const int Rainbow = 3;
    public const int Dingding = 10;

    public const int SpeedSlow = 0;
    public const int SpeedMedium = 1;
    public const int SpeedHigh = 2;

    public static int Mode = Static;
    public static Color Color1 = Color.White;
    public static Color Color2 = Color.Black;
    public static int Speed = SpeedSlow;

    public static byte[] AuraMessage(int mode, Color color, Color color2, int speed)
    {
        byte[] msg = new byte[17];
        msg[0] = 0x5d;
        msg[1] = 0xb3;
        msg[2] = 0x00; // Zone 
        msg[3] = (byte)mode; // Aura Mode
        msg[4] = (byte)(color.R); // R
        msg[5] = (byte)(color.G); // G
        msg[6] = (byte)(color.B); // B
        msg[7] = (byte)speed; // aura.speed as u8;
        msg[8] = 0; // aura.direction as u8;
        msg[10] = (byte)(color2.R); // R
        msg[11] = (byte)(color2.G); // G
        msg[12] = (byte)(color2.B); // B
        return msg;
    }

    public static void ApplyAura()
    {

        HidDevice[] HidDeviceList;
        int[] deviceIds = { 0x1854, 0x1869, 0x1866, 0x19b6 };

        HidDeviceList = HidDevices.Enumerate(0x0b05, deviceIds).ToArray();

        foreach (HidDevice device in HidDeviceList)
        {
            if (device.IsConnected)
            {
                if (device.Description.IndexOf("HID") >= 0)
                {
                    device.OpenDevice();
                    byte[] msg = AuraMessage(Mode, Color1, Color2, Speed);
                    device.Write(msg);
                    device.Write(MESSAGE_SET);
                    device.Write(MESSAGE_APPLY);
                    device.CloseDevice();
                }

            }
        }

    }
}


public class UpdateVisitor : IVisitor
{
    public void VisitComputer(IComputer computer)
    {
        computer.Traverse(this);
    }
    public void VisitHardware(IHardware hardware)
    {
        hardware.Update();
        foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
    }
    public void VisitSensor(ISensor sensor) { }
    public void VisitParameter(IParameter parameter) { }
}


public class HardwareMonitor
{

    Computer computer;

    public float? cpuTemp = -1;
    public float? gpuTemp = -1;
    public float? batteryDischarge = -1;
    public float? batteryCharge = -1;

    public HardwareMonitor()
    {
        computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsBatteryEnabled = true,
        };

    }

    public void ReadSensors()
    {

        computer.Open();
        computer.Accept(new UpdateVisitor());

        cpuTemp = -1;
        gpuTemp = -1;
        batteryDischarge = -1;
        batteryCharge = -1;

        foreach (IHardware hardware in computer.Hardware)
        {
            //Debug.WriteLine("Hardware: {0}", hardware.Name);
            //Debug.WriteLine("Hardware: {0}", hardware.HardwareType);

            foreach (ISensor sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature)
                {
                    if (hardware.HardwareType.ToString().Contains("Cpu") && sensor.Name.Contains("Core"))
                    {
                        cpuTemp = sensor.Value;
                        //Debug.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);
                    }

                    if (hardware.HardwareType.ToString().Contains("Gpu") && sensor.Name.Contains("Core"))
                    {
                        gpuTemp = sensor.Value;
                    }

                    //Debug.WriteLine("\tSensor: {0}, value: {1}", sensor.Name, sensor.Value);

                }
                else if (sensor.SensorType == SensorType.Power)
                {
                    if (sensor.Name.Contains("Discharge"))
                    {
                        batteryDischarge = sensor.Value;
                    }

                    if (sensor.Name.Contains("Charge"))
                    {
                        batteryCharge = sensor.Value;
                    }
                }



            }
        }

    }

    public void StopReading()
    {
        computer.Close();
    }

}



namespace GHelper
{
    static class Program
    {
        public static NotifyIcon trayIcon;

        public static ASUSWmi wmi;
        public static AppConfig config;

        public static SettingsForm settingsForm;

        public static Startup scheduler;
        public static HardwareMonitor hwmonitor;

        // The main entry point for the application
        public static void Main()
        {
            trayIcon = new NotifyIcon
            {
                Text = "G-Helper",
                Icon = GHelper.Properties.Resources.standard,
                Visible = true
            };

            trayIcon.MouseClick += TrayIcon_MouseClick; ;

            config = new AppConfig();

            wmi = new ASUSWmi();
            wmi.SubscribeToEvents(WatcherEventArrived);

            scheduler = new Startup();

            settingsForm = new SettingsForm();

            settingsForm.InitGPUMode();
            settingsForm.InitBoost();
            settingsForm.InitAura();

            settingsForm.SetPerformanceMode(config.getConfig("performance_mode"));
            settingsForm.SetBatteryChargeLimit(config.getConfig("charge_limit"));

            settingsForm.VisualiseGPUAuto(config.getConfig("gpu_auto"));
            settingsForm.VisualiseScreenAuto(config.getConfig("screen_auto"));

            settingsForm.SetStartupCheck(scheduler.IsScheduled());

            bool isPlugged = (System.Windows.Forms.SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online);
            settingsForm.AutoGPUMode(isPlugged ? 1 : 0);
            settingsForm.AutoScreen(isPlugged ? 1 : 0);

            hwmonitor = new HardwareMonitor();
            IntPtr dummy = settingsForm.Handle;

            Application.Run();

        }


        static void WatcherEventArrived(object sender, EventArrivedEventArgs e)
        {
            var collection = (ManagementEventWatcher)sender;

            if (e.NewEvent is null) return;

            int EventID = int.Parse(e.NewEvent["EventID"].ToString());

            Debug.WriteLine(EventID);

            switch (EventID)
            {
                case 56:    // Rog button
                case 174:   // FN+F5

                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.CyclePerformanceMode();
                    });

                    return;
                case 179:   // FN+F4
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.CycleAuraMode();
                    });
                    return;
                case 87:  // Battery
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.AutoGPUMode(0);
                        settingsForm.AutoScreen(0);
                    });
                    return;
                case 88:  // Plugged
                    settingsForm.SetBatteryChargeLimit(config.getConfig("charge_limit"));
                    settingsForm.BeginInvoke(delegate
                    {
                        settingsForm.AutoGPUMode(1);
                        settingsForm.AutoScreen(1);
                    });
                    return;

            }


        }

        static void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            {
                if (settingsForm.Visible)
                    settingsForm.Hide();
                else
                {
                    settingsForm.Show();
                    settingsForm.Activate();
                }

                trayIcon.Icon = trayIcon.Icon; // refreshing icon as it get's blurred when screen resolution changes
            }
        }



        static void OnExit(object sender, EventArgs e)
        {
            trayIcon.Visible = false;
            Application.Exit();
        }
    }

}