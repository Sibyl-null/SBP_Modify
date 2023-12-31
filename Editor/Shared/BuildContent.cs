using System;
using System.Collections.Generic;
#if UNITY_2019_3_OR_NEWER
using UnityEditor.Build.Content;
#endif
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Pipeline.Utilities;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of ICustomAssets. Stores the list of Custom Assets generated during the Scriptable Build Pipeline.
    /// <seealso cref="ICustomAssets"/>
    /// </summary>
    [Serializable]
    public class CustomAssets : ICustomAssets
    {
        /// <inheritdoc />
        public List<GUID> Assets { get; private set; }

        /// <summary>
        /// Default constructor, creates an empty CustomAssets.
        /// </summary>
        public CustomAssets()
        {
            Assets = new List<GUID>();
        }
    }

    /// <summary>
    /// Basic implementation of IBuildContent. Stores the list of Assets to feed the Scriptable Build Pipeline.
    /// <seealso cref="IBuildContent"/>
    /// </summary>
    [Serializable]
    public class BuildContent : IBuildContent
    {
        public List<GUID> Assets { get; private set; }
        public List<GUID> Scenes { get; private set; }
        public List<CustomContent> CustomAssets { get; private set; }
        
        public BuildContent() {}

        /// <summary>
        /// Default constructor, takes a set of Assets and converts them to the appropriate properties.
        /// </summary>
        /// <param name="assets">The set of Assets identified by GUID to ensure are packaged with the build</param>
        public BuildContent(IEnumerable<GUID> assets)
        {
            if (assets == null)
                throw new ArgumentNullException(nameof(assets));

            Assets = new List<GUID>();
            Scenes = new List<GUID>();
            CustomAssets = new List<CustomContent>();

            foreach (GUID asset in assets)
            {
                ValidationMethods.Status assetType = ValidationMethods.ValidAsset(asset);
                if (assetType == ValidationMethods.Status.Asset)
                    Assets.Add(asset);
                else if (assetType == ValidationMethods.Status.Scene)
                    Scenes.Add(asset);
                else
                    throw new ArgumentException($"Asset '{asset.ToString()}' is not a valid Asset or Scene.");
            }
        }
    }

    /// <summary>
    /// Basic implementation of IBundleBuildContent. Stores the list of Assets with explicit Asset Bundle layout to feed the Scriptable Build Pipeline.
    /// <seealso cref="IBundleBuildContent"/>
    /// </summary>
    [Serializable]
    public class BundleBuildContent : IBundleBuildContent
    {
        public List<GUID> Assets { get; private set; }
        public List<GUID> Scenes { get; private set; }
        public List<CustomContent> CustomAssets { get; private set; }
        public Dictionary<string, List<ResourceFile>> AdditionalFiles { get; private set; }
        public Dictionary<GUID, string> Addresses { get; private set; }
        public Dictionary<string, List<GUID>> BundleLayout { get; private set; }
        
        public BundleBuildContent() {}

        /// <summary>
        /// Default constructor, takes a set of AssetBundleBuild and converts them to the appropriate properties.
        /// </summary>
        /// <param name="bundleBuilds">The set of AssetBundleBuild to be built.</param>
        public BundleBuildContent(IEnumerable<AssetBundleBuild> bundleBuilds)
        {
            if (bundleBuilds == null)
                throw new ArgumentNullException(nameof(bundleBuilds));

            SetUp();
            
            foreach (AssetBundleBuild bundleBuild in bundleBuilds)
                ReadAssetBundleBuild(bundleBuild);
        }
        
        private void SetUp()
        {
            Assets = new List<GUID>();
            Scenes = new List<GUID>();
            CustomAssets = new List<CustomContent>();
            AdditionalFiles = new Dictionary<string, List<ResourceFile>>();
            Addresses = new Dictionary<GUID, string>();
            BundleLayout = new Dictionary<string, List<GUID>>();
        }

        private void ReadAssetBundleBuild(AssetBundleBuild bundleBuild)
        {
            List<GUID> guids = BundleLayout.GetOrAdd(bundleBuild.assetBundleName);
            ValidationMethods.Status bundleType = ValidationMethods.Status.Invalid;

            for (int i = 0; i < bundleBuild.assetNames.Length; i++)
            {
                string assetPath = bundleBuild.assetNames[i];
                GUID asset = new GUID(AssetDatabase.AssetPathToGUID(assetPath));

                ValidationMethods.Status status =
                    CheckValidation(assetPath, asset, bundleBuild.assetBundleName, ref bundleType);
                
                string address = GetAddress(bundleBuild, i, assetPath);

                // Add the guid to the bundle map
                guids.Add(asset);
                // Add the guid & address
                Addresses.Add(asset, address);

                // Add the asset to the correct collection
                if (status == ValidationMethods.Status.Asset)
                    Assets.Add(asset);
                else if (status == ValidationMethods.Status.Scene)
                    Scenes.Add(asset);
            }
        }

        private string GetAddress(AssetBundleBuild bundleBuild, int index, string assetPath)
        {
            bool addressValid = bundleBuild.addressableNames != null &&
                                index < bundleBuild.addressableNames.Length &&
                                !string.IsNullOrEmpty(bundleBuild.addressableNames[index]);

            return addressValid ? bundleBuild.addressableNames[index] : assetPath;
        }

        private ValidationMethods.Status CheckValidation(string assetPath, GUID asset, 
            string assetBundleName, ref ValidationMethods.Status bundleType)
        {
            // 确保路径有效
            ValidationMethods.Status status = ValidationMethods.ValidAsset(asset);
            if (status == ValidationMethods.Status.Invalid)
                throw new ArgumentException($"Asset '{assetPath}' is not a valid Asset or Scene.");

            // Ensure we do not have a mixed bundle
            if (bundleType == ValidationMethods.Status.Invalid)
            {
                bundleType = status;
            }
            else if (bundleType != status)
            {
                // Asset 和 Scene 不能打在同一个 Bundle 里
                throw new ArgumentException($"Asset Bundle '{assetBundleName}' is invalid " +
                                            "because it contains mixed Asset and Scene types.");
            }

            return status;
        }
    }
}
