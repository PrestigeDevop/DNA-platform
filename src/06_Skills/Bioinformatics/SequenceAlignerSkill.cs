using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.Bioinformatics
{
    /// <summary>
    /// Sequence Aligner Skill - Performs Needleman-Wunsch global alignment
    /// </summary>
    public class SequenceAlignerSkill : ISkill
    {
        public string SkillId => "sequence-aligner";
        public string Name => "Sequence Aligner";
        public string Description => "Align two DNA sequences using Needleman-Wunsch algorithm";
        public string Category => "Bioinformatics";
        public string Icon => "🔬";

        private const int MatchScore = 2;
        private const int MismatchScore = -1;
        private const int GapPenalty = -2;

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("sequence1", out var seq1Obj) || seq1Obj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'sequence1'");

                if (!inputs.TryGetValue("sequence2", out var seq2Obj) || seq2Obj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'sequence2'");

                string sequence1 = seq1Obj.ToString()?.ToUpper() ?? string.Empty;
                string sequence2 = seq2Obj.ToString()?.ToUpper() ?? string.Empty;

                if (string.IsNullOrEmpty(sequence1) || string.IsNullOrEmpty(sequence2))
                    return SkillOutput.CreateError("Sequences cannot be empty");

                var result = await AlignSequences(sequence1, sequence2);
                return SkillOutput.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"Alignment error: {ex.Message}");
            }
        }

        private Task<Dictionary<string, object>> AlignSequences(string seq1, string seq2)
        {
            int m = seq1.Length;
            int n = seq2.Length;

            int[,] scoreMatrix = new int[m + 1, n + 1];

            for (int i = 0; i <= m; i++)
                scoreMatrix[i, 0] = i * GapPenalty;
            for (int j = 0; j <= n; j++)
                scoreMatrix[0, j] = j * GapPenalty;

            for (int i = 1; i <= m; i++)
            {
                for (int j = 1; j <= n; j++)
                {
                    int match = scoreMatrix[i - 1, j - 1] + GetScore(seq1[i - 1], seq2[j - 1]);
                    int delete = scoreMatrix[i - 1, j] + GapPenalty;
                    int insert = scoreMatrix[i, j - 1] + GapPenalty;
                    scoreMatrix[i, j] = Math.Max(Math.Max(match, delete), insert);
                }
            }

            string aligned1 = string.Empty;
            string aligned2 = string.Empty;
            int x = m, y = n;

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && scoreMatrix[x, y] == scoreMatrix[x - 1, y - 1] + GetScore(seq1[x - 1], seq2[y - 1]))
                {
                    aligned1 = seq1[x - 1] + aligned1;
                    aligned2 = seq2[y - 1] + aligned2;
                    x--;
                    y--;
                }
                else if (x > 0 && scoreMatrix[x, y] == scoreMatrix[x - 1, y] + GapPenalty)
                {
                    aligned1 = seq1[x - 1] + aligned1;
                    aligned2 = "-" + aligned2;
                    x--;
                }
                else
                {
                    aligned1 = "-" + aligned1;
                    aligned2 = seq2[y - 1] + aligned2;
                    y--;
                }
            }

            int matches = 0, mismatches = 0, gaps = 0;
            for (int i = 0; i < aligned1.Length; i++)
            {
                if (aligned1[i] == aligned2[i]) matches++;
                else if (aligned1[i] == '-' || aligned2[i] == '-') gaps++;
                else mismatches++;
            }

            double identity = aligned1.Length > 0 ? (double)matches / aligned1.Length : 0;

            return Task.FromResult(new Dictionary<string, object>
            {
                { "aligned_sequence1", aligned1 },
                { "aligned_sequence2", aligned2 },
                { "score", scoreMatrix[m, n] },
                { "matches", matches },
                { "mismatches", mismatches },
                { "gaps", gaps },
                { "alignment_length", aligned1.Length },
                { "identity_percentage", Math.Round(identity * 100, 2) },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            });
        }

        private int GetScore(char a, char b)
        {
            return a == b ? MatchScore : MismatchScore;
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "sequence1", "string - First DNA sequence (required)" },
                { "sequence2", "string - Second DNA sequence (required)" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "aligned_sequence1", "string - Aligned first sequence" },
                { "aligned_sequence2", "string - Aligned second sequence" },
                { "score", "int - Alignment score" },
                { "matches", "int - Number of matches" },
                { "mismatches", "int - Number of mismatches" },
                { "gaps", "int - Number of gaps" },
                { "alignment_length", "int - Total alignment length" },
                { "identity_percentage", "float - Percentage identity" },
                { "timestamp", "string - Execution timestamp" }
            };
        }

        public SkillMetadata GetMetadata()
        {
            return new SkillMetadata
            {
                SkillId = SkillId,
                Name = Name,
                Description = Description,
                Category = Category,
                Icon = Icon,
                InputSchema = GetInputSchema(),
                OutputSchema = GetOutputSchema(),
                Version = "1.0.0"
            };
        }
    }
}
