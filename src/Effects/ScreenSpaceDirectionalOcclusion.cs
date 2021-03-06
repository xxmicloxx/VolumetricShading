using VolumetricShading.Patch;

namespace VolumetricShading.Effects
{
    public class ScreenSpaceDirectionalOcclusion
    {
        public ScreenSpaceDirectionalOcclusion(VolumetricShadingMod mod)
        {
            var injector = mod.ShaderInjector;
            injector.RegisterBoolProperty("SSDO", () => ModSettings.SSDOEnabled);
        }
    }
}