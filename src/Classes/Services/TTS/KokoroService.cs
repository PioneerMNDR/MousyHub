using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Processing;
using KokoroSharp.Utilities;
using MousyHub.Classes.Misc;
using static MousyHub.Models.Services.URLHandle.HFDownloaderService;

namespace MousyHub.Classes.Services.TTS
{
    public class KokoroService
    {
        public KokoroService(IHostEnvironment environment) 
        {
            _environment = environment;
            if (_environment.IsDevelopment())
            {
                Tokenizer.eSpeakNGPath = Path.Combine(AppContext.BaseDirectory, "espeak");
                KokoroVoiceManager.LoadVoicesFromPath(Path.Combine(AppContext.BaseDirectory, "voices"));
            }
        }
        private IHostEnvironment _environment;
        private KokoroWavSynthesizer? TTS { get; set; }
        public bool IsRun { get; private set; }

        public void TryRunModel(string modelPath)
        {
            try
            {
                if (IsRun && TTS!=null)
                {
                    return;
                }
                TTS = new KokoroWavSynthesizer(modelPath);
       
                IsRun = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed run kokoro model: " + ex.Message);
                IsRun = false;
            }
        }

        public async Task<byte[]> GetSpeak(string text, string KokoroVoice)
        {
            if (IsRun) 
            {
                text = StringHelperBuilder.RemoveAsterisks(text);
                var voice = KokoroVoiceManager.GetVoice(KokoroVoice);
                var bytes = await TTS.SynthesizeAsync(text, voice);
              
                return bytes;
            }
            return Array.Empty<byte>();       
        }

        public List<KokoroVoice> GetVoices()
        {
            return KokoroVoiceManager.Voices;
        }

    }


}
public class KokoroDownloader
{
    // Дублируем необходимые константы
    public static readonly Dictionary<KModel, string> ModelFileNames = new Dictionary<KModel, string>() {
        { KModel.float32, "kokoro.onnx" },
        { KModel.float16, "kokoro-quant.onnx" },
        { KModel.int8, "kokoro-quant-convinteger.onnx" }
    };

    private static string GetModelUrl(KModel model) =>
        $"https://github.com/taylorchu/kokoro-onnx/releases/download/v0.2.0/{ModelFileNames[model]}";

    /// <summary>
    /// Downloads the specified model to a custom directory without loading it.
    /// </summary>
    /// <param name="model">The model type to download</param>
    /// <param name="customDirectory">The directory where the model should be saved</param>
    /// <param name="progress">An IProgress implementation to report download progress as percentage (0-100)</param>
    /// <returns>The full path to the downloaded model file</returns>
    public static async Task<string> DownloadModelToDirectoryAsync(KModel model, string customDirectory, IProgress<int> progress = null)
    {
        // Ensure the directory exists
        if (!Directory.Exists(customDirectory))
        {
            Directory.CreateDirectory(customDirectory);
        }

        // Define the target file path
        string fileName = ModelFileNames[model];
        string filePath = Path.Combine(customDirectory, fileName);

        // Set the filename in the progress tracker if it's our DownloadProgress type
        if (progress is DownloadProgress downloadProgress)
        {
            downloadProgress.FileName = fileName;
        }

        // Check if the file already exists in the custom directory
        if (File.Exists(filePath))
        {
            return filePath;
        }

        // Download the model
        using var client = new HttpClient();
        using var response = await client.GetAsync(GetModelUrl(model), HttpCompletionOption.ResponseHeadersRead);
        using var responseStream = await response.Content.ReadAsStreamAsync();

        var fileSize = response.Content.Headers.ContentLength ?? 400_000_000L;
        var (buffer, bytesRead, totalRead) = (new byte[8192], 0, 0L);

        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        while ((bytesRead = await responseStream.ReadAsync(buffer)) > 0)
        {
            totalRead += bytesRead;
            fileStream.Write(buffer, 0, bytesRead);

            // Report progress as percentage (0-100)
            if (progress != null)
            {
                int percentComplete = (int)((totalRead * 100) / fileSize);
                progress.Report(percentComplete);
            }
        }

        return filePath;
    }

    /// <summary>
    /// Synchronously downloads the specified model to a custom directory without loading it.
    /// </summary>
    /// <param name="model">The model type to download</param>
    /// <param name="customDirectory">The directory where the model should be saved</param>
    /// <param name="progress">An IProgress implementation to report download progress as percentage (0-100)</param>
    /// <returns>The full path to the downloaded model file</returns>
    public static string DownloadModelToDirectory(KModel model, string customDirectory, IProgress<int> progress = null)
    {
        return Task.Run(() => DownloadModelToDirectoryAsync(model, customDirectory, progress)).Result;
    }
}
