using DocumentFormat.OpenXml.Wordprocessing;
using Json.Schema.Generation;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace LLMRP.Components.Models.Misc
{
    public static class Util
    {
        public static string TruncateString(string str, int maxwords)
        {
            if (string.IsNullOrEmpty(str))
            {
                return str;
            }
            var words = str.Split(new[] { ' ', '\n', '\r' });
            if (words.Length < maxwords)
            {
                return str;
            }
            var truncate = words.Take(maxwords).ToArray();
            var result = string.Join(" ", truncate);
            return $"{result}...";
        }
        public static object CopyPropertiesLite<T>(this T source, T destination)
        {

            var sourceProps = typeof(T).GetProperties().Where(x => x.CanRead).ToList();
            var destProps = typeof(T).GetProperties().Where(x => x.CanWrite).ToList();

            foreach (var sorceProp in sourceProps)
            {
                if (destProps.Any(x => x.Name == sorceProp.Name))
                {
                    var p = destProps.First(x => x.Name == sorceProp.Name);
                    if (p.CanWrite)
                    {
                        p.SetValue(destination, sorceProp.GetValue(source, null), null);
                    }
                }

            }
            return destination;
        }
        public static string[] ExtractFastAnswers(string input)
        {
            // Обновленное регулярное выражение для поиска всех текстов внутри квадратных скобок
            string pattern = @"\[(.*?)\]";
            MatchCollection matches = Regex.Matches(input, pattern);

            // Создаем массив для хранения результатов
            string[] elements = new string[matches.Count];

            // Извлекаем все найденные строки
            for (int i = 0; i < matches.Count; i++)
            {
                elements[i] = matches[i].Groups[1].Value;
            }

            return elements;
        }

        public static MudBlazor.Color RandomColor()
        {
            Random random = new Random();
            Array array = Enum.GetValues(typeof(MudBlazor.Color));

            MudBlazor.Color rColor = (MudBlazor.Color)array.GetValue(random.Next(array.Length));
            if (rColor==MudBlazor.Color.Transparent || rColor == MudBlazor.Color.Surface)
            {
                rColor = RandomColor();
            }
            return rColor;

        }


        public static object CloneObject<T>(T source)
        {
            Type objtype = typeof(T);
            var ClonedJson = JsonConvert.SerializeObject(source, objtype, null);
            return JsonConvert.DeserializeObject(ClonedJson, objtype);


        }
    }
}
