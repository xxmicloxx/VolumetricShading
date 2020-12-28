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
    }
}
