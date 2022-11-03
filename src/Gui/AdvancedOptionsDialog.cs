using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VolumetricShading.Gui
{
    public abstract class AdvancedOptionsDialog : GuiDialog
    {
        public class ConfigOption
        {
            public string SwitchKey;
            public string SliderKey;

            public string Text;
            public string Tooltip;

            public Action<bool> ToggleAction;
            public ActionConsumable<int> SlideAction;

            public bool InstantSlider;
        }

        protected List<ConfigOption> ConfigOptions = new List<ConfigOption>();
        private bool _isSetup;
        
        protected AdvancedOptionsDialog(ICoreClientAPI capi) : base(capi)
        {
        }

        protected void RegisterOption(ConfigOption option)
        {
            ConfigOptions.Add(option);
        }

        protected void SetupDialog()
        {
            _isSetup = true;
            
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

            var composer = capi.Gui.CreateCompo(DialogKey, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds);

            foreach (var option in ConfigOptions)
            {
                composer.AddStaticText(option.Text, font, textBounds);
                if (option.Tooltip != null)
                {
                    composer.AddHoverText(option.Tooltip, font, 260, textBounds);
                }
                
                if (option.SliderKey != null)
                {
                    composer.AddSlider(option.SlideAction, switchBounds.FlatCopy().WithFixedWidth(sliderWidth),
                        option.SliderKey);
                }
                else if (option.SwitchKey != null)
                {
                    composer.AddSwitch(option.ToggleAction, switchBounds, option.SwitchKey, switchSize);
                }

                textBounds = textBounds.BelowCopy(fixedDeltaY: switchPadding);
                switchBounds = switchBounds.BelowCopy(fixedDeltaY: switchPadding);
            }

            SingleComposer = composer.EndChildElements().Compose();

            foreach (var option in ConfigOptions.Where(option => option.SliderKey != null && !option.InstantSlider))
            {
                SingleComposer.GetSlider(option.SliderKey).TriggerOnlyOnMouseUp();
            }
        }
        
        public override bool TryOpen()
        {
            if (!_isSetup) SetupDialog();
            
            var success = base.TryOpen();
            if (!success) return false;

            VolumetricShadingMod.Instance.CurrentDialog = this;
            RefreshValues();

            return true;
        }

        protected abstract string DialogKey { get; }
        protected abstract string DialogTitle { get; }
        
        protected abstract void RefreshValues();

        protected void OnTitleBarCloseClicked()
        {
            TryClose();
            VolumetricShadingMod.Instance.ConfigGui.TryOpen();
        }

        public override string ToggleKeyCombinationCode => null;
    }
}