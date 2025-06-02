using MousyHub.Classes.Misc;
using MousyHub.Models.Misc;
using MousyHub.Models.Model;
using MousyHub.Classes.Model;
using MousyHub.Classes.User;
using DocumentFormat.OpenXml.Bibliography;
using static MudBlazor.CategoryTypes;
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


        private void DetectAPIType(bool isChatCompletions)
        {
            isInstructed = _providerService.SelectType.Key is ProviderService.APIType.Cloud && isChatCompletions ? false : true;
        }
        public async Task<MessageResponse> Generate(string SystemPromt, string UserPromt, GenerationConfig generationConfig, Instruct instruct, string BotName = "Assistant", string UserName = "User", string PromtAfterOutputSequence = "", bool isChatCompletions = false)
        {
            DetectAPIType(isChatCompletions);
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
            DetectAPIType(isChatCompletions);
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

        public async Task<MessageResponse> GenerateWithExistingChat(string UserPromt, ChatState chatState, SettingsService settings, bool isChatCompletions = false)
        {
            DetectAPIType(isChatCompletions);
            if (_providerService.Status && chatState.ChatHistory is not null)
            {
                var person = await chatState.AddNewUserMessageInChat(UserPromt);
                if (person.IsUser)
                {
                    throw new Exception("Queue error in AdvancedQueryService");
                }
                string stop_seq = settings.CurrentInstruct.stop_sequence + StringHelperBuilder.TagPlaceholder(",{{user}}:,{{char}}:", chatState.ChatHistory.MainUser.Name, chatState.ChatHistory.MainCharacter.Name);
                Promt promt = new Promt(chatHistory: chatState.ChatHistory, instruct: settings.CurrentInstruct, person, reasoningOptions: settings.User.ReasoningOptions, isChatCompletions);
                Console.WriteLine("Query Promt: " + promt.FullContent);

                var res =  await _providerService.LLModel.GenerateTextAsync(promt, settings.CurrentGenerationConfig, maxTokens: settings.User.CurrentMaxToken, stop_sequence: stop_seq);
                chatState.NextPerson = chatState.ChatHistory.QueueMoveOrder();
                return res;
            }
            return new MessageResponse("", false, "No connection or open chat history");

        }


    }
}
