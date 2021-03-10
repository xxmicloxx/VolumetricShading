using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace VolumetricShading
{
    public class Uniforms : IRenderer
    {
        private readonly VolumetricShadingMod _mod;

        // ReSharper disable once InconsistentNaming
        private readonly Vec4f _tempVec4f = new Vec4f();
        
        public readonly float[] InvProjectionMatrix = Mat4f.Create();
        public readonly float[] InvModelViewMatrix = Mat4f.Create();
        public readonly Vec4f CameraWorldPosition = new Vec4f();
        public float DayLight { get; private set; }
        
        public Uniforms(VolumetricShadingMod mod)
        {
            _mod = mod;
            
            mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Before);
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            // before rendering: update our values
            Mat4f.Invert(InvProjectionMatrix, _mod.CApi.Render.CurrentProjectionMatrix);
            Mat4f.Invert(InvModelViewMatrix, _mod.CApi.Render.CameraMatrixOriginf);

            _tempVec4f.Set(0, 0, 0, 1);
            Mat4f.MulWithVec4(InvModelViewMatrix, _tempVec4f, CameraWorldPosition);
            
            DayLight = 1.25f * GameMath.Max(
                           _mod.CApi.World.Calendar.DayLightStrength -
                           _mod.CApi.World.Calendar.MoonLightStrength / 2f, 0.05f);
        }

        public void Dispose()
        {
        }

        public double RenderOrder => 0.1; // after camera!
        public int RenderRange => int.MaxValue;
    }
}