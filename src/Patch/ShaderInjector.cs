using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace VolumetricShading.Patch
{
    public class ShaderInjector
    {
        public bool Debug = true;
        public IList<IShaderProperty> ShaderProperties { get; }
        public IDictionary<string, string> GeneratedValues { get; }

        private static readonly Regex GeneratedRegex = new Regex("^\\s*#\\s*generated\\s+(.*)$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public ShaderInjector()
        {
            ShaderProperties = new List<IShaderProperty>();
            GeneratedValues = new Dictionary<string, string>();

            this.RegisterStaticProperty("#define VOLUMETRICSHADINGMOD\r\n");
        }

        public void RegisterShaderProperty(IShaderProperty property)
        {
            ShaderProperties.Add(property);
        }

        public string this[string key]
        {
            get => GeneratedValues[key];
            set => GeneratedValues[key] = value;
        }

        public void OnShaderLoaded(ShaderProgram program, EnumShaderType shaderType)
        {
            var ext = ".unknown";
            Shader shader = null;
            switch (shaderType)
            {
                case EnumShaderType.FragmentShader:
                    shader = program.FragmentShader;
                    ext = ".frag";
                    break;
                case EnumShaderType.VertexShader:
                    shader = program.VertexShader;
                    ext = ".vert";
                    break;
                case EnumShaderType.GeometryShader:
                    shader = program.GeometryShader;
                    ext = ".geom";
                    break;
            }

            if (shader == null) return;

            var prefixBuilder = new StringBuilder();
            foreach (var property in ShaderProperties)
            {
                prefixBuilder.Append(property.GenerateOutput());
            }

            shader.PrefixCode += prefixBuilder.ToString();

            shader.Code = HandleGenerated(shader.Code);

            if (!Debug) return;
            
            var targetPath = Path.Combine(GamePaths.DataPath, "ShaderDebug");
            Directory.CreateDirectory(targetPath);
            targetPath = Path.Combine(targetPath, program.PassName + ext);
            
            var text = shader.Code;
            var startIndex = text.IndexOf("\n", Math.Max(0, text.IndexOf("#version", StringComparison.Ordinal)),
                StringComparison.Ordinal) + 1;
            
            text = text.Insert(startIndex, shader.PrefixCode);
            File.WriteAllText(targetPath, text);
        }

        private string HandleGenerated(string code)
        {
            return GeneratedRegex.Replace(code, InsertGenerated);
        }

        private string InsertGenerated(Match match)
        {
            var key = match.Groups[1].Value.Trim();
            return GeneratedValues[key];
        }
    }
}