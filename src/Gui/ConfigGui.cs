using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace VolumetricShading.Gui
{
    public abstract class MainConfigDialog : GuiDialog
    {
        private bool _isSetup;

        protected List<ConfigOption> ConfigOptions = new List<ConfigOption>();

        protected MainConfigDialog(ICoreClientAPI capi) : base(capi)
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
            var font = CairoFont.WhiteSmallText();

            var switchBounds = ElementBounds.Fixed(210.0, GuiStyle.TitleBarHeight,
                switchSize, switchSize);

            var textBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight + 1.0,
                200.0, switchSize);

            var advancedButtonBounds = ElementBounds.Fixed(240.0, GuiStyle.TitleBarHeight,
                110.0, switchSize);

            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            var composer = capi.Gui.CreateCompo("volumetricShadingConfigure", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Volumetric Shading Configuration", OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds);

            foreach (var option in ConfigOptions)
            {
                composer.AddStaticText(option.Text, font, textBounds);
                if (option.Tooltip != null) composer.AddHoverText(option.Tooltip, font, 250, textBounds.FlatCopy());

                if (option.SwitchKey != null)
                    composer.AddSwitch(option.ToggleAction, switchBounds, option.SwitchKey, switchSize);

                if (option.AdvancedAction != null)
                    composer.AddSmallButton("Advanced...", option.AdvancedAction, advancedButtonBounds);

                switchBounds = switchBounds.BelowCopy(fixedDeltaY: switchPadding);
                textBounds = textBounds.BelowCopy(fixedDeltaY: switchPadding);
                advancedButtonBounds = advancedButtonBounds.BelowCopy(fixedDeltaY: switchPadding);
            }

            SingleComposer = composer.EndChildElements().Compose();
        }

        public override bool TryOpen()
        {
            if (!_isSetup) SetupDialog();

            var success = base.TryOpen();
            if (!success) return false;

            RefreshValues();
            VolumetricShadingMod.Instance.CurrentDialog = this;

            return true;
        }

        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }

        protected abstract void RefreshValues();

        public class ConfigOption
        {
            public ActionConsumable AdvancedAction;
            public string SwitchKey;

            public string Text;

            public Action<bool> ToggleAction;

            public string Tooltip;
        }
    }

    public class ConfigGui : MainConfigDialog
    {
        public ConfigGui(ICoreClientAPI capi) : base(capi)
        {
            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleVolumetricLighting",
                Text = "Volumetric Lighting",
                Tooltip = "Enables realistic scattering of light",
                ToggleAction = ToggleVolumetricLighting,
                AdvancedAction = OnVolumetricAdvancedClicked
            });

            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleSSR",
                Text = "Screen Space Reflections",
                Tooltip = "Enables reflections, for example on water",
                ToggleAction = ToggleScreenSpaceReflections,
                AdvancedAction = OnSSRAdvancedClicked
            });

            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleOverexposure",
                Text = "Overexposure",
                Tooltip = "Adds overexposure at brightly sunlit places",
                ToggleAction = ToggleOverexposure,
                AdvancedAction = OnOverexposureAdvancedClicked
            });

            RegisterOption(new ConfigOption
            {
                Text = "Shadow Tweaks",
                Tooltip = "Allows for some shadow tweaks that might make them look better",
                AdvancedAction = OnShadowTweaksAdvancedClicked
            });

            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleUnderwater",
                Text = "Underwater Tweaks",
                Tooltip = "Better underwater looks",
                ToggleAction = ToggleUnderwater
            });

            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleDeferred",
                Text = "Deferred Lighting",
                Tooltip = "Aims to improve lighting performance by deferring lighting operations. Requires SSAO.",
                ToggleAction = ToggleDeferredLighting
            });

            RegisterOption(new ConfigOption
            {
                SwitchKey = "toggleSSDO",
                Text = "Improve SSAO",
                Tooltip = "Replaces SSAO with SSDO. Results in marginally faster and better looking occlusions.",
                ToggleAction = ToggleSSDO
            });

            SetupDialog();

            capi.Settings.AddWatcher<int>("godRays", _ => RefreshValues());
        }

        public override string ToggleKeyCombinationCode => "volumetriclightingconfigure";

        protected override void RefreshValues()
        {
            if (!IsOpened()) return;

            SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
            SingleComposer.GetSwitch("toggleSSR").On = ModSettings.ScreenSpaceReflectionsEnabled;
            SingleComposer.GetSwitch("toggleSSDO").On = ModSettings.SSDOEnabled;
            SingleComposer.GetSwitch("toggleOverexposure").On = ModSettings.OverexposureIntensity > 0;
            SingleComposer.GetSwitch("toggleUnderwater").On = ModSettings.UnderwaterTweaksEnabled;
            SingleComposer.GetSwitch("toggleDeferred").On = ModSettings.DeferredLightingEnabled;
        }

        private void ToggleUnderwater(bool enabled)
        {
            ModSettings.UnderwaterTweaksEnabled = enabled;

            RefreshValues();
        }

        private void ToggleDeferredLighting(bool enabled)
        {
            ModSettings.DeferredLightingEnabled = enabled;
            capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private void ToggleVolumetricLighting(bool on)
        {
            if (on && ClientSettings.ShadowMapQuality == 0)
                // need shadowmapping
                ClientSettings.ShadowMapQuality = 1;

            ClientSettings.GodRayQuality = on ? 1 : 0;

            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private void ToggleScreenSpaceReflections(bool on)
        {
            ModSettings.ScreenSpaceReflectionsEnabled = on;

            capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private void ToggleSSDO(bool on)
        {
            if (on && ClientSettings.SSAOQuality == 0)
            {
                ClientSettings.SSAOQuality = 1;
                capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            }

            ModSettings.SSDOEnabled = on;
            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private void ToggleOverexposure(bool on)
        {
            if (on && ModSettings.OverexposureIntensity <= 0) ModSettings.OverexposureIntensity = 50;
            else if (!on && ModSettings.OverexposureIntensity > 0) ModSettings.OverexposureIntensity = 0;

            capi.Shader.ReloadShaders();
            RefreshValues();
        }

        private bool OnVolumetricAdvancedClicked()
        {
            TryClose();
            var advancedGui = new VolumetricLightingGui(capi);
            advancedGui.TryOpen();
            return true;
        }

        private bool OnSSRAdvancedClicked()
        {
            TryClose();
            var advancedGui = new ScreenSpaceReflectionsGui(capi);
            advancedGui.TryOpen();
            return true;
        }

        private bool OnOverexposureAdvancedClicked()
        {
            TryClose();
            var advancedGui = new OverexposureGui(capi);
            advancedGui.TryOpen();
            return true;
        }

        private bool OnShadowTweaksAdvancedClicked()
        {
            TryClose();
            var shadowTweaksGui = new ShadowTweaksGui(capi);
            shadowTweaksGui.TryOpen();
            return true;
        }
    }
}