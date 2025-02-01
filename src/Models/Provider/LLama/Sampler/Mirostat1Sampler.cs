using LLama.Native;
using LLama.Sampling;

namespace MousyHub.Models.Provider.LLama.Sampler
{
    public class Mirostat1Sampler : BaseSamplingPipeline
    {
        private const int MIROSTAT_M = 100;

        private const float DEFAULT_TAU = 5f;

        private float _mu = 10f;

        private float _tau = 5f;

        //
        // Сводка:
        //     Currently learned mu value
        public float Mu => _mu;

        //
        // Сводка:
        //     target entropy
        public float Tau
        {
            get
            {
                return _tau;
            }
            set
            {
                _tau = value;
                _mu = value * 2f;
            }
        }

        //
        // Сводка:
        //     learning rate
        public float Eta { get; set; } = 0.1f;
        public uint Seed { get; set; } = GetRandomSeed();

        private static Random RandomSeedGenerator = new();
        private static uint GetRandomSeed()
        {
            lock (RandomSeedGenerator)
                return (uint)RandomSeedGenerator.Next(0, int.MaxValue) + (uint)RandomSeedGenerator.Next(0, int.MaxValue);
        }

        protected override SafeLLamaSamplerChainHandle CreateChain(SafeLLamaContextHandle context)
        {
            var chain = SafeLLamaSamplerChainHandle.Create(LLamaSamplerChainParams.Default());

            chain.AddMirostat1Sampler(context.VocabCount, Seed,Tau,Eta,MIROSTAT_M);

            return chain;
        }
    }
}
