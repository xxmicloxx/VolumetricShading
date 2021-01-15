namespace VolumetricShading
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