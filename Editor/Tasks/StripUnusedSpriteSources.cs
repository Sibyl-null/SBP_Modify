using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// 从资源引用和场景引用中删除所有未使用的 sprite source data
    /// </summary>
    public class StripUnusedSpriteSources : IBuildTask
    {
        public int Version => 2;

        [InjectContext] private IDependencyData m_DependencyData;
        [InjectContext(ContextUsage.In, true)] private IBuildSpriteData m_SpriteData;
        [InjectContext(ContextUsage.InOut, true)] private IBuildExtendedAssetData m_ExtendedAssetData;

        public ReturnCode Run()
        {
            if (m_SpriteData == null || m_SpriteData.ImporterData.Count == 0)
                return ReturnCode.SuccessNotRun;

            if (EditorSettings.spritePackerMode == SpritePackerMode.Disabled)
                return ReturnCode.SuccessNotRun;

            HashSet<ObjectIdentifier> unusedSources = new HashSet<ObjectIdentifier>();
            IEnumerable<ObjectIdentifier> textures = m_SpriteData.ImporterData.Values.Where(x => x.PackedSprite)
                .Select(x => x.SourceTexture);
            unusedSources.UnionWith(textures);

            // Count refs from assets
            var assetRefs = m_DependencyData.AssetInfo
                .SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in assetRefs)
                unusedSources.Remove(reference);

            // Count refs from scenes
            var sceneRefs = m_DependencyData.SceneInfo
                .SelectMany(x => x.Value.referencedObjects);
            foreach (ObjectIdentifier reference in sceneRefs)
                unusedSources.Remove(reference);

            SetOutputInformation(unusedSources);
            return ReturnCode.Success;
        }

        void SetOutputInformation(HashSet<ObjectIdentifier> unusedSources)
        {
            foreach (var source in unusedSources)
            {
                var assetInfo = m_DependencyData.AssetInfo[source.guid];
                assetInfo.includedObjects.RemoveAt(0);

                ExtendedAssetData extendedData;
                if (m_ExtendedAssetData != null && m_ExtendedAssetData.ExtendedData.TryGetValue(source.guid, out extendedData))
                {
                    extendedData.Representations.Remove(source);
                    if (extendedData.Representations.Count == 1)
                        m_ExtendedAssetData.ExtendedData.Remove(source.guid);
                }
            }
        }
    }
}
