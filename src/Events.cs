using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class Events
    {
        public event Action<List<FrameBufferRef>> RebuildFramebuffers;
        public event Action<ShaderProgramFinal> PreFinalRender;
        public event Action<ShaderProgramGodrays> PreGodraysRender;
        public event Action<ShaderProgramStandard> PreSunRender;
        public event Action<ShaderProgramBase> PostUseShader;
        public event Action PostWaterChangeSight;
        
        public void EmitRebuildFramebuffers(List<FrameBufferRef> framebuffers)
        {
            RebuildFramebuffers?.Invoke(framebuffers);
        }

        public void EmitPreFinalRender(ShaderProgramFinal final)
        {
            PreFinalRender?.Invoke(final);
        }

        public void EmitPreGodraysRender(ShaderProgramGodrays godrays)
        {
            PreGodraysRender?.Invoke(godrays);
        }

        public void EmitPreSunRender(ShaderProgramStandard standard)
        {
            PreSunRender?.Invoke(standard);
        }

        public void EmitPostUseShader(ShaderProgramBase shader)
        {
            PostUseShader?.Invoke(shader);
        }

        public void EmitPostWaterChangeSight()
        {
            PostWaterChangeSight?.Invoke();
        }
    }
}