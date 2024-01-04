using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Build.Content;
using Object = UnityEngine.Object;

namespace AbBuilderTest.Editor
{
    public class ObjectIdentifierShowWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Tests Window/Open ObjectIdentifierShowWindow")]
        private static void ShowWindow()
        {
            GetWindow<ObjectIdentifierShowWindow>();
        }

        [Serializable]
        public struct DrawInfo
        {
            public string guid;
            public string localId;
            public FileType fileType;
            public string type;
        }
        
        public Object asset;
        public List<DrawInfo> infos = new List<DrawInfo>();

        [Button]
        private void Draw()
        {
            GUID guid = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(asset));
            ObjectIdentifier[] identifiers =
                ContentBuildInterface.GetPlayerObjectIdentifiersInAsset(guid, BuildTarget.Android);

            infos.Clear();
            foreach (ObjectIdentifier identifier in identifiers)
            {
                infos.Add(new DrawInfo
                {
                    guid = identifier.guid.ToString(),
                    localId = identifier.localIdentifierInFile.ToString(),
                    fileType = identifier.fileType,
                    type = ContentBuildInterface.GetTypeForObject(identifier).FullName
                });
            }
        }
    }
}