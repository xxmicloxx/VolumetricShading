using Vintagestory.API.Client;
using Vintagestory.API.Common;
using HarmonyLib;

namespace VolumetricShading
{
    public class VolumetricShadingMod : ModSystem
    {
        public static VolumetricShadingMod Instance;

        public ICoreClientAPI CApi;

        public ShaderInjector ShaderInjector;
        public ScreenSpaceReflections ScreenSpaceReflections;
        public VolumetricLighting VolumetricLighting;
        public ConfigGui ConfigGui;
        public GuiDialog CurrentDialog;

        private Harmony _harmony;

        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            Instance = this;
            CApi = api;
            
            ShaderInjector = new ShaderInjector();
            VolumetricLighting = new VolumetricLighting(this);
            ScreenSpaceReflections = new ScreenSpaceReflections(this);

            PatchGame();
            RegisterHotkeys();
            SetConfigDefaults();
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
            _harmony = new Harmony("com.xxmicloxx.vsvolumetricshading");
            _harmony.PatchAll();

            var myOriginalMethods = _harmony.GetPatchedMethods();
            foreach (var method in myOriginalMethods)
            {
                Mod.Logger.Event("Patched " + method.FullDescription());
            }
        }
        
        private static void SetConfigDefaults()
        {
            if (ModSettings.VolumetricLightingFlatness == 0)
            {
                ModSettings.VolumetricLightingFlatness = 145;
            }

            if (ModSettings.VolumetricLightingIntensity == 0)
            {
                ModSettings.VolumetricLightingIntensity = 35;
            }

            if (!ModSettings.SSRWaterTransparencySet)
            {
                ModSettings.SSRWaterTransparency = 50;
            }

            if (ModSettings.SSRReflectionDimming == 0)
            {
                ModSettings.SSRReflectionDimming = 170;
            }

            if (!ModSettings.SSRTintInfluenceSet)
            {
                ModSettings.SSRTintInfluence = 50;
            }

            if (!ModSettings.SSRSkyMixinSet)
            {
                ModSettings.SSRSkyMixin = 15;
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
            
            _harmony?.UnpatchAll();

            Instance = null;
        }
    }
}
