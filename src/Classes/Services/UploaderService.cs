using Microsoft.JSInterop;
using Newtonsoft.Json;
using MousyHub.Models;
using MousyHub.Models.Misc;
using MousyHub.Models.Model;
using MousyHub.Models.User;
using MousyHub.Models.Misc.Tutorial;
using MousyHub.Classes.Misc;
namespace MousyHub.Models.Services
{
    public class UploaderService
    {

        public SettingsService settingsService;
        private readonly IConfiguration Configuration;
        public readonly string ModelsPath;
        public readonly string EmbeddingModelsPath;
        public readonly string KokoroModelsPath;

        public UploaderService(IConfiguration configuration)
        {
            Configuration = configuration;
            ModelsPath = "/wwwroot/LocalModels/";
            EmbeddingModelsPath = configuration["EmbeddingModelsPath"] ?? "/wwwroot/LocalModels/EmbeddingModel/";
            KokoroModelsPath = configuration["KokoroModelsPath"] ?? "/wwwroot/LocalModels/KokoroModels/";
            if (!Directory.Exists(ModelsPath))
            {
                ModelsPath = Environment.CurrentDirectory + ModelsPath;
            }
            if (!Directory.Exists(EmbeddingModelsPath))
            {
                EmbeddingModelsPath = Environment.CurrentDirectory + EmbeddingModelsPath;
            }
            if (!Directory.Exists(KokoroModelsPath))
            {
                KokoroModelsPath = Environment.CurrentDirectory + KokoroModelsPath;
            }

        }

        public bool isBusy { get; private set; }
        public event EventHandler SaveInfoEvent;
        public event EventHandler ReloadCardEvent;

        public void ReloadCards()
        {
            ReloadCardEvent.Invoke(null, EventArgs.Empty);
        }
        public List<CharCard> LoadCards()
        {
            string userDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot", "Cards");
            string defaultDirectory = Path.Combine(Environment.CurrentDirectory, "wwwroot", "default", "Cards");
            List<CharCard> charCards = new List<CharCard>();

            // Сначала загружаем карты из директории по умолчанию
            LoadCardsFromDirectory(defaultDirectory, charCards);

            // Затем загружаем пользовательские карты, они могут перезаписать стандартные с тем же system_name
            LoadCardsFromDirectory(userDirectory, charCards);

            return charCards;
        }

        private void LoadCardsFromDirectory(string directory, List<CharCard> charCards)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                CharCard charCard = JsonConvert.DeserializeObject<CharCard>(json);

                // Проверяем, существует ли уже карта с таким же system_name
                int existingIndex = charCards.FindIndex(c => c.system_name == charCard.system_name);

