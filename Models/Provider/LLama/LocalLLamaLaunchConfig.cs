using LLama.Abstractions;
using LLama.Native;

namespace MousyHub.Models.Provider.LLama
{
    public class LocalLLamaLaunchConfig
    {
        public int ContextSize { get; set; } = 4096;

        public int GpuLayerCount { get; set; } = 10;


        public bool UseMemorymap { get; set; } = true;


        public bool UseMemoryLock { get; set; } = false;

        public string ModelPath { get; set; }

        public uint? Threads { get; set; }

        public uint? BatchThreads { get; set; }

        public uint BatchSize { get; set; } = 512;

        public bool UseFlashAttention { get; set; }  = false;

    }
}
