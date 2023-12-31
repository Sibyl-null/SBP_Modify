using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace UnityEditor.Build.Pipeline.Utilities
{
    internal static class ValidationMethods
    {
        internal static System.Func<GUID, Status> ValidAssetFake;

        public enum Status
        {
            Invalid,
            Asset,
            Scene
        }

        public static Status ValidAsset(GUID asset)
        {
            if (ValidAssetFake != null)
                return ValidAssetFake(asset);

            string path = AssetDatabase.GUIDToAssetPath(asset.ToString());
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return Status.Invalid;

            if (path.EndsWith(".unity"))
                return Status.Scene;

            return Status.Asset;
        }

        public static bool ValidSceneBundle(List<GUID> assets)
        {
            return assets.All(x => ValidAsset(x) == Status.Scene);
        }

        public static bool ValidAssetBundle(List<GUID> assets)
        {
            return assets.All(x => ValidAsset(x) == Status.Asset);
        }

        public static bool HasDirtyScenes()
        {
            bool unsavedChanges = false;
            int sceneCount = SceneManager.sceneCount;
            
            for (int i = 0; i < sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isDirty)
                    continue;
                
                unsavedChanges = true;
                break;
            }

            return unsavedChanges;
        }
    }
}
