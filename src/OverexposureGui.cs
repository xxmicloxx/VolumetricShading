using Vintagestory.API.Client;

namespace VolumetricShading
{
    public class OverexposureGui : AdvancedOptionsDialog
    {
        public OverexposureGui(ICoreClientAPI capi) : base(capi)
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

            SingleComposer = capi.Gui.CreateCompo("vsmodOverexposureConfigure", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Overexposure Options", OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)
                .AddStaticText("Intensity", font, textBounds)
                .AddHoverText("The intensity of the Overexposure effect", font, 260, textBounds)
                .AddSlider(OnIntensitySliderChanged, switchBounds.FlatCopy().WithFixedWidth(sliderWidth),
                    "intensitySlider")
                .AddStaticText("Sun Bloom", font, (textBounds = textBounds.BelowCopy(fixedDeltaY: switchPadding)))
                .AddHoverText("Defines how strong the additional sun blooming is", font, 260, textBounds)
                .AddSlider(OnSunBloomChanged,
                    (switchBounds = switchBounds.BelowCopy(fixedDeltaY: switchPadding)).FlatCopy()
                    .WithFixedWidth(sliderWidth), "sunBloomSlider")
                .EndChildElements()
                .Compose();

            SingleComposer.GetSlider("intensitySlider").TriggerOnlyOnMouseUp();
        }

        protected override void RefreshValues()
        {
            SingleComposer.GetSlider("intensitySlider").SetValues(ModSettings.OverexposureIntensity, 0, 200, 1);
            SingleComposer.GetSlider("sunBloomSlider").SetValues(ModSettings.SunBloomIntensity, 0, 100, 1);
        }

        private bool OnIntensitySliderChanged(int t1)
        {
            ModSettings.OverexposureIntensity = t1;
            capi.Shader.ReloadShaders();
            return true;
        }

        private bool OnSunBloomChanged(int t1)
        {
            ModSettings.SunBloomIntensity = t1;
            return true;
        }
    }
}