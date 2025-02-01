using DocumentFormat.OpenXml.Drawing;

namespace MousyHub.Models.Misc.Tutorial
{
    public class TutorialStep
    {
        public TutorialStep(int index)
        {
            Index = index;
            Key = "TutorialStep_" + index;
        }
        public int Index { get; set; }

        public string Key { get; set; }
        public string Text { get; set; }

        public string PNG_Name { get; set; } = "TutorialMouse1.png";

        public int TextSlowdown { get; set; } = 1;

        public MudBlazor.Position Position  = MudBlazor.Position.Bottom;

        public List<TutorialAction> Actions { get; set; } =  new List<TutorialAction>();

    }
}
