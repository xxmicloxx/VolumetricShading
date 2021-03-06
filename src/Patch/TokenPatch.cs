using System.Text;
using System.Text.RegularExpressions;

namespace VolumetricShading.Patch
{
    public class TokenPatch : RegexPatch
    {
        private const string TokenSeparators = ".,+-*/;{}[]()=:|^&?#";
        private const string StartToken = "(^|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])";
        private const string EndToken = "($|[\\.,+\\-*/;{}[\\]()=:|^&?#\\s])";
        private const string OptionalRegexSeparator = "\\s*?";
        private const string RegexSeparator = "\\s+?";

        private static Regex BuildRegex(string tokenStr)
        {
            var sb = new StringBuilder(tokenStr.Length);
            // remove unnecessary spaces around tokens
            
            var lastIsSpace = false;
            var lastIsToken = false;
            foreach (var token in tokenStr)
            {
                if (char.IsWhiteSpace(token))
                {
                    if (lastIsSpace || lastIsToken) continue;
                    
                    sb.Append(' ');
                    lastIsSpace = true;
                } else if (TokenSeparators.Contains(token.ToString()))
                {
                    if (lastIsSpace)
                        sb.Remove(sb.Length - 1, 1);

                    sb.Append(token);
                    lastIsSpace = false;
                    lastIsToken = true;
                }
                else
                {
                    sb.Append(token);
                    lastIsSpace = false;
                    lastIsToken = false;
                }
            }

            var extendedTokenStr = sb.ToString().Trim();
            sb.Clear();
            sb.Append(StartToken);

            // start building regex
            var wasSeparator = true;
            var lastLength = 0;
            foreach (var token in extendedTokenStr)
            {
                var regexToken = Regex.Escape(token.ToString());
                
                if (token == ' ')
                {
                    if (wasSeparator && lastLength > 0)
                        sb.Remove(sb.Length - lastLength, lastLength);
                    
                    sb.Append(RegexSeparator);
                    wasSeparator = true;
                    lastLength = RegexSeparator.Length;
                } else if (TokenSeparators.Contains(token.ToString()))
                {
                    if (lastLength != 0 && !wasSeparator)
                        sb.Append(OptionalRegexSeparator);
                    
                    sb.Append(regexToken);
                    sb.Append(OptionalRegexSeparator);
                    
                    wasSeparator = true;
                    lastLength = OptionalRegexSeparator.Length;
                }
                else
                {
                    sb.Append(regexToken);
                    wasSeparator = false;
                    lastLength = regexToken.Length;
                }
            }
            
            if (wasSeparator && lastLength > 0)
                sb.Remove(sb.Length - lastLength, lastLength);

            sb.Append(EndToken);
            return new Regex(sb.ToString(), RegexOptions.IgnoreCase);
        }

        public TokenPatch(string tokenString)
            : base(BuildRegex(tokenString))
        {
            DoReplace = TokenReplace;
        }
        
        public TokenPatch(string filename, string tokenString)
            : base(filename, BuildRegex(tokenString))
        {
            DoReplace = TokenReplace;
        }

        private void TokenReplace(StringBuilder sb, Match match)
        {
            sb.Append(match.Groups[1].Value);
            sb.Append(ReplacementString ?? "");
            sb.Append(match.Groups[2].Value);
        }
    }
}