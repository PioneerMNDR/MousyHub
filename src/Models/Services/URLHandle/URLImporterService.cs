using System.Net.Http.Headers;
using static MousyHub.Models.Services.URLHandle.URLParser;
using Newtonsoft.Json;
using System.Text;
using MousyHub.Models;
using MousyHub.Models.Misc;
using MousyHub.Models.Services;


namespace MousyHub.Models.Services.URLHandle
{
    public partial class URLImporterService
    {
        private readonly UploaderService _uploaderService;
        private readonly ProviderService _providerService;
        private readonly HttpClient _httpClient = new HttpClient();

        public URLImporterService(UploaderService uploaderService, ProviderService providerService)
        {
            _uploaderService = uploaderService;
            _providerService = providerService;
        }

        public async Task ImportURL(string url, bool generateShortDes)
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
                        CharCard card =  await DownloadChubCharacterAsync(result.Id);
                        card.isNew = true;
                        card.date = DateTime.Now;
                        if (generateShortDes && _providerService.Status)
                        {
                            var res = await _providerService.Wizard.WizardRequest(card.data.description, Wizard.WizardFunction.CharDescription);
                            if (res.IsSuccess) card.data.short_description = res.Content;
                        }                   
                        Saver.SaveToJson(card, card.system_name);
                        _uploaderService.ReloadCards();
                    }

                }
            }
        }
        private async Task<CharCard> DownloadChubCharacterAsync(string fullPath, string format = "tavern", string version = "main")
        {
            var request = new
            {
                format,
                fullPath,
                version
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
