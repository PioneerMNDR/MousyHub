using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
namespace MousyHub.Models.Services
{
    public class DiagnosticsService
    {
        public string Available_RAM { get; private set; } = "No data";
        public string CPU_Usage { get; private set; } = "No data";
        public bool isRun { get; private set; } = false;

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
