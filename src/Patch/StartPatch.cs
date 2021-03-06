using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace VolumetricShading.Patch
{
    public class StartPatch : TargetedPatch
    {
        public string Content;
        public static readonly Regex SkipLineRegex =
            new Regex("^\\s*?#\\s*?(?:version|extension)", RegexOptions.IgnoreCase);

        public StartPatch() {}

        public StartPatch(string filename)
        {
            TargetFile = filename;
            ExactFilename = true;
        }

        public override string Patch(string filename, string code)
        {
            var sb = new StringBuilder(code.Length);
            using (var reader = new StringReader(code))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (SkipLineRegex.IsMatch(line))
                    {
                        sb.AppendLine(line);
                        continue;
                    }

                    // insert here
                    sb.AppendLine(Content);
                    sb.AppendLine(line);
                    break;
                }

                sb.Append(reader.ReadToEnd());
            }
            
            return sb.ToString();
        }
    }
}