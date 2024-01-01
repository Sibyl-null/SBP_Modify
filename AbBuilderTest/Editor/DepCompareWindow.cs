using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Serialization;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Player;
using UnityEngine;

namespace AbBuilderTest.Editor
{
    public class DepCompareWindow : OdinEditorWindow
    {
        [MenuItem("Tools/Open DepCompareWindow")]
        private static void ShowWindow()
        {
            GetWindow<DepCompareWindow>();
        }

        [OdinSerialize] private Object _asset;
        
        // -------------------------------------------------------------------------------------
        
        [OdinSerialize, TitleGroup("AssetDatabase")] 
        private readonly Dictionary<string, Object> _assetDatabaseDepMap = new();

        [Button, TitleGroup("AssetDatabase")]
        public void FindDepByAssetDatabase()
        {
            string assetPath = AssetDatabase.GetAssetPath(_asset);
            string[] depPaths = AssetDatabase.GetDependencies(assetPath);
            
            _assetDatabaseDepMap.Clear();
            foreach (string depPath in depPaths)
            {
                if (depPath == assetPath)
                    continue;
                
                _assetDatabaseDepMap.Add(depPath, AssetDatabase.LoadMainAssetAtPath(depPath));
            }
        }
        
        // -------------------------------------------------------------------------------------
        
        [OdinSerialize, TitleGroup("ContentBuildInterface")] 
        private readonly Dictionary<string, Object> _contentBuildDepMap = new();
        
        [Button, TitleGroup("ContentBuildInterface")]
        public void FindDepByContentBuild()
        {
            TypeDB typeDB = PlayerBuildInterface.CompilePlayerScripts(new ScriptCompilationSettings
            {
                group = BuildTargetGroup.Standalone,
                target = BuildTarget.StandaloneWindows,
                options = ScriptCompilationOptions.None
            }, ContentPipeline.kScriptBuildPath).typeDB;

            if (!ObjectIdentifier.TryGetObjectIdentifier(_asset, out ObjectIdentifier id)) 
                return;

            ObjectIdentifier[] objectIdentifiers =
                ContentBuildInterface.GetPlayerDependenciesForObject(id, BuildTarget.StandaloneWindows, typeDB);

            _contentBuildDepMap.Clear();
            foreach (ObjectIdentifier identifier in objectIdentifiers)
            {
                if (identifier.guid == id.guid)
                    continue;

                Object assetObject = ObjectIdentifier.ToObject(identifier);
                if (assetObject != null)
                {
                    string path = AssetDatabase.GetAssetPath(assetObject);
                    _contentBuildDepMap.TryAdd(path, assetObject);
                }
                else
                {
                    Debug.Log("ToObject Failed: " + identifier);
                }
            }
        }
    }
}