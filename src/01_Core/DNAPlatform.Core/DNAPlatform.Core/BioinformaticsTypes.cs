using System;
using System.Text;

namespace DNAPlatform.Core.CSnakes
{
    /// <summary>
    /// Bioinformatics Sequence Metrics computed across the polyglot C#/Python engine.
    /// </summary>
    public class DnaSequenceMetrics
    {
        public int Length { get; set; }
        public double GcContent { get; set; }
        public double GcSkew { get; set; }
        public string ReverseComplement { get; set; } = string.Empty;
        public string Engine { get; set; } = "C# Core";

        public static DnaSequenceMetrics ComputeFromCSharp(string sequence)
        {
            if (string.IsNullOrEmpty(sequence))
                return new DnaSequenceMetrics();

            var upper = sequence.ToUpperInvariant();
            int g = 0, c = 0, a = 0, t = 0;
            var sb = new StringBuilder(upper.Length);

            for (int i = upper.Length - 1; i >= 0; i--)
            {
                char ch = upper[i];
                switch (ch)
                {
                    case 'A': a++; sb.Append('T'); break;
                    case 'T': t++; sb.Append('A'); break;
                    case 'C': c++; sb.Append('G'); break;
                    case 'G': g++; sb.Append('C'); break;
                    default: sb.Append(ch); break;
                }
            }

            double totalGc = g + c;
            double gcPct = upper.Length > 0 ? (totalGc / upper.Length) * 100.0 : 0.0;
            double skew = totalGc > 0 ? (double)(g - c) / totalGc : 0.0;

            return new DnaSequenceMetrics
            {
                Length = upper.Length,
                GcContent = Math.Round(gcPct, 2),
                GcSkew = Math.Round(skew, 4),
                ReverseComplement = sb.ToString(),
                Engine = "DNA Platform C# Core Engine"
            };
        }
    }
}
