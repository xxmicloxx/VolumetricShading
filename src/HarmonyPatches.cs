using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    [HarmonyPatch(typeof(ClientPlatformWindows))]
    internal class PlatformPatches
    {
        private static readonly MethodInfo PlayerViewVectorSetter =
            typeof(ShaderProgramGodrays).GetProperty("PlayerViewVector")?.SetMethod;

        private static readonly MethodInfo GodrayCallsiteMethod = typeof(PlatformPatches).GetMethod("GodrayCallsite");


        private static readonly MethodInfo PrimaryScene2DSetter =
            typeof(ShaderProgramFinal).GetProperty("PrimaryScene2D")?.SetMethod;

        private static readonly MethodInfo FinalCallsiteMethod = typeof(PlatformPatches).GetMethod("FinalCallsite");

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

            if (found is false) throw new Exception("Could not patch RenderPostprocessingEffects!");
        }

        public static void GodrayCallsite(ShaderProgramGodrays rays)
        {
            VolumetricShadingMod.Instance.Events.EmitPreGodraysRender(rays);
        }

        [HarmonyPatch("RenderFinalComposition")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> FinalTranspiler(IEnumerable<CodeInstruction> instructions)
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

            if (found is false) throw new Exception("Could not patch RenderFinalComposition!");
        }

        public static void FinalCallsite(ShaderProgramFinal final)
        {
            VolumetricShadingMod.Instance.Events.EmitPreFinalRender(final);
        }


        [HarmonyPatch("SetupDefaultFrameBuffers")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void SetupDefaultFrameBuffersPostfix(List<FrameBufferRef> __result)
        {
            VolumetricShadingMod.Instance.Events.EmitRebuildFramebuffers(__result);
        }
    }

    [HarmonyPatch(typeof(ShaderRegistry))]
    internal class ShaderRegistryPatches
    {
        private static readonly MethodInfo HandleIncludesMethod = typeof(ShaderRegistry)
            .GetMethod("HandleIncludes", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo LoadShaderCallsiteMethod =
            typeof(ShaderRegistryPatches).GetMethod("LoadShaderCallsite");

        private static readonly FieldInfo IncludesField = typeof(ShaderRegistry)
            .GetField("includes", BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo LoadRegisteredCallsiteMethod =
            typeof(ShaderRegistryPatches).GetMethod("LoadRegisteredCallsite");

        [HarmonyPatch("LoadShader")]
        [HarmonyPostfix]
        public static void LoadShaderPostfix(ShaderProgram program, EnumShaderType shaderType)
        {
            VolumetricShadingMod.Instance.ShaderInjector.OnShaderLoaded(program, shaderType);
        }

        [HarmonyPatch("HandleIncludes")]
        [HarmonyReversePatch]
        public static string HandleIncludes(ShaderProgram program, string code, HashSet<string> filenames)
        {
            throw new InvalidOperationException("Stub, replaced by Harmony");
        }

        [HarmonyPatch("LoadShader")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LoadShaderTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            foreach (var instruction in instructions)
                if (instruction.Calls(HandleIncludesMethod))
                {
                    found = true;
                    // current stack: ShaderProgram, string (shader code), HashSet<string> (already included filenames, always null)
                    // load EnumShaderType to stack
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    // call our own instead of original, signature needs to match stack!
                    yield return new CodeInstruction(OpCodes.Call, LoadShaderCallsiteMethod);
                }
                else
                {
                    yield return instruction;
                }

            if (!found) throw new Exception("Could not transpile LoadShader");
        }

        public static string LoadShaderCallsite(ShaderProgram shader, string code, HashSet<string> filenames,
            EnumShaderType type)
        {
            string ext;
            switch (type)
            {
                case EnumShaderType.FragmentShader:
                    ext = ".fsh";
                    break;
                case EnumShaderType.VertexShader:
                    ext = ".vsh";
                    break;
                case EnumShaderType.GeometryShader:
                    ext = ".gsh";
                    break;
                default:
                    ext = ".unknown";
                    break;
            }

            var filename = shader.PassName + ext;
            code = VolumetricShadingMod.Instance.ShaderPatcher.Patch(filename, code);
            return HandleIncludes(shader, code, filenames);
        }

        [HarmonyPatch("loadRegisteredShaderPrograms")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> LoadRegisteredShaderProgramsTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var generated = false;
            foreach (var instruction in instructions)
            {
                if (found && !generated)
                {
                    generated = true;
                    yield return new CodeInstruction(OpCodes.Ldsfld, IncludesField)
                        .WithLabels(instruction.labels);
                    yield return new CodeInstruction(OpCodes.Call, LoadRegisteredCallsiteMethod);

                    instruction.labels.Clear();
                }

                yield return instruction;
                if (instruction.opcode != OpCodes.Endfinally) continue;

                found = true;
            }

            if (!found) throw new Exception("Could not patch loadRegisteredShaderPrograms");
        }

        public static void LoadRegisteredCallsite(Dictionary<string, string> includes)
        {
            VolumetricShadingMod.Instance.ShaderPatcher.Reload();

            foreach (var entry in includes.ToList())
            {
                var value = VolumetricShadingMod.Instance.ShaderPatcher
                    .Patch(entry.Key, entry.Value, true);
                includes[entry.Key] = value;
            }
        }
    }

    [HarmonyPatch(typeof(SystemRenderShadowMap))]
    internal class SystemRenderShadowMapPatches
    {
        private static readonly MethodInfo OnRenderShadowNearBaseWidthCallsiteMethod =
            typeof(SystemRenderShadowMapPatches).GetMethod("OnRenderShadowNearBaseWidthCallsite");

        private static readonly MethodInfo PrepareForShadowRenderingMethod = typeof(SystemRenderShadowMap)
            .GetMethod("PrepareForShadowRendering", BindingFlags.Instance | BindingFlags.NonPublic);

        [HarmonyPatch("OnRenderShadowNear")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnRenderShadowNearBaseWidthTranspiler(
            IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            var replaceNext = false;
            foreach (var instruction in instructions)
            {
                if (replaceNext)
                {
                    replaceNext = false;
                    yield return new CodeInstruction(OpCodes.Call, OnRenderShadowNearBaseWidthCallsiteMethod)
                        .WithLabels(instruction.labels);
                    
                    continue;
                }

                yield return instruction;

                if (!found && instruction.opcode == OpCodes.Ret)
                {
                    found = true;
                    replaceNext = true;
                }
            }
        }

        public static int OnRenderShadowNearBaseWidthCallsite()
        {
            return VolumetricShadingMod.Instance.ShadowTweaks.NearShadowBaseWidth;
        }

        [HarmonyPatch("OnRenderShadowNear")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> OnRenderShadowNearZExtend(IEnumerable<CodeInstruction> instructions)
        {
            var found = false;
            CodeInstruction previousInstruction = null;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(PrepareForShadowRenderingMethod))
                {
                    found = true;

                    // fixes some shadow glitches by increasing the extra culling range for shadows
                    yield return new CodeInstruction(OpCodes.Ldc_R4, (float)32);
                }
                else if (previousInstruction != null)
                {
                    yield return previousInstruction;
                }

                previousInstruction = instruction;
            }

            yield return previousInstruction;

            if (!found) throw new Exception("Could not patch OnRenderShadowNear for further Z extension");
        }
    }

    [HarmonyPatch(typeof(SystemRenderSunMoon))]
    internal class SunMoonPatches
    {
        private static readonly MethodInfo StandardShaderTextureSetter = typeof(ShaderProgramStandard)
            .GetProperty("Tex2D")?.SetMethod;

        private static readonly MethodInfo AddRenderFlagsSetter = typeof(ShaderProgramStandard)
            .GetProperty("AddRenderFlags")?.SetMethod;

        private static readonly MethodInfo RenderCallsiteMethod = typeof(SunMoonPatches)
            .GetMethod("RenderCallsite");

        [HarmonyPatch("OnRenderFrame3D")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RenderTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;

                if (!instruction.Calls(StandardShaderTextureSetter)) continue;

                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Call, RenderCallsiteMethod);
                found = true;
            }

            if (found is false) throw new Exception("Could not patch RenderPostprocessingEffects!");
        }

        [HarmonyPatch("OnRenderFrame3DPost")]
        [HarmonyPostfix]
        public static void RenderPostPostfix()
        {
            VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();
        }

        [HarmonyPatch("OnRenderFrame3D")]
        [HarmonyPostfix]
        public static void RenderPostfix()
        {
            VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();
        }

        [HarmonyPatch("OnRenderFrame3DPost")]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> RenderPostTranspiler(IEnumerable<CodeInstruction> instructions,
            ILGenerator generator)
        {
            var found = false;
            var previousInstructions = new CodeInstruction[2];
            foreach (var instruction in instructions)
            {
                var currentOld = previousInstructions[1];
                yield return instruction;

                previousInstructions[1] = previousInstructions[0];
                previousInstructions[0] = instruction;
                if (!instruction.Calls(AddRenderFlagsSetter)) continue;

                // currentOld contains the code to load our shader program to the stack
                yield return currentOld;
                yield return new CodeInstruction(OpCodes.Call, RenderCallsiteMethod);
                found = true;
            }

            if (found is false) throw new Exception("Could not patch RenderFinalComposition!");
        }

        public static void RenderCallsite(ShaderProgramStandard standard)
        {
            VolumetricShadingMod.Instance.Events.EmitPreSunRender(standard);
        }
    }

    [HarmonyPatch]
    internal class IceAndGlassPatches
    {
        [HarmonyPatch(typeof(CubeTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void CubeTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        [HarmonyPatch(typeof(LiquidTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void LiquidTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        [HarmonyPatch(typeof(TopsoilTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void TopsoilTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        [HarmonyPatch(typeof(JsonTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void JsonTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        [HarmonyPatch(typeof(JsonAndSnowLayerTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void JsonAndSnowLayerTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        [HarmonyPatch(typeof(JsonAndLiquidTesselator), "Tesselate")]
        [HarmonyPrefix]
        public static void JsonAndLiquidTesselator(ref TCTCache vars)
        {
            Tesselate(ref vars);
        }

        public static void Tesselate(ref TCTCache vars)
        {
            if (vars.block.BlockMaterial == EnumBlockMaterial.Ice ||
                vars.block.BlockMaterial == EnumBlockMaterial.Glass)
                vars.VertexFlags |= VertexFlags.ReflectiveBitMask;
        }
    }

    [HarmonyPatch(typeof(ShaderProgramBase))]
    internal class ShaderProgramBasePatches
    {
        [HarmonyPatch("Use")]
        [HarmonyPostfix]
        // ReSharper disable once InconsistentNaming
        public static void UsePostfix(ShaderProgramBase __instance)
        {
            VolumetricShadingMod.Instance.Events.EmitPostUseShader(__instance);
        }
    }

    [HarmonyPatch(typeof(AmbientManager))]
    internal class AmbientManagerPatches
    {
        [HarmonyPatch("OnPlayerSightBeingChangedByWater")]
        [HarmonyPostfix]
        public static void OnPlayerSightBeingChangedByWaterPostfix()
        {
            VolumetricShadingMod.Instance.Events.EmitPostWaterChangeSight();
        }
    }
}