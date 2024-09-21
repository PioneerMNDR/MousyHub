using MudBlazor;

namespace MousyHub.Components.Models.Misc
{
    public class QuickReplySetting
    {
        public QuickReplySetting(ResponseEmotion emotion)
        {
            Emotion = emotion;
            MudColor = Util.RandomColor();
        }

        public Color MudColor { get; set; } = Color.Default;

        public ResponseEmotion Emotion { get; set; } = ResponseEmotion.Positive;

        public enum ResponseEmotion
        {
            Positive,
            Negative,
            Neutral,
            Happy,
            Sad,
            Angry,
            Scared,
            Surprised,
            Disgusted,
            Excited,
            Anxious,
            Calm,
            Confused,
            Proud,
            Ashamed,
            Grateful,
            Disappointed,
            Frustrated,
            Amused,
            Bored,
            Curious,
            Confident,
            Insecure,
            Content,
            Overwhelmed,
            Relieved,
            Empathetic,
            Indifferent,
            Optimistic,
            Pessimistic,
            Stressed,
            Relaxed,
            Enthusiastic,
            Irritated,
            Satisfied,
            Inspired,
            Uncomfortable,
            Regretful,
            Sarcastic,
            Sympathetic,
            Skeptical,
            Hopeful,
            Defensive,
            Agreeable,
            Disagreeable,
            Sexual
        }

        public readonly Dictionary<ResponseEmotion, string> EmojiMap = new Dictionary<ResponseEmotion, string>
    {
        { ResponseEmotion.Positive, "👍" },
        { ResponseEmotion.Negative, "👎" },
        { ResponseEmotion.Neutral, "😐" },
        { ResponseEmotion.Happy, "😊" },
        { ResponseEmotion.Sad, "😢" },
        { ResponseEmotion.Angry, "😠" },
        { ResponseEmotion.Scared, "😨" },
        { ResponseEmotion.Surprised, "😮" },
        { ResponseEmotion.Disgusted, "🤢" },
        { ResponseEmotion.Excited, "🤩" },
        { ResponseEmotion.Anxious, "😰" },
        { ResponseEmotion.Calm, "😌" },
        { ResponseEmotion.Confused, "🤔" },
        { ResponseEmotion.Proud, "😎" },
        { ResponseEmotion.Ashamed, "😳" },
        { ResponseEmotion.Grateful, "🙏" },
        { ResponseEmotion.Disappointed, "😞" },
        { ResponseEmotion.Frustrated, "😤" },
        { ResponseEmotion.Amused, "😄" },
        { ResponseEmotion.Bored, "😑" },
        { ResponseEmotion.Curious, "🧐" },
        { ResponseEmotion.Confident, "💪" },
        { ResponseEmotion.Insecure, "🥺" },
        { ResponseEmotion.Content, "😊" },
        { ResponseEmotion.Overwhelmed, "😵" },
        { ResponseEmotion.Relieved, "😅" },
        { ResponseEmotion.Empathetic, "🤗" },
        { ResponseEmotion.Indifferent, "🤷" },
        { ResponseEmotion.Optimistic, "🌟" },
        { ResponseEmotion.Pessimistic, "☁️" },
        { ResponseEmotion.Stressed, "😫" },
        { ResponseEmotion.Relaxed, "😎" },
        { ResponseEmotion.Enthusiastic, "🎉" },
        { ResponseEmotion.Irritated, "😒" },
        { ResponseEmotion.Satisfied, "😌" },
        { ResponseEmotion.Inspired, "💡" },
        { ResponseEmotion.Uncomfortable, "😖" },
        { ResponseEmotion.Regretful, "😔" },
        { ResponseEmotion.Sarcastic, "😏" },
        { ResponseEmotion.Sympathetic, "💕" },
        { ResponseEmotion.Skeptical, "🤨" },
        { ResponseEmotion.Hopeful, "🤞" },
        { ResponseEmotion.Defensive, "🛡️" },
        { ResponseEmotion.Agreeable, "👌" },
        { ResponseEmotion.Disagreeable, "🙅" },
        { ResponseEmotion.Sexual, "🥰" }
    };

        public string GetEmoji(ResponseEmotion emotion)
        {
            return EmojiMap.TryGetValue(emotion, out string emoji) ? emoji : string.Empty;
        }


    }
}
