using System;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline
{
    /// <summary>
    /// Basic implementation of IDependencyCallback, IPackingCallback, IWritingCallback, and IScriptsCallback.
    /// Uses Func implementation for callbacks. <seealso cref="IDependencyCallback"/>, <seealso cref="IPackingCallback"/>
    /// <seealso cref="IWritingCallback"/>, and <seealso cref="IScriptsCallback"/>
    /// </summary>
    public class BuildCallbacks : IDependencyCallback, IPackingCallback, IWritingCallback, IScriptsCallback
    {
        public Func<IBuildParameters, IBuildResults, ReturnCode> PostScriptsCallbacks { get; set; }
        public Func<IBuildParameters, IDependencyData, ReturnCode> PostDependencyCallback { get; set; }
        public Func<IBuildParameters, IDependencyData, IWriteData, ReturnCode> PostPackingCallback { get; set; }
        public Func<IBuildParameters, IDependencyData, IWriteData, IBuildResults, ReturnCode> PostWritingCallback { get; set; }

        // 脚本编译完成回调。其他回调也是同样的实现方式
        public ReturnCode PostScripts(IBuildParameters parameters, IBuildResults results)
        {
            if (PostScriptsCallbacks != null)
                return PostScriptsCallbacks(parameters, results);
            return ReturnCode.Success;
        }

        public ReturnCode PostDependency(IBuildParameters buildParameters, IDependencyData dependencyData)
        {
            if (PostDependencyCallback != null)
                return PostDependencyCallback(buildParameters, dependencyData);
            return ReturnCode.Success;
        }

        public ReturnCode PostPacking(IBuildParameters buildParameters, IDependencyData dependencyData, IWriteData writeData)
        {
            if (PostPackingCallback != null)
                return PostPackingCallback(buildParameters, dependencyData, writeData);
            return ReturnCode.Success;
        }

        public ReturnCode PostWriting(IBuildParameters parameters, IDependencyData dependencyData, IWriteData writeData, IBuildResults results)
        {
            if (PostWritingCallback != null)
                return PostWritingCallback(parameters, dependencyData, writeData, results);
            return ReturnCode.Success;
        }
    }
}
