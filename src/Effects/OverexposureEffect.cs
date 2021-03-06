using Vintagestory.Client.NoObf;
using VolumetricShading.Patch;

namespace VolumetricShading.Effects
{
    public class OverexposureEffect
    {
        private int _currentBloom;

        public OverexposureEffect(VolumetricShadingMod mod)
        {
            mod.CApi.Settings.AddWatcher<int>("volumetricshading_sunBloomIntensity", OnSunBloomChanged);
            _currentBloom = ModSettings.SunBloomIntensity;

            mod.Events.PreSunRender += OnRenderSun;
            
            RegisterInjectorProperties(mod);
        }

        private void RegisterInjectorProperties(VolumetricShadingMod mod)
        {
            var injector = mod.ShaderInjector;

            injector.RegisterFloatProperty("VSMOD_OVEREXPOSURE",
                () => ModSettings.OverexposureIntensity * 0.01f);

            injector.RegisterBoolProperty("VSMOD_OVEREXPOSURE_ENABLED",
                () => ModSettings.OverexposureIntensity > 0);
        }

        private void OnSunBloomChanged(int bloom)
        {
            _currentBloom = bloom;
        }

        public void OnRenderSun(ShaderProgramStandard shader)
        {
            shader.Uniform("extraOutGlow", _currentBloom * 0.01f);
        }

        public void OnRenderedSun()
        {
            var shader = ShaderPrograms.Standard;
            shader.Use();
            shader.Uniform("extraOutGlow", 0.0f);
            shader.Stop();
        }
    }
}