using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class ShaderInjector
    {
        private readonly VolumetricShadingMod _mod;

        public ShaderInjector(VolumetricShadingMod mod)
        {
            _mod = mod;
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

            if (shader == null) return;

            var volFlatnessInt = ModSettings.VolumetricLightingFlatness;
            var volFlatness = ((200 - volFlatnessInt) * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);

            var volIntensity = (ModSettings.VolumetricLightingIntensity * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);

            shader.PrefixCode += "#define VOLUMETRICSHADINGMOD\r\n";
            shader.PrefixCode += "#define VSMOD_SSR " + (ModSettings.ScreenSpaceReflectionsEnabled ? "1" : "0") + "\r\n";
            shader.PrefixCode += "#define VOLUMETRIC_FLATNESS " + volFlatness + "f\r\n";
            shader.PrefixCode += "#define VOLUMETRIC_INTENSITY " + volIntensity + "\r\n";
            shader.PrefixCode += "#define SSDO " + (ModSettings.SSDOEnabled ? "1" : "0") + "\r\n";
        }
    }
}
