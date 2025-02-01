using Figgle;
using ICSharpCode.SharpZipLib.Zip;
using Octokit;

using Spectre.Console;
using System.IO.Compression;
using System.Net.Http.Headers;
using ZipFile = ICSharpCode.SharpZipLib.Zip.ZipFile;

namespace MousyUpdater;

class Program
{
    private static readonly string owner = "PioneerMNDR";
    private static readonly string repoName = "MousyHub";
    private static readonly string zipName = "MousyHub.zip";
    private static string newVersion { get; set; }
    private static string mainAppPath { get; set; }
    private static string tempfilePath { get; set; }
    static async Task Main(string[] args)
    {
        //////Временно
        //args = new string[2];
        //args[0] = "C:\\Users\\user\\Desktop\\LLMRP-master\\llmrp\\LLMRP\\bin\\Debug";
        //args[1] = "0.2";
        tempfilePath = $"{repoName}.zip";
        if (args.Length > 1)
        {
      
            mainAppPath = args[0];
            newVersion = args[1];
            Console.WriteLine(FiggleFonts.Slant.Render("Time to Update"));
            Console.WriteLine($"Path to MainApp: {mainAppPath}");
            Console.WriteLine($"New version: {newVersion}");
            if (!Directory.Exists(mainAppPath))
            {
                Console.WriteLine($"The directory {mainAppPath} does not exist.");
                return;
            }
            await DownloadAsset(newVersion);
            UpdateApp();
        }
        else
        {
            Console.WriteLine("Path to MainApp is not specified.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }



    static async Task DownloadAsset(string newVersion)
    {


        var client = new GitHubClient(new Octokit.ProductHeaderValue("MousyHub"));
        var release = await client.Repository.Release.GetAll(owner, repoName);
        var releaseByTag = release.Where(x => x.TagName == newVersion).FirstOrDefault();

        if (releaseByTag != null)
        {
            Console.WriteLine($"Release found: {releaseByTag.Name}, downloading...");
            var asset = releaseByTag.Assets.Where(x => x.Name == zipName).FirstOrDefault();
            if (asset != null)
            {
                Console.WriteLine($"Found asset: {asset.Name}");
                tempfilePath = $"{repoName}.zip";
                var numberOfChunks = 4; // Количество параллельных потоков

                using (var httpClient = new HttpClient())
                {
                    // Получаем размер файла
                    var response = await httpClient.GetAsync(asset.BrowserDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength ?? -1L;

                    if (totalBytes == -1)
                    {
                        Console.WriteLine("Failed to get file size.");
                        return;
                    }
                    // Проверяем, чтобы количество частей не превышало количество байтов, если файл слишком маленький
                    if (totalBytes < numberOfChunks)
                    {
                        numberOfChunks = 1;
                    }
                    // Размер каждого сегмента
                    var chunkSize = totalBytes / numberOfChunks;

                    // Создаем массив задач для параллельной загрузки
                    var tasks = new Task[numberOfChunks];

                    // Используем временные файлы для хранения каждой части
                    var tempFiles = new string[numberOfChunks];

                    // Прогресс-бар
                    await AnsiConsole.Progress()
                        .StartAsync(async ctx =>
                        {
                            var task = ctx.AddTask("[green]Loading archive...[/]", maxValue: totalBytes);

                            for (int i = 0; i < numberOfChunks; i++)
                            {
                                int localIndex = i; // Захватываем текущее значение i
                                var start = localIndex * chunkSize;
                                var end = (localIndex == numberOfChunks - 1) ? totalBytes - 1 : start + chunkSize - 1;

                                // Временный файл для каждой части
                                tempFiles[localIndex] = Path.GetTempFileName();

                                tasks[localIndex] = Task.Run(async () =>
                                {
                                    var rangeHeader = new RangeHeaderValue(start, end);
                                    var request = new HttpRequestMessage(HttpMethod.Get, asset.BrowserDownloadUrl);
                                    request.Headers.Range = rangeHeader;

                                    // Загружаем сегмент
                                    using var chunkResponse = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                                    chunkResponse.EnsureSuccessStatusCode();

                                    using var contentStream = await chunkResponse.Content.ReadAsStreamAsync();
                                    using var fileStream = new FileStream(tempFiles[localIndex], System.IO.FileMode.Create, FileAccess.Write, FileShare.None);

                                    var buffer = new byte[8192]; // 8 KB буфер
                                    int bytesRead;
                                    long totalRead = 0;

                                    while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                    {
                                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                                        totalRead += bytesRead;
                                        task.Increment(bytesRead);
                                    }
                                });
                            }

                            // Ожидаем завершения загрузки всех частей
                            await Task.WhenAll(tasks);

                            // Собираем все части в один файл
                            using (var finalFile = new FileStream(tempfilePath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                foreach (var tempFile in tempFiles)
                                {
                                    using (var tempFileStream = new FileStream(tempFile, System.IO.FileMode.Open, FileAccess.Read))
                                    {
                                        await tempFileStream.CopyToAsync(finalFile);
                                    }

                                    // Удаляем временный файл после того, как его данные были перенесены
                                    File.Delete(tempFile);
                                }
                            }

                            Console.WriteLine($"Archive downloaded successfully: {tempfilePath}");
                        });
                }

            }
            else
            {
                Console.WriteLine("Release with the specified tag not found.");
            }
        }
    }
    static void UpdateApp()
    {
     

        // Ensure the zip file exists
        if (!File.Exists(tempfilePath))
        {
            Console.WriteLine($"The file {tempfilePath} does not exist.");
            return;
        }

        // Open the ZIP archive
        using (FileStream fs = File.OpenRead(tempfilePath))
        using (ICSharpCode.SharpZipLib.Zip.ZipFile zipFile = new ZipFile(fs))
        {
            foreach (ZipEntry entry in zipFile)
            {
                if (!entry.IsFile) continue; // Skip directories

                string fullZipToPath = Path.Combine(mainAppPath, entry.Name);
                string directoryName = Path.GetDirectoryName(fullZipToPath);

                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }

                // Extract file
                using (FileStream fileStream = File.Create(fullZipToPath))
                using (Stream zipStream = zipFile.GetInputStream(entry))
                {
                    zipStream.CopyTo(fileStream);
                }
            }
        }

        Console.WriteLine("Files successfully extracted and replaced.");
    }

}

