using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;
using Vintagestory.API.Util;
using VolumetricShading.Effects;
using VolumetricShading.Gui;
using VolumetricShading.Patch;

namespace VolumetricShading
{
    public class VolumetricShadingMod : ModSystem
    {
        public static VolumetricShadingMod Instance { get; private set; }

        public ICoreClientAPI CApi { get; private set; }
        public Events Events { get; private set; }
        public Uniforms Uniforms { get; private set; }
        public bool Debug { get; private set; }

        public ShaderPatcher ShaderPatcher { get; private set; }
        public ShaderInjector ShaderInjector { get; private set; }
        public ScreenSpaceReflections ScreenSpaceReflections { get; private set; }
        public VolumetricLighting VolumetricLighting { get; private set; }
        public OverexposureEffect OverexposureEffect { get; private set; }
        public ScreenSpaceDirectionalOcclusion ScreenSpaceDirectionalOcclusion { get; private set; }
        public ShadowTweaks ShadowTweaks { get; private set; }
        public DeferredLighting DeferredLighting { get; private set; }
        public UnderwaterTweaks UnderwaterTweaks { get; private set; }

        public ConfigGui ConfigGui;
        public GuiDialog CurrentDialog;

        private Harmony _harmony;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            PatchGame();
            RegisterHotkeys();
        }

        public override void StartPre(ICoreAPI api)
        {
            if (!(api is ICoreClientAPI))
            {
                return;
            }

            SetConfigDefaults();
            
            Instance = this;
            CApi = (ICoreClientAPI) api;
            Events = new Events();
            Uniforms = new Uniforms(this);
            Debug = Environment.GetEnvironmentVariable("VOLUMETRICSHADING_DEBUG").ToBool();
            if (Debug)
                Mod.Logger.Event("Debugging activated");

            ShaderPatcher = new ShaderPatcher(CApi, Mod.Info.ModID);
            ShaderInjector = new ShaderInjector(CApi, Mod.Info.ModID);
            VolumetricLighting = new VolumetricLighting(this);
            ScreenSpaceReflections = new ScreenSpaceReflections(this);
            OverexposureEffect = new OverexposureEffect(this);
            ScreenSpaceDirectionalOcclusion = new ScreenSpaceDirectionalOcclusion(this);
            ShadowTweaks = new ShadowTweaks(this);
            DeferredLighting = new DeferredLighting(this);
            UnderwaterTweaks = new UnderwaterTweaks(this);

            ShaderInjector.Debug = Debug;
        }

        private void RegisterHotkeys()
        {
            CApi.Input.RegisterHotKey("volumetriclightingconfigure", "Volumetric Lighting Configuration", GlKeys.C,
                HotkeyType.GUIOrOtherControls, ctrlPressed: true);
            CApi.Input.SetHotKeyHandler("volumetriclightingconfigure", OnConfigurePressed);
        }

        private void PatchGame()
        {
            Mod.Logger.Event("Loading harmony for patching...");
            Harmony.DEBUG = Debug;
            _harmony = new Harmony("com.xxmicloxx.vsvolumetricshading");
            _harmony.PatchAll();

            var myOriginalMethods = _harmony.GetPatchedMethods();
            foreach (var method in myOriginalMethods)
            {
                Mod.Logger.Event("Patched " + method.FullDescription());
            }
            
            ShaderPatcher.Reload();
        }

        private static void SetConfigDefaults()
        {
            if (ModSettings.VolumetricLightingFlatness == 0)
            {
                ModSettings.VolumetricLightingFlatness = 140;
            }

            if (ModSettings.VolumetricLightingIntensity == 0)
            {
                ModSettings.VolumetricLightingIntensity = 50;
            }

            if (!ModSettings.SSRWaterTransparencySet)
            {
                ModSettings.SSRWaterTransparency = 25;
            }

            if (ModSettings.SSRReflectionDimming == 0)
            {
                ModSettings.SSRReflectionDimming = 110;
            }

            if (!ModSettings.SSRTintInfluenceSet)
            {
                ModSettings.SSRTintInfluence = 35;
            }

            if (!ModSettings.SSRSkyMixinSet)
            {
                ModSettings.SSRSkyMixin = 0;
            }

            if (!ModSettings.SSRSplashTransparencySet)
            {
                ModSettings.SSRSplashTransparency = 65;
            }

            if (ModSettings.NearShadowBaseWidth == 0)
            {
                ModSettings.NearShadowBaseWidth = 15;
            }

            if (ModSettings.SoftShadowSamples == 0)
            {
                ModSettings.SoftShadowSamples = 16;
            }

            if (!ModSettings.NearPeterPanningAdjustmentSet)
            {
                ModSettings.NearPeterPanningAdjustment = 2;
            }

            if (!ModSettings.FarPeterPanningAdjustmentSet)
            {
                ModSettings.FarPeterPanningAdjustment = 5;
            }

            if (!ModSettings.SSRRainReflectionsEnabledSet)
            {
                ModSettings.SSRRainReflectionsEnabled = true;
            }

            if (!ModSettings.SSRRefractionsEnabledSet)
            {
                ModSettings.SSRRefractionsEnabled = true;
            }

            if (!ModSettings.SSRCausticsEnabledSet)
            {
                ModSettings.SSRCausticsEnabled = true;
            }

            if (!ModSettings.UnderwaterTweaksEnabledSet)
            {
                ModSettings.UnderwaterTweaksEnabled = true;
            }
        }

        private bool OnConfigurePressed(KeyCombination cb)
        {
            if (ConfigGui == null)
            {
                ConfigGui = new ConfigGui(CApi);
            }

            if (CurrentDialog != null && CurrentDialog.IsOpened())
            {
                CurrentDialog.TryClose();
                return true;
            }

            ConfigGui.TryOpen();
            return true;
        }

        public override void Dispose()
        {
            if (CApi == null) return;
            
            ShadowTweaks.Dispose();
            _harmony?.UnpatchAll();

            Instance = null;
        }
    }
}