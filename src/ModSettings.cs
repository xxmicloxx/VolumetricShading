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

        public static bool SSRRainReflectionsEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_SSRRainReflections");
            set => ClientSettings.Inst.Bool["volumetricshading_SSRRainReflections"] = value;
        }
        
        public static bool SSRRainReflectionsEnabledSet =>
            ClientSettings.Inst.Bool.Exists("volumetricshading_SSRRainReflections");

        public static bool SSRRefractionsEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_SSRRefractions");
            set => ClientSettings.Inst.Bool["volumetricshading_SSRRefractions"] = value;
        }

        public static bool SSRRefractionsEnabledSet =>
            ClientSettings.Inst.Bool.Exists("volumetricshading_SSRRefractions");

        public static bool SSRCausticsEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_SSRCaustics");
            set => ClientSettings.Inst.Bool["volumetricshading_SSRCaustics"] = value;
        }

        public static bool SSRCausticsEnabledSet =>
            ClientSettings.Inst.Bool.Exists("volumetricshading_SSRCaustics");

        public static int SSRWaterTransparency
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRWaterTransparency");
            set => ClientSettings.Inst.Int["volumetricshading_SSRWaterTransparency"] = value;
        }

        public static bool SSRWaterTransparencySet =>
            ClientSettings.Inst.Int.Exists("volumetricshading_SSRWaterTransparency");

        public static int SSRSplashTransparency
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_SSRSplashTransparency");
            set => ClientSettings.Inst.Int["volumetricshading_SSRSplashTransparency"] = value;
        }

        public static bool SSRSplashTransparencySet =>
            ClientSettings.Inst.Int.Exists("volumetricshading_SSRSplashTransparency");

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

        public static int OverexposureIntensity
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_overexposureIntensity");
            set => ClientSettings.Inst.Int["volumetricshading_overexposureIntensity"] = value;
        }

        public static int SunBloomIntensity
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_sunBloomIntensity");
            set => ClientSettings.Inst.Int["volumetricshading_sunBloomIntensity"] = value;
        }

        public static int NearShadowBaseWidth
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_nearShadowBaseWidth");
            set => ClientSettings.Inst.Int["volumetricshading_nearShadowBaseWidth"] = value;
        }

        public static bool SoftShadowsEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_softShadows");
            set => ClientSettings.Inst.Bool["volumetricshading_softShadows"] = value;
        }

        public static int SoftShadowSamples
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_softShadowSamples");
            set => ClientSettings.Inst.Int["volumetricshading_softShadowSamples"] = value;
        }
        
        public static int NearPeterPanningAdjustment
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_nearPeterPanningAdjustment");
            set => ClientSettings.Inst.Int["volumetricshading_nearPeterPanningAdjustment"] = value;
        }

        public static bool NearPeterPanningAdjustmentSet =>
            ClientSettings.Inst.Int.Exists("volumetricshading_nearPeterPanningAdjustment");
        
        public static int FarPeterPanningAdjustment
        {
            get => ClientSettings.Inst.GetIntSetting("volumetricshading_farPeterPanningAdjustment");
            set => ClientSettings.Inst.Int["volumetricshading_farPeterPanningAdjustment"] = value;
        }
        
        public static bool FarPeterPanningAdjustmentSet =>
            ClientSettings.Inst.Int.Exists("volumetricshading_farPeterPanningAdjustment");

        public static bool UnderwaterTweaksEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_underwaterTweaks");
            set => ClientSettings.Inst.Bool["volumetricshading_underwaterTweaks"] = value;
        }

        public static bool UnderwaterTweaksEnabledSet =>
            ClientSettings.Inst.Bool.Exists("volumetricshading_underwaterTweaks");

        public static bool DeferredLightingEnabled
        {
            get => ClientSettings.Inst.GetBoolSetting("volumetricshading_deferredLighting");
            set => ClientSettings.Inst.Bool["volumetricshading_deferredLighting"] = value;
        }
    }
}