using LLMRP.Components.Models.Misc;
using LLMRP.Components.Models.Model;

namespace LLMRP.Components.Models.Services
{
    public class AdvancedQueryService
    {
        private readonly ProviderService _providerService;
        public AdvancedQueryService(ProviderService providerService)
        {
            _providerService = providerService;
        }
        public double CustomTemperature = 1.0;
        public int CustomMaxTokens = 300;

        public async Task<MessageResponse> Generate(string SystemPromt, string Promt, GenerationConfig generationConfig, Instruct instruct, string BotName = "Assistant", string UserName = "User", string PromtAfterOutputSequence = "")
        {
            if (_providerService.Status)
            {
                //Set custom temp
                GenerationConfig newGenConfig = (GenerationConfig)Util.CloneObject(generationConfig);
                newGenConfig.temp = CustomTemperature;
                //Make request
                string RequestPromt = PromtBuilder.WizardSystemMessage(instruct, SystemPromt) + PromtBuilder.WizardRequestMessage(instruct, Promt) + PromtAfterOutputSequence;
                RequestPromt = PromtBuilder.TagPlaceholder(RequestPromt, UserName, BotName);
                //Debug
                Console.WriteLine("Query Promt: " + RequestPromt);

                return await _providerService.LLModel.GenerateTextAsync(RequestPromt, newGenConfig, maxTokens: CustomMaxTokens, stop_sequence: instruct.stop_sequence, key: "Wizard");
            }
            return new MessageResponse("", false, "No connection");

        }
        public async Task<MessageResponse> Continue(string SystemPromt, string Promt, string PromtForContinue, GenerationConfig generationConfig, Instruct instruct, string BotName = "Assistant", string UserName = "User", string PromtAfterOutputSequence = "")
        {               //Make request
            string RequestPromt = PromtBuilder.WizardSystemMessage(instruct, SystemPromt) + PromtBuilder.WizardRequestMessage(instruct, Promt) + PromtAfterOutputSequence + PromtForContinue;
            RequestPromt = PromtBuilder.TagPlaceholder(RequestPromt, UserName, BotName);
            //Set custom temp
            GenerationConfig newGenConfig = (GenerationConfig)Util.CloneObject(generationConfig);
            newGenConfig.temp = CustomTemperature;
            //Debug
            Console.WriteLine("Query Promt: " + PromtForContinue);
            return await _providerService.LLModel.GenerateTextAsync(RequestPromt, newGenConfig, maxTokens: CustomMaxTokens, stop_sequence: instruct.stop_sequence, key: "Wizard");
        }
    }
}
