using Microsoft.JSInterop;

namespace LLMRP.Components.Models.Misc
{
    public class ScreenSize
    {
        private readonly IJSRuntime runtime;
        public ScreenSize(IJSRuntime runtime)
        {
            this.runtime = runtime;
        }
        public async Task<WindowSize> GetScreenSize()
        {
            try
            {
                return await runtime.InvokeAsync<WindowSize>("getScreenSize");
            }
            catch (JSDisconnectedException ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
         
        }
        public class WindowSize
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }
    }
}
