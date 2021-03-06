using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public static class ReflectionHelper
    {
        public static ClientMain GetClient(this ICoreClientAPI api)
        {
            return (ClientMain) api.World;
        }

        public static ClientPlatformAbstract GetClientPlatformAbstract(this ClientMain client)
        {
            var platform = (ClientPlatformAbstract) typeof(ClientMain)
                .GetField("Platform", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(client);

            if (platform == null)
            {
                throw new Exception("Could not fetch platform via reflection!");
            }

            return platform;
        }

        public static ClientPlatformWindows GetClientPlatformWindows(this ClientMain client)
        {
            return (ClientPlatformWindows) client.GetClientPlatformAbstract();
        }

        public static ClientPlatformAbstract GetClientPlatformAbstract(this ICoreClientAPI api)
        {
            return api.GetClient().GetClientPlatformAbstract();
        }

        public static ClientPlatformWindows GetClientPlatformWindows(this ICoreClientAPI api)
        {
            return api.GetClient().GetClientPlatformWindows();
        }

        public static ChunkRenderer GetChunkRenderer(this ClientMain client)
        {
            var renderer = (ChunkRenderer) typeof(ClientMain)
                .GetField("chunkRenderer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(client);

            if (renderer == null)
            {
                throw new Exception("Could not fetch chunk renderer!");
            }

            return renderer;
        }

        public static MeshRef GetScreenQuad(this ClientPlatformWindows platform)
        {
            var quad = (MeshRef) typeof(ClientPlatformWindows)
                .GetField("screenQuad", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(platform);

            if (quad == null)
            {
                throw new Exception("Could not fetch screen quad");
            }

            return quad;
        }

        public static void TriggerOnlyOnMouseUp(this GuiElementSlider slider, bool trigger = true)
        {
            var method = typeof(GuiElementSlider).GetMethod("TriggerOnlyOnMouseUp",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new Exception("Could not get trigger only on mouse up method.");
            }

            method.Invoke(slider, new object[] {trigger});
        }
    }
}