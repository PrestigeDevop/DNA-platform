using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DNAPlatform.Skills;

namespace DNAPlatform.Skills.Bioinformatics
{
    /// <summary>
    /// FASTA Loader Skill - Loads and parses FASTA format files
    /// </summary>
    public class FastaLoaderSkill : ISkill
    {
        public string SkillId => "fasta-loader";
        public string Name => "FASTA Loader";
        public string Description => "Load and parse FASTA format sequence files from disk or URL";
        public string Category => "Bioinformatics";
        public string Icon => "🧬";

        public async Task<SkillOutput> Execute(Dictionary<string, object>? inputs = null)
        {
            try
            {
                if (inputs == null)
                    return SkillOutput.CreateError("No inputs provided");

                if (!inputs.TryGetValue("source", out var sourceObj) || sourceObj == null)
                    return SkillOutput.CreateError("Missing required parameter: 'source'");

                string source = sourceObj.ToString() ?? string.Empty;
                bool validateSequences = true;

                if (inputs.TryGetValue("validate", out var validateObj) && validateObj != null)
                    bool.TryParse(validateObj.ToString(), out validateSequences);

                var result = await LoadFasta(source, validateSequences);
                return SkillOutput.CreateSuccess(result);
            }
            catch (Exception ex)
            {
                return SkillOutput.CreateError($"FASTA loading error: {ex.Message}");
            }
        }

        private async Task<Dictionary<string, object>> LoadFasta(string source, bool validate)
        {
            string fastaContent;

            if (File.Exists(source))
            {
                fastaContent = await File.ReadAllTextAsync(source);
            }
            else if (source.StartsWith("http://") || source.StartsWith("https://"))
            {
                using var client = new System.Net.Http.HttpClient();
                fastaContent = await client.GetStringAsync(source);
            }
            else
            {
                fastaContent = source;
            }

            var sequences = ParseFasta(fastaContent, validate);

            return new Dictionary<string, object>
            {
                { "source_type", File.Exists(source) ? "file" : (source.StartsWith("http") ? "url" : "content") },
                { "total_sequences", sequences.Count },
                { "total_bases", sequences.Sum(s => s.Length) },
                { "sequences", sequences },
                { "timestamp", DateTime.UtcNow.ToString("o") }
            };
        }

        private List<FastaSequence> ParseFasta(string content, bool validate)
        {
            var sequences = new List<FastaSequence>();
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string currentHeader = string.Empty;
            var currentSequence = new System.Text.StringBuilder();

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                if (trimmedLine.StartsWith(">"))
                {
                    if (currentSequence.Length > 0)
                    {
                        sequences.Add(CreateFastaSequence(currentHeader, currentSequence.ToString(), validate));
                        currentSequence.Clear();
                    }
                    currentHeader = trimmedLine[1..].Trim();
                }
                else
                {
                    currentSequence.Append(trimmedLine);
                }
            }

            if (currentSequence.Length > 0)
            {
                sequences.Add(CreateFastaSequence(currentHeader, currentSequence.ToString(), validate));
            }

            return sequences;
        }

        private FastaSequence CreateFastaSequence(string header, string sequence, bool validate)
        {
            var seq = new FastaSequence
            {
                Header = header,
                Sequence = sequence,
                Length = sequence.Length
            };

            var parts = header.Split(' ', 2);
            seq.Id = parts[0];
            seq.Description = parts.Length > 1 ? parts[1] : string.Empty;

            int gcCount = sequence.Count(c => c == 'G' || c == 'C' || c == 'g' || c == 'c');
            seq.GcContent = sequence.Length > 0 ? (double)gcCount / sequence.Length : 0;

            if (validate)
            {
                seq.IsValid = IsValidDnaSequence(sequence);
                seq.ValidationMessage = seq.IsValid ? "Valid DNA sequence" : "Invalid characters found";
            }

            return seq;
        }

        private bool IsValidDnaSequence(string sequence)
        {
            const string validChars = "ACGTURYKMSWBDHVNXacgturykmswbdhvnx-";
            return sequence.All(c => validChars.Contains(c));
        }

        public Dictionary<string, string> GetInputSchema()
        {
            return new Dictionary<string, string>
            {
                { "source", "string - File path, URL, or raw FASTA content (required)" },
                { "validate", "bool - Whether to validate sequences (default: true)" }
            };
        }

        public Dictionary<string, string> GetOutputSchema()
        {
            return new Dictionary<string, string>
            {
                { "source_type", "string - Type of source: file, url, content" },
                { "total_sequences", "int - Number of sequences loaded" },
                { "total_bases", "int - Total base count" },
                { "sequences", "array - Parsed FASTA sequences" },
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

    /// <summary>
    /// Represents a parsed FASTA sequence
    /// </summary>
    public class FastaSequence
    {
        public string Id { get; set; } = string.Empty;
        public string Header { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Sequence { get; set; } = string.Empty;
        public int Length { get; set; }
        public double GcContent { get; set; }
        public bool IsValid { get; set; }
        public string? ValidationMessage { get; set; }
    }
}