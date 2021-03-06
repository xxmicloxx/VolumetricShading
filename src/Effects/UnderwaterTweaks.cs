using Vintagestory.API.MathTools;

namespace VolumetricShading.Effects
{
    public class UnderwaterTweaks
    {
        private VolumetricShadingMod _mod;

        private bool _enabled;

        private float _oldFogDensity;
        private float _oldFogMin;

        private WeightedFloatArray _ambient;
        private WeightedFloatArray _oldAmbient;

        public UnderwaterTweaks(VolumetricShadingMod mod)
        {
            _mod = mod;
            
            mod.CApi.Settings.AddWatcher<bool>("volumetricshading_underwaterTweaks", SetEnabled);
            SetEnabled(ModSettings.UnderwaterTweaksEnabled);

            mod.Events.PostWaterChangeSight += OnWaterModifierChanged;
        }

        private void SetEnabled(bool enabled)
        {
            if (enabled && !_enabled)
            {
                PatchAmbientManager();
            }
            else if (!enabled && _enabled)
            {
                RestoreAmbientManager();
            }

            _enabled = enabled;
        }
        
        private void RestoreAmbientManager()
        {
            var waterModifier = _mod.CApi.Ambient.CurrentModifiers["water"];
            waterModifier.FogDensity.Value = _oldFogDensity;
            waterModifier.FogMin.Value = _oldFogMin;
            waterModifier.AmbientColor = _oldAmbient;
        }
        
        private void PatchAmbientManager()
        {
            var waterModifier = _mod.CApi.Ambient.CurrentModifiers["water"];
            _oldFogDensity = waterModifier.FogDensity.Value;
            _oldFogMin = waterModifier.FogMin.Value;
            _oldAmbient = waterModifier.AmbientColor;

            waterModifier.FogMin.Value = 0.25f;
            waterModifier.FogDensity.Value = 0.015f;

            _ambient = waterModifier.AmbientColor.Clone();
            waterModifier.AmbientColor = _ambient;
        }

        private void OnWaterModifierChanged()
        {
            if (!_enabled) return;

            var waterModifier = _mod.CApi.Ambient.CurrentModifiers["water"];
            waterModifier.AmbientColor = _ambient;
            var ambient = _ambient.Value;
            ambient[0] = 1.5f;
            ambient[1] = 1.5f;
            ambient[2] = 2.0f;

            waterModifier.FogColor.Weight = _ambient.Weight;

            var fog = waterModifier.FogColor.Value;
            var fogInt = (int) (fog[0] * 255) | ((int) (fog[1] * 255) << 8) | ((int) (fog[2] * 255) << 16);
            const int blueInt = 0xd68100;
            fogInt = ColorUtil.ColorOverlay(fogInt, blueInt, 0.5f);
            fog[0] = (fogInt & 255) / 255.0f;
            fog[1] = ((fogInt >> 8) & 255) / 255.0f;
            fog[2] = ((fogInt >> 16) & 255) / 255.0f;
        }
    }
}