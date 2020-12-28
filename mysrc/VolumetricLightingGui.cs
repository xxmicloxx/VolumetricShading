using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    class VolumetricLightingGui : AdvancedOptionsDialog
    {
        public VolumetricLightingGui(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
        }

        private void SetupDialog()
        {
            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, -GuiStyle.DialogToScreenPadding);

            const int switchSize = 20;
            const int switchPadding = 10;
            const double sliderWidth = 200.0;
            var font = CairoFont.WhiteSmallText();

            var switchBounds = ElementBounds.Fixed(230, GuiStyle.TitleBarHeight, switchSize, switchSize);
            var textBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight + 1.0, 220.0, switchSize);

            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("vsmodVolumetricLightingConfigure", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Volumetric Lighting Options", OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)

                .AddSwitch(ToggleVolumetricLighting, switchBounds, "toggleVolumetricLighting", switchSize)
                .AddStaticText("Enable Volumetric Lighting", font, textBounds)
                
                .AddStaticText("Intensity", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("The intensity of the Volumetric Lighting effect", font, 260, textBounds)
                .AddSlider(OnIntensitySliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "intensitySlider")
                
                .AddStaticText("Flatness", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("Defines how noticeable the difference between low and high scattering is", font, 260, textBounds)
                .AddSlider(OnFlatnessSliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "flatnessSlider")

                .EndChildElements()
                .Compose();

            SingleComposer.GetSlider("flatnessSlider").TriggerOnlyOnMouseUp();
            SingleComposer.GetSlider("intensitySlider").TriggerOnlyOnMouseUp();
        }

        protected override void RefreshValues()
        {
            if (!IsOpened()) return;

            SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
            SingleComposer.GetSlider("flatnessSlider").SetValues(
                ModSettings.VolumetricLightingFlatness, 1, 199, 1);
            SingleComposer.GetSlider("intensitySlider").SetValues(
                ModSettings.VolumetricLightingIntensity, 1, 100, 1);
        }

        private void ToggleVolumetricLighting(bool on)
        {
            ClientSettings.GodRayQuality = on ? 1 : 0;
            if (on && ClientSettings.ShadowMapQuality == 0)
            {
                // need shadowmapping
                ClientSettings.ShadowMapQuality = 1;
            }
            
            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private bool OnFlatnessSliderChanged(int value)
        {
            ModSettings.VolumetricLightingFlatness = value;
            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }
        
        private bool OnIntensitySliderChanged(int value)
        {
            ModSettings.VolumetricLightingIntensity = value;
            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }
    }
}
