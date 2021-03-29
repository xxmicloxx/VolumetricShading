using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using VolumetricShading.Patch;

namespace VolumetricShading.Effects
{
    public class ShadowTweaks
    {
        private readonly VolumetricShadingMod _mod;
        private bool _softShadowsEnabled;
        private int _softShadowSamples;
        public int NearShadowBaseWidth { get; private set; }
        public ISet<string> ExcludedShaders { get; }

        public ShadowTweaks(VolumetricShadingMod mod)
        {
            _mod = mod;
            ExcludedShaders = new HashSet<string> { "sky", "clouds", "gui", "guigear", "guitopsoil", "texture2texture" };

            _mod.CApi.Settings.AddWatcher<int>("volumetricshading_nearShadowBaseWidth", OnNearShadowBaseWidthChanged);
            _mod.CApi.Settings.AddWatcher<bool>("volumetricshading_softShadows", OnSoftShadowsChanged);
            _mod.CApi.Settings.AddWatcher<int>("volumetricshading_softShadowSamples", OnSoftShadowSamplesChanged);
            
            NearShadowBaseWidth = ModSettings.NearShadowBaseWidth;
            _softShadowsEnabled = ModSettings.SoftShadowsEnabled;
            _softShadowSamples = ModSettings.SoftShadowSamples;
            
            _mod.ShaderInjector.RegisterFloatProperty("VSMOD_NEARSHADOWOFFSET",
                () => ModSettings.NearPeterPanningAdjustment);
            
            _mod.ShaderInjector.RegisterFloatProperty("VSMOD_FARSHADOWOFFSET",
                () => ModSettings.FarPeterPanningAdjustment);
            
            _mod.ShaderInjector.RegisterBoolProperty("VSMOD_SOFTSHADOWS", () => _softShadowsEnabled);
            _mod.ShaderInjector.RegisterIntProperty("VSMOD_SOFTSHADOWSAMPLES", () => _softShadowSamples);

            _mod.CApi.Event.ReloadShader += OnReloadShaders;
            _mod.Events.PostUseShader += OnUseShader;
        }

        private bool OnReloadShaders()
        {
            return true;
        }

        private void OnNearShadowBaseWidthChanged(int newVal)
        {
            NearShadowBaseWidth = newVal;
        }
        
        private void OnSoftShadowsChanged(bool enabled)
        {
            _softShadowsEnabled = enabled;
        }
        
        private void OnSoftShadowSamplesChanged(int samples)
        {
            _softShadowSamples = samples;
        }
        
        private void OnUseShader(ShaderProgramBase shader)
        {
            if (!_softShadowsEnabled) return;
            if (!shader.includes.Contains("fogandlight.fsh")) return;
            if (ExcludedShaders.Contains(shader.PassName)) return;
            if (ShaderProgramBase.shadowmapQuality <= 0) return;

            const string uniformFar = "shadowMapFarTex";
            const string uniformNear = "shadowMapNearTex";
            const string uniformShadowFar = "shadowMapFar";
            const string uniformShadowNear = "shadowMapNear";
            if (!shader.customSamplers.ContainsKey(uniformFar))
            {
                var lookupSamplers = new int[2];
                for (var i = 0; i < lookupSamplers.Length; i++)
                {
                    var s = lookupSamplers[i] = GL.GenSampler();
                    GL.SamplerParameter(s, SamplerParameterName.TextureCompareMode, (int) TextureCompareMode.None);
                    GL.SamplerParameter(s, SamplerParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
                    GL.SamplerParameter(s, SamplerParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);
                    GL.SamplerParameter(s, SamplerParameterName.TextureBorderColor, new [] { 1f, 1f, 1f, 1f });
                    GL.SamplerParameter(s, SamplerParameterName.TextureWrapS, (int) TextureWrapMode.ClampToBorder);
                    GL.SamplerParameter(s, SamplerParameterName.TextureWrapT, (int) TextureWrapMode.ClampToBorder);
                }

                // emulate old texture lookup, because new lookup disables internal sampler parameters
                var oldSamplers = new int[2];
                for (var i = 0; i < oldSamplers.Length; ++i)
                {
                    var s = oldSamplers[i] = GL.GenSampler();
                    GL.SamplerParameter(s, SamplerParameterName.TextureCompareMode, (int) TextureCompareMode.CompareRToTexture);
                    GL.SamplerParameter(s, SamplerParameterName.TextureCompareFunc, (int) DepthFunction.Lequal);
                    GL.SamplerParameter(s, SamplerParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
                    GL.SamplerParameter(s, SamplerParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
                    GL.SamplerParameter(s, SamplerParameterName.TextureBorderColor, new [] { 1f, 1f, 1f, 1f });
                    GL.SamplerParameter(s, SamplerParameterName.TextureWrapS, (int) TextureWrapMode.ClampToBorder);
                    GL.SamplerParameter(s, SamplerParameterName.TextureWrapT, (int) TextureWrapMode.ClampToBorder);
                }
                
                shader.customSamplers[uniformFar] = lookupSamplers[0];
                shader.customSamplers[uniformNear] = lookupSamplers[1];
                shader.customSamplers[uniformShadowFar] = oldSamplers[0];
                shader.customSamplers[uniformShadowNear] = oldSamplers[1];
            }
            
            var fbs = _mod.CApi.Render.FrameBuffers;
            var fbFar = fbs[(int) EnumFrameBuffer.ShadowmapFar];
            var fbNear = fbs[(int) EnumFrameBuffer.ShadowmapNear];

            shader.BindTexture2D(uniformFar, fbFar.DepthTextureId);
            shader.BindTexture2D(uniformNear, fbNear.DepthTextureId);
            shader.BindTexture2D(uniformShadowFar, fbFar.DepthTextureId);
            shader.BindTexture2D(uniformShadowNear, fbNear.DepthTextureId);
        }

        public void Dispose()
        {
        }
    }
}