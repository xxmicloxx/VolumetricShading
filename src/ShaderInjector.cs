using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class ShaderInjector
    {
        public IList<IShaderProperty> ShaderProperties { get; }

        public ShaderInjector()
        {
            ShaderProperties = new List<IShaderProperty>();

            this.RegisterStaticProperty("#define VOLUMETRICSHADINGMOD\r\n");
        }

        public void RegisterShaderProperty(IShaderProperty property)
        {
            ShaderProperties.Add(property);
        }

        public void OnShaderLoaded(ShaderProgram program, EnumShaderType shaderType)
        {
            Shader shader = null;
            if (shaderType == EnumShaderType.FragmentShader)
            {
                shader = program.FragmentShader;
            }
            else if (shaderType == EnumShaderType.VertexShader)
            {
                shader = program.VertexShader;
            }
            else if (shaderType == EnumShaderType.GeometryShader)
            {
                shader = program.GeometryShader;
            }

            if (shader == null) return;

            var prefixBuilder = new StringBuilder();
            foreach (var property in ShaderProperties)
            {
                prefixBuilder.Append(property.GenerateOutput());
            }

            shader.PrefixCode += prefixBuilder.ToString();
        }
    }
}