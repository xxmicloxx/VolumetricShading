using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class ShaderInjector
    {
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

            var waterTransparency =
                ((100 - ModSettings.SSRWaterTransparency) * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);

            var reflectionDimming =
                (ModSettings.SSRReflectionDimming * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);

            var tintInfluence = (ModSettings.SSRTintInfluence * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);

            var skyMixin = (ModSettings.SSRSkyMixin * 0.01f).ToString("0.00", CultureInfo.InvariantCulture);
                                
            shader.PrefixCode += "#define VOLUMETRICSHADINGMOD\r\n";
            shader.PrefixCode += "#define VSMOD_SSR " + (ModSettings.ScreenSpaceReflectionsEnabled ? "1" : "0") + "\r\n";
            shader.PrefixCode += $"#define VOLUMETRIC_FLATNESS {volFlatness}f\r\n";
            shader.PrefixCode += $"#define VOLUMETRIC_INTENSITY {volIntensity}\r\n";
            shader.PrefixCode += "#define SSDO " + (ModSettings.SSDOEnabled ? "1" : "0") + "\r\n";
            shader.PrefixCode += $"#define VSMOD_SSR_WATER_TRANSPARENCY {waterTransparency}\r\n";
            shader.PrefixCode += $"#define VSMOD_SSR_REFLECTION_DIMMING {reflectionDimming}\r\n";
            shader.PrefixCode += $"#define VSMOD_SSR_TINT_INFLUENCE {tintInfluence}\r\n";
            shader.PrefixCode += $"#define VSMOD_SSR_SKY_MIXIN {skyMixin}\r\n";
        }
    }
}
