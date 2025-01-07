
using HuggingfaceHub;
using Microsoft.AspNetCore.Components;
using NRedisStack.Search;
using static MousyHub.Models.Services.URLHandle.HFDownloaderService;
namespace MousyHub.Models.Services.URLHandle
{
    public class HFDownloaderService
    {
        public bool isBusy { get; private set; }

        UploaderService UploaderService { get; set; }
        public DownloadProgress downloadProgress { get; private set; } = new DownloadProgress();
        public event EventHandler DownloadTickEvent;
        public event EventHandler EndDownloadEvent;
        public HFDownloaderService(UploaderService uploader)
        {
            UploaderService = uploader;

        }


        public class DownloadProgress : IProgress<int>
        {
            public int Progress { get; private set; }
            public string FileName { get; set; }

            public void Report(int value)
            {
                Progress = value;
            }
     
        }

        public async Task DownloadModel(string id, string GGUF_FileName)
        {
            if (isBusy)
            {
                return;
            }
            isBusy = true;
            _ = Tick();
            downloadProgress.FileName = GGUF_FileName;
            var path = await HFDownloader.DownloadFileAsync(id, GGUF_FileName, progress: downloadProgress, localDir: UploaderService.ModelsPath);
            isBusy = false;
            if (EndDownloadEvent != null)
                EndDownloadEvent.Invoke(path, EventArgs.Empty);
        }

        private async Task Tick()
        {
            while (isBusy)
            {
                if (DownloadTickEvent != null)
                DownloadTickEvent.Invoke(null,EventArgs.Empty);
                await Task.Delay(1000);
            }
        }

        public async Task GetModelInfo(string id)
        {

        }

    }
}
