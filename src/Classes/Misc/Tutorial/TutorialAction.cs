namespace MousyHub.Models.Misc.Tutorial
{
    public class TutorialAction
    {
   

        public ActionType Type { get; set; }

        public string ElementId { get; set; }

        public string Value1 { get; set; }
        public enum ActionType
        {
            None,
            AddOverlay,
            RemoveOverlay,
            AddPulse,
            RemovePulse,
            ActivateTab
        }


    }

}
