using System;
using System.Text;
using System.Text.RegularExpressions;

namespace VolumetricShading.Patch
{
    public class RegexPatch : TargetedPatch
    {
        public delegate void ReplacementFunction(StringBuilder sb, Match match);
        
        public Regex Regex { get; }

        public bool Optional;
        public bool Multiple;

        public string ReplacementString;
        public ReplacementFunction DoReplace;

        public RegexPatch(Regex regex)
        {
            Regex = regex;
            DoReplace = DefaultReplace;
        }

        public RegexPatch(string filename, Regex regex)
            : this(regex)
        {
            TargetFile = filename;
            ExactFilename = true;
        }

        public RegexPatch(string pattern, RegexOptions options = RegexOptions.IgnoreCase) 
            : this(new Regex(pattern, options))
        {
        }
        
        public RegexPatch(string filename, string pattern, RegexOptions options = RegexOptions.IgnoreCase) 
            : this(filename, new Regex(pattern, options))
        {
        }

        public override string Patch(string filename, string code)
        {
            var match = Regex.Match(code);
            if (!match.Success)
            {
                if (!Optional)
                {
                    throw new InvalidOperationException(
                        $"Could not execute non-optional patch: Regex {Regex} not matched");
                }

                return code;
            }
            
            var sb = new StringBuilder(code.Length);
            var startIndex = 0;
            
            do
            {
                if (match.Index != startIndex)
                    sb.Append(code, startIndex, match.Index - startIndex);

                startIndex = match.Index + match.Length;
                DoReplace(sb, match);
                match = match.NextMatch();
            } while (match.Success && Multiple);

            if (match.Success)
            {
                // we have multiple matches, but should only have one
                throw new InvalidOperationException($"Multiple regex matches, but only one wanted: {Regex}");
            }

            if (startIndex < code.Length)
                sb.Append(code, startIndex, code.Length - startIndex);

            return sb.ToString();
        }

        private void DefaultReplace(StringBuilder sb, Match match)
        {
            sb.Append(ReplacementString ?? "");
        }
    }
}