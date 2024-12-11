
using HuggingfaceHub;
namespace MousyHub.Models.Services
{
    public class HFDownloaderService
    {
        public HFDownloaderService() { }

        public async Task DownloadModel()
        {
            var ss = await HFDownloader.GetModelInfoAsync("bartowski/Meta-Llama-3.1-8B-Instruct-GGUF", filesMetadata: true);
            var info =  HFDownloader.GetHuggingfaceFileUrl("bartowski/Meta-Llama-3.1-8B-Instruct-GGUF", "Meta-Llama-3.1-8B-Instruct-Q2_K_L.gguf");
            var info2 = await HFDownloader.GetHfFileMetadata(new Uri(info));
        }

    }
}
