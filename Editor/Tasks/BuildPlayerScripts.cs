using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Build.Player;
using System.IO;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Compiles all player scripts.
    /// </summary>
    public class BuildPlayerScripts : IBuildTask
    {
        public int Version => 1;
        
        [InjectContext] private IBuildParameters m_Parameters;
        [InjectContext] private IBuildResults m_Results;

        public ReturnCode Run()
        {
            if (m_Parameters.ScriptInfo != null)
            {
                BuildCacheUtility.SetTypeDB(m_Parameters.ScriptInfo);
                return ReturnCode.SuccessNotRun;    // 表示操作成功，但没有实际执行
            }

            // 我们需要确保该目录为空，以便该目录中的先前结果或其他构件不会影响构建结果
            // 该路径默认为 Library/PlayerScriptAssemblies
            if (Directory.Exists(m_Parameters.ScriptOutputFolder))
            {
                Directory.Delete(m_Parameters.ScriptOutputFolder, true);
                Directory.CreateDirectory(m_Parameters.ScriptOutputFolder);
            }

            // 将用户脚本编译成一个或多个程序集，将返回值(程序集名字数组和 TypeDB )存到 buildResult 中
            m_Results.ScriptResults = PlayerBuildInterface.CompilePlayerScripts(
                m_Parameters.GetScriptCompilationSettings(), m_Parameters.ScriptOutputFolder);
            
            // 设置 TypeDB
            m_Parameters.ScriptInfo = m_Results.ScriptResults.typeDB;
            BuildCacheUtility.SetTypeDB(m_Parameters.ScriptInfo);

            if (m_Results.ScriptResults.assemblies.IsNullOrEmpty() && m_Results.ScriptResults.typeDB == null)
                return ReturnCode.Error;
            return ReturnCode.Success;
        }
    }
}
