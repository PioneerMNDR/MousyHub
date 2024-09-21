namespace MousyHub.Components.Models.Services.URLHandle
{
    public static class URLParser
    {
        public static URLParsedResult? ParseChubUrl(string str)
        {
            var splitStr = str.Split('/');
            int length = splitStr.Length;

            if (length < 2)
            {
                return null;
            }

            int domainIndex = -1;
            string[] domains = { "www.chub.ai", "chub.ai", "www.characterhub.org", "characterhub.org" };

            for (int i = 0; i < splitStr.Length; i++)
            {
                if (domains.Contains(splitStr[i]))
                {
                    domainIndex = i;
                    break;
                }
            }

            var lastTwo = domainIndex != -1 ? splitStr.Skip(domainIndex + 1).ToArray() : splitStr;

            string firstPart = lastTwo[0].ToLower();

            if (firstPart == "characters" || firstPart == "lorebooks")
            {
                string type = firstPart == "characters" ? "character" : "lorebook";
                string id = type == "character" ? string.Join("/", lastTwo.Skip(1)) : string.Join("/", lastTwo);
                return new URLParsedResult
                {
                    Id = id,
                    Type = type
                };
            }
            else if (length == 2)
            {
                return new URLParsedResult
                {
                    Id = string.Join("/", lastTwo),
                    Type = "character"
                };
            }

            return null;
        }
        public class URLParsedResult
        {
            public string Id { get; set; }
            public string Type { get; set; }
        }
    }



}
