namespace MousyHub
{
    public static class AppVersion
    {
        public static string _version { get; private set; } = string.Empty;

        public static void SetVersion(string version) 
        {
            _version = version;
        }
       
    }
}
