using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    [HarmonyPatch(typeof(ClientPlatformWindows))]
    internal class PlatformPatches
    {
        private static readonly MethodInfo PlayerViewVectorSetter =
            typeof(ShaderProgramGodrays).GetProperty("PlayerViewVector")?.SetMethod;

        private static readonly MethodInfo GodrayCallsiteMethod = typeof(PlatformPatches).GetMethod("GodrayCallsite");
        
        [HarmonyPatch("RenderPostprocessingEffects")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> PostprocessingTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                
                if (!instruction.Calls(PlayerViewVectorSetter)) continue;

                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Call, GodrayCallsiteMethod);
                found = true;
            }

            if (found is false)
            {
                throw new Exception("Could not patch RenderPostprocessingEffects!");
            }
        }

        public static void GodrayCallsite(ShaderProgramGodrays rays)
        {
            VolumetricShadingMod.Instance.VolumetricLighting.OnSetGodrayUniforms(rays);
        }
        
        
        
        private static readonly MethodInfo PrimaryScene2DSetter =
            typeof(ShaderProgramFinal).GetProperty("PrimaryScene2D")?.SetMethod;

        private static readonly MethodInfo FinalCallsiteMethod = typeof(PlatformPatches).GetMethod("FinalCallsite");
        
        [HarmonyPatch("RenderFinalComposition")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FinalTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var found = false;
            var previousInstructions = new CodeInstruction[2];
            foreach (var instruction in instructions)
            {
                var currentOld = previousInstructions[1];
                yield return instruction;

                previousInstructions[1] = previousInstructions[0];
                previousInstructions[0] = instruction;
                if (!instruction.Calls(PrimaryScene2DSetter)) continue;

                // currentOld contains the code to load our shader program to the stack
                yield return currentOld;
                yield return new CodeInstruction(OpCodes.Call, FinalCallsiteMethod);
                found = true;
            }

            if (found is false)
            {
                throw new Exception("Could not patch RenderFinalComposition!");
            }
        }

        public static void FinalCallsite(ShaderProgramFinal final)
        {
            VolumetricShadingMod.Instance.ScreenSpaceReflections.OnSetFinalUniforms(final);
        }
        
        
        
        
        [HarmonyPatch("SetupDefaultFrameBuffers")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void SetupDefaultFrameBuffersPostfix(List<FrameBufferRef> __result)
        {
            VolumetricShadingMod.Instance.ScreenSpaceReflections.SetupFramebuffers(__result);
        }
    }

    [HarmonyPatch(typeof(ShaderRegistry))]
    internal class ShaderRegistryPatches
    {
        [HarmonyPatch("LoadShader")]
        [HarmonyPostfix]
        public static void LoadShaderPostfix(ShaderProgram program, EnumShaderType shaderType)
        {
            VolumetricShadingMod.Instance.ShaderInjector.OnShaderLoaded(program, shaderType);
        }
    }
}
