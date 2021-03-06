using System;
using System.Text;
using System.Text.RegularExpressions;

namespace VolumetricShading.Patch
{
    public class FunctionExtractor
    {
        private static readonly Regex FunctionPrototypeRegex =
            new Regex("^\\S+\\s+([a-z_][a-z0-9_]*)\\s*\\([^)]*\\)\\s*{$",
                RegexOptions.Singleline | RegexOptions.IgnoreCase);

        private readonly StringBuilder _sb = new StringBuilder();

        public string ExtractedContent => _sb.ToString();

        public bool Extract(string code, string functionName)
        {
            var sb = new StringBuilder();
            var depth = 0;
            var comment = false;
            var multilineComment = false;
            var preprocessor = false;
            var preprocessorEscaped = false;
            var lastChar = '\0';
            var lineEmpty = true;
            var isCapturing = false;

            foreach (var chr in code)
            {
                if (preprocessor)
                {
                    switch (chr)
                    {
                        case '\\':
                            preprocessorEscaped = true;
                            break;
                        case '\n' when !preprocessorEscaped:
                            preprocessor = false;
                            break;
                        default:
                        {
                            if (chr != '\r')
                            {
                                preprocessorEscaped = false;
                            }

                            break;
                        }
                    }
                }
                else if (multilineComment)
                {
                    if (chr == '/' && lastChar == '*')
                    {
                        multilineComment = false;
                    }
                }
                else if (comment)
                {
                    if (chr == '\n')
                    {
                        comment = false;
                    }
                }
                else
                {
                    if (depth == 0 || isCapturing)
                        sb.Append(chr);
                    
                    switch (chr)
                    {
                        // actual code
                        case '#' when lineEmpty:
                            preprocessor = true;
                            sb.Remove(sb.Length - 1, 1);
                            break;
                        case '/' when lastChar == '/':
                            comment = true;
                            sb.Remove(sb.Length - 2, 2);
                            break;
                        case '*' when lastChar == '/':
                            multilineComment = true;
                            sb.Remove(sb.Length - 2, 2);
                            break;
                        case ';' when depth == 0:
                            sb.Clear();
                            break;
                        case '{' when depth > 0:
                            depth++;
                            break;
                        case '}':
                        {
                            depth--;
                            if (depth < 0)
                            {
                                throw new InvalidOperationException("Depth got too low - parsing error");
                            }

                            if (depth == 0)
                            {
                                if (isCapturing)
                                {
                                    _sb.Append(sb);
                                    _sb.Append('\n');
                                    return true;
                                }
                            
                                sb.Clear();
                            }

                            break;
                        }
                        case '{':
                        {
                            // depth is 0
                            // sb should now contain function prototype
                            var match = FunctionPrototypeRegex.Match(sb.ToString().Trim());
                            if (!match.Success)
                            {
                                throw new InvalidOperationException("Parsing error - function header doesn't match.");
                            }

                            var name = match.Groups[1].Value;
                            isCapturing = name == functionName;
                            depth++;
                            break;
                        }
                    }
                }
                
                if (chr == '\n')
                {
                    lineEmpty = true;
                } else if (!char.IsWhiteSpace(chr))
                {
                    lineEmpty = false;
                }

                lastChar = chr;
            }

            return false;
        }
    }
}