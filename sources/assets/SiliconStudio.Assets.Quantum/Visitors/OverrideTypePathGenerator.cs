﻿using SiliconStudio.Assets.Yaml;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;

namespace SiliconStudio.Assets.Quantum.Visitors
{
    /// <summary>
    /// An implementation of <see cref="AssetNodeMetadataCollectorBase"/> that generates the path to all object references in the given asset.
    /// </summary>
    public class OverrideTypePathGenerator : AssetNodeMetadataCollectorBase
    {
        /// <summary>
        /// Gets the resulting metadata that can be passed to YAML serialization.
        /// </summary>
        public YamlAssetMetadata<OverrideType> Result { get; } = new YamlAssetMetadata<OverrideType>();

        /// <inheritdoc/>
        protected override void VisitMemberNode(IAssetMemberNode memberNode, int inNonIdentifiableType)
        {
            if (memberNode?.IsContentOverridden() == true)
            {
                Result.Set(ConvertPath(CurrentPath, inNonIdentifiableType), memberNode.GetContentOverride());
            }
        }

        /// <inheritdoc/>
        protected override void VisitObjectNode(IAssetObjectNode objectNode, int inNonIdentifiableType)
        {
            foreach (var index in objectNode.GetOverriddenItemIndices())
            {
                var id = objectNode.IndexToId(index);
                var itemPath = ConvertPath(CurrentPath, inNonIdentifiableType);
                itemPath.PushItemId(id);
                Result.Set(itemPath, objectNode.GetItemOverride(index));
            }
            foreach (var index in objectNode.GetOverriddenKeyIndices())
            {
                var id = objectNode.IndexToId(index);
                var itemPath = ConvertPath(CurrentPath, inNonIdentifiableType);
                itemPath.PushIndex(id);
                Result.Set(itemPath, objectNode.GetKeyOverride(index));
            }
        }
    }
}