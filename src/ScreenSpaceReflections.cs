using System;
using System.Collections.Generic;
using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class ScreenSpaceReflections : IRenderer
    {
        private readonly VolumetricShadingMod _mod;

        private bool _enabled;
        private bool _rainEnabled;

        private FrameBufferRef _ssrFramebuffer;
        private FrameBufferRef _ssrOutFramebuffer;
        
        private IShaderProgram _ssrLiquidShader;
        private IShaderProgram _ssrOpaqueShader;
        private IShaderProgram _ssrTransparentShader;
        private IShaderProgram _ssrTopsoilShader;
        private IShaderProgram _ssrOutShader;

        private readonly ClientMain _game;
        private readonly ClientPlatformWindows _platform;
        private ChunkRenderer _chunkRenderer;
        private MeshRef _screenQuad;
        private readonly FieldInfo _textureIdsField;

        private int _fbWidth;
        private int _fbHeight;
        
        private float _currentRain;
        private float _targetRain;
        private float _rainAccumulator;

        private readonly float[] _invProjectionMatrix;
        private readonly float[] _invModelViewMatrix;

        public ScreenSpaceReflections(VolumetricShadingMod mod)
        {
            _mod = mod;

            _game = mod.CApi.GetClient();
            _platform = _game.GetClientPlatformWindows();

            RegisterInjectorProperties();

            mod.CApi.Event.ReloadShader += ReloadShaders;
            mod.Events.PreFinalRender += OnSetFinalUniforms;

            _enabled = ModSettings.ScreenSpaceReflectionsEnabled;
            _rainEnabled = ModSettings.SSRRainReflectionsEnabled;
            
            mod.CApi.Settings.AddWatcher<bool>("volumetricshading_screenSpaceReflections", OnEnabledChanged);
            mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRRainReflections", OnRainReflectionsChanged);

            mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "ssrWorldSpace");
            mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssrOut");

            _textureIdsField =
                typeof(ChunkRenderer).GetField("textureIds", BindingFlags.Instance | BindingFlags.NonPublic);

            _invProjectionMatrix = Mat4f.Create();
            _invModelViewMatrix = Mat4f.Create();

            mod.Events.RebuildFramebuffers += SetupFramebuffers;
            SetupFramebuffers(_platform.FrameBuffers);
        }

        private void RegisterInjectorProperties()
        {
            var injector = _mod.ShaderInjector;
            injector.RegisterBoolProperty("VSMOD_SSR", () => ModSettings.ScreenSpaceReflectionsEnabled);

            injector.RegisterFloatProperty("VSMOD_SSR_WATER_TRANSPARENCY",
                () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);

            injector.RegisterFloatProperty("VSMOD_SSR_SPLASH_TRANSPARENCY",
                () => (100 - ModSettings.SSRSplashTransparency) * 0.01f);

            injector.RegisterFloatProperty("VSMOD_SSR_REFLECTION_DIMMING",
                () => ModSettings.SSRReflectionDimming * 0.01f);

            injector.RegisterFloatProperty("VSMOD_SSR_TINT_INFLUENCE",
                () => ModSettings.SSRTintInfluence * 0.01f);

            injector.RegisterFloatProperty("VSMOD_SSR_SKY_MIXIN",
                () => ModSettings.SSRSkyMixin * 0.01f);
        }

        private void OnEnabledChanged(bool enabled)
        {
            _enabled = enabled;
        }
        
        private void OnRainReflectionsChanged(bool enabled)
        {
            _rainEnabled = enabled;
        }

        private bool ReloadShaders()
        {
            var success = true;

            _ssrLiquidShader?.Dispose();
            _ssrOpaqueShader?.Dispose();
            _ssrTransparentShader?.Dispose();
            _ssrTopsoilShader?.Dispose();
            _ssrOutShader?.Dispose();

            _ssrLiquidShader = _mod.RegisterShader("ssrliquid", ref success);

            _ssrOpaqueShader = _mod.RegisterShader("ssropaque", ref success);
            ((ShaderProgram) _ssrOpaqueShader).SetCustomSampler("terrainTexLinear", true);

            _ssrTransparentShader = _mod.RegisterShader("ssrtransparent", ref success);

            _ssrTopsoilShader = _mod.RegisterShader("ssrtopsoil", ref success);

            _ssrOutShader = _mod.RegisterShader("ssrout", ref success);

            return success;
        }

        public void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
        {
            _mod.Mod.Logger.Event("Recreating framebuffers");

            if (_ssrFramebuffer != null)
            {
                // dispose the old framebuffer
                _platform.DisposeFrameBuffer(_ssrFramebuffer);
            }

            if (_ssrOutFramebuffer != null)
            {
                _platform.DisposeFrameBuffer(_ssrOutFramebuffer);
            }

            // create new framebuffer
            _fbWidth = (int) (_platform.window.Width * ClientSettings.SSAA);
            _fbHeight = (int) (_platform.window.Height * ClientSettings.SSAA);
            _ssrFramebuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(), Width = _fbWidth, Height = _fbHeight, DepthTextureId = GL.GenTexture()
            };
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssrFramebuffer.FboId);
            _ssrFramebuffer.SetupDepthTexture();

            // create our normal and position textures
            _ssrFramebuffer.ColorTextureIds = ArrayUtil.CreateFilled(3, _ => GL.GenTexture());

            // bind and setup textures
            _ssrFramebuffer.SetupVertexTexture(0);
            _ssrFramebuffer.SetupVertexTexture(1);
            _ssrFramebuffer.SetupColorTexture(2);

            GL.DrawBuffers(3,
                new[]
                {
                    DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1, DrawBuffersEnum.ColorAttachment2
                });

            Framebuffers.CheckStatus();

            // setup output framebuffer
            _ssrOutFramebuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(), Width = _fbWidth, Height = _fbHeight
            };
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _ssrOutFramebuffer.FboId);
            _ssrOutFramebuffer.ColorTextureIds = new[] {GL.GenTexture()};

            _ssrOutFramebuffer.SetupColorTexture(0);

            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Framebuffers.CheckStatus();

            _screenQuad = _platform.GetScreenQuad();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (!_enabled) return;
            if (_chunkRenderer == null)
            {
                _chunkRenderer = _game.GetChunkRenderer();
            }

            if (stage == EnumRenderStage.Opaque)
            {
                OnPreRender(deltaTime);
                OnRenderSsrChunks();
            }
            else if (stage == EnumRenderStage.AfterOIT)
            {
                OnRenderSsrOut();
            }
        }

        private void OnPreRender(float dt)
        {
            _rainAccumulator += dt;
            if (_rainAccumulator > 5f)
            {
                _rainAccumulator = 0f;
                var climate = _game.BlockAccessor.GetClimateAt(_game.EntityPlayer.Pos.AsBlockPos,
                    EnumGetClimateMode.NowValues);
                
                var rainMul = GameMath.Clamp((climate.Temperature + 1f) / 4f, 0f, 1f);
                _targetRain = climate.Rainfall * rainMul;
            }

            if (_targetRain > _currentRain)
            {
                _currentRain = Math.Min(_currentRain + dt * 0.15f, _targetRain);
            }
            else if (_targetRain < _currentRain)
            {
                _currentRain = Math.Max(_currentRain - dt * 0.01f, _targetRain);
            }
        }

        private void OnRenderSsrOut()
        {
            if (_ssrOutFramebuffer == null) return;
            if (_ssrOutShader == null) return;

            _platform.LoadFrameBuffer(_ssrOutFramebuffer);

            var uniforms = _mod.CApi.Render.ShaderUniforms;
            var ambient = _mod.CApi.Ambient;

            // thanks for declaring this internal btw
            var dayLight = 1.25f *
                           GameMath.Max(
                               _mod.CApi.World.Calendar.DayLightStrength -
                               _mod.CApi.World.Calendar.MoonLightStrength / 2f, 0.05f);

            var shader = _ssrOutShader;
            shader.Use();

            shader.BindTexture2D("primaryScene",
                _platform.FrameBuffers[(int) EnumFrameBuffer.Primary].ColorTextureIds[0], 0);
            shader.BindTexture2D("gPosition", _ssrFramebuffer.ColorTextureIds[0], 1);
            shader.BindTexture2D("gNormal", _ssrFramebuffer.ColorTextureIds[1], 2);
            shader.BindTexture2D("gDepth", _platform.FrameBuffers[(int) EnumFrameBuffer.Primary].DepthTextureId, 3);
            shader.BindTexture2D("gTint", _ssrFramebuffer.ColorTextureIds[2], 4);
            shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            shader.UniformMatrix("invProjectionMatrix",
                Mat4f.Invert(_invProjectionMatrix, _mod.CApi.Render.CurrentProjectionMatrix));
            shader.UniformMatrix("invModelViewMatrix",
                Mat4f.Invert(_invModelViewMatrix, _mod.CApi.Render.CameraMatrixOriginf));
            shader.Uniform("zNear", uniforms.ZNear);
            shader.Uniform("zFar", uniforms.ZNear);
            shader.Uniform("sunPosition", _mod.CApi.World.Calendar.SunPositionNormalized);
            shader.Uniform("dayLight", dayLight);
            shader.Uniform("horizonFog", ambient.BlendedCloudDensity);
            shader.Uniform("fogDensityIn", ambient.BlendedFogDensity);
            shader.Uniform("fogMinIn", ambient.BlendedFogMin);
            shader.Uniform("rgbaFog", ambient.BlendedFogColor);

            GL.Disable(EnableCap.Blend);
            _platform.RenderFullscreenTriangle(_screenQuad);
            shader.Stop();
            GL.Enable(EnableCap.Blend);
            _platform.UnloadFrameBuffer(_ssrOutFramebuffer);

            _platform.CheckGlError("Error while calculating SSR");
        }

        private void OnRenderSsrChunks()
        {
            if (_ssrFramebuffer == null) return;
            if (_ssrLiquidShader == null) return;

            if (!(_textureIdsField.GetValue(_chunkRenderer) is int[] textureIds)) return;

            // copy the depth buffer so we can work with it
            var primaryBuffer = _platform.FrameBuffers[(int) EnumFrameBuffer.Primary];
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, primaryBuffer.FboId);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, _ssrFramebuffer.FboId);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.BlitFramebuffer(0, 0, primaryBuffer.Width, primaryBuffer.Height,
                0, 0, _fbWidth, _fbHeight, ClearBufferMask.DepthBufferBit,
                BlitFramebufferFilter.Nearest);
            
            // bind our framebuffer
            _platform.LoadFrameBuffer(_ssrFramebuffer);
            GL.ClearBuffer(ClearBuffer.Color, 0, new[] {0f, 0f, 0f, 1f});
            GL.ClearBuffer(ClearBuffer.Color, 1, new[] {0f, 0f, 0f, 1f});
            GL.ClearBuffer(ClearBuffer.Color, 2, new[] {0f, 0f, 0f, 1f});

            _platform.GlEnableCullFace();
            _platform.GlDepthMask(true);
            _platform.GlEnableDepthTest();
            _platform.GlToggleBlend(false);

            var climateAt =
                _game.BlockAccessor.GetClimateAt(_game.EntityPlayer.Pos.AsBlockPos, EnumGetClimateMode.NowValues);
            var curRainFall = climateAt.Rainfall;

            var cameraPos = _game.EntityPlayer.CameraPos;

            // render stuff
            _game.GlPushMatrix();
            _game.GlLoadMatrix(_mod.CApi.Render.CameraMatrixOrigin);

            var shader = _ssrOpaqueShader;
            shader.Use();
            shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            var pools = _chunkRenderer.poolsByRenderPass[(int) EnumChunkRenderPass.Opaque];
            for (var i = 0; i < textureIds.Length; ++i)
            {
                shader.BindTexture2D("terrainTex", textureIds[i], 0);
                shader.BindTexture2D("terrainTexLinear", textureIds[i], 0);
                pools[i].Render(cameraPos, "origin");
            }

            shader.Stop();

            if (_rainEnabled)
            {
                shader = _ssrTopsoilShader;
                shader.Use();
                shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
                shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
                shader.Uniform("rainStrength", _currentRain);
                pools = _chunkRenderer.poolsByRenderPass[(int) EnumChunkRenderPass.TopSoil];
                for (var i = 0; i < textureIds.Length; ++i)
                {
                    shader.BindTexture2D("terrainTex", textureIds[i], 0);
                    pools[i].Render(cameraPos, "origin");
                }

                shader.Stop();
            }

            shader = _ssrLiquidShader;
            shader.Use();
            shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            shader.Uniform("dropletIntensity", curRainFall);
            shader.Uniform("waterFlowCounter", _platform.ShaderUniforms.WaterFlowCounter);
            shader.Uniform("windSpeed", _platform.ShaderUniforms.WindSpeed);
            pools = _chunkRenderer.poolsByRenderPass[(int) EnumChunkRenderPass.Liquid];
            for (var i = 0; i < textureIds.Length; ++i)
            {
                shader.BindTexture2D("terrainTex", textureIds[i], 0);
                pools[i].Render(cameraPos, "origin");
            }

            shader.Stop();

            shader = _ssrTransparentShader;
            shader.Use();
            shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            pools = _chunkRenderer.poolsByRenderPass[(int) EnumChunkRenderPass.Transparent];
            for (var i = 0; i < textureIds.Length; ++i)
            {
                shader.BindTexture2D("terrainTex", textureIds[i], 0);
                pools[i].Render(cameraPos, "origin");
            }
            
            shader.Stop();

            _game.GlPopMatrix();
            _platform.UnloadFrameBuffer(_ssrFramebuffer);

            _platform.GlDepthMask(false);
            _platform.GlToggleBlend(true);

            _platform.CheckGlError("Error while rendering solid liquids");
        }

        public void OnSetFinalUniforms(ShaderProgramFinal final)
        {
            if (!_enabled) return;
            if (_ssrOutFramebuffer == null) return;

            final.BindTexture2D("ssrScene", _ssrOutFramebuffer.ColorTextureIds[0]);
        }

        public void Dispose()
        {
            var windowsPlatform = _mod.CApi.GetClientPlatformWindows();

            if (_ssrFramebuffer != null)
            {
                // dispose the old framebuffer
                windowsPlatform.DisposeFrameBuffer(_ssrFramebuffer);
                _ssrFramebuffer = null;
            }

            if (_ssrOutFramebuffer != null)
            {
                windowsPlatform.DisposeFrameBuffer(_ssrOutFramebuffer);
                _ssrOutFramebuffer = null;
            }

            if (_ssrLiquidShader != null)
            {
                _ssrLiquidShader.Dispose();
                _ssrLiquidShader = null;
            }

            if (_ssrOutShader != null)
            {
                _ssrOutShader.Dispose();
                _ssrOutShader = null;
            }

            if (_ssrOpaqueShader != null)
            {
                _ssrOpaqueShader.Dispose();
                _ssrOutShader = null;
            }

            if (_ssrTransparentShader != null)
            {
                _ssrTransparentShader.Dispose();
                _ssrTransparentShader = null;
            }

            if (_ssrTopsoilShader != null)
            {
                _ssrTopsoilShader.Dispose();
                _ssrTopsoilShader = null;
            }

            _chunkRenderer = null;
            _screenQuad = null;
        }

        public double RenderOrder => 1;

        public int RenderRange => int.MaxValue;
    }
}