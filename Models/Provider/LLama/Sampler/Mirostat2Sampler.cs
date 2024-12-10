using LLama.Native;
using LLama.Sampling;

namespace MousyHub.Models.Provider.LLama.Sampler
{
    public class Mirostat2Sampler : BaseSamplingPipeline
    {

        private const float DefaultTau = 5f;

     
        private float _tau = 5f;

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

            chain.AddMirostat2Sampler(Seed, Tau, Eta);

            return chain;
        }
    }
}
