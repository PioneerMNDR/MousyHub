using LLMRP.Components.Models.Misc;
using LLMRP.Components.Models.Model;
using LLMRP.Components.Models.User;
using Newtonsoft.Json;

namespace LLMRP.Components.Models
{
    public static class Saver
    {


        public static void SaveToJson<T>(T obj, string fileName) where T : class
        {

            try
            {
                string directory = Environment.CurrentDirectory + "/wwwroot/";

                if (typeof(T) == typeof(Instruct))
                {
                    directory += "InstructConfigs/";
                }
                if (typeof(T) == typeof(GenerationConfig))
                {
                    directory += "Presets/";
                }
                if (typeof(T) == typeof(UserState))
                {
                    directory += "config/";
                }
                if (typeof(T) == typeof(CharCard))
                {
                    directory += "testfiles/";
                    fileName = fileName.Replace('/', '_');
                }
                if (typeof(T) == typeof(Theme))
                {
                    directory += "MudThemes/";
                }
                string path = Path.Combine(directory, fileName + ".json");

                string json = JsonConvert.SerializeObject(obj, Formatting.Indented);
                File.WriteAllText(path, json);
                Console.WriteLine("Saved to " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveToJson Error: " + ex.Message);

            }

        }
        public static void Delete<T>(T obj, string fileName) where T : class
        {

            try
            {
                string directory = Environment.CurrentDirectory + "/wwwroot/";

                if (typeof(T) == typeof(Instruct))
                {
                    directory += "InstructConfigs/";
                }
                if (typeof(T) == typeof(GenerationConfig))
                {
                    directory += "Presets/";
                }
                if (typeof(T) == typeof(UserState))
                {
                    directory += "config/";
                }
                if (typeof(T) == typeof(CharCard))
                {
                    directory += "testfiles/";
                }
                if (typeof(T) == typeof(Theme))
                {
                    directory += "MudThemes/";
                }
                string path = Path.Combine(directory, fileName + ".json");
                File.Delete(path);
                Console.WriteLine("Deleted " + path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveToJson Error: " + ex.Message);

            }

        }
        public static void SaveListJson<T>(List<T> List) where T : class
        {

            try
            {
                string directory = Environment.CurrentDirectory + "/wwwroot/";
                string filename = "NewFile";
                if (typeof(T) == typeof(Instruct))
                {
                    directory += "InstructConfigs/";

                }
                if (typeof(T) == typeof(GenerationConfig))
                {
                    directory += "Presets/";
                }
                if (typeof(T) == typeof(Person))
                {
                    directory += "config/Profiles/";
                }

                //Delete all old presets
                foreach (var file in Directory.GetFiles(directory))
                {
                    if (directory != Environment.CurrentDirectory + "/wwwroot/")
                    {
                        File.Delete(file);
                    }
                }
                //Write all new presets and any old
                foreach (var item in List)
                {
                    if (typeof(T) == typeof(Instruct))
                    {
                        var i = item as Instruct;
                        filename = i.name;

                    }
                    if (typeof(T) == typeof(GenerationConfig))
                    {
                        var i = item as GenerationConfig;
                        filename = i.ConfigName;
                    }
                    if (typeof(T) == typeof(Person))
                    {
                        var i = item as Person;
                        filename = i.Name;
                    }
                    string path = Path.Combine(directory, filename + ".json");
                    string json = JsonConvert.SerializeObject(item, Formatting.Indented);
                    File.WriteAllText(path, json);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveListJson Error: " + ex.Message);

            }

        }

        public static void SaveChatHistory(ChatHistory chatHistory)
        {

            try
            {
                string directory = Environment.CurrentDirectory + "/wwwroot/chatHistory";
                string filename = chatHistory.ChatName;
                string json = JsonConvert.SerializeObject(chatHistory, Formatting.Indented);
                string path = Path.Combine(directory, filename + ".json");
                File.WriteAllText(path, json);
                Console.WriteLine("Saved chat to " + path);

            }
            catch (Exception ex)
            {
                Console.WriteLine("SaveChatHistory Error: " + ex.Message);

            }

        }
    }
}
