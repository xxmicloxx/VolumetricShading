using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class VolumetricLighting
    {
        private readonly VolumetricShadingMod _mod;
        private readonly ClientPlatformAbstract _platform;

        public VolumetricLighting(VolumetricShadingMod mod)
        {
            _mod = mod;
            _platform = mod.CApi.GetClientPlatformAbstract();

            _mod.CApi.Settings.AddWatcher<int>("shadowMapQuality", OnShadowMapChanged);
            _mod.CApi.Settings.AddWatcher<int>("godRays", OnGodRaysChanged);

            _mod.Events.PreGodraysRender += OnSetGodrayUniforms;

            RegisterInjectorProperties();
        }

        private void RegisterInjectorProperties()
        {
            var injector = _mod.ShaderInjector;

            injector.RegisterFloatProperty("VOLUMETRIC_FLATNESS", () =>
            {
                var volFlatnessInt = ModSettings.VolumetricLightingFlatness;
                return (200 - volFlatnessInt) * 0.01f;
            });

            injector.RegisterFloatProperty("VOLUMETRIC_INTENSITY",
                () => ModSettings.VolumetricLightingIntensity * 0.01f);
        }

        private static void OnShadowMapChanged(int quality)
        {
            if (quality == 0)
            {
                // turn off VL
                ClientSettings.GodRayQuality = 0;
            }
        }

        private void OnGodRaysChanged(int quality)
        {
            if (quality != 1 || ClientSettings.ShadowMapQuality != 0) return;

            // turn on shadow mapping
            ClientSettings.ShadowMapQuality = 1;
            _mod.CApi.GetClientPlatformAbstract().RebuildFrameBuffers();
        }

        public void OnSetGodrayUniforms(ShaderProgramGodrays rays)
        {
            // custom uniform calls
            var calendar = _mod.CApi.World.Calendar;
            var dropShadowIntensityObj = typeof(AmbientManager)
                .GetField("DropShadowIntensity", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(_mod.CApi.Ambient);

            if (dropShadowIntensityObj == null)
            {
                _mod.Mod.Logger.Fatal("DropShadowIntensity not found!");
                return;
            }

            var dropShadowIntensity = (float) dropShadowIntensityObj;

            rays.Uniform("moonLightStrength", calendar.MoonLightStrength);
            rays.Uniform("sunLightStrength", calendar.SunLightStrength);
            rays.Uniform("dayLightStrength", calendar.DayLightStrength);
            rays.Uniform("shadowIntensity", dropShadowIntensity);
            rays.Uniform("flatFogDensity", _mod.CApi.Ambient.BlendedFlatFogDensity);
            rays.Uniform("temperature", _mod.CApi.Render.ShaderUniforms.SeasonTemperature);
        }
    }
}