using System.Reflection;
using Vintagestory.Client.NoObf;
using VolumetricShading.Patch;

namespace VolumetricShading.Effects
{
    public class VolumetricLighting
    {
        private readonly VolumetricShadingMod _mod;
        private readonly ClientMain _game;
        private readonly FieldInfo _dropShadowIntensityField;
        private bool _enabled;

        public VolumetricLighting(VolumetricShadingMod mod)
        {
            _mod = mod;
            _game = _mod.CApi.GetClient();

            _dropShadowIntensityField = typeof(AmbientManager)
                .GetField("DropShadowIntensity", BindingFlags.NonPublic | BindingFlags.Instance);

            _enabled = ClientSettings.GodRayQuality > 0;
            
            _mod.CApi.Settings.AddWatcher<int>("shadowMapQuality", OnShadowMapChanged);
            _mod.CApi.Settings.AddWatcher<int>("godRays", OnGodRaysChanged);

            _mod.Events.PreGodraysRender += OnSetGodrayUniforms;
            _mod.Events.PostUseShader += OnPostUseShader;

            RegisterPatches();
        }

        private void RegisterPatches()
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
            _enabled = quality > 0;
            
            if (quality != 1 || ClientSettings.ShadowMapQuality != 0) return;

            // turn on shadow mapping
            ClientSettings.ShadowMapQuality = 1;
            _mod.CApi.GetClientPlatformAbstract().RebuildFrameBuffers();
        }

        public void OnSetGodrayUniforms(ShaderProgramGodrays rays)
        {
            // custom uniform calls
            var calendar = _mod.CApi.World.Calendar;
            var ambient = _mod.CApi.Ambient;
            var uniforms = _mod.CApi.Render.ShaderUniforms;
            var myUniforms = _mod.Uniforms;
            var dropShadowIntensityObj = _dropShadowIntensityField?.GetValue(_mod.CApi.Ambient);

            if (dropShadowIntensityObj == null)
            {
                _mod.Mod.Logger.Fatal("DropShadowIntensity not found!");
                return;
            }

            var dropShadowIntensity = (float) dropShadowIntensityObj;
            
            var playerWaterDepth = _game.playerProperties.EyesInWaterDepth;

            rays.Uniform("moonLightStrength", calendar.MoonLightStrength);
            rays.Uniform("sunLightStrength", calendar.SunLightStrength);
            rays.Uniform("dayLightStrength", calendar.DayLightStrength);
            rays.Uniform("shadowIntensity", dropShadowIntensity);
            rays.Uniform("flatFogDensity", ambient.BlendedFlatFogDensity);
            rays.Uniform("playerWaterDepth", playerWaterDepth);
            rays.Uniform("fogColor", ambient.BlendedFogColor);
            rays.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
            rays.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
        }
        
        private void OnPostUseShader(ShaderProgramBase shader)
        {
            if (!_enabled) return;
            if (!shader.includes.Contains("shadowcoords.vsh")) return;
            
            shader.Uniform("cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        }
    }
}