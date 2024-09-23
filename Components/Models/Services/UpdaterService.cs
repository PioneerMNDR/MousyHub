using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using MousyHub.Components.Models.Misc;
using NRedisStack.Search;
using Octokit;
using System.Diagnostics;
using System.Globalization;

namespace MousyHub.Components.Models.Services
{
    public class UpdaterService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private static readonly string owner = "PioneerMNDR";
        private static readonly string repoName = "MousyHub";
        public string lastVersion { get; private set; } = "?";
        private string appPath {  get; set; }   
        public bool ReadyToUpdate { get; set; } = false;

        public UpdaterService(IHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
            _hostEnvironment = hostEnvironment;
             appPath = AppDomain.CurrentDomain.BaseDirectory;
             string v = Directory.GetParent(appPath).FullName;
             appPath = Directory.GetParent(v).FullName;

          
        }
        private bool IsApplicationDevelopmentVersion()
        {
            return _hostEnvironment.IsDevelopment();
        }
        private string FindFile(string fileName)
        {
            // Получаем путь к текущей директории приложения
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Определяем, является ли версия приложения "Development"
            bool isDevelopment = IsApplicationDevelopmentVersion();

            // Определяем количество уровней, на которые нужно подняться
            int levelsToAscend = isDevelopment ? 5 : 2;

            // Поднимаемся на указанное количество уровней вверх
            string rootDirectory = currentDirectory;
            for (int i = 0; i < levelsToAscend; i++)
            {
                rootDirectory = Directory.GetParent(rootDirectory).FullName;
            }

            return Util.FindFileRecursive(rootDirectory, fileName);
        }

        public void LaunchUpdater()
        {
            string updaterFileName = "MousyUpdater.exe";
            string updaterPath = FindFile(updaterFileName);

            if (!string.IsNullOrEmpty(updaterPath))
            {   
         
                using (var scope = _serviceProvider.CreateScope())
                {
                    var settings = scope.ServiceProvider.GetRequiredService<UploaderService>();
                    settings.SavePresets();
                }
                _hostApplicationLifetime.StopApplication();
                // Запускаем Updater.exe с аргументом в виде пути к MainApp
                ProcessStartInfo startInfo = new ProcessStartInfo(updaterPath);
                startInfo.ArgumentList.Add($"{appPath}");
                startInfo.ArgumentList.Add(lastVersion);
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                try
                {
                    Process.Start(startInfo);
                    Console.WriteLine("Updater запущен.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при запуске Updater: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("Updater не найден.");
            }
        }
       

        public async Task CheckUpdate()
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("MousyHub"));
                var releases = await client.Repository.Release.GetAll(owner, repoName);
                lastVersion = releases.Where(x => float.Parse(x.TagName, CultureInfo.InvariantCulture.NumberFormat) >= float.Parse(AppVersion._version, CultureInfo.InvariantCulture.NumberFormat)).FirstOrDefault().TagName;
                if (float.Parse(lastVersion, CultureInfo.InvariantCulture.NumberFormat)< float.Parse(AppVersion._version, CultureInfo.InvariantCulture.NumberFormat))
                {
                    ReadyToUpdate = true;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("CheckUpdate Error");

            }

        }


    }
}
