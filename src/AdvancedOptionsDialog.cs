using Vintagestory.API.Client;

namespace VolumetricShading
{
    public abstract class AdvancedOptionsDialog : GuiDialog
    {
        protected AdvancedOptionsDialog(ICoreClientAPI capi) : base(capi)
        {
        }

        protected abstract void RefreshValues();

        protected void OnTitleBarCloseClicked()
        {
            TryClose();
            VolumetricShadingMod.Instance.ConfigGui.TryOpen();
        }

        public override bool TryOpen()
        {
            var success = base.TryOpen();
            if (!success) return false;
            
            VolumetricShadingMod.Instance.CurrentDialog = this;
            RefreshValues();

            return true;
        }

        public override string ToggleKeyCombinationCode => null;
    }
}
