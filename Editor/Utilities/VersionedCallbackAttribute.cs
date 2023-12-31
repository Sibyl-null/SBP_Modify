#if UNITY_2019_4_OR_NEWER
using System;

namespace UnityEditor.Build.Pipeline.Utilities
{
    /// <summary>
    /// 提供了 IProcessScene, IProcessSceneWithReport, IPreprocessShaders 和 IPreprocessComputeShaders 回调的版本细节。
    /// 当回调函数发生变化并且需要更改构建结果时，增加版本号。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class VersionedCallbackAttribute : Attribute
    {
        public readonly float version;

        /// <summary>
        /// Attribute provides the version details for IProcessScene, IProcessSceneWithReport, IPreprocessShaders, and IPreprocessComputeShaders callbacks.
        /// Increment the version number when the callback changes and the build result needs to change.
        /// </summary>
        /// <param name="version">The version of this callback.</param>
        public VersionedCallbackAttribute(float version)
        {
            this.version = version;
        }
    }
}
#endif