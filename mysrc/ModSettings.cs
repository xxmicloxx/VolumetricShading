using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public static class ModSettings
    {
        public static bool ScreenSpaceReflectionsEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_screenSpaceReflections");
            set => ClientSettings.Inst.Bool["volumetricshading_screenSpaceReflections"] = value;
        }

        public static int VolumetricLightingFlatness
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_volumetricLightingFlatness");
            set => ClientSettings.Inst.Int["volumetricshading_volumetricLightingFlatness"] = value;
        }

        public static int VolumetricLightingIntensity
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_volumetricLightingIntensity");
            set => ClientSettings.Inst.Int["volumetricshading_volumetricLightingIntensity"] = value;
        }

        public static bool SSDOEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_SSDO");
            set => ClientSettings.Inst.Bool["volumetricshading_SSDO"] = value;
        }

        public static int SSRWaterTransparency
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRWaterTransparency");
            set => ClientSettings.Inst.Int["volumetricshading_SSRWaterTransparency"] = value;
        }

        public static bool SSRWaterTransparencySet => ClientSettings.Inst.Int.Exists("volumetricshading_SSRWaterTransparency");

        public static int SSRReflectionDimming
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRReflectionDimming");
            set => ClientSettings.Inst.Int["volumetricshading_SSRReflectionDimming"] = value;
        }

        public static int SSRTintInfluence
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRTintInfluence");
            set => ClientSettings.Inst.Int["volumetricshading_SSRTintInfluence"] = value;
        }

        public static bool SSRTintInfluenceSet => ClientSettings.Inst.Int.Exists("volumetricshading_SSRTintInfluence");

        public static int SSRSkyMixin
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRSkyMixin");
            set => ClientSettings.Inst.Int["volumetricshading_SSRSkyMixin"] = value;
        }

        public static bool SSRSkyMixinSet => ClientSettings.Inst.Int.Exists("volumetricshading_SSRSkyMixin");
    }
}
