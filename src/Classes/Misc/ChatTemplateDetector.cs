using MousyHub.Models.Model;

namespace MousyHub.Classes.Misc
{
    public static class ChatTemplateDetector
    {
        public static Instruct? FindMatchingInstruct(string chatTemplate, List<Instruct> instructs)
        {
            if (string.IsNullOrEmpty(chatTemplate) || instructs == null || instructs.Count == 0)
                return null;

            Instruct bestMatch = null;
            int maxScore = 0;

            foreach (var instruct in instructs)
            {
                int currentScore = CalculateMatchScore(chatTemplate, instruct);

                if (currentScore > maxScore)
                {
                    maxScore = currentScore;
                    bestMatch = instruct;
                }
                else if (currentScore == maxScore && currentScore > 0)
                {
                    // Дополнительные критерии при равенстве баллов
                    bestMatch = ResolveTie(chatTemplate, bestMatch, instruct);
                }
            }
            return maxScore > 0 ? bestMatch : null;
        }

        private static int CalculateMatchScore(string chatTemplate, Instruct instruct)
        {
            int score = 0;
            var fieldsToCheck = new List<string>
        {
            instruct.input_sequence,
            instruct.output_sequence,
            instruct.system_sequence,
            instruct.stop_sequence,
            instruct.input_sequence,
            instruct.output_sequence,
            instruct.system_suffix,
        };

            foreach (var field in fieldsToCheck)
            {
                if (!string.IsNullOrEmpty(field))
                {
                    string trimmedField = field.Trim();
                    if (!string.IsNullOrEmpty(trimmedField) && chatTemplate.Contains(trimmedField))
                    {
                        score += trimmedField.Length;
                    }
                }
            }

            return score;
        }
        private static Instruct ResolveTie(string chatTemplate, Instruct currentBest, Instruct candidate)
        {
            // Приоритет у Instruct с более длинным совпадением в ключевых полях
            int currentBestScore = CalculateMatchScore(chatTemplate, currentBest);
            int candidateScore = CalculateMatchScore(chatTemplate, candidate);

            if (candidateScore > currentBestScore)
                return candidate;

            // Если баллы равны, приоритет у Instruct с более длинным совпадением в InputSequence
            if (candidateScore == currentBestScore)
            {
                int currentBestInputSequenceLength = currentBest.input_sequence?.Length ?? 0;
                int candidateInputSequenceLength = candidate.input_sequence?.Length ?? 0;

                if (candidateInputSequenceLength > currentBestInputSequenceLength)
                    return candidate;
            }

            return currentBest;
        }
    }
}
