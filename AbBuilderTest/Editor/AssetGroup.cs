using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;
using UnityEngine.Build.Pipeline;
using BuildCompression = UnityEngine.BuildCompression;

namespace AbBuilderTest.Editor
{
    [CreateAssetMenu]
    public class AssetGroup : ScriptableObject
    {
        [FolderPath] public string output;

        public List<Sprite> sprites = new List<Sprite>();
        public List<AudioClip> audioClips = new List<AudioClip>();
        public List<Material> materials = new List<Material>();
        public List<GameObject> prefabs = new List<GameObject>();
        public List<Object> anyThings = new List<Object>();
        public Object asset;
        
        // --------------------------------------------
        
        [Button, PropertySpace]
        private void BuildIn_Build()
        {
            string path = Path.Combine(output, "BuildIn");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(path, 
                GetAssetBundleBuilds(),
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows);

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }
        
        [Button]
        private void CompatibilityBuildPipeline_Build()
        {
            string path = Path.Combine(output, "CompatibilityBuildPipeline");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            CompatibilityAssetBundleManifest manifest = CompatibilityBuildPipeline.BuildAssetBundles(path, 
                GetAssetBundleBuilds(),
                BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows);

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }

        [Button]
        private void ContentPipeline_Build()
        {
            string path = Path.Combine(output, "ContentPipeline");
            if (Directory.Exists(path))
                Directory.Delete(path, true);
            Directory.CreateDirectory(path);

            AssetBundleBuild[] builds = GetAssetBundleBuilds();
            BundleBuildContent buildContent = new BundleBuildContent(builds);

            BundleBuildParameters buildParameters =
                new BundleBuildParameters(BuildTarget.Android, BuildTargetGroup.Android, path)
                    {
                        BundleCompression = BuildCompression.LZ4,
                        UseCache = false,
                        WriteLinkXML = true
                    };

            IList<IBuildTask> taskList = DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleCompatible);

            ContentPipeline.BuildCallbacks.PostScriptsCallbacks += OnPostScriptsCallback;

            ContentPipeline.BuildAssetBundles(buildParameters, buildContent, 
                    out IBundleBuildResults results, taskList);

            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
        }

        [Button]
        private void LoadAssetRepresentations_AssetDatabase()
        {
            Object[] objects = AssetDatabase.LoadAllAssetRepresentationsAtPath(AssetDatabase.GetAssetPath(asset));
            Debug.Log(objects.Length + " Sub Assets");
            foreach (var obj in objects)
                Debug.Log(obj);
        }
        
        [Button]
        private void LoadAssetRepresentations_ContentBuildInterface()
        {
            GUID guid = new GUID(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(asset)));
            ObjectIdentifier[] identifiers =
                ContentBuildInterface.GetPlayerAssetRepresentations(guid, BuildTarget.Android);

            Debug.Log(identifiers.Length + " Sub Assets");
            foreach (ObjectIdentifier identifier in identifiers)
            {
                Object obj = ObjectIdentifier.ToObject(identifier);
                if (obj != null)
                    Debug.Log(obj);
            }
        }

        private ReturnCode OnPostScriptsCallback(IBuildParameters parameters, IBuildResults results)
        {
            foreach (string assembly in results.ScriptResults.assemblies)
                Debug.Log(assembly);

            return ReturnCode.Success;
        }

        private AssetBundleBuild[] GetAssetBundleBuilds()
        {
            List<AssetBundleBuild> builds = new List<AssetBundleBuild>
            {
                new AssetBundleBuild
                {
                    assetBundleName = "Sprites",
                    assetNames = sprites.Select(AssetDatabase.GetAssetPath).ToArray()
                },
                new AssetBundleBuild()
                {
                    assetBundleName = "AudioClips",
                    assetNames = audioClips.Select(AssetDatabase.GetAssetPath).ToArray(),
                    addressableNames = audioClips.Select(x => Path.GetFileName(AssetDatabase.GetAssetPath(x))).ToArray()
                },
                new AssetBundleBuild()
                {
                    assetBundleName = "Materials",
                    assetNames = materials.Select(AssetDatabase.GetAssetPath).ToArray()
                },
                new AssetBundleBuild()
                {
                    assetBundleName = "Prefabs",
                    assetNames = prefabs.Select(AssetDatabase.GetAssetPath).ToArray()
                },
                new AssetBundleBuild()
                {
                    assetBundleName = "AnyThings",
                    assetNames = anyThings.Select(AssetDatabase.GetAssetPath).ToArray()
                }
            };
            return builds.ToArray();
        }
    }
}