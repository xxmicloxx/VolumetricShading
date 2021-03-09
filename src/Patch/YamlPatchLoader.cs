using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace VolumetricShading.Patch
{
    public class YamlPatchLoader
    {
        public static readonly AssetCategory ShaderPatches =
            new AssetCategory("shaderpatches", false, EnumAppSide.Client);

        public static readonly AssetCategory ShaderSnippets =
            new AssetCategory("shadersnippets", false, EnumAppSide.Client);

        // ReSharper disable once ClassNeverInstantiated.Local
        private class PatchEntry
        {
#pragma warning disable 649
            public string Type;
            public string Filename;
            public string Content;
            public string Snippet;
            public string Tokens;
            public string Regex;
            public bool Multiple;
            public bool Optional;
#pragma warning restore 649
        }

        private readonly ShaderPatcher _patcher;
        private readonly string _domain;
        private readonly ICoreClientAPI _capi;
        
        public YamlPatchLoader(ShaderPatcher patcher, string domain, ICoreClientAPI capi)
        {
            _patcher = patcher;
            _domain = domain;
            _capi = capi;
        }
        
        public void Load()
        {
            _capi.Assets.Reload(ShaderPatches);
            _capi.Assets.Reload(ShaderSnippets);

            var assets = _capi.Assets.GetMany("shaderpatches", _domain);
            foreach (var asset in assets)
            {
                LoadFromYaml(asset.ToText());
            }
        }

        public void LoadFromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var patches = deserializer.Deserialize<IList<PatchEntry>>(yaml);
            foreach (var patch in patches)
            {
                var content = patch.Content;
                if (!string.IsNullOrEmpty(patch.Snippet))
                {
                    content = _capi.Assets.Get(new AssetLocation(_domain, "shadersnippets/" + patch.Snippet))
                        .ToText();
                }

                switch (patch.Type)
                {
                    case "start":
                        AddStartPatch(patch, content);
                        break;
                        
                    case "end":
                        AddEndPatch(patch, content);
                        break;

                    case "regex":
                        AddRegexPatch(patch, content);
                        break;
                    
                    case "token":
                        AddTokenPatch(patch, content);
                        break;
                    
                    default:
                        throw new ArgumentException($"Invalid type {patch.Type}");
                }
            }
        }

        private void AddTokenPatch(PatchEntry patch, string content)
        {
            if (patch.Filename == null)
                _patcher.AddTokenPatch(patch.Tokens, content);
            else
                _patcher.AddTokenPatch(patch.Filename, patch.Tokens, content);
        }

        private void AddRegexPatch(PatchEntry patch, string content)
        {
            var patchObj = patch.Filename == null
                ? new RegexPatch(patch.Regex)
                : new RegexPatch(patch.Filename, patch.Regex);

            patchObj.Multiple = patch.Multiple;
            patchObj.Optional = patch.Optional;
            patchObj.ReplacementString = content;
            _patcher.AddPatch(patchObj);
        }

        private void AddEndPatch(PatchEntry patch, string content)
        {
            if (patch.Filename == null)
                _patcher.AddAtEnd(content);
            else
                _patcher.AddAtEnd(patch.Filename, content);
        }

        private void AddStartPatch(PatchEntry patch, string content)
        {
            if (patch.Filename == null)
                _patcher.AddAtStart(content);
            else
                _patcher.AddAtStart(patch.Filename, content);
        }
    }
}