using MousyHub.Classes.Misc;
using MousyHub.Models.Misc;
using MousyHub.Models.Model;
using MousyHub.Classes.Model;
namespace MousyHub.Models.Services
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
        bool isInstructed;


        private void DetectAPIType()
        {
            isInstructed = _providerService.SelectType.Key is ProviderService.APIType.Cloud ? false : true;
        }
        public async Task<MessageResponse> Generate(string SystemPromt, string UserPromt, GenerationConfig generationConfig, Instruct instruct, string BotName = "Assistant", string UserName = "User", string PromtAfterOutputSequence = "", bool isChatCompletions=false)
        {
            DetectAPIType();
            if (_providerService.Status)
            {
                //Set custom temp
                GenerationConfig newGenConfig = (GenerationConfig)Util.CloneObject(generationConfig);
                newGenConfig.temp = CustomTemperature;
                //Make request
                string PreparedSystemPromt = StringHelperBuilder.TagPlaceholder(StringHelperBuilder.WizardSystemMessage(instruct, SystemPromt, isInstructed), UserName, BotName);
                string PreparedUserPromt = StringHelperBuilder.TagPlaceholder(StringHelperBuilder.WizardRequestMessage(instruct, UserPromt, isInstructed) + PromtAfterOutputSequence, UserName, BotName);
                Promt promt = new Promt(PreparedSystemPromt, PreparedUserPromt, isChatCompletions);
                //Debug
                Console.WriteLine("Query Promt: " + promt.FullContent);

                return await _providerService.LLModel.GenerateTextAsync(promt, newGenConfig, maxTokens: CustomMaxTokens, stop_sequence: instruct.stop_sequence, key: "Wizard");
            }
            return new MessageResponse("", false, "No connection");

        }
        public async Task<MessageResponse> Continue(string SystemPromt, string UserPromt, string PromtForContinue, GenerationConfig generationConfig, Instruct instruct, string BotName = "Assistant", string UserName = "User", string PromtAfterOutputSequence = "", bool isChatCompletions = false)
        {
            DetectAPIType();
            if (_providerService.Status)
            {
                //Make request
                string PreparedSystemPromt = StringHelperBuilder.TagPlaceholder(StringHelperBuilder.WizardSystemMessage(instruct, SystemPromt, isInstructed), UserName, BotName);
                string PreparedUserPromt = StringHelperBuilder.TagPlaceholder(StringHelperBuilder.WizardRequestMessage(instruct, UserPromt, isInstructed) + PromtAfterOutputSequence + PromtForContinue, UserName, BotName);
                Promt promt = new Promt(PreparedSystemPromt, PreparedUserPromt, isChatCompletions);
                //Set custom temp
                GenerationConfig newGenConfig = (GenerationConfig)Util.CloneObject(generationConfig);
                newGenConfig.temp = CustomTemperature;
                //Debug
                Console.WriteLine("Query Promt: " + PromtForContinue);
                return await _providerService.LLModel.GenerateTextAsync(promt, newGenConfig, maxTokens: CustomMaxTokens, stop_sequence: instruct.stop_sequence, key: "Wizard");
            }
            return new MessageResponse("", false, "No connection");
        }
    }
}
