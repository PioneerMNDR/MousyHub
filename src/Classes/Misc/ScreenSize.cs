using Microsoft.JSInterop;

namespace MousyHub.Models.Misc
{
    public class ScreenSize
    {
        private readonly IJSRuntime runtime;
        public bool IsMobile { get; private set; }  =false;
        private WindowSize screenSize;
        public ScreenSize(IJSRuntime runtime)
        {
            this.runtime = runtime;
       }
        public async Task<WindowSize> GetScreenSize()
        {
            try
            {
                screenSize =  await runtime.InvokeAsync<WindowSize>("getScreenSize");      
                return screenSize;
            }
            catch (JSDisconnectedException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
  
        public async Task<bool> IsMobileAsync()
        {
            var size = screenSize ?? await GetScreenSize();
            if (size.Width <= 600)
            {
                IsMobile = true;
            }
            return IsMobile;
        }

        public class WindowSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
