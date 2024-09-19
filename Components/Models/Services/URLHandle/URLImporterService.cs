using System.Net.Http.Headers;
using static LLMRP.Components.Models.Services.URLHandle.URLParser;
using Newtonsoft.Json;
using System.Text;
using LLMRP.Components.Models.Misc;


namespace LLMRP.Components.Models.Services.URLHandle
{
    public partial class URLImporterService
    {
        private readonly UploaderService _uploaderService;
        private  readonly HttpClient _httpClient = new HttpClient();

        public URLImporterService(UploaderService uploaderService)
        {
            _uploaderService = uploaderService;
        }

        public async Task ImportURL(string url)
        {
            bool isChub = url.Contains("chub.ai") || url.Contains("characterhub.org");

            if (isChub)
            {
                URLParsedResult? result = ParseChubUrl(url);
                if (result != null)
                {
                    if (result.Type == "character")
                    {
                        Console.WriteLine("Downloading chub character:" + result.Id);
                        await DownloadChubCharacterAsync(result.Id);
                    }
                   
                }
            }
        }
        private async Task<CharCard> DownloadChubCharacterAsync(string fullPath, string format = "tavern", string version = "main")
        {
            var request = new
            {
                format = format,
                fullPath = fullPath,
                version = version
            };
            var json = JsonConvert.SerializeObject(request);  
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.chub.ai/api/characters/download")
            {
                Content = content
            };
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            var response = await _httpClient.SendAsync(requestMessage);
  
            if (response.IsSuccessStatusCode)
            {
                CharacterDataReader reader = new CharacterDataReader();

                byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();

                string base64String = Convert.ToBase64String(imageBytes);
                string jsoncard = await reader.ReadCharacterDataAsync(base64String);
                CharCard card = JsonConvert.DeserializeObject<CharCard>(jsoncard);
                card.avatarPNG = Util.CompressImage(imageBytes);
                card.isNew = true;
                card.date = DateTime.Now;
                Saver.SaveToJson(card, card.system_name);
                _uploaderService.ReloadCards();
                return card;
            }
            else
            {
                // Обработка ошибки
                Console.WriteLine($"Ошибка: {response.StatusCode}");
                return null;
            }
        }






    }
}
