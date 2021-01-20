namespace VolumetricShading
{
    public class ShadowTweaks
    {
        private readonly VolumetricShadingMod _mod;
        public int NearShadowBaseWidth { get; private set; }

        public ShadowTweaks(VolumetricShadingMod mod)
        {
            _mod = mod;

            _mod.CApi.Settings.AddWatcher<int>("volumetricshading_nearShadowBaseWidth", OnNearShadowBaseWidthChanged);
            NearShadowBaseWidth = ModSettings.NearShadowBaseWidth;
            
            _mod.ShaderInjector.RegisterFloatProperty("VSMOD_NEARSHADOWOFFSET",
                () => ModSettings.NearPeterPanningAdjustment);
            
            _mod.ShaderInjector.RegisterFloatProperty("VSMOD_FARSHADOWOFFSET",
                () => ModSettings.FarPeterPanningAdjustment);
        }
        
        private void OnNearShadowBaseWidthChanged(int newVal)
        {
            NearShadowBaseWidth = newVal;
        }
    }
}