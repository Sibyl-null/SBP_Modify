using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Player;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// 计算所有资产的依赖项数据
    /// </summary>
    public class CalculateAssetDependencyData : IBuildTask
    {
        internal struct TaskInput
        {
            public IBuildCache BuildCache;
            public BuildTarget Target;
            public TypeDB TypeDB;
            public List<GUID> Assets;
            public IProgressTracker ProgressTracker;
            public BuildUsageTagGlobal GlobalUsage;
            public BuildUsageCache DependencyUsageCache;
            public bool NonRecursiveDependencies;
            public IBuildLogger Logger;
        }

        internal struct AssetOutput
        {
            public GUID Asset;
            public AssetLoadInfo AssetInfo;
            public BuildUsageTagSet UsageTags;
            public SpriteImporterData SpriteData;
            public ExtendedAssetData ExtendedData;
            public List<ObjectTypes> ObjectTypes;
        }

        internal struct TaskOutput
        {
            public AssetOutput[] AssetResults;
            public int CachedAssetCount;
        }
        
        internal const int kVersion = 5;

        public int Version => kVersion;

        [InjectContext(ContextUsage.InOut, true)] private IBuildSpriteData m_SpriteData;
        [InjectContext(ContextUsage.InOut, true)] private IBuildExtendedAssetData m_ExtendedAssetData;
        [InjectContext(ContextUsage.In, true)] private IProgressTracker m_Tracker;
        [InjectContext(ContextUsage.In, true)] private IBuildCache m_Cache;
        [InjectContext(ContextUsage.In, true)] private IBuildLogger m_Log;
        
        [InjectContext(ContextUsage.In)] private IBundleBuildParameters m_Parameters;
        [InjectContext(ContextUsage.In)] private IBuildContent m_Content;
        [InjectContext] private IDependencyData m_DependencyData;

        private TaskInput _taskInput;
        
        public ReturnCode Run()
        {
            _taskInput = CreateTaskInput();

            // 场景光照信息合并
            foreach (SceneDependencyInfo sceneInfo in m_DependencyData.SceneInfo.Values)
                _taskInput.GlobalUsage |= sceneInfo.globalUsage;

            ReturnCode code = RunInternal(out TaskOutput output);
            if (code == ReturnCode.Success)
                PostSuccessRun(output);

            return code;
        }

        private TaskInput CreateTaskInput()
        {
            TaskInput input = new TaskInput
            {
                BuildCache = m_Parameters.UseCache ? m_Cache : null,
                Target = m_Parameters.Target,
                TypeDB = m_Parameters.ScriptInfo,
                Assets = m_Content.Assets,
                ProgressTracker = m_Tracker,
                GlobalUsage = m_DependencyData.GlobalUsage,
                DependencyUsageCache = m_DependencyData.DependencyUsageCache,
#if NONRECURSIVE_DEPENDENCY_DATA
                NonRecursiveDependencies = m_Parameters.NonRecursiveDependencies,
#else
                NonRecursiveDependencies = false,
#endif
                Logger = m_Log
            };
            
            return input;
        }
        
        private ReturnCode RunInternal(out TaskOutput output)
        {
            output = new TaskOutput();
            output.AssetResults = new AssetOutput[_taskInput.Assets.Count];

            IList<CachedInfo> cachedInfo = GatheringCacheEntriesToLoad();

            for (int i = 0; i < _taskInput.Assets.Count; i++)
            {
                using (_taskInput.Logger.ScopedStep(LogLevel.Info, "Calculate Asset Dependencies"))
                {
                    AssetOutput assetResult = new AssetOutput();
                    assetResult.Asset = _taskInput.Assets[i];
                    
                    if (cachedInfo != null && cachedInfo[i] != null)
                    {
                        List<ObjectTypes> objectTypes = (List<ObjectTypes>)cachedInfo[i].Data[4];
                        AssetLoadInfo assetInfos = (AssetLoadInfo)cachedInfo[i].Data[0];

                        bool useCachedData = true;
                        foreach (ObjectTypes objectType in objectTypes)
                        {
                            // 自上次缓存数据以来，SpriteAtlas 和 Sprite 的关联可能发生了变化
                            // 这可能意味着我们的缓存中有过时的数据，如果是这样，请确保我们生成数据
                            if (objectType.Types[0] == typeof(UnityEngine.Sprite))
                            {
                                ObjectIdentifier[] referencedObjectOld = assetInfos.referencedObjects.ToArray();
                                ObjectIdentifier[] referencedObjectsNew = null;
#if NONRECURSIVE_DEPENDENCY_DATA
                                if (_taskInput.NonRecursiveDependencies)
                                {
                                    referencedObjectsNew = ContentBuildInterface.GetPlayerDependenciesForObjects(assetInfos.includedObjects.ToArray(), _taskInput.Target, _taskInput.TypeDB, DependencyType.ValidReferences);
                                    referencedObjectsNew = ExtensionMethods.FilterReferencedObjectIDs(_taskInput.Assets[i], referencedObjectsNew, _taskInput.Target, _taskInput.TypeDB, new HashSet<GUID>(_taskInput.Assets));
                                }
                                else
#endif
                                {
                                    referencedObjectsNew = ContentBuildInterface.GetPlayerDependenciesForObjects(assetInfos.includedObjects.ToArray(), _taskInput.Target, _taskInput.TypeDB);
                                }

                                if (referencedObjectOld.SequenceEqual(referencedObjectsNew) == false)
                                {
                                    useCachedData = false;
                                }
                                break;
                            }
                        }
                        
                        if (useCachedData)
                        { 
                            assetResult.AssetInfo = assetInfos;
                            assetResult.UsageTags = cachedInfo[i].Data[1] as BuildUsageTagSet;
                            assetResult.SpriteData = cachedInfo[i].Data[2] as SpriteImporterData;
                            assetResult.ExtendedData = cachedInfo[i].Data[3] as ExtendedAssetData;
                            assetResult.ObjectTypes = objectTypes;
                            output.AssetResults[i] = assetResult;
                            output.CachedAssetCount++;
                            _taskInput.Logger.AddEntrySafe(LogLevel.Info, $"{assetResult.Asset} (cached)");
                            continue;
                        }
                    }

                    GUID asset = _taskInput.Assets[i];
                    string assetPath = AssetDatabase.GUIDToAssetPath(asset.ToString());

                    if (!_taskInput.ProgressTracker.UpdateInfoUnchecked(assetPath))
                        return ReturnCode.Canceled;

                    _taskInput.Logger.AddEntrySafe(LogLevel.Info, $"{assetResult.Asset}");

                    assetResult.AssetInfo = new AssetLoadInfo();
                    assetResult.UsageTags = new BuildUsageTagSet();

                    assetResult.AssetInfo.asset = asset;
                    var includedObjects = ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(asset, _taskInput.Target);
                    assetResult.AssetInfo.includedObjects = new List<ObjectIdentifier>(includedObjects);
                    ObjectIdentifier[] referencedObjects;
#if NONRECURSIVE_DEPENDENCY_DATA
                    if (_taskInput.NonRecursiveDependencies)
                    {
                        referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, _taskInput.Target, _taskInput.TypeDB, DependencyType.ValidReferences);
                        referencedObjects = ExtensionMethods.FilterReferencedObjectIDs(asset, referencedObjects, _taskInput.Target, _taskInput.TypeDB, new HashSet<GUID>(_taskInput.Assets));
                    }
                    else
#endif
                    {
                        referencedObjects = ContentBuildInterface.GetPlayerDependenciesForObjects(includedObjects, _taskInput.Target, _taskInput.TypeDB);
                    }

                    assetResult.AssetInfo.referencedObjects = new List<ObjectIdentifier>(referencedObjects);
                    var allObjects = new List<ObjectIdentifier>(includedObjects);
                    allObjects.AddRange(referencedObjects);
                    ContentBuildInterface.CalculateBuildUsageTags(allObjects.ToArray(), includedObjects,
                        _taskInput.GlobalUsage, assetResult.UsageTags, _taskInput.DependencyUsageCache);

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null && importer.textureType == TextureImporterType.Sprite)
                    {
                        assetResult.SpriteData = new SpriteImporterData();
                        assetResult.SpriteData.PackedSprite = false;
                        assetResult.SpriteData.SourceTexture = includedObjects.FirstOrDefault();

                        if (EditorSettings.spritePackerMode != SpritePackerMode.Disabled)
                            assetResult.SpriteData.PackedSprite = referencedObjects.Length > 0;
                    }
                    
                    GatherAssetRepresentations(asset, _taskInput.Target, includedObjects, out assetResult.ExtendedData);
                    output.AssetResults[i] = assetResult;
                }
            }

            GatheringCacheEntriesToSave(output, cachedInfo);

            return ReturnCode.Success;
        }

        private IList<CachedInfo> GatheringCacheEntriesToLoad()
        {
            IList<CachedInfo> cachedInfo = null;
            using (_taskInput.Logger.ScopedStep(LogLevel.Info, "Gathering Cache Entries to Load"))
            {
                if (_taskInput.BuildCache != null)
                {
                    IList<CacheEntry> entries = _taskInput.Assets.Select(x =>
                        GetAssetCacheEntry(_taskInput.BuildCache, x, _taskInput.NonRecursiveDependencies)).ToList();
                    _taskInput.BuildCache.LoadCachedData(entries, out cachedInfo);
                }
            }

            return cachedInfo;
        }

        private void GatheringCacheEntriesToSave(TaskOutput output, IList<CachedInfo> cachedInfo)
        {
            using (_taskInput.Logger.ScopedStep(LogLevel.Info, "Gathering Cache Entries to Save"))
            {
                if (_taskInput.BuildCache != null)
                {
                    List<CachedInfo> toCache = new List<CachedInfo>();
                    for (int i = 0; i < _taskInput.Assets.Count; i++)
                    {
                        AssetOutput r = output.AssetResults[i];
                        if (cachedInfo[i] == null)
                        {
                            toCache.Add(GetCachedInfo(_taskInput.BuildCache, _taskInput.Assets[i], r.AssetInfo, r.UsageTags,
                                r.SpriteData, r.ExtendedData, _taskInput.NonRecursiveDependencies));
                        }
                    }

                    _taskInput.BuildCache.SaveCachedData(toCache);
                }
            }
        }

        private void PostSuccessRun(TaskOutput output)
        {
            foreach (AssetOutput o in output.AssetResults)
            {
                m_DependencyData.AssetInfo.Add(o.Asset, o.AssetInfo);
                m_DependencyData.AssetUsage.Add(o.Asset, o.UsageTags);

                if (o.SpriteData != null)
                {
                    m_SpriteData ??= new BuildSpriteData();
                    m_SpriteData.ImporterData.Add(o.Asset, o.SpriteData);
                }

                if (!m_Parameters.DisableVisibleSubAssetRepresentations && o.ExtendedData != null)
                {
                    m_ExtendedAssetData ??= new BuildExtendedAssetData();
                    m_ExtendedAssetData.ExtendedData.Add(o.Asset, o.ExtendedData);
                }

                if (o.ObjectTypes != null)
                    BuildCacheUtility.SetTypeForObjects(o.ObjectTypes);
            }
        }

        private static void GatherAssetRepresentations(GUID asset, BuildTarget target, ObjectIdentifier[] includedObjects, out ExtendedAssetData extendedData)
        {
            extendedData = null;
            var includeSet = new HashSet<ObjectIdentifier>(includedObjects);
            // GetPlayerAssetRepresentations can return editor only objects, filter out those to only include what is in includedObjects
            ObjectIdentifier[] representations = ContentBuildInterface.GetPlayerAssetRepresentations(asset, target);
            var filteredRepresentations = representations.Where(includeSet.Contains);
            // Main Asset always returns at index 0, we only want representations, so check for greater than 1 length
            if (representations.IsNullOrEmpty() || filteredRepresentations.Count() < 2)
                return;

            extendedData = new ExtendedAssetData();
            extendedData.Representations.AddRange(filteredRepresentations.Skip(1));
        }
        
        static CacheEntry GetAssetCacheEntry(IBuildCache cache, GUID asset, bool nonRecursiveDependencies)
        {
            CacheEntry entry = cache.GetCacheEntry(asset, nonRecursiveDependencies ? -kVersion : kVersion);
            return entry;
        }

        static CachedInfo GetCachedInfo(IBuildCache cache, GUID asset, AssetLoadInfo assetInfo, BuildUsageTagSet usageTags, SpriteImporterData importerData, ExtendedAssetData assetData, bool NonRecursiveDependencies)
        {
            var info = new CachedInfo();
            info.Asset = GetAssetCacheEntry(cache, asset, NonRecursiveDependencies);

            var uniqueTypes = new HashSet<System.Type>();
            var objectTypes = new List<ObjectTypes>();
            var dependencies = new HashSet<CacheEntry>();
            ExtensionMethods.ExtractCommonCacheData(cache, assetInfo.includedObjects, assetInfo.referencedObjects, uniqueTypes, objectTypes, dependencies);
            info.Dependencies = dependencies.ToArray();

            info.Data = new object[] { assetInfo, usageTags, importerData, assetData, objectTypes };
            return info;
        }
    }
}
