using System;
using Vintagestory.API.Client;

namespace VolumetricShading.Patch
{
    public abstract class TargetedPatch : IShaderPatch
    {
        public string TargetFile;
        public bool ExactFilename;

        public bool ShouldPatch(string filename, string code)
        {
            var targetFile = TargetFile ?? "";

            return ExactFilename ?
                targetFile.Equals(filename, StringComparison.InvariantCultureIgnoreCase) :
                filename.ToLowerInvariant().Contains(targetFile.ToLowerInvariant());
        }

        public abstract string Patch(string filename, string code);
    }
}