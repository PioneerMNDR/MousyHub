using LLama.Abstractions;
using LLama.Native;

namespace MousyHub.Components.Models.LLama
{
    public class LocalLLamaLaunchConfig
    {
        public int ContextSize { get; set; } = 2048;

        public int GpuLayerCount { get; set; } = 0;


        public bool UseMemorymap { get; set; } = false;


        public bool UseMemoryLock { get; set; } = true;

        public string ModelPath { get; set; }

        public uint? Threads { get; set; } = 7;

        public uint? BatchThreads { get; set; } = 7;

        public uint BatchSize { get; set; } = 512;

    }
}
