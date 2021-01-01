using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading
{
    public class ConfigGui : GuiDialog
    {
        public ConfigGui(ICoreClientAPI capi) : base(capi)
        {
            SetupDialog();
            
            capi.Settings.AddWatcher<int>("godRays", _ => RefreshValues());
        }

        private void SetupDialog()
        {
            var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom)
                .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, -GuiStyle.DialogToScreenPadding);

            const int switchSize = 20;
            const int switchPadding = 10;
            var font = CairoFont.WhiteSmallText();
            
            var switchBounds = ElementBounds.Fixed(210.0, GuiStyle.TitleBarHeight, switchSize, switchSize);
            var textBounds = ElementBounds.Fixed(0, GuiStyle.TitleBarHeight + 1.0, 200.0, switchSize);
            var advancedButtonBounds = ElementBounds.Fixed(240.0, GuiStyle.TitleBarHeight, 110.0, switchSize);
            
            var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            SingleComposer = capi.Gui.CreateCompo("volumetricShadingConfigure", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Volumetric Shading Configuration", OnTitleBarCloseClicked)
                .BeginChildElements(bgBounds)
                
                .AddSwitch(ToggleVolumetricLighting, switchBounds, "toggleVolumetricLighting", switchSize)
                .AddStaticText("Volumetric Lighting", font, textBounds)
                .AddHoverText("Enables realistic scattering of light.", font, 250, textBounds.FlatCopy())
                .AddSmallButton("Advanced...", OnVolumetricAdvancedClicked, advancedButtonBounds)
                
                .AddSwitch(ToggleScreenSpaceReflections, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)), "toggleSSR", switchSize)
                .AddStaticText("Screen Space Reflections", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("Toggles reflections on water.", font, 250, textBounds.FlatCopy())
                .AddSmallButton("Advanced...", OnSSRAdvancedClicked, (advancedButtonBounds = advancedButtonBounds.BelowCopy(fixedDeltaY:switchPadding)))
                
                .AddSwitch(ToggleSSDO, (switchBounds = switchBounds.BelowCopy(fixedDeltaY:switchPadding)), "toggleSSDO", switchSize)
                .AddStaticText("Improve SSAO", font, (textBounds = textBounds.BelowCopy(fixedDeltaY:switchPadding)))
                .AddHoverText("Replaces SSAO with SSDO. Results in marginally faster and better looking occlusions.", font, 250, textBounds.FlatCopy())
                
                .EndChildElements()
                .Compose();
        }

        public override bool TryOpen()
        {
            var success = base.TryOpen();
            if (!success) return false;
            
            RefreshValues();
            VolumetricShadingMod.Instance.CurrentDialog = this;

            return true;
        }
        
        private void RefreshValues()
        {
            if (!IsOpened()) return;
            
            SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
            SingleComposer.GetSwitch("toggleSSR").On = ModSettings.ScreenSpaceReflectionsEnabled;
            SingleComposer.GetSwitch("toggleSSDO").On = ModSettings.SSDOEnabled;
        }
        
        private void ToggleVolumetricLighting(bool on)
        {
            if (on && ClientSettings.ShadowMapQuality == 0)
            {
                // need shadowmapping
                ClientSettings.ShadowMapQuality = 1;
            }
            
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
        
        private void OnTitleBarCloseClicked()
        {
            TryClose();
        }
        
        public override string ToggleKeyCombinationCode => "volumetriclightingconfigure";
    }
}
