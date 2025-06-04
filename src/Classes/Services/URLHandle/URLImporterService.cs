using System.Net.Http.Headers;
using static MousyHub.Models.Services.URLHandle.URLParser;
using Newtonsoft.Json;
using System.Text;
using MousyHub.Models;
using MousyHub.Models.Misc;
using MousyHub.Models.Services;
using System.Text.Json;
using Newtonsoft.Json.Linq;


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
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
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
        private async Task<CharCard> DownloadChubCharacterAsync(string fullPath)
        {
            const string USER_AGENT = "YourAppName/1.0 (your-contact-info)";

            var parts = fullPath.Split('/');
            if (parts.Length < 2) throw new ArgumentException("Invalid character path format");

            string creatorName = parts[0];
            string projectName = parts[1];

            // 1. Получение метаданных персонажа
            var metadataUrl = $"https://api.chub.ai/api/characters/{creatorName}/{projectName}";
            using var metadataRequest = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
            metadataRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            metadataRequest.Headers.UserAgent.ParseAdd(USER_AGENT);

            using var metadataResponse = await _httpClient.SendAsync(metadataRequest);

            if (!metadataResponse.IsSuccessStatusCode)
            {
                var errorText = await metadataResponse.Content.ReadAsStringAsync();
                throw new Exception($"Metadata fetch failed: {metadataResponse.StatusCode} - {errorText}");
            }

            var metadataJson = await metadataResponse.Content.ReadAsStringAsync();
            var metadata = JObject.Parse(metadataJson);
            string downloadUrl = metadata?["node"]?["max_res_url"]?.ToString();

            if (string.IsNullOrEmpty(downloadUrl))
                throw new Exception("Download URL not found in metadata");

            // 2. Скачивание изображения
            using var downloadResponse = await _httpClient.GetAsync(downloadUrl);
            if (!downloadResponse.IsSuccessStatusCode)
            {
                var errorText = await downloadResponse.Content.ReadAsStringAsync();
                throw new Exception($"Image download failed: {downloadResponse.StatusCode} - {errorText}");
            }

            // 3. Обработка изображения
            byte[] imageBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            CharacterDataReader reader = new CharacterDataReader();

            // Прямая передача байтов вместо base64 конвертации
            string jsoncard = reader.ParseCharacterDataDirect(imageBytes);

            CharCard card = JsonConvert.DeserializeObject<CharCard>(jsoncard);
            card.SetAvatar(Util.CompressImage(imageBytes));

            return card;
        }


        public static async Task<List<string>> GetModelIdsAsync(string baseUrl, string apiKey = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Base URL cannot be empty", nameof(baseUrl));

            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            // Добавляем API ключ, если он предоставлен
            if (!string.IsNullOrEmpty(apiKey))
            {
                httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }

            try
            {
                // Формируем URL
                string requestUrl = $"{baseUrl.TrimEnd('/')}/v1/models";

                // Выполняем запрос
                HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException(
                        $"Failed to get models. Status: {response.StatusCode}, Error: {errorContent}");
                }

                // Читаем и парсим ответ
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument document = JsonDocument.Parse(jsonResponse);

                var modelIds = new List<string>();
                JsonElement root = document.RootElement;

                // Проверяем, что есть свойство "data" и это массив
                if (root.TryGetProperty("data", out JsonElement dataArray) &&
                    dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement model in dataArray.EnumerateArray())
                    {
                        // Извлекаем id модели
                        if (model.TryGetProperty("id", out JsonElement idElement) &&
                            idElement.ValueKind == JsonValueKind.String)
                        {
                            modelIds.Add(idElement.GetString());
                        }
                    }
                }

                return modelIds;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"HTTP request failed: {ex.Message}", ex);
            }
            catch (System.Text.Json.JsonException ex)
            {
                throw new Exception($"Failed to parse models response: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new Exception($"An unexpected error occurred: {ex.Message}", ex);
            }
        }



    }
}