                if (existingIndex >= 0)
                {
                    // Заменяем существующую карту новой (предполагаем, что пользовательские карты имеют приоритет)
                    charCards[existingIndex] = charCard;
                }
                else
                {
                    // Добавляем новую карту
                    charCards.Add(charCard);
                }
            }
        }
        public List<Instruct> LoadInstructs(bool Default = false)
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/InstructConfigs/";
            string defaultDirectory = Environment.CurrentDirectory + "/wwwroot/default/InstructConfigs/";

            List<Instruct> list = new List<Instruct>();

            // Load user configs first
            if (!Default && Directory.Exists(directory))
            {
                foreach (string file in Directory.GetFiles(directory, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    Instruct ints = JsonConvert.DeserializeObject<Instruct>(json);
                    list.Add(ints);
                }
            }

            // Load default configs
            string loadDir = Default ? directory : defaultDirectory;
            if (Directory.Exists(loadDir))
            {
                foreach (string file in Directory.GetFiles(loadDir, "*.json"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string json = File.ReadAllText(file);
                    Instruct defaultInst = JsonConvert.DeserializeObject<Instruct>(json);

                    // Check if this config already exists in the list by name
                    bool exists = list.Any(i => i.name == fileName);

                    if (!exists)
                    {
                        list.Add(defaultInst);
                    }
                }
            }

            // If still empty and not already looking for defaults, load defaults
            if (list.Count == 0 && !Default)
                list = LoadInstructs(Default: true);

            return list;
        }


        public List<GenerationConfig> LoadPresets(bool Default = false)
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/Presets/";
            string defaultDirectory = Environment.CurrentDirectory + "/wwwroot/default/Presets/kobold";

            List<GenerationConfig> list = new List<GenerationConfig>();

            // Load user configs first
            if (!Default && Directory.Exists(directory))
            {
                foreach (string file in Directory.GetFiles(directory, "*.json"))
                {
                    string json = File.ReadAllText(file);
                    GenerationConfig config = JsonConvert.DeserializeObject<GenerationConfig>(json);
                    config.ConfigName = Path.GetFileNameWithoutExtension(file);
                    list.Add(config);
                }
            }

            // Load default configs
            string loadDir = Default ? directory : defaultDirectory;
            if (Directory.Exists(loadDir))
            {
                foreach (string file in Directory.GetFiles(loadDir, "*.json"))
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    // Check if this config already exists in the list by name
                    bool exists = list.Any(c => c.ConfigName == fileName);

                    if (!exists)
                    {
                        string json = File.ReadAllText(file);
                        GenerationConfig config = JsonConvert.DeserializeObject<GenerationConfig>(json);
                        config.ConfigName = fileName;
                        list.Add(config);
                    }
                }
            }

            // If still empty and not already looking for defaults, load defaults
            if (list.Count == 0 && !Default)
                list = LoadPresets(Default: true);

            return list;
        }
        public UserState LoadSettings()
        {
            string path = Environment.CurrentDirectory + "/wwwroot/config/User.json";
            UserState setting = new UserState();
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<UserState>(json);
            }
            return setting;
        }

        public List<Person> LoadProfileList()
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/config/Profiles";
            List<Person> list = new List<Person>();
            if (!Directory.Exists(directory))
            {
                return list;
            }
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                Person ints = JsonConvert.DeserializeObject<Person>(json);
                list.Add(ints);
            }
            return list;
        }

        public List<Theme> LoadThemes()
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/MudThemes/";
            List<Theme> list = new List<Theme>();
            if (!Directory.Exists(directory))
            {
                return list;
            }
            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                Theme ints = JsonConvert.DeserializeObject<Theme>(json);
                list.Add(ints);
            }
            return list;
        }
        public ChatHistory? LoadChatHistory(CharCard charCard)
        {
            string path = Environment.CurrentDirectory + "/wwwroot/chatHistory/" + charCard.system_name + ".json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ChatHistory>(json);
            }
            return null;
        }

        public List<CloudPreset> LoadCloudPresets()
        {
            List<CloudPreset> list = new List<CloudPreset>();
            string path = Environment.CurrentDirectory + "/wwwroot/default/CloudPresets.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                list = JsonConvert.DeserializeObject<CloudPreset[]>(json).ToList();

            }
            foreach (CloudPreset cloudPreset in list) 
            {
                cloudPreset.Icon = LoadCustomIcon(cloudPreset.IconName);
            }
            return list;

        }
        public string LoadCustomIcon(string iconName)
        {
            string iconsFolderPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "Content", "icons");
            string iconPath = Path.Combine(iconsFolderPath, iconName);

            if (File.Exists(iconPath))
            {
                return File.ReadAllText(iconPath);
            }

            return string.Empty;
        }
        public List<RecommendedModel> LoadRecommendedModels()
        {
            List<RecommendedModel> list = new List<RecommendedModel>();
            string path = Environment.CurrentDirectory + "/wwwroot/default/RecommendedModelsList.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                list = JsonConvert.DeserializeObject<RecommendedModel[]>(json).ToList();
            }
            return list;

        }
        public List<TutorialStep> LoadTutorial()
        {
            List<TutorialStep> list = new List<TutorialStep>();
            string path = Environment.CurrentDirectory + "/wwwroot/default/TutorialJSON.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                list = JsonConvert.DeserializeObject<TutorialStep[]>(json).ToList();
            }
            return list;
        }

        public List<string> LoadModelsPath(string CustomModelPath)
        {
            string directory = ModelsPath;
            if (!string.IsNullOrEmpty(CustomModelPath) && Directory.Exists(CustomModelPath))
                directory = CustomModelPath;
            List<string> list = new List<string>();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return list;
            }
            foreach (string file in Directory.GetFiles(directory, "*.gguf"))
            {
                list.Add(file);
            }
            return list;
        }
        public string LoadFirstEmbeddingModelPath()
        {
            string directory = EmbeddingModelsPath;
            List<string> list = new List<string>();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return "";
            }
            foreach (string file in Directory.GetFiles(directory, "*.gguf"))
            {
                list.Add(file);
            }
            return list.FirstOrDefault("");

        }
        public string LoadFirstKokoroModelPath()
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/LocalModels/KokoroModels/";
            List<string> list = new List<string>();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                return "";
            }
            foreach (string file in Directory.GetFiles(directory, "*.onnx"))
            {
                list.Add(file);
            }
            return list.FirstOrDefault("");

        }

        public string LoadGrammar(string FileNameWithExtension)
        {
            string path = Environment.CurrentDirectory + "/wwwroot/default/Grammar/" + FileNameWithExtension;
            if (File.Exists(path))
            {
                string grammar = File.ReadAllText(path).Trim();
                return grammar;
            }
            return "";
        }
        public void SavePresets()
        {
            if (settingsService != null && SaveInfoEvent != null)
            {
                isBusy = true;
                Console.WriteLine("Saving Presets~");
                SaveInfoEvent.Invoke(null, EventArgs.Empty);
                settingsService.LinkModelToCurrentInstruct();
                Saver.SaveListJson(settingsService.PresetsList);
                Saver.SaveListJson(settingsService.InstructList);
                Saver.SaveListJson(settingsService.ProfileList);
                settingsService.User.SaveSettings(settingsService);
                Console.WriteLine("~Finished saving presets");
                isBusy = false;
            }

        }
        public async Task SaveToJsonAndDownload<T>(T data, string fileName, IJSRuntime JSRuntime)
        {
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
            var dataUrl = $"data:text/json;charset=utf-8;base64,{base64}";
            await JSRuntime.InvokeVoidAsync("downloadFile", dataUrl, $"{fileName}.json");
        }
        public static byte[] LoadDefaultAvatar()
        {
            byte[] avatar;
            string directoryAvatar = Environment.CurrentDirectory + "/wwwroot/Content/Avatar.png";
            using (FileStream fs = new FileStream(directoryAvatar, FileMode.Open, FileAccess.Read))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    fs.CopyTo(ms);
                    avatar = ms.ToArray();
                }

            }
            return avatar;
        }

    }
}
