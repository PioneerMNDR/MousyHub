using LLMRP.Components.Models.Misc;
using LLMRP.Components.Models.Model;
using LLMRP.Components.Models.User;
using Microsoft.JSInterop;
using Newtonsoft.Json;
namespace LLMRP.Components.Models.Services
{
    public class UploaderService
    {

        public SettingsService settingsService;
        public bool isBusy { get; private set; }
        public event EventHandler SaveInfoEvent;




        public List<CharCard> TestLocalLoadCard()
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/testfiles/";
            List<CharCard> charCards = new List<CharCard>();

            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                CharCard charCard = JsonConvert.DeserializeObject<CharCard>(json);
                charCard.avatarPNG = LoadDefaultAvatar();
                charCards.Add(charCard);
            }
            return charCards;
        }
        public List<Instruct> LoadInstructs(bool Default = false)
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/InstructConfigs/";
            if (Default)
                directory = Environment.CurrentDirectory + "/wwwroot/default/InstructConfigs/";

            List<Instruct> list = new List<Instruct>();


            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                Instruct ints = JsonConvert.DeserializeObject<Instruct>(json);


                list.Add(ints);
            }
            if (list.Count == 0 && Default == false)
                list = LoadInstructs(Default: true);
            return list;
        }
        public List<GenerationConfig> LoadPresets(bool Default = false)
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/Presets/";
            if (Default)
                directory = Environment.CurrentDirectory + "/wwwroot/default/Presets/kobold";
            List<GenerationConfig> list = new List<GenerationConfig>();


            foreach (string file in Directory.GetFiles(directory, "*.json"))
            {
                string json = File.ReadAllText(file);
                GenerationConfig ints = JsonConvert.DeserializeObject<GenerationConfig>(json);
                ints.ConfigName = Path.GetFileNameWithoutExtension(file);
                list.Add(ints);
            }
            if (list.Count == 0 && Default == false)
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
            string path = Environment.CurrentDirectory + "/wwwroot/chatHistory/" + charCard.data.name + ".json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<ChatHistory>(json);
            }
            return null;
        }

        public List<string> LoadModelsPath()
        {
            string directory = Environment.CurrentDirectory + "/wwwroot/LocalModels/";
            List<string> list = new List<string>();
            foreach (string file in Directory.GetFiles(directory, "*.gguf"))
            {
                list.Add(file);
            }
            return list;
        }
        public void SavePresets()
        {
            if (settingsService != null)
            {
                isBusy = true;
                Console.WriteLine("Saving Presets~");
                SaveInfoEvent.Invoke(null, EventArgs.Empty);
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
