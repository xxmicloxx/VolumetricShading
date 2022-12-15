using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using VolumetricShading.Patch;

namespace VolumetricShading.Effects
{
    public class DeferredLightingRenderer : IRenderer
    {
        private readonly DeferredLighting _lighting;

        public DeferredLightingRenderer(DeferredLighting lighting)
        {
            _lighting = lighting;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            _lighting.OnEndRender();
        }

        public void Dispose()
        {
        }

        public double RenderOrder => 1;
        public int RenderRange => int.MaxValue;
    }
    
    public class DeferredLightingPreparer : IRenderer
    {
        private readonly DeferredLighting _lighting;

        public DeferredLightingPreparer(DeferredLighting lighting)
        {
            _lighting = lighting;
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            _lighting.OnBeginRender();
        }

        public void Dispose()
        {
            _lighting.Dispose();
        }

        public double RenderOrder => 0;
        public int RenderRange => int.MaxValue;
    }
    
    public class DeferredLighting
    {
        private readonly VolumetricShadingMod _mod;
        private readonly ClientPlatformWindows _platform;
        
        private ShaderProgram _shader;
        private FrameBufferRef _frameBuffer;
        private MeshRef _screenQuad;

        private bool _enabled;

        public DeferredLighting(VolumetricShadingMod mod)
        {
            _mod = mod;
            _platform = _mod.CApi.GetClientPlatformWindows();
            
            _mod.CApi.Settings.AddWatcher<bool>("volumetricshading_deferredLighting", OnDeferredLightingChanged);
            _mod.CApi.Settings.AddWatcher<int>("ssaoQuality", OnSSAOQualityChanged);
            
            _enabled = ModSettings.DeferredLightingEnabled;

            _mod.CApi.Event.RegisterRenderer(new DeferredLightingPreparer(this), EnumRenderStage.Opaque,
                "vsmod-deferred-lighting-prepare");
            
            _mod.CApi.Event.RegisterRenderer(new DeferredLightingRenderer(this), EnumRenderStage.Opaque,
                "vsmod-deferred-lighting");
            
            _mod.ShaderInjector.RegisterBoolProperty("VSMOD_DEFERREDLIGHTING", () => _enabled);

            _mod.CApi.Event.ReloadShader += OnReloadShaders;
            _mod.Events.RebuildFramebuffers += SetupFramebuffers;
            SetupFramebuffers(_platform.FrameBuffers);
        }

        private void OnDeferredLightingChanged(bool enabled)
        {
            _enabled = enabled;
            if (enabled && ClientSettings.SSAOQuality == 0)
            {
                ClientSettings.SSAOQuality = 1;
            }
        }

        private void OnSSAOQualityChanged(int quality)
        {
            if (quality == 0 && _enabled)
            {
                ModSettings.DeferredLightingEnabled = false;
                _platform.RebuildFrameBuffers();
                _mod.CApi.Shader.ReloadShaders();
            }
        }

        private bool OnReloadShaders()
        {
            var success = true;
            
            _shader?.Dispose();
            _shader = (ShaderProgram) _mod.RegisterShader("deferredlighting", ref success);

            return success;
        }
        
        private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
        {
            if (_frameBuffer != null)
            {
                _platform.DisposeFrameBuffer(_frameBuffer);
                _frameBuffer = null;
            }
            
            var ssao = ClientSettings.SSAOQuality > 0;
            if (!ssao || !_enabled)
                return;

            var fbPrimary = mainBuffers[(int) EnumFrameBuffer.Primary];
            
            var fbWidth = (int) (_platform.window.Width * ClientSettings.SSAA);
            var fbHeight = (int) (_platform.window.Height * ClientSettings.SSAA);
            if (fbWidth == 0 || fbHeight == 0)
                return;

            var fb = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = fbWidth,
                Height = fbHeight,
                ColorTextureIds = ArrayUtil.CreateFilled(2, _ => GL.GenTexture())
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fb.FboId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, fbPrimary.DepthTextureId, 0);
            fb.SetupColorTexture(0);
            fb.SetupColorTexture(1);
            
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
                TextureTarget.Texture2D, fbPrimary.ColorTextureIds[2], 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
                TextureTarget.Texture2D, fbPrimary.ColorTextureIds[3], 0);
            GL.DrawBuffers(4, new []
            {
                DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3
            });
            
            Framebuffers.CheckStatus();
            _frameBuffer = fb;

            _screenQuad = _platform.GetScreenQuad();
        }

        public void OnBeginRender()
        {
            if (_frameBuffer == null) return;
            _platform.LoadFrameBuffer(_frameBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void OnEndRender()
        {
            if (_frameBuffer == null) return;
            _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
            GL.ClearBuffer(ClearBuffer.Color, 0, new [] { 0f, 0f, 0f, 1f });
            GL.ClearBuffer(ClearBuffer.Color, 1, new [] { 0f, 0f, 0f, 1f });

            var render = _mod.CApi.Render;
            var uniforms = render.ShaderUniforms;
            var myUniforms = _mod.Uniforms;
            
            var fb = _frameBuffer;
            var fbPrimary = _platform.FrameBuffers[(int) EnumFrameBuffer.Primary];
            
            _platform.GlDisableDepthTest();
            _platform.GlToggleBlend(false);
            GL.DrawBuffers(2, new [] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });

            var s = _shader;
            s.Use();
            
            s.BindTexture2D("gDepth", fbPrimary.DepthTextureId);
            s.BindTexture2D("gNormal", fbPrimary.ColorTextureIds[2]);
            s.BindTexture2D("inColor", fb.ColorTextureIds[0]);
            s.BindTexture2D("inGlow", fb.ColorTextureIds[1]);
            s.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
            s.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
            s.Uniform("dayLight", myUniforms.DayLight);
            s.Uniform("sunPosition", uniforms.SunPosition3D);

            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                s.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
                s.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
                s.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                s.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
            }
            
            s.Uniform("fogDensityIn", render.FogDensity);
            s.Uniform("fogMinIn", render.FogMin);
            s.Uniform("rgbaFog", render.FogColor);
            s.Uniform("flatFogDensity", uniforms.FlagFogDensity);
            s.Uniform("flatFogStart", uniforms.FlatFogStartYPos - uniforms.PlayerPos.Y);
            s.Uniform("viewDistance", ClientSettings.ViewDistance);
            s.Uniform("viewDistanceLod0", ClientSettings.ViewDistance * ClientSettings.LodBias);

            _platform.RenderFullscreenTriangle(_screenQuad);
            s.Stop();
            _platform.CheckGlError("Error while calculating deferred lighting");
            
            GL.DrawBuffers(4, new []
            {
                DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2, DrawBuffersEnum.ColorAttachment3
            });
            _platform.GlEnableDepthTest();
        }

        public void Dispose()
        {
            _shader?.Dispose();
            _shader = null;
            
            if (_frameBuffer != null)
            {
                _platform.DisposeFrameBuffer(_frameBuffer);
                _frameBuffer = null;
            }
        }
    }
}