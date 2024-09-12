using LLama;
using LLama.Abstractions;
using LLama.Batched;
using LLama.Common;
using LLama.Native;
using LLama.Sampling;
using LLMRP.Components.Models.Model;
using Spectre.Console;

namespace LLMRP.Components.Models.LLama
{
    public class LocalLlamaCore : IDisposable
    {
        string modelPath = "";
        LLamaWeights? weights = null;
        BatchedExecutor? executor = null;
        ModelParams? Params = null;
        private CustomSampler _sampler = null;
        StatelessExecutor? _executor = null;

        public async Task<bool> Run(ModelParams modelParams)
        {
            try
            {
          
               await Task.Run(() =>
                {
                    weights = LLamaWeights.LoadFromFile(modelParams);
                });

                executor = new BatchedExecutor(weights, modelParams);
                Params = modelParams;
                return true;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(ex.ToString());
                Console.ResetColor();
                return false;
            }


        }


        public async Task<MessageResponse> Generate(string promt, string id, InferenceParams param, CancellationToken cancellationToken)
        {
            MessageResponse Response = new MessageResponse("", true, "");
            try
            {
                await GenerateStream(promt, id, param, cancellationToken: cancellationToken, onTokenRecieved: async messageResponse =>
                {
                    Response.Content += messageResponse.Content;
                });
                return Response;
            }
            catch (Exception ex)
            {

                Response.IsSuccess = false;
                Response.ErrorMessage = ex.Message;
                return Response;
            }
        }

        public async Task GenerateStream(string promt, string id, InferenceParams param, Func<MessageResponse, Task> onTokenRecieved, CancellationToken cancellationToken)
        {

            ConversationElement Element = GetConversationElement(id);

            List<LLamaToken> NewTokens = executor.Context.Tokenize(promt, true, true).ToList();
            List<LLamaToken>? OnlyNewTokens = GetOnlyNewTokens(NewTokens, Element._tokens)?.ToList();
            if (OnlyNewTokens != null && CheckContextSize(OnlyNewTokens.Count + param.MaxTokens, Element) == false)
            {
                await onTokenRecieved(new MessageResponse(null, false, "The limit a context of local model is exceeded"));
                return;
            }

            //If the order of the previous request at least for one token did not coincide with order of new inquiry (that is the old context received changes)
            bool isOrderIsNotBroken = true;
            if (Element._tokens.Count > 0)
            {
                var decoder = new StreamingTokenDecoder(executor.Context);

                Console.WriteLine("OldTokensCount: " + Element._tokens.Count);
                Console.WriteLine("NewTokensCount: " + NewTokens.Count);
                int RewindCount = 0;

                if (Element._tokens.Count > NewTokens.Count)
                {
                    var emptyTokens = new LLamaToken[Element._tokens.Count - NewTokens.Count];
                    NewTokens.AddRange(emptyTokens);
                }
                for (int i = 0; i < Element._tokens.Count; i++)
                {

                    //decoder.Add(NewTokens[i]);
                    //Console.WriteLine("токен2: " + decoder.Read());
                    if (Element._tokens[i] != NewTokens[i])
                    {
                        isOrderIsNotBroken = false;
                        Console.WriteLine("No match on token: " + i);
                        decoder.Add(Element._tokens[i]);
                        Console.WriteLine("Token symbol: " + decoder.Read());
                        decoder.Add(NewTokens[i]);
                        Console.WriteLine("New token symbol: " + decoder.Read());
                        RewindCount = Element._tokens.Count - i;
                        Console.WriteLine("Rollback on: " + RewindCount);

                        if (Element.Conversation.RequiresInference)
                            await executor.Infer(cancellationToken);
                        Element.Conversation.Rewind(RewindCount);
                        if (Element.Conversation.RequiresInference)
                            await executor.Infer(cancellationToken);

                        var newTokensForPromt = TokenGuard(NewTokens.Skip(i).ToList());
                        //if there is nothing to add to the promt (that is, we went back and did not add anything new.
                        //We must artificially add at least one token to the promt. Therefore, we cut off 1 from the end
                        if (newTokensForPromt.Length == 0)
                        {
                            Console.WriteLine("Rollback without new tokens detected");
                            LLamaToken lastToken = NewTokens.Last();
                            if (Element.Conversation.RequiresInference)
                                await executor.Infer(cancellationToken);
                            Element.Conversation.Rewind(1);
                            newTokensForPromt = new LLamaToken[1] { lastToken };
                        }
                        Element.Conversation.Prompt(newTokensForPromt);
                        Element._tokens = Element._tokens.Take(i).ToList();
                        var s = TokenGuard(NewTokens.Skip(i).ToList());
                        Element._tokens.AddRange(TokenGuard(NewTokens.Skip(i).ToList()));
                        break;
                    }
                }
                if (isOrderIsNotBroken)
                {
                    if (Element.Conversation.RequiresInference)
                        await executor.Infer(cancellationToken);
                    NewTokens = NewTokens.Skip(Element._tokens.Count).ToList();
                    Element._tokens.AddRange(NewTokens);
                    if (Element.Conversation.RequiresInference)
                        await executor.Infer(cancellationToken);
                    Element.Conversation.Prompt(NewTokens);
                }
            }
            else
            {
                if (Element.Conversation.RequiresInference)
                    await executor.Infer(cancellationToken);

                NewTokens = executor.Context.Tokenize(promt, true, true).ToList();
                Element._tokens.AddRange(NewTokens);
                Element.Conversation.Prompt(NewTokens);
            }
            if (Element._tokens.Count == Element.Conversation.TokenCount)
            {
                Console.WriteLine("Test 1 passed!");
            }
            try
            {
                int tokenCountBefore = Element._tokens.Count;
                if (Element.Conversation.RequiresInference)
                    await executor.Infer(cancellationToken);
                var decoder = new StreamingTokenDecoder(executor.Context);
                AntipromptProcessor antiprocessor = new AntipromptProcessor(param.AntiPrompts);
                _sampler = (CustomSampler)param.SamplingPipeline;
                int repeat_last_n = Math.Max(0, _sampler.RepeatLastTokensCount < 0 ? weights.ContextSize : _sampler.RepeatLastTokensCount);
                List<LLamaToken> lastTokens = new List<LLamaToken>(repeat_last_n);
                for (int j = 0; j < repeat_last_n; j++)
                {
                    lastTokens.Add(0);
                }
                lastTokens.AddRange(NewTokens);
            
                if (Element.Conversation.RequiresInference)
                    await executor.Infer(cancellationToken);

                for (int i = 0; i < param.MaxTokens; i++)
                {
                    int count = Math.Min((int)executor.Context.ContextSize, repeat_last_n);
                    LLamaToken[] array = lastTokens.TakeLast(count).ToArray();
                    if (Element.Conversation.RequiresInference)
                        await executor.Infer(cancellationToken);
                    LLamaToken newToken = new LLamaToken();
                    if (Element.Conversation.RequiresSampling)
                    {
                        newToken = _sampler.Sample(Element.Conversation.Executor.Context.NativeHandle, Element.Conversation.Sample(), array);
                    }
                    else
                    {
                        continue;
                    }
                    decoder.Add(newToken);
                    string tokenValue = decoder.Read();
                    if (antiprocessor.Add(tokenValue))
                    {
                        break;
                    }
                    if (weights.Tokens.IsEndOfGeneration(newToken))
                    {
                        break;
                    }
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    Element._tokens.Add(newToken);
                    lastTokens.Add(newToken);
                    await onTokenRecieved(new MessageResponse(tokenValue, true, ""));
                    //Add new tokens to history(kv)
                    Element.Conversation.Prompt(newToken);
                }
                if (Element._tokens.Count == Element.Conversation.TokenCount)
                {
                    Console.WriteLine("Test 2 passed!");
                }
                var timings = executor.Context.NativeHandle.GetTimings();
                var ctxSizeConv = GetNowContextSize();
                Console.WriteLine("-----Timings-----");
                Console.ForegroundColor = ConsoleColor.Green;
                AnsiConsole.MarkupLine($"All Time: {(timings.Sampling.Seconds + timings
                    .Eval.Seconds)}s ");
                AnsiConsole.MarkupLine($"Token/s: {(Element._tokens.Count - tokenCountBefore) / timings.Eval.TotalSeconds}t/s ");
                AnsiConsole.MarkupLine($"Context limit: {ctxSizeConv}/{executor.Context.ContextSize} tokens ");
                //AnsiConsole.MarkupLine($"Context limit KvCache: {contextSize}/{executor.Context.ContextSize} tokens ");
                AnsiConsole.MarkupLine($"Context branch count: {conversationElements.Count} ");
                foreach (var item in conversationElements)
                {
                    AnsiConsole.MarkupLine($"Branch {item.Id}: {item._tokens.Count} tokens ");
                }
                Console.ResetColor();
                executor.Context.NativeHandle.ResetTimings();
                Console.WriteLine("------------------");
            }
            catch (Exception exc)
            {
                await onTokenRecieved(new MessageResponse(null, false, exc.Message));
                Console.WriteLine(exc.StackTrace);
                return;
            }
        }
        private ConversationElement GetConversationElement(string id)
        {
            ConversationElement? element = null;
            element = conversationElements.Where(element => element.Id == id).FirstOrDefault();
            if (element == null)
            {
                var newConversation = executor.Create();
                element = new ConversationElement(newConversation, id);
                conversationElements.Add(element);
            }
            element.Priority++;

            return element;

        }
        /// <summary>
        /// Return false if the quantity of new tokens exceeds a context limit
        /// </summary>
        /// <param name="newTokensCount"></param>
        /// <returns></returns>
        private bool CheckContextSize(int newTokensCount, ConversationElement currentConversation)
        {

            int TokenCount = 0;
            foreach (var item in conversationElements)
            {
                TokenCount += item.Conversation.TokenCount;
            }
            TokenCount += newTokensCount;

            if (executor.Context.ContextSize <= TokenCount)
            {
                if (conversationElements.Count > 1)
                {
                    if (ConversationOptimization(TokenCount - (int)executor.Context.ContextSize, currentConversation) == false)
                    {
                        //If no conversation could be deleted, then the memory is full, returns false
                        return false;
                    }
                    //If we were able to delete something, then we check again
                    return CheckContextSize(newTokensCount, currentConversation);
                }
                //If the memory is full, but we only have one conversation, then all GG
                return false;
            }
            //If the memory is not full, then skip context optimization
            return true;

        }
        private bool ConversationOptimization(int needReleaseTokens, ConversationElement currentConversation)
        {
            Console.WriteLine("Let's start cleaning...");

            conversationElements = conversationElements.OrderBy(x => x.Priority).ToList();
            int TokensCount = 0;
            List<ConversationElement> trash = new List<ConversationElement>();

            foreach (var item in conversationElements.Where(x => x != currentConversation && x.Id != "main").ToList())
            {

                TokensCount += item._tokens.Count;
                trash.Add(item);
                item.Conversation.Dispose();
                Console.WriteLine("Clear Conversation of id: " + item.Id);
                //We delete all talk except the basic tokens so far there will be no necessary quantity
                if (TokensCount > needReleaseTokens)
                {
                    break;
                }
            }
            if (trash.Count > 0)
            {
                conversationElements.RemoveAll(x => trash.Contains(x));
                return true;
            }
            else
            {
                return false;
            }
        }
        private int GetNowContextSize()
        {
            int oldTokenCount = 0;
            foreach (var item in conversationElements)
            {
                oldTokenCount += item.Conversation.TokenCount;
            }
            return oldTokenCount;
        }

