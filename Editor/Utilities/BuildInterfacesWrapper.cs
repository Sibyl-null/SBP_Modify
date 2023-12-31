using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEngine;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// Internal interface so switch platform build task can initialize editor build callbacks
    /// </summary>
    internal interface IEditorBuildCallbacks : IContextObject
    {
        /// <summary>
        /// Callbacks need to be Initialized after platform switch
        /// </summary>
        void InitializeCallbacks();
    }

    /// <summary>
    /// 管理编辑器下 IPreprocessShaders, IProcessScene, IProcessSceneWithReport 回调的初始化和清理
    /// </summary>
    public class BuildInterfacesWrapper : IDisposable, IEditorBuildCallbacks
    {
        private Type m_Type = null;
        private bool m_Disposed = false;

        internal static Hash128 SceneCallbackVersionHash = new Hash128();
        internal static Hash128 ShaderCallbackVersionHash = new Hash128();
        
        public BuildInterfacesWrapper()
        {
            m_Type = Type.GetType("UnityEditor.Build.BuildPipelineInterfaces, UnityEditor");
            InitializeCallbacks();
        }
        
        public void InitializeCallbacks()
        {
            MethodInfo init = m_Type.GetMethod("InitializeBuildCallbacks", 
                BindingFlags.NonPublic | BindingFlags.Static);
            
            // 274 = BuildCallbacks.SceneProcessors | BuildCallbacks.ShaderProcessors | BuildCallbacks.ComputeShader
            init.Invoke(null, new object[] { 274 });
            
            // 收集 [VersionedCallback] 特性记录的版本号，计算 Hash 值
            GatherCallbackVersions();
        }

        internal void GatherCallbackVersions()
        {
            Type versionedType = typeof(VersionedCallbackAttribute);
            TypeCache.TypeCollection typeCollection = TypeCache.GetTypesWithAttribute(versionedType);
            
            List<Hash128> sceneInputs = new List<Hash128>();
            List<Hash128> shaderInputs = new List<Hash128>();
            
            foreach (Type type in typeCollection)
            {
                var attribute = (VersionedCallbackAttribute)Attribute.GetCustomAttribute(type, versionedType);
                if (typeof(IPreprocessShaders).IsAssignableFrom(type) || typeof(IPreprocessComputeShaders).IsAssignableFrom(type))
                {
                    shaderInputs.Add(HashingMethods.Calculate(type.AssemblyQualifiedName, attribute.version).ToHash128());
                }
#pragma warning disable CS0618 // Type or member is obsolete
                else if (typeof(IProcessScene).IsAssignableFrom(type) || typeof(IProcessSceneWithReport).IsAssignableFrom(type))
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    sceneInputs.Add(HashingMethods.Calculate(type.AssemblyQualifiedName, attribute.version).ToHash128());
                }
            }

            SceneCallbackVersionHash = new Hash128();
            if (sceneInputs.Count > 0)
            {
                sceneInputs.Sort();
                SceneCallbackVersionHash = HashingMethods.Calculate(sceneInputs).ToHash128();
            }

            ShaderCallbackVersionHash = new Hash128();
            if (shaderInputs.Count > 0)
            {
                shaderInputs.Sort();
                ShaderCallbackVersionHash = HashingMethods.Calculate(shaderInputs).ToHash128();
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (m_Disposed)
                return;

            CleanupCallbacks();
            m_Disposed = true;
        }
        
        /// <summary>
        /// Cleanup Unity Editor IPreprocessShaders, IProcessScene, &amp; IProcessSceneWithReport build callbacks.
        /// </summary>
        // 清理回调，Dispose 时调用
        public void CleanupCallbacks()
        {
            MethodInfo clean = m_Type.GetMethod("CleanupBuildCallbacks", 
                BindingFlags.NonPublic | BindingFlags.Static);
            clean.Invoke(null, null);
        }
    }
}
