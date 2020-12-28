using Vintagestory.API.Client;

namespace VolumetricShading
{
    class ScreenSpaceReflectionsGui : AdvancedOptionsDialog
    {
        public ScreenSpaceReflectionsGui(ICoreClientAPI capi) : base(capi)
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

            var switchBounds = ElementBounds.Fixed(250, GuiStyle.TitleBarHeight, switchSize, switchSize);
            var textBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight + 1.0, 240.0, switchSize);

            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("vsmodVolumetricLightingConfigure", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Volumetric Lighting Options", OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)

                .AddSwitch(ToggleSSR, switchBounds, "toggleSSR", switchSize)
                .AddStaticText("Enable Screen Space Reflections", font, textBounds)
                
                .AddStaticText("Reflection dimming", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("The dimming effect strength on the reflected image", font, 260, textBounds)
                .AddSlider(OnDimmingSliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "dimmingSlider")
                
                .AddStaticText("Water transparency", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("Sets the transparency of the vanilla water effect", font, 260, textBounds)
                .AddSlider(OnTransparencySliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "transparencySlider")

                .AddStaticText("Tint influence", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("Sets the influence an object's tint has on it's reflection color", font, 260, textBounds)
                .AddSlider(OnTintSliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "tintSlider")

                .AddStaticText("Sky mixin", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("The amount of sky color that is always visible, even when fully reflecting", font, 260, textBounds)
                .AddSlider(OnSkyMixinSliderChanged, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)).FlatCopy().WithFixedWidth(sliderWidth), "skyMixinSlider")

                .EndChildElements()
                .Compose();

            SingleComposer.GetSlider("dimmingSlider").TriggerOnlyOnMouseUp();
            SingleComposer.GetSlider("transparencySlider").TriggerOnlyOnMouseUp();
            SingleComposer.GetSlider("tintSlider").TriggerOnlyOnMouseUp();
            SingleComposer.GetSlider("skyMixinSlider").TriggerOnlyOnMouseUp();
        }

        protected override void RefreshValues()
        {
            SingleComposer.GetSwitch("toggleSSR").SetValue(ModSettings.ScreenSpaceReflectionsEnabled);
            SingleComposer.GetSlider("dimmingSlider").SetValues(ModSettings.SSRReflectionDimming, 1, 400, 1);
            SingleComposer.GetSlider("transparencySlider").SetValues(ModSettings.SSRWaterTransparency, 0, 100, 1);
            SingleComposer.GetSlider("tintSlider").SetValues(ModSettings.SSRTintInfluence, 0, 100, 1);
            SingleComposer.GetSlider("skyMixinSlider").SetValues(ModSettings.SSRSkyMixin, 0, 100, 1);
        }

        private void ToggleSSR(bool on)
        {
            ModSettings.ScreenSpaceReflectionsEnabled = on;

            capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private bool OnDimmingSliderChanged(int value)
        {
            ModSettings.SSRReflectionDimming = value;

            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }

        private bool OnTransparencySliderChanged(int value)
        {
            ModSettings.SSRWaterTransparency = value;

            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }

        private bool OnTintSliderChanged(int value)
        {
            ModSettings.SSRTintInfluence = value;

            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }

        private bool OnSkyMixinSliderChanged(int value)
        {
            ModSettings.SSRSkyMixin = value;

            capi.Shader.ReloadShaders();
            RefreshValues();
            return true;
        }
    }
}
