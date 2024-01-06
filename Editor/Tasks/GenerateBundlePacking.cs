using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Utilities;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// 组装每个 AssetBundle 并计算资产加载文件依赖项列表.
    /// </summary>
    public class GenerateBundlePacking : IBuildTask
    {
        public int Version => 1;

        [InjectContext(ContextUsage.In)] private IBundleBuildContent m_BuildContent;
        [InjectContext(ContextUsage.In)] private IDependencyData m_DependencyData;
        [InjectContext(ContextUsage.In)] private IDeterministicIdentifiers m_PackingMethod;
        [InjectContext(ContextUsage.In, true)] private ICustomAssets m_CustomAssets;
        [InjectContext] private IBundleWriteData m_WriteData;

        static bool ValidAssetBundle(List<GUID> assets, HashSet<GUID> customAssets)
        {
            // 使用 AssetDatabase 确认是否所有的资产都是 asset 而不是 scene，或者是 custom asset
            return assets.All(x =>
                ValidationMethods.ValidAsset(x) == ValidationMethods.Status.Asset || customAssets.Contains(x));
        }

        public ReturnCode Run()
        {
            Dictionary<GUID, List<GUID>> assetToReferences = new Dictionary<GUID, List<GUID>>();
            HashSet<GUID> customAssets = new HashSet<GUID>();
            if (m_CustomAssets != null)
                customAssets.UnionWith(m_CustomAssets.Assets);

            PackAllBundles(customAssets, assetToReferences);
            CalculateLoadDependencyList(assetToReferences);

            return ReturnCode.Success;
        }

        private void PackAllBundles(HashSet<GUID> customAssets, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            foreach (KeyValuePair<string, List<GUID>> bundle in m_BuildContent.BundleLayout)
            {
                if (ValidAssetBundle(bundle.Value, customAssets)) // 资源组装
                    PackAssetBundle(bundle.Key, bundle.Value, assetToReferences);
                else if (ValidationMethods.ValidSceneBundle(bundle.Value)) // 场景组装
                    PackSceneBundle(bundle.Key, bundle.Value, assetToReferences);
            }
        }
        
        private void CalculateLoadDependencyList(Dictionary<GUID, List<GUID>> assetToReferences)
        {
            // Calculate Asset file load dependency list
            foreach (List<GUID> guids in m_BuildContent.BundleLayout.Values)
            {
                foreach (GUID asset in guids)
                {
                    List<string> depFiles = m_WriteData.AssetToFiles[asset];
                    
                    foreach (GUID reference in assetToReferences[asset])
                    {
                        List<string> referenceFiles = m_WriteData.AssetToFiles[reference];
                        
                        // referenceFiles[0] 是该 asset 的主文件
                        if (!depFiles.Contains(referenceFiles[0]))
                            depFiles.Add(referenceFiles[0]);
                    }
                }
            }
        }

        void PackAssetBundle(string bundleName, List<GUID> includedAssets, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            // CommonStrings.AssetBundleNameFormat = "archive:/{0}/{0}"
            string internalName = string.Format(CommonStrings.AssetBundleNameFormat,
                m_PackingMethod.GenerateInternalFileName(bundleName));

            HashSet<ObjectIdentifier> allObjects = new HashSet<ObjectIdentifier>();
            Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets =
                new Dictionary<GUID, HashSet<ObjectIdentifier>>();
            
            // 遍历 bundle 内所有的 asset（反映的是外部设定的 AssetBundleBuild ）
            foreach (GUID asset in includedAssets)
            {
                AssetLoadInfo assetInfo = m_DependencyData.AssetInfo[asset];
                allObjects.UnionWith(assetInfo.includedObjects);

                List<ObjectIdentifier> references = new List<ObjectIdentifier>();
                references.AddRange(assetInfo.referencedObjects);
                
                assetToReferences[asset] = FilterReferencesForAsset(asset, references, 
                    null, null, assetObjectIdentifierHashSets);

                allObjects.UnionWith(references);
                m_WriteData.AssetToFiles[asset] = new List<string> { internalName };
            }

            m_WriteData.FileToBundle.Add(internalName, bundleName);
            m_WriteData.FileToObjects.Add(internalName, allObjects.ToList());
        }

        void PackSceneBundle(string bundleName, List<GUID> includedScenes, Dictionary<GUID, List<GUID>> assetToReferences)
        {
            if (includedScenes.IsNullOrEmpty())
                return;

            string firstFileName = "";
            HashSet<ObjectIdentifier> previousSceneObjects = new HashSet<ObjectIdentifier>();
            HashSet<GUID> previousSceneAssets = new HashSet<GUID>();
            List<string> sceneInternalNames = new List<string>();
            Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets = new Dictionary<GUID, HashSet<ObjectIdentifier>>();
            foreach (var scene in includedScenes)
            {
                var scenePath = AssetDatabase.GUIDToAssetPath(scene.ToString());
                var internalSceneName = m_PackingMethod.GenerateInternalFileName(scenePath);
                if (string.IsNullOrEmpty(firstFileName))
                    firstFileName = internalSceneName;
                var internalName = string.Format(CommonStrings.SceneBundleNameFormat, firstFileName, internalSceneName);

                SceneDependencyInfo sceneInfo = m_DependencyData.SceneInfo[scene];

                var references = new List<ObjectIdentifier>();
                references.AddRange(sceneInfo.referencedObjects);
                assetToReferences[scene] = FilterReferencesForAsset(scene, references, previousSceneObjects,
                    previousSceneAssets, assetObjectIdentifierHashSets);
                previousSceneObjects.UnionWith(references);
                previousSceneAssets.UnionWith(assetToReferences[scene]);

                m_WriteData.FileToObjects.Add(internalName, references);
                m_WriteData.FileToBundle.Add(internalName, bundleName);

                var files = new List<string> { internalName };
                files.AddRange(sceneInternalNames);
                m_WriteData.AssetToFiles[scene] = files;

                sceneInternalNames.Add(internalName);
            }
        }

        static HashSet<ObjectIdentifier> GetRefObjectIdLookup(AssetLoadInfo referencedAsset, Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets)
        {
            HashSet<ObjectIdentifier> refObjectIdLookup;
            if (assetObjectIdentifierHashSets == null || !assetObjectIdentifierHashSets.TryGetValue(referencedAsset.asset, out refObjectIdLookup))
            {
                refObjectIdLookup = new HashSet<ObjectIdentifier>(referencedAsset.referencedObjects);
                assetObjectIdentifierHashSets?.Add(referencedAsset.asset, refObjectIdLookup);
            }
            return refObjectIdLookup;
        }

        private List<GUID> FilterReferencesForAsset(GUID asset, List<ObjectIdentifier> references, 
            HashSet<ObjectIdentifier> previousSceneObjects = null, HashSet<GUID> previousSceneReferences = null, 
            Dictionary<GUID, HashSet<ObjectIdentifier>> assetObjectIdentifierHashSets = null)
        {
            HashSet<AssetLoadInfo> referencedAssets = new HashSet<AssetLoadInfo>();
            List<GUID> referencedAssetsGuids = new List<GUID>(referencedAssets.Count);
            List<ObjectIdentifier> referencesPruned = new List<ObjectIdentifier>(references.Count);
            
            // 剔除内置资源引用
            foreach (ObjectIdentifier reference in references)
            {
                // CommonStrings.UnityDefaultResourcePath = "library/unity default resources"
                // 忽略大小写的字符串比较
                if (reference.filePath.Equals(CommonStrings.UnityDefaultResourcePath, StringComparison.OrdinalIgnoreCase))
                    continue;
                
                if (m_DependencyData.AssetInfo.TryGetValue(reference.guid, out AssetLoadInfo referenceInfo))
                {
                    if (referencedAssets.Add(referenceInfo))
                        referencedAssetsGuids.Add(referenceInfo.asset);
                    continue;
                }
                referencesPruned.Add(reference);
            }
            
            references.Clear();
            references.AddRange(referencesPruned);

            // Remove References also included by non-circular Referenced Assets
            // Remove References also included by circular Referenced Assets if Asset's GUID is higher than Referenced Asset's GUID
            foreach (AssetLoadInfo referencedAsset in referencedAssets)
            {
                if (asset > referencedAsset.asset || asset == referencedAsset.asset)
                {
                    references.RemoveAll(GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets).Contains);
                }
                else
                {
                    bool exists =
                        referencedAsset.referencedObjects.Any(referencedObject => referencedObject.guid == asset);
                    
                    if (!exists)
                        references.RemoveAll(GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets).Contains);
                }
            }

            // Special path for scenes, they can reference the same assets previously references
            if (!previousSceneReferences.IsNullOrEmpty())
            {
                foreach (GUID reference in previousSceneReferences)
                {
                    if (!m_DependencyData.AssetInfo.TryGetValue(reference, out AssetLoadInfo referencedAsset))
                        continue;

                    var refObjectIdLookup = GetRefObjectIdLookup(referencedAsset, assetObjectIdentifierHashSets);
                    // NOTE: 资产不可能依赖于场景，因此不需要循环参考检查，所以如果需要依赖它，只需删除并添加对资产的依赖
                    if (references.RemoveAll(refObjectIdLookup.Contains) > 0)
                        referencedAssetsGuids.Add(referencedAsset.asset);
                }
            }

            // Special path for scenes, they can use data from previous sharedAssets in the same bundle
            if (!previousSceneObjects.IsNullOrEmpty())
                references.RemoveAll(previousSceneObjects.Contains);
            
            return referencedAssetsGuids;
        }
    }
}
