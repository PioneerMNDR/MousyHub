using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
namespace MousyHub.Models.Services
{
    public class DiagnosticsService
    {
        public string Available_RAM { get; private set; } = "No data";
        public string CPU_Usage { get; private set; } = "No data";

        public int LogicalProcessorCount { get; private set; }
        public bool isRun { get; private set; } = false;

        public List<VideoAdapterInfo> GPUs = new List<VideoAdapterInfo>();
        public DiagnosticsService()
        {
            _ = RunAutoUpdate();
        }

        public async Task RunAutoUpdate()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Tracking performance is available only to Windows");
                return;
            }
            if (isRun)
            {
                return;
            }
            try
            {
                                 
                GPUs = GetVideoAdapterInfo();
                GetLogicalProcessorCount();
                isRun = true;
                while (true)
                {
                    await Task.Delay(800);
                    var ramCounter = new PerformanceCounter("Memory", "Available MBytes");
                    var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    ulong totalmemory = GetTotalPhysicalMemory();
                    float availableMemory = ramCounter.NextValue() / 1024f;
                    float usedMemory = totalmemory / 1024f / 1024f / 1024f - availableMemory;
                    float cpuUsage = cpuCounter.NextValue();
                    await Task.Delay(200);
                    cpuUsage = cpuCounter.NextValue();
                    Available_RAM = $"RAM: {usedMemory:F1}/{availableMemory + usedMemory:F1} GB";
                    CPU_Usage = $"CPU: {cpuUsage:F1}%";
                }
            }
            catch (Exception ex)
            {
                isRun = false;
                Console.WriteLine($"Failed to run tracking performance: {ex.Message}");
                throw;
            }
        }


        public static List<VideoAdapterInfo> GetVideoAdapterInfo()
        {
            List<VideoAdapterInfo> adapterInfoList = new List<VideoAdapterInfo>();
            string registryKeyPath = @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryKeyPath))
            {
                if (key != null)
                {
                    foreach (string subkeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using (RegistryKey subkey = key.OpenSubKey(subkeyName))
                            {
                                // Проверяем, что подключ является подключем видеоадаптера по наличию ключа "DriverDesc"
                                if (subkey != null && subkey.GetValue("DriverDesc") != null)
                                {
                                    string adapterString = subkey.GetValue("HardwareInformation.AdapterString") as string;
                                    object memorySizeValue = subkey.GetValue("HardwareInformation.qwMemorySize");

                                    if (!string.IsNullOrEmpty(adapterString) && memorySizeValue != null)
                                    {
                                        long memorySize;
                                        if (long.TryParse(memorySizeValue.ToString(), out memorySize))
                                        {
                                            adapterInfoList.Add(new VideoAdapterInfo
                                            {
                                                Model = adapterString,
                                                VRAM_GB = Math.Round((double)memorySize / (1024 * 1024 * 1024))
                                            });
                                        }
                                    }
                                }
                            }
                        }
                        catch (SecurityException ex)
                        {

                            // Возвращаем собранные данные, даже если не удалось получить доступ ко всем подключам
                            return adapterInfoList;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при обработке подключа {subkeyName}: {ex.Message}");
                            // Логируем ошибку, но продолжаем обработку других подключей
                        }
                    }
                }
            }

            return adapterInfoList;
        }

        public class VideoAdapterInfo
        {
            public string Model { get; set; }
            public double VRAM_GB { get; set; }
        }
        void GetLogicalProcessorCount()
        {
            LogicalProcessorCount =  Environment.ProcessorCount;
        }
        ulong GetTotalPhysicalMemory()
        {
            ulong totalMemory = 0;
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_ComputerSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();

            foreach (ManagementObject result in results)
            {
                totalMemory = Convert.ToUInt64(result["TotalPhysicalMemory"]);
            }

            return totalMemory;
        }

    }
}
