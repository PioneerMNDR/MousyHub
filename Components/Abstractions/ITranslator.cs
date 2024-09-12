namespace LLMRP.Components.Abstractions
{
    public interface ITranslator
    {
         Task<string> Translate(string text);
    }
}
