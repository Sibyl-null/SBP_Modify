using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Processes all callbacks after the script building task.
    /// </summary>
    public class PostScriptsCallback : IBuildTask
    {
        public int Version => 1;

        [InjectContext] private IBuildParameters m_Parameters;
        [InjectContext] private IBuildResults m_Results;
        [InjectContext(ContextUsage.In)] private IScriptsCallback m_Callback;

        public ReturnCode Run()
        {
            return m_Callback.PostScripts(m_Parameters, m_Results);
        }
    }
}
