using DocumentFormat.OpenXml.Drawing.Charts;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;

namespace MousyHub.Models.Provider.LLama
{
    public class CustomSampler()
         : BaseSamplingPipeline
    {

        private float _alphaFreq;

        private float _alphaPresence;

        //
        // Сводка:
        //     Bias values to add to certain logits
        public Dictionary<LLamaToken, float> LogitBias { get; } = new Dictionary<LLamaToken, float>();


        //
        // Сводка:
        //     Repetition penalty, as described in https://arxiv.org/abs/1909.05858
        public float RepeatPenalty { get; set; }

        //
        // Сводка:
        //     Frequency penalty as described by OpenAI: https://platform.openai.com/docs/api-reference/chat/create
        //
        //     Number between -2.0 and 2.0. Positive values penalize new tokens based on their
        //     existing frequency in the text so far, decreasing the model's likelihood to repeat
        //     the same line verbatim.
        public float AlphaFrequency
        {
            get
            {
                return _alphaFreq;
            }
            set
            {
                if (value < -2f)
                {
                    throw new ArgumentOutOfRangeException("value", "AlphaFrequency must be greater than -2");
                }

                if (value > 2f)
                {
                    throw new ArgumentOutOfRangeException("value", "AlphaFrequency must be less than 2");
                }

                _alphaFreq = value;
            }
        }

        //
        // Сводка:
        //     Presence penalty as described by OpenAI: https://platform.openai.com/docs/api-reference/chat/create
        //
        //     Number between -2.0 and 2.0. Positive values penalize new tokens based on whether
        //     they appear in the text so far, increasing the model's likelihood to talk about
        //     new topics.
        public float AlphaPresence
        {
            get
            {
                return _alphaPresence;
            }
            set
            {
                if (value < -2f)
                {
                    throw new ArgumentOutOfRangeException("value", "AlphaFrequency must be greater than -2");
                }

                if (value > 2f)
                {
                    throw new ArgumentOutOfRangeException("value", "AlphaFrequency must be less than 2");
                }

                _alphaPresence = value;
            }
        }

        //
        // Сводка:
        //     Temperature to apply (higher temperature is more "creative")
        public float Temperature { get; set; } = 0.75f;


        //
        // Сводка:
        //     Number of tokens to keep in TopK sampling
        public int TopK { get; set; }

        //
        // Сводка:
        //     Z value for tail free sampling
        public float TailFreeZ { get; set; }

        //
        // Сводка:
        //     P value for locally typical sampling
        public float TypicalP { get; set; }

        //
        // Сводка:
        //     P value for TopP sampling
        public float TopP { get; set; } = 1f;


        //
        // Сводка:
        //     P value for MinP sampling
        public float MinP { get; set; }

        //
        // Сводка:
        //     Whether the newline value should be protected from being modified by logit bias
        //     and repeat penalty
        public bool PenalizeNewline { get; set; }

        public int RepeatLastTokensCount { get; set; } = 64;

        protected override void ProcessLogits(SafeLLamaContextHandle ctx, Span<float> logits, ReadOnlySpan<LLamaToken> lastTokens)
        {
            foreach (var (lLamaToken2, num2) in LogitBias)
            {
                logits[(int)lLamaToken2] += num2;
            }
        }

        protected override LLamaToken ProcessTokenDataArray(SafeLLamaContextHandle ctx, LLamaTokenDataArray candidates, ReadOnlySpan<LLamaToken> lastTokens)
        {
            if (lastTokens.Length > 0 && (RepeatPenalty != 0f || AlphaFrequency != 0f || AlphaPresence != 0f))
            {
                int indexHint;
                float logit;
                if (PenalizeNewline)
                {
                    (indexHint, logit) = GetNewlineLogit(ctx, candidates);
                }
                else
                {
                    indexHint = -1;
                    logit = 0f;
                }

                candidates.RepetitionPenalty(ctx, lastTokens, RepeatPenalty, AlphaFrequency, AlphaPresence);
                if (!PenalizeNewline)
                {
                    SetNewlineLogit(ctx, candidates, indexHint, logit);
                }
            }

            candidates.ApplyGrammar(ctx, Grammar);
            candidates.TopK(ctx, TopK, 1uL);
            candidates.TailFree(ctx, TailFreeZ, 1uL);
            candidates.LocallyTypical(ctx, TypicalP, 1uL);
            candidates.TopP(ctx, TopP, 1uL);
            candidates.MinP(ctx, MinP, 1uL);
            candidates.Temperature(ctx, Temperature);
            return candidates.SampleToken(ctx);
        }

        private static (int, float) GetNewlineLogit(SafeLLamaContextHandle ctx, LLamaTokenDataArray candidates)
        {
            LLamaToken? newline = ctx.ModelHandle.Tokens.Newline;
            if (newline.HasValue)
            {
                LLamaToken id = candidates.Data.Span[(int)newline.Value].id;
                LLamaToken? lLamaToken = newline;
                if (id == lLamaToken)
                {
                    return ((int)newline.Value, candidates.Data.Span[(int)newline.Value].logit);
                }

                Span<LLamaTokenData> span = candidates.Data.Span;
                for (int i = 0; i < span.Length; i++)
                {
                    id = span[i].id;
                    lLamaToken = newline;
                    if (id == lLamaToken)
                    {
                        return (i, span[i].logit);
                    }
                }
            }

            return (-1, 0f);
        }

        private static void SetNewlineLogit(SafeLLamaContextHandle ctx, LLamaTokenDataArray candidates, int indexHint, float logit)
        {
            LLamaToken? newline = ctx.ModelHandle.Tokens.Newline;
            if (!newline.HasValue)
            {
                return;
            }

            if (indexHint >= 0)
            {
                LLamaToken id = candidates.Data.Span[indexHint].id;
                LLamaToken? lLamaToken = newline;
                if (id == lLamaToken)
                {
                    candidates.Data.Span[indexHint].logit = logit;
                    return;
                }
            }

            Span<LLamaTokenData> span = candidates.Data.Span;
            for (int i = 0; i < candidates.Data.Length; i++)
            {
                LLamaToken id = span[i].id;
                LLamaToken? lLamaToken = newline;
                if (id == lLamaToken)
                {
                    span[i].logit = logit;
                    break;
                }
            }
        }

        public override void Accept(SafeLLamaContextHandle ctx, LLamaToken token)
        {
            Grammar?.AcceptToken(ctx, token);
        }

        public override ISamplingPipeline Clone()
        {
            CustomSampler defaultSamplingPipeline = new CustomSampler();
            foreach (var (key, value) in LogitBias)
            {
                defaultSamplingPipeline.LogitBias.Add(key, value);
            }

            defaultSamplingPipeline.Grammar = Grammar?.Clone();
            defaultSamplingPipeline.RepeatPenalty = RepeatPenalty;
            defaultSamplingPipeline.AlphaFrequency = AlphaFrequency;
            defaultSamplingPipeline.AlphaPresence = AlphaPresence;
            defaultSamplingPipeline.Temperature = Temperature;
            defaultSamplingPipeline.TopK = TopK;
            defaultSamplingPipeline.TailFreeZ = TailFreeZ;
            defaultSamplingPipeline.TypicalP = TypicalP;
            defaultSamplingPipeline.TopP = TopP;
            defaultSamplingPipeline.MinP = MinP;
            defaultSamplingPipeline.PenalizeNewline = PenalizeNewline;
            return defaultSamplingPipeline;
        }
    }
}