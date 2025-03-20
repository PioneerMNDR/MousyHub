using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;


namespace MousyHub.Models.Services
{
    public class RAGService
    {
        private IKernelMemory? Kernel { get; set; }
        public bool IsAvailable { get; private set; } = false;

        public RAGService()
        {
       
        }

        public async Task<bool> TryRunAsync(string model_path)
        {
            try
            {
                var searchClientConfig = new SearchClientConfig
                {
                    MaxMatchesCount = 2,
                    AnswerTokens = 100,
                };
                var llamaConfig = new LlamaSharpModelConfig
                {
                    GpuLayerCount = 0,
                    ModelPath = model_path,
                    MaxTokenTotal = 2048,
                };
                var textPartOptions = new TextPartitioningOptions
                {
                    // Max 20 tokens per sentence
                    MaxTokensPerLine = 20,
                    // When sentences are merged into paragraphs (aka partitions), stop at 100 tokens
                    MaxTokensPerParagraph = 100,
                    // Each paragraph contains the last 20 tokens from the previous one
                    OverlappingTokens = 20,
                };

      
                Kernel = await Task.Run(() =>
                {
                    return new KernelMemoryBuilder()
                        .WithSearchClientConfig(searchClientConfig)
                        .WithCustomTextPartitioningOptions(textPartOptions)
                        .WithoutTextGenerator()
                        .WithLlamaTextEmbeddingGeneration(llamaConfig)
                        .Build();
                });

                Console.WriteLine("Embedding model is run");
                IsAvailable = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running embedding model: {ex.Message}"); 
                                                                                   
                IsAvailable = false;
                return false;
            }
        }
        public async Task ImportMemory(string dialog, string chatId)
        {
            if (Kernel!=null)
            {
                if (await Kernel.IsDocumentReadyAsync(chatId))
                    await Kernel.DeleteDocumentAsync(chatId);
                Console.WriteLine($"Import chat {chatId} in memory");
                await Kernel.ImportTextAsync(dialog, documentId: chatId);
                Console.WriteLine($"Import successful");
            }


        }
        public async Task<bool> CheckMemoryAvailable(string chatId)
        {
            if (Kernel != null)
                return await Kernel.IsDocumentReadyAsync(chatId);
            return false;
        }
        public async Task ClearMemories(string chatId)
        {
            if (Kernel!=null)
            if (await Kernel.IsDocumentReadyAsync(chatId))
                await Kernel.DeleteDocumentAsync(chatId);
        }
        public async Task<string> SearchInMemory(string request, string chatId)
        {
            if (Kernel!=null && await Kernel.IsDocumentReadyAsync(chatId))
            {
                var searchResult = await Kernel.SearchAsync(request, filter: MemoryFilters.ByDocument(chatId));
                var g = await Kernel.ListIndexesAsync();
                if (searchResult.NoResult || searchResult.Results.Count==0)
                {
                    return "";
                }
                Citation bigResult = searchResult.Results[0];
                // Store the document IDs so we can load all their records later
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.WriteLine($"***Memory Search Result***");
                Console.WriteLine($"Document ID: {bigResult.DocumentId}");
                Console.WriteLine($"Relevant partitions: {bigResult.Partitions.Count}");
                foreach (Citation.Partition smallResult in bigResult.Partitions)
                {
                    Console.WriteLine($"----Partition {smallResult.PartitionNumber}----Relevance {smallResult.Relevance * 100:F1}% -------");
                    Console.WriteLine($"{smallResult.Text}");

                }
                Console.ResetColor();
                return bigResult.Partitions.OrderByDescending(x => x.Relevance).FirstOrDefault()?.Text ?? "";
            }
            return "";

        }
    }

    public static class MemoryFilters
    {
        public static MemoryFilter ByTag(string name, string value)
        {
            return new MemoryFilter().ByTag(name, value);
        }

        public static MemoryFilter ByDocument(string docId)
        {
            return new MemoryFilter().ByDocument(docId);
        }
    }
}
