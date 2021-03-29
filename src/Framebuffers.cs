using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public static class Framebuffers
    {
        public static void SetupDepthTexture(this FrameBufferRef fbRef)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.DepthTextureId);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32,
                fbRef.Width, fbRef.Height, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, fbRef.DepthTextureId, 0);
        }
        
        public static void SetupVertexTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba16f,
                fbRef.Width, fbRef.Height, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor,
                new[] {1f, 1f, 1f, 1f});
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int) TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int) TextureWrapMode.ClampToBorder);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Nearest);
        }
        
        public static void SetupColorTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                fbRef.Width, fbRef.Height, 0, PixelFormat.Rgba, PixelType.UnsignedShort, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
        }

        public static void SetupSingleColorTexture(this FrameBufferRef fbRef, int textureId)
        {
            GL.BindTexture(TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId]);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8,
                fbRef.Width, fbRef.Height, 0, PixelFormat.Red, PixelType.UnsignedShort, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int) TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int) TextureMagFilter.Nearest);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.ColorAttachment0 + textureId,
                TextureTarget.Texture2D, fbRef.ColorTextureIds[textureId], 0);
        }
        
        public static void CheckStatus()
        {
            var errorCode = GL.Ext.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (errorCode != FramebufferErrorCode.FramebufferComplete)
            {
                throw new Exception("Could not create framebuffer: " + errorCode);
            }
        }
        
        public static void Blit(this ClientPlatformWindows platform, MeshRef quad, int source)
        {
            var blit = ShaderPrograms.Blit;
            blit.Use();
            blit.Scene2D = source;
            platform.RenderFullscreenTriangle(quad);
            blit.Stop();
        }
    }
}