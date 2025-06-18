using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using Guna.UI2.WinForms;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace hoanlepro
{
    public partial class Form1 : Form
    {
        [DllImport("psapi.dll")]
        static extern bool EmptyWorkingSet(IntPtr hProcess);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        // Import các hàm từ user32.dll để thao tác với cửa sổ
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private const uint WM_CLOSE = 0x0010; // Lệnh đóng cửa sổ

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        private const string StartupKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string StartupValue = "HoanLeOptimizer";
        private const string GameDVRKey = "SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers";
        private Timer performanceTimer;
        private PerformanceCounter cpuCounter;
        private PerformanceCounter ramAvailableCounter;
        private ManagementObjectSearcher ramSearcher;
        private NetworkInterface[] networkInterfaces;
        private long[] lastBytesReceived;
        private long[] lastBytesSent;
        private DateTime lastSpeedCheck;
        private double maxDownloadSpeed;
        private double maxUploadSpeed;
        private Timer activeWindowTimer;
        private Timer processUpdateTimer;
        private string currentProcessName = "";
        private bool isFPSAdjustEnabled = false;
        private Dictionary<string, int> processOriginalFPS = new Dictionary<string, int>();

        // Constants for ShowWindow
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        private Dictionary<string, string> timezoneMap = new Dictionary<string, string>()
        {
            {"(UTC-12:00) International Date Line West", "Dateline Standard Time"},
            {"(UTC-11:00) Coordinated Universal Time-11", "UTC-11"},
            {"(UTC-10:00) Hawaii", "Hawaiian Standard Time"},
            {"(UTC-09:00) Alaska", "Alaskan Standard Time"},
            {"(UTC-08:00) Baja California", "Pacific Standard Time (Mexico)"},
            {"(UTC-08:00) Pacific Time (US & Canada)", "Pacific Standard Time"},
            {"(UTC-07:00) Arizona", "US Mountain Standard Time"},
            {"(UTC-07:00) Chihuahua, La Paz, Mazatlan", "Mountain Standard Time (Mexico)"},
            {"(UTC-07:00) Mountain Time (US & Canada)", "Mountain Standard Time"},
            {"(UTC-06:00) Central America", "Central America Standard Time"},
            {"(UTC-06:00) Central Time (US & Canada)", "Central Standard Time"},
            {"(UTC-06:00) Guadalajara, Mexico City, Monterrey", "Central Standard Time (Mexico)"},
            {"(UTC-06:00) Saskatchewan", "Canada Central Standard Time"},
            {"(UTC-05:00) Bogota, Lima, Quito, Rio Branco", "SA Pacific Standard Time"},
            {"(UTC-05:00) Eastern Time (US & Canada)", "Eastern Standard Time"},
            {"(UTC-05:00) Indiana (East)", "US Eastern Standard Time"},
            {"(UTC-04:30) Caracas", "Venezuela Standard Time"},
            {"(UTC-04:00) Asuncion", "Paraguay Standard Time"},
            {"(UTC-04:00) Atlantic Time (Canada)", "Atlantic Standard Time"},
            {"(UTC-04:00) Cuiaba", "Central Brazilian Standard Time"},
            {"(UTC-04:00) Georgetown, La Paz, Manaus, San Juan", "SA Western Standard Time"},
            {"(UTC-04:00) Santiago", "Pacific SA Standard Time"},
            {"(UTC-03:30) Newfoundland", "Newfoundland Standard Time"},
            {"(UTC-03:00) Brasilia", "E. South America Standard Time"},
            {"(UTC-03:00) Buenos Aires", "Argentina Standard Time"},
            {"(UTC-03:00) Cayenne, Fortaleza", "SA Eastern Standard Time"},
            {"(UTC-03:00) Greenland", "Greenland Standard Time"},
            {"(UTC-03:00) Montevideo", "Montevideo Standard Time"},
            {"(UTC-03:00) Salvador", "Bahia Standard Time"},
            {"(UTC-02:00) Coordinated Universal Time-02", "UTC-02"},
            {"(UTC-01:00) Azores", "Azores Standard Time"},
            {"(UTC-01:00) Cape Verde Is.", "Cape Verde Standard Time"},
            {"(UTC) Casablanca", "Morocco Standard Time"},
            {"(UTC) Coordinated Universal Time", "UTC"},
            {"(UTC) Dublin, Edinburgh, Lisbon, London", "GMT Standard Time"},
            {"(UTC) Monrovia, Reykjavik", "Greenwich Standard Time"},
            {"(UTC+01:00) Amsterdam, Berlin, Bern, Rome, Stockholm, Vienna", "W. Europe Standard Time"},
            {"(UTC+01:00) Belgrade, Bratislava, Budapest, Ljubljana, Prague", "Central Europe Standard Time"},
            {"(UTC+01:00) Brussels, Copenhagen, Madrid, Paris", "Romance Standard Time"},
            {"(UTC+01:00) Sarajevo, Skopje, Warsaw, Zagreb", "Central European Standard Time"},
            {"(UTC+01:00) West Central Africa", "W. Central Africa Standard Time"},
            {"(UTC+02:00) Amman", "Jordan Standard Time"},
            {"(UTC+02:00) Athens, Bucharest", "GTB Standard Time"},
            {"(UTC+02:00) Beirut", "Middle East Standard Time"},
            {"(UTC+02:00) Cairo", "Egypt Standard Time"},
            {"(UTC+02:00) Damascus", "Syria Standard Time"},
            {"(UTC+02:00) Harare, Pretoria", "South Africa Standard Time"},
            {"(UTC+02:00) Helsinki, Kyiv, Riga, Sofia, Tallinn, Vilnius", "FLE Standard Time"},
            {"(UTC+02:00) Istanbul", "Turkey Standard Time"},
            {"(UTC+02:00) Jerusalem", "Israel Standard Time"},
            {"(UTC+02:00) Kaliningrad, Minsk", "Kaliningrad Standard Time"},
            {"(UTC+02:00) Tripoli", "Libya Standard Time"},
            {"(UTC+02:00) Windhoek", "Namibia Standard Time"},
            {"(UTC+03:00) Baghdad", "Arabic Standard Time"},
            {"(UTC+03:00) Kuwait, Riyadh", "Arab Standard Time"},
            {"(UTC+03:00) Moscow, St. Petersburg, Volgograd", "Russian Standard Time"},
            {"(UTC+03:00) Nairobi", "E. Africa Standard Time"},
            {"(UTC+03:30) Tehran", "Iran Standard Time"},
            {"(UTC+04:00) Abu Dhabi, Muscat", "Arabian Standard Time"},
            {"(UTC+04:00) Baku", "Azerbaijan Standard Time"},
            {"(UTC+04:00) Port Louis", "Mauritius Standard Time"},
            {"(UTC+04:00) Tbilisi", "Georgian Standard Time"},
            {"(UTC+04:00) Yerevan", "Caucasus Standard Time"},
            {"(UTC+04:30) Kabul", "Afghanistan Standard Time"},
            {"(UTC+05:00) Ashgabat, Tashkent", "West Asia Standard Time"},
            {"(UTC+05:00) Ekaterinburg", "Ekaterinburg Standard Time"},
            {"(UTC+05:00) Islamabad, Karachi", "Pakistan Standard Time"},
            {"(UTC+05:30) Chennai, Kolkata, Mumbai, New Delhi", "India Standard Time"},
            {"(UTC+05:30) Sri Jayawardenepura", "Sri Lanka Standard Time"},
            {"(UTC+05:45) Kathmandu", "Nepal Standard Time"},
            {"(UTC+06:00) Astana", "Central Asia Standard Time"},
            {"(UTC+06:00) Dhaka", "Bangladesh Standard Time"},
            {"(UTC+06:00) Novosibirsk", "N. Central Asia Standard Time"},
            {"(UTC+06:30) Yangon (Rangoon)", "Myanmar Standard Time"},
            {"(UTC+07:00) Bangkok, Hanoi, Jakarta", "SE Asia Standard Time"},
            {"(UTC+07:00) Krasnoyarsk", "North Asia Standard Time"},
            {"(UTC+08:00) Beijing, Chongqing, Hong Kong, Urumqi", "China Standard Time"},
            {"(UTC+08:00) Irkutsk", "North Asia East Standard Time"},
            {"(UTC+08:00) Kuala Lumpur, Singapore", "Singapore Standard Time"},
            {"(UTC+08:00) Perth", "W. Australia Standard Time"},
            {"(UTC+08:00) Taipei", "Taipei Standard Time"},
            {"(UTC+08:00) Ulaanbaatar", "Ulaanbaatar Standard Time"},
            {"(UTC+09:00) Osaka, Sapporo, Tokyo", "Tokyo Standard Time"},
            {"(UTC+09:00) Seoul", "Korea Standard Time"},
            {"(UTC+09:00) Yakutsk", "Yakutsk Standard Time"},
            {"(UTC+09:30) Adelaide", "Cen. Australia Standard Time"},
            {"(UTC+09:30) Darwin", "AUS Central Standard Time"},
            {"(UTC+10:00) Brisbane", "E. Australia Standard Time"},
            {"(UTC+10:00) Canberra, Melbourne, Sydney", "AUS Eastern Standard Time"},
            {"(UTC+10:00) Guam, Port Moresby", "West Pacific Standard Time"},
            {"(UTC+10:00) Hobart", "Tasmania Standard Time"},
            {"(UTC+10:00) Vladivostok", "Vladivostok Standard Time"},
            {"(UTC+11:00) Magadan", "Magadan Standard Time"},
            {"(UTC+11:00) Solomon Is., New Caledonia", "Central Pacific Standard Time"},
            {"(UTC+12:00) Auckland, Wellington", "New Zealand Standard Time"},
            {"(UTC+12:00) Coordinated Universal Time+12", "UTC+12"},
            {"(UTC+12:00) Fiji", "Fiji Standard Time"},
            {"(UTC+12:00) Petropavlovsk-Kamchatsky", "Kamchatka Standard Time"},
            {"(UTC+13:00) Nuku'alofa", "Tonga Standard Time"},
            {"(UTC+13:00) Samoa", "Samoa Standard Time"}
        };

        public Form1()
        {
            InitializeComponent();
            // Không cho phép thay đổi kích thước form
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;

            // Cấu hình label để tự động xuống dòng
            lblNameApplicationSelected.AutoSize = false;
            lblNameApplicationSelected.AutoEllipsis = true;
            lblNameApplicationSelected.Width = groupBox7.Width - 10;
            lblNameApplicationSelected.Height = groupBox7.Height - 10;
            lblNameApplicationSelected.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            // Khởi tạo timer để cập nhật thông tin process
            processUpdateTimer = new Timer();
            processUpdateTimer.Interval = 1000;
            processUpdateTimer.Tick += ProcessUpdateTimer_Tick;
            processUpdateTimer.Start();

            // Thêm event handlers cho các nút
            btnTatUngDung.Click += BtnTatUngDung_Click;
            btnAnXuongTaskbar.Click += BtnAnXuongTaskbar_Click;
            btnHienLenDesktop.Click += BtnHienLenDesktop_Click;

            this.btnConfig.Click += BtnConfig_Click;
            this.ckbKhoiDongCungWindows.CheckedChanged += CkbKhoiDongCungWindows_CheckedChanged;
            this.txtNameAplication.TextChanged += TxtNameAplication_TextChanged;

            // Populate timezone combobox
            foreach (var tz in timezoneMap.Keys)
            {
                cbTimeZone.Items.Add(tz);
            }
            // Set default timezone
            cbTimeZone.SelectedItem = "(UTC+07:00) Bangkok, Hanoi, Jakarta";

            // Kiểm tra trạng thái khởi động cùng Windows
            CheckStartupStatus();

            // Khởi tạo performance counters và network monitoring
            InitializePerformanceCounters();
            InitializeNetworkMonitoring();
            GetMaxNetworkSpeed();

            // Timer để theo dõi cửa sổ đang focus
            activeWindowTimer = new Timer();
            activeWindowTimer.Interval = 100; // Cập nhật mỗi 100ms
            activeWindowTimer.Tick += ActiveWindowTimer_Tick;
            activeWindowTimer.Start();
        }

        private void GetMaxNetworkSpeed()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE PhysicalAdapter = True AND NetEnabled = True"))
                {
                    foreach (ManagementObject adapter in searcher.Get())
                    {
                        var speed = Convert.ToInt64(adapter["Speed"]);
                        if (speed > 0)
                        {
                            // Chuyển đổi từ bits/second sang Mbps
                            var speedMbps = speed / 1_000_000.0;
                            maxDownloadSpeed = Math.Max(maxDownloadSpeed, speedMbps);
                            maxUploadSpeed = Math.Max(maxUploadSpeed, speedMbps);
                        }
                    }
                }
            }
            catch
            {
                try
                {
                    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                        .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                               (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                                ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet));

                    var maxSpeed = interfaces.Max(ni => ni.Speed) / 1_000_000.0;
                    maxDownloadSpeed = maxSpeed;
                    maxUploadSpeed = maxSpeed;
                }
                catch
                {
                    maxDownloadSpeed = 0;
                    maxUploadSpeed = 0;
                }
            }
        }

        private void InitializeNetworkMonitoring()
        {
            try
            {
                networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(ni => ni.OperationalStatus == OperationalStatus.Up &&
                           (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                            ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet))
                    .ToArray();

                lastBytesReceived = new long[networkInterfaces.Length];
                lastBytesSent = new long[networkInterfaces.Length];

                for (int i = 0; i < networkInterfaces.Length; i++)
                {
                    var stats = networkInterfaces[i].GetIPv4Statistics();
                    lastBytesReceived[i] = stats.BytesReceived;
                    lastBytesSent[i] = stats.BytesSent;
                }

                lastSpeedCheck = DateTime.Now;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi tạo network monitoring: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateNetworkSpeed()
        {
            try
            {
                if (networkInterfaces == null || networkInterfaces.Length == 0)
                    return;

                double totalDownloadSpeed = 0;
                double totalUploadSpeed = 0;
                TimeSpan timeDiff = DateTime.Now - lastSpeedCheck;

                for (int i = 0; i < networkInterfaces.Length; i++)
                {
                    var stats = networkInterfaces[i].GetIPv4Statistics();

                    long bytesReceivedDiff = stats.BytesReceived - lastBytesReceived[i];
                    long bytesSentDiff = stats.BytesSent - lastBytesSent[i];

                    // Tính tốc độ theo Mbps
                    double downloadSpeed = (bytesReceivedDiff * 8.0) / (1024 * 1024 * timeDiff.TotalSeconds);
                    double uploadSpeed = (bytesSentDiff * 8.0) / (1024 * 1024 * timeDiff.TotalSeconds);

                    totalDownloadSpeed += downloadSpeed;
                    totalUploadSpeed += uploadSpeed;

                    lastBytesReceived[i] = stats.BytesReceived;
                    lastBytesSent[i] = stats.BytesSent;
                }

                lastSpeedCheck = DateTime.Now;

                // Cập nhật labels với cả tốc độ tối đa và tốc độ hiện tại
                lblSpeedDownload.Text = $"Download: {totalDownloadSpeed:F1}/{maxDownloadSpeed:F0} Mbps";
                lblSpeedUpload.Text = $"Upload: {totalUploadSpeed:F1}/{maxUploadSpeed:F0} Mbps";
            }
            catch (Exception)
            {
                lblSpeedDownload.Text = "Download: N/A";
                lblSpeedUpload.Text = "Upload: N/A";
            }
        }

        private void InitializePerformanceCounters()
        {
            try
            {
                cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                ramAvailableCounter = new PerformanceCounter("Memory", "Available MBytes");
                ramSearcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize,FreePhysicalMemory FROM Win32_OperatingSystem");

                performanceTimer = new Timer();
                performanceTimer.Interval = 1000; // Cập nhật mỗi giây
                performanceTimer.Tick += PerformanceTimer_Tick;
                performanceTimer.Start();

                // Set progress bar properties
                txtProgressBarCPU.Minimum = 0;
                txtProgressBarCPU.Maximum = 100;
                txtProgressBarRAM.Minimum = 0;
                txtProgressBarRAM.Maximum = 100;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi tạo performance counters: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Cập nhật tốc độ mạng
                UpdateNetworkSpeed();

                // CPU và RAM monitoring code...
                float cpuValue = cpuCounter.NextValue();
                txtProgressBarCPU.Value = (int)cpuValue;
                lblCPU.Text = $"CPU: {cpuValue:F1}%";

                // Set màu cho CPU progress bar
                if (cpuValue <= 60)
                {
                    txtProgressBarCPU.ProgressColor = Color.FromArgb(0, 192, 0);       // Xanh lá
                    txtProgressBarCPU.ProgressColor2 = Color.FromArgb(0, 192, 0);
                }
                else if (cpuValue <= 85)
                {
                    txtProgressBarCPU.ProgressColor = Color.FromArgb(255, 192, 0);     // Vàng
                    txtProgressBarCPU.ProgressColor2 = Color.FromArgb(255, 192, 0);
                }
                else
                {
                    txtProgressBarCPU.ProgressColor = Color.FromArgb(255, 0, 0);       // Đỏ
                    txtProgressBarCPU.ProgressColor2 = Color.FromArgb(255, 0, 0);
                }

                // Lấy thông tin RAM
                foreach (ManagementObject obj in ramSearcher.Get())
                {
                    ulong totalMemoryKB = Convert.ToUInt64(obj["TotalVisibleMemorySize"]);
                    ulong freeMemoryKB = Convert.ToUInt64(obj["FreePhysicalMemory"]);
                    ulong usedMemoryKB = totalMemoryKB - freeMemoryKB;

                    double totalMemoryGB = totalMemoryKB / (1024.0 * 1024.0);
                    double usedMemoryGB = usedMemoryKB / (1024.0 * 1024.0);
                    double ramPercentage = (usedMemoryKB * 100.0) / totalMemoryKB;

                    txtProgressBarRAM.Value = (int)ramPercentage;
                    lblRAM.Text = $"RAM: {ramPercentage:F1}% ({usedMemoryGB:F1}/{totalMemoryGB:F1} GB)";

                    // Set màu cho RAM progress bar
                    if (ramPercentage <= 60)
                    {
                        txtProgressBarRAM.ProgressColor = Color.FromArgb(0, 192, 0);   // Xanh lá
                        txtProgressBarRAM.ProgressColor2 = Color.FromArgb(0, 192, 0);
                    }
                    else if (ramPercentage <= 85)
                    {
                        txtProgressBarRAM.ProgressColor = Color.FromArgb(255, 192, 0); // Vàng
                        txtProgressBarRAM.ProgressColor2 = Color.FromArgb(255, 192, 0);
                    }
                    else
                    {
                        txtProgressBarRAM.ProgressColor = Color.FromArgb(255, 0, 0);   // Đỏ
                        txtProgressBarRAM.ProgressColor2 = Color.FromArgb(255, 0, 0);
                    }
                    break;
                }
            }
            catch (Exception ex)
            {
                performanceTimer.Stop();
                MessageBox.Show($"Lỗi khi cập nhật thông tin hiệu năng: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (performanceTimer != null)
            {
                performanceTimer.Stop();
                performanceTimer.Dispose();
            }
            if (cpuCounter != null) cpuCounter.Dispose();
            if (ramAvailableCounter != null) ramAvailableCounter.Dispose();
            if (ramSearcher != null) ramSearcher.Dispose();
            if (activeWindowTimer != null)
            {
                activeWindowTimer.Stop();
                activeWindowTimer.Dispose();
            }
            if (processUpdateTimer != null)
            {
                processUpdateTimer.Stop();
                processUpdateTimer.Dispose();
            }

            base.OnFormClosing(e);
        }

        private void CheckStartupStatus()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey))
            {
                if (key != null)
                {
                    string value = (string)key.GetValue(StartupValue);
                    ckbKhoiDongCungWindows.Checked = value != null && value == Application.ExecutablePath;
                }
            }
        }

        private void CkbKhoiDongCungWindows_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
                {
                    if (key != null)
                    {
                        if (ckbKhoiDongCungWindows.Checked)
                        {
                            key.SetValue(StartupValue, Application.ExecutablePath);
                        }
                        else
                        {
                            key.DeleteValue(StartupValue, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể cập nhật cài đặt khởi động: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ckbKhoiDongCungWindows.Checked = !ckbKhoiDongCungWindows.Checked; // Revert the checkbox state
            }
        }

        private void BtnConfig_Click(object sender, EventArgs e)
        {
            try
            {
                btnConfig.Enabled = false;

                bool optimize = this.ckbOptimization.Checked;

                // Thực hiện tất cả các tối ưu
                DisableUAC(optimize);
                DisableIEESC(optimize);
                DisableWindowsUpdate(optimize);
                ShowAllTrayIcons(optimize);
                SetSmallTaskbar(optimize);
                DisableFirewall(optimize);
                DisableSleep(optimize);
                DisableHibernation(optimize);
                DisableRecovery(optimize);
                DisableDriverSignature(optimize);
                OptimizeRDP(optimize);
                RefreshSystemUI();
                if (optimize) DeleteTempFolder();

                // Thay đổi múi giờ
                if (cbTimeZone.SelectedItem != null)
                {
                    ChangeTimeZone(cbTimeZone.SelectedItem.ToString());
                }

                MessageBox.Show(optimize ? "Đã tối ưu hóa Windows!" : "Đã khôi phục cài đặt gốc!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Có lỗi xảy ra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnConfig.Enabled = true;
            }
        }

        private void ChangeTimeZone(string selectedTimeZone)
        {
            if (timezoneMap.TryGetValue(selectedTimeZone, out string tzId))
            {
                RunCommand($"tzutil /s \"{tzId}\"");
            }
        }

        private void RunCommand(string command)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c {command}";
            psi.UseShellExecute = true;
            psi.Verb = "runas";
            psi.CreateNoWindow = true;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            using (var process = Process.Start(psi))
            {
                process.WaitForExit();
            }
        }

        private void DisableUAC(bool disable)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", true))
            {
                if (key != null)
                {
                    key.SetValue("EnableLUA", disable ? 0 : 1, RegistryValueKind.DWord);
                }
            }
        }

        private void DisableIEESC(bool disable)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Active Setup\Installed Components\{A509B1A7-37EF-4b3f-8CFC-4F3A74704073}", true))
            {
                if (key != null)
                {
                    key.SetValue("IsInstalled", disable ? 0 : 1, RegistryValueKind.DWord);
                }
            }
        }

        private void DisableWindowsUpdate(bool disable)
        {
            if (disable)
            {
                RunCommand("net stop wuauserv");
                RunCommand("sc config wuauserv start=disabled");
            }
            else
            {
                RunCommand("sc config wuauserv start=auto");
                RunCommand("net start wuauserv");
            }
        }

        private void ShowAllTrayIcons(bool show)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer", true))
            {
                if (key != null)
                {
                    key.SetValue("EnableAutoTray", show ? 0 : 1, RegistryValueKind.DWord);
                }
            }
        }

        private void SetSmallTaskbar(bool small)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
            {
                if (key != null)
                {
                    key.SetValue("TaskbarSmallIcons", small ? 1 : 0, RegistryValueKind.DWord);
                }
            }
        }

        private void DisableFirewall(bool disable)
        {
            RunCommand($"netsh advfirewall set allprofiles state {(disable ? "off" : "on")}");
        }

        private void DisableSleep(bool disable)
        {
            if (disable)
            {
                RunCommand("powercfg /change standby-timeout-ac 0");
                RunCommand("powercfg /change standby-timeout-dc 0");
            }
            else
            {
                RunCommand("powercfg /change standby-timeout-ac 30");
                RunCommand("powercfg /change standby-timeout-dc 15");
            }
        }

        private void DisableHibernation(bool disable)
        {
            RunCommand($"powercfg /h {(disable ? "off" : "on")}");
        }

        private void DisableRecovery(bool disable)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon", true))
            {
                if (key != null)
                {
                    key.SetValue("AutoRestartShell", disable ? 0 : 1, RegistryValueKind.DWord);
                }
            }
        }

        private void DisableDriverSignature(bool disable)
        {
            RunCommand($"bcdedit /set nointegritychecks {(disable ? "on" : "off")}");
        }

        private void OptimizeRDP(bool enable)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services", true))
            {
                if (key != null)
                {
                    key.SetValue("fEnableTimeZoneRedirection", enable ? 1 : 0, RegistryValueKind.DWord);
                    key.SetValue("MaxIdleTime", enable ? 0 : 1, RegistryValueKind.DWord);
                }
            }
        }

        private void DeleteTempFolder()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                Directory.Delete(tempPath, true);
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception)
            {

            }
        }
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam,
            uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

        private const uint HWND_BROADCAST = 0xffff;
        private const uint WM_SETTINGCHANGE = 0x001A;
        private const uint SMTO_ABORTIFHUNG = 0x0002;
        private void RefreshSystemUI()
        {
            IntPtr result;
            SendMessageTimeout((IntPtr)HWND_BROADCAST, WM_SETTINGCHANGE, IntPtr.Zero,
                IntPtr.Zero, SMTO_ABORTIFHUNG, 100, out result);
        }
        private void ProcessUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(currentProcessName))
            {
                UpdateProcessInfo(currentProcessName);
            }
        }

        private void UpdateProcessInfo(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                lblApplicationRunning.Text = $"Running: {processes.Length}";

                int maxNumber = 0;
                foreach (Process process in processes)
                {
                    try
                    {
                        string title = process.MainWindowTitle;
                        if (!string.IsNullOrEmpty(title))
                        {
                            // Tìm các số nguyên trong tiêu đề bằng Regex
                            MatchCollection matches = Regex.Matches(title, @"\d+");
                            foreach (Match match in matches)
                            {
                                int num = int.Parse(match.Value);
                                maxNumber = Math.Max(maxNumber, num);
                            }
                        }
                    }
                    catch { }
                }

                lblMaxID.Text = $"Max ID: {maxNumber}";
            }
            catch
            {
                lblApplicationRunning.Text = "Running: Error";
                lblMaxID.Text = "Max ID: Error";
            }
        }

        private void TxtNameAplication_TextChanged(object sender, EventArgs e)
        {
            string processName = txtNameAplication.Text.Trim();

            // Nếu textbox trống
            if (string.IsNullOrEmpty(processName))
            {
                currentProcessName = "";
                lblApplicationRunning.Text = "Running: 0";
                lblMaxID.Text = "Max ID: 0";
                return;
            }

            // Nếu người dùng nhập cả .exe, bỏ nó đi
            if (processName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                processName = processName.Substring(0, processName.Length - 4);
            }

            currentProcessName = processName;
            UpdateProcessInfo(processName);
        }

        private void ActiveWindowTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                IntPtr handle = GetForegroundWindow();
                if (handle != IntPtr.Zero)
                {
                    StringBuilder windowTitle = new StringBuilder(256);
                    GetWindowText(handle, windowTitle, 256);

                    string windowName = windowTitle.ToString().Trim();

                    // Đổi font để hỗ trợ Unicode
                    lblNameApplicationSelected.Font = new Font("Segoe UI", 9);

                    lblNameApplicationSelected.Text = !string.IsNullOrEmpty(windowName) ? windowName : "Unknown";
                }
            }
            catch
            {
                lblNameApplicationSelected.Text = "Unknown";
            }
        }

        private void BtnTatUngDung_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentProcessName)) return;

            try
            {
                Process[] processes = Process.GetProcessesByName(currentProcessName);
                foreach (Process process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch { }
                }
            }
            catch { }
        }
        // Dùng API của Windows để thao tác với cửa sổ
        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);
        private void BtnAnXuongTaskbar_Click(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(currentProcessName);

            foreach (var process in processes)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero) // Kiểm tra xem tiến trình có cửa sổ chính không
                    {
                        // Ẩn cửa sổ chính của tiến trình xuống thanh taskbar
                        ShowWindowAsync(process.MainWindowHandle, SW_MINIMIZE); // Ẩn cửa sổ
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể ẩn cửa sổ: " + ex.Message);
                }
            }
        }

        private void BtnHienLenDesktop_Click(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(currentProcessName);

            foreach (var process in processes)
            {
                try
                {
                    // Tìm các cửa sổ thuộc tiến trình và phục hồi chúng
                    ShowWindowAsync(process.MainWindowHandle, SW_RESTORE); // Khôi phục cửa sổ
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể khôi phục cửa sổ: " + ex.Message);
                }
            }
        }
        private void RestartProcesses(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    try
                    {
                        string exePath = process.MainModule?.FileName;
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            process.Kill();
                            Process.Start(exePath);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
        private bool isAutoHideEnabled = false;
        private Timer autoHideTimer;
        private void btnTuDongAnTaskbar_Click(object sender, EventArgs e)
        {
            isAutoHideEnabled = !isAutoHideEnabled;
            if (isAutoHideEnabled)
            {
                btnTuDongAnTaskbar.Text = "tự động ẩn[on]";
                btnTuDongAnTaskbar.FillColor = Color.FromArgb(0, 192, 0);
            }
            else
            {
                btnTuDongAnTaskbar.Text = "tự động ẩn[off]";
                btnTuDongAnTaskbar.FillColor = Color.FromArgb(94, 148, 255);
            }

            if (isAutoHideEnabled)
            {
                if (autoHideTimer == null)
                {
                    autoHideTimer = new Timer();
                    autoHideTimer.Interval = 5000; // 5 giây
                    autoHideTimer.Tick += AutoHideTimer_Tick;
                }
                autoHideTimer.Start();
            }
            else
            {
                autoHideTimer?.Stop();
            }
        }
        private void AutoHideTimer_Tick(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName(currentProcessName);

            foreach (var process in processes)
            {
                try
                {
                    if (process.MainWindowHandle != IntPtr.Zero) // Kiểm tra xem tiến trình có cửa sổ chính không
                    {
                        // Ẩn cửa sổ chính của tiến trình xuống thanh taskbar
                        ShowWindowAsync(process.MainWindowHandle, SW_MINIMIZE); // Ẩn cửa sổ
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể ẩn cửa sổ: " + ex.Message);
                }
            }
        }
        private void RestartComputer()
        {
            try
            {
                // Sử dụng Process để chạy lệnh "shutdown -r -t 0"
                Process.Start("shutdown", "/r /f /t 0"); // -r: restart, -f: force close apps, -t 0: không delay
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể khởi động lại máy: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnRestart_Click(object sender, EventArgs e)
        {
            RestartComputer();
        }

        bool isAutoCleanEnabled = false;
        int countdown = 0;

        private void btnCleanRam_Click(object sender, EventArgs e)
        {
            if (!isAutoCleanEnabled)
            {
                if (!int.TryParse(txtThoiGian.Text, out int seconds) || seconds <= 0)
                {
                    MessageBox.Show("Vui lòng nhập số giây hợp lệ (> 0).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                countdown = seconds;
                timerCleanRam.Interval = 1000; // tick mỗi giây
                timerCleanRam.Start();

                isAutoCleanEnabled = true;
                btnCleanRam.Text = "clean ram[on]";
                btnCleanRam.FillColor = Color.FromArgb(0, 192, 0); // Xanh lá
                lblTrangThai.Text = $"clean ram sau: {countdown} giây";
            }
            else
            {
                timerCleanRam.Stop();
                isAutoCleanEnabled = false;
                btnCleanRam.Text = "clean ram[off]";
                btnCleanRam.FillColor = Color.FromArgb(94, 148, 255);
                lblTrangThai.Text = "Đã tắt tự động clean RAM";
            }
        }
        private void timerCleanRam_Tick(object sender, EventArgs e)
        {
            if (countdown > 0)
            {
                countdown--;
                lblTrangThai.Text = $"clean RAM sau: {countdown} giây";
            }
            else
            {
                CleanRAM();

                if (int.TryParse(txtThoiGian.Text, out int seconds) && seconds > 0)
                {
                    countdown = seconds;
                    lblTrangThai.Text = $"clean RAM sau: {countdown} giây";
                }
                else
                {
                    timerCleanRam.Stop();
                    lblTrangThai.Text = "Lỗi nhập thời gian, dừng clean RAM.";
                }
            }
        }

        private void CleanRAM()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    // Bỏ qua tiến trình hệ thống hoặc không có MainModule
                    if (string.IsNullOrWhiteSpace(p.ProcessName)) continue;
                    if (p.SessionId == 0) continue; // Session 0 thường là tiến trình hệ thống
                    if (p.Id == 0 || p.Id == 4) continue; // System Idle Process hoặc System

                    EmptyWorkingSet(p.Handle);
                }
                catch
                {
                    // Không đủ quyền hoặc lỗi => bỏ qua
                    continue;
                }
            }
        }

        // Hàm quét và đóng cửa sổ lỗi
        private bool isClosingCrashWindows = false; // Biến trạng thái để kiểm soát việc đóng cửa sổ lỗi
        // Hàm quét và đóng cửa sổ lỗi
        private async Task CloseCrashWindowsAsync()
        {
            while (isClosingCrashWindows)
            {
                List<int> suspectedPids = new List<int>();

                EnumWindows((hWnd, lParam) =>
                {
                    const int maxChars = 256;
                    StringBuilder className = new StringBuilder(maxChars);
                    StringBuilder windowText = new StringBuilder(maxChars);

                    GetClassName(hWnd, className, maxChars);
                    GetWindowText(hWnd, windowText, maxChars);

                    string title = windowText.ToString().ToLowerInvariant();

                    if (
                        title.Contains("exception") ||
                        title.Contains("has stopped working") ||
                        title.Contains("crash") ||
                        title.Contains("error") ||
                        title.Contains("lỗi") ||
                        title.Contains("not responding") ||
                        title.Contains("Oops!") ||
                        title.Contains("oops")
                    )
                    {
                        // Gửi lệnh đóng cửa sổ lỗi
                        PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                        Debug.WriteLine($"[Đóng cửa sổ lỗi] Title: {title}");

                        // Lấy PID của process
                        GetWindowThreadProcessId(hWnd, out uint pid);

                        if (!suspectedPids.Contains((int)pid))
                            suspectedPids.Add((int)pid);
                    }

                    return true;
                }, IntPtr.Zero);

                // Chờ 2 giây để ứng dụng có thể tự đóng
                await Task.Delay(2000);

                // Kiểm tra lại từng process, nếu vẫn tồn tại thì kill
                foreach (int pid in suspectedPids)
                {
                    try
                    {
                        Process p = Process.GetProcessById(pid);
                        if (!p.HasExited)
                        {
                            // Kiểm tra xem cửa sổ lỗi có còn tồn tại không
                            bool stillExists = false;

                            EnumWindows((hWnd, lParam) =>
                            {
                                GetWindowThreadProcessId(hWnd, out uint windowPid);
                                if ((int)windowPid == pid)
                                {
                                    StringBuilder windowText = new StringBuilder(256);
                                    GetWindowText(hWnd, windowText, 256);
                                    string title = windowText.ToString().ToLowerInvariant();

                                    if (
                                        title.Contains("exception") ||
                                        title.Contains("has stopped working") ||
                                        title.Contains("crash") ||
                                        title.Contains("error") ||
                                        title.Contains("lỗi") ||
                                        title.Contains("not responding") ||
                                        title.Contains("Oops!") ||
                                        title.Contains("oops")
                                    )
                                    {
                                        stillExists = true;
                                        return false; // dừng vòng lặp sớm
                                    }
                                }
                                return true;
                            }, IntPtr.Zero);

                            if (stillExists)
                            {
                                p.Kill();
                                Debug.WriteLine($"[Đã diệt process lỗi] PID: {pid}, Name: {p.ProcessName}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[Lỗi khi diệt process] PID: {pid}, {ex.Message}");
                    }
                }

                await Task.Delay(1000);
            }      
        }
        private void BtnXoaCrash_Click(object sender, EventArgs e)
        {
            // Kiểm tra trạng thái của việc đóng cửa sổ lỗi
            if (isClosingCrashWindows)
            {
                // Dừng việc kiểm tra
                isClosingCrashWindows = false;
                BtnXoaCrash.Text = "xóa crash[off]"; // Cập nhật nút để bật lại
                BtnXoaCrash.FillColor = Color.FromArgb(94, 148, 255);
            }
            else
            {
                // Bắt đầu việc kiểm tra và đóng cửa sổ lỗi
                isClosingCrashWindows = true;
                Task.Run(() => CloseCrashWindowsAsync()); // Chạy kiểm tra trên một task nền
                BtnXoaCrash.Text = "xóa crash[on]"; // Cập nhật nút khi bật
                BtnXoaCrash.FillColor = Color.FromArgb(0, 192, 0);
            }
        }
        bool isAutoCleanEnabled2 = false;
        int countdown2 = 0;
        private void btnGiamCPU_Click(object sender, EventArgs e)
        {
            if (!isAutoCleanEnabled2)
            {
                if (!int.TryParse(txtThoiGian.Text, out int seconds) || seconds <= 0)
                {
                    MessageBox.Show("Vui lòng nhập số giây hợp lệ (> 0).", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                countdown2 = seconds;
                timerGiamCPU.Interval = 1000; // tick mỗi giây
                timerGiamCPU.Start();

                isAutoCleanEnabled2 = true;
                btnGiamCPU.Text = "Giảm CPU[on]";
                btnGiamCPU.FillColor = Color.FromArgb(0, 192, 0); // Xanh lá
                lblTrangThai2.Text = $"giảm CPU sau: {countdown2} giây";
            }
            else
            {
                timerGiamCPU.Stop();
                isAutoCleanEnabled2 = false;
                btnGiamCPU.Text = "Giảm CPU[off]";
                btnGiamCPU.FillColor = Color.FromArgb(94, 148, 255);
                lblTrangThai2.Text = "Đã tắt tự động giảm CPU";
            }
        }

        private void timerGiamCPU_Tick(object sender, EventArgs e)
        {
            if (countdown2 > 0)
            {
                countdown2--;
                lblTrangThai2.Text = $"giảm CPU sau: {countdown2} giây";
            }
            else
            {
                GiamCPU();

                if (int.TryParse(txtThoiGian.Text, out int seconds) && seconds > 0)
                {
                    countdown2 = seconds;
                    lblTrangThai2.Text = $"giảm CPU sau: {countdown2} giây";
                }
                else
                {
                    timerGiamCPU.Stop();
                    lblTrangThai2.Text = "Lỗi nhập thời gian, dừng giảm CPU.";
                }
            }
        }
        public static void GiamCPU()
        {
            foreach (Process p in Process.GetProcesses())
            {
                try
                {
                    // Bỏ qua tiến trình hệ thống (Session 0 hoặc PID đặc biệt)
                    if (p.SessionId == 0 || p.Id <= 4)
                        continue;

                    // Kiểm tra quyền truy cập (gây exception nếu bị hạn chế)
                    var _ = p.MainModule;

                    // Chỉ giảm nếu đang ở mức cao hơn
                    if (p.PriorityClass == ProcessPriorityClass.Normal ||
                        p.PriorityClass == ProcessPriorityClass.High ||
                        p.PriorityClass == ProcessPriorityClass.RealTime)
                    {
                        p.PriorityClass = ProcessPriorityClass.BelowNormal;
                    }
                }
                catch
                {
                    // Bỏ qua tiến trình không thể truy cập
                }
            }   
        } 
    }
}

