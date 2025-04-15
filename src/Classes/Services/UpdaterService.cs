
using MousyHub.Models.Misc;
using Octokit;
using System.Diagnostics;
using System.Globalization;

namespace MousyHub.Models.Services
{
    public class UpdaterService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private readonly IHostApplicationLifetime _hostApplicationLifetime;
        private readonly IServiceProvider _serviceProvider;
        private static readonly string owner = "PioneerMNDR";
        private static readonly string repoName = "MousyHub";
        public string lastVersion { get; private set; } = "?";
        private string appPath { get; set; }
        public bool ReadyToUpdate { get; set; } = false;
        public bool isUpdating { get; set; } = false;
        public UpdaterService(IHostEnvironment hostEnvironment, IHostApplicationLifetime hostApplicationLifetime, IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;
            _hostEnvironment = hostEnvironment;
            appPath = AppDomain.CurrentDomain.BaseDirectory;

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

        public void LaunchUpdater(string updateSource = null)
        {
            isUpdating = true;
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

                // Запускаем Updater.exe с аргументами
                ProcessStartInfo startInfo = new ProcessStartInfo(updaterPath);
                startInfo.ArgumentList.Add($"{appPath}");

                // Определяем, что передать в качестве второго аргумента
                if (!string.IsNullOrEmpty(updateSource) && updateSource.EndsWith(".zip") && File.Exists(updateSource))
                {
                    // Если передан путь к zip-файлу, используем его
                    startInfo.ArgumentList.Add(updateSource);
                }
                else
                {
                    // Иначе используем номер версии
                    startInfo.ArgumentList.Add(updateSource ?? lastVersion);
                }

                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = false;

                try
                {
                    Process.Start(startInfo);
                    Console.WriteLine("Updater запущен.");
                }
                catch (Exception ex)
                {
                    isUpdating = false;
                    Console.WriteLine($"Ошибка при запуске Updater: {ex.Message}");
                }
            }
            else
            {
                isUpdating = false;
                Console.WriteLine("Updater не найден.");
            }
        }


        public async Task CheckUpdate()
        {
            try
            {
                if (IsApplicationDevelopmentVersion())
                {
                    return;
                }
                var client = new GitHubClient(new ProductHeaderValue("MousyHub"));
                var miscellaneousRateLimit = await client.RateLimit.GetRateLimits();

                //  The "core" object provides your rate limit status except for the Search API.
                var coreRateLimit = miscellaneousRateLimit.Resources.Core;

                var howManyCoreRequestsCanIMakePerHour = coreRateLimit.Limit;
                var howManyCoreRequestsDoIHaveLeft = coreRateLimit.Remaining;
                var whenDoesTheCoreLimitReset = coreRateLimit.Reset; // UTC time

                // the "search" object provides your rate limit status for the Search API.
                var searchRateLimit = miscellaneousRateLimit.Resources.Search;

                var howManySearchRequestsCanIMakePerMinute = searchRateLimit.Limit;
                var howManySearchRequestsDoIHaveLeft = searchRateLimit.Remaining;
                var whenDoesTheSearchLimitReset = searchRateLimit.Reset; // UTC time

                var releases = await client.Repository.Release.GetAll(owner, repoName);

                var latestRelease = releases.Where(x => float.Parse(x.TagName, CultureInfo.InvariantCulture.NumberFormat) >= float.Parse(AppVersion._version, CultureInfo.InvariantCulture.NumberFormat)).FirstOrDefault();
                lastVersion = latestRelease != null ? latestRelease.TagName : AppVersion._version;
                if (float.Parse(lastVersion, CultureInfo.InvariantCulture.NumberFormat) > float.Parse(AppVersion._version, CultureInfo.InvariantCulture.NumberFormat))
                {
                    ReadyToUpdate = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("CheckUpdate Error: " +ex.Message);

            }

        }


    }
}