        public int TokenCount(string Promt)
        {

            if (executor != null)
            {
                return executor.Context.Tokenize(Promt, true, true).ToList().Count;
            }
            return 0;
        }
        public int ContextSize()
        {

            if (executor != null)
            {
                return (int)executor.Context.ContextSize;
            }
            return 0;
        }

        private List<ConversationElement> conversationElements { get; set; } = new List<ConversationElement>();

        //Returns a sequence of tokens without placeholder zeros
        LLamaToken[] TokenGuard(List<LLamaToken> tokens)
        {
            var t = tokens.Where(x => x.ToString() != "0").ToArray();
            return t;
        }
        //Compares the original sequence with the new one and returns only the new tokens
        private LLamaToken[]? GetOnlyNewTokens(List<LLamaToken> NewTokens, List<LLamaToken> OldTokens)
        {
            if (OldTokens.Count > NewTokens.Count)
            {
                var emptyTokens = new LLamaToken[OldTokens.Count - NewTokens.Count];
                NewTokens.AddRange(emptyTokens);
            }
            for (int i = 0; i < OldTokens.Count; i++)
            {
                if (OldTokens[i] != NewTokens[i])
                {

                    var newTokensForPromt = TokenGuard(NewTokens.Skip(i).ToList());
                    Console.WriteLine("The GetOnlyNewTokens method returned new tokens -" + newTokensForPromt.Length);
                    return newTokensForPromt;
                }
            }
            return NewTokens.ToArray();

        }
        public async Task<string> ModelInfo()
        {
            return Params.ModelPath;
        }
        public async Task<bool> Status()
        {
            if (executor != null)
            {
                return true;
            }
            return false;

        }

        public void Dispose()
        {
            weights?.Dispose();
            executor?.Dispose();
        }
    }
}
