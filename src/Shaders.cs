using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public static class Shaders
    {
        public static IShaderProgram RegisterShader(this VolumetricShadingMod mod, string name, ref bool success)
        {
            var shader = (ShaderProgram) mod.CApi.Shader.NewShaderProgram();
            shader.AssetDomain = mod.Mod.Info.ModID;
            mod.CApi.Shader.RegisterFileShaderProgram(name, shader);
            if (!shader.Compile()) success = false;
            return shader;
        }
    }
}