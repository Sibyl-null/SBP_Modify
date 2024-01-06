using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// 处理依赖计算任务后的所有回调.
    /// </summary>
    public class PostDependencyCallback : IBuildTask
    {
        public int Version => 1;

        [InjectContext] private IBuildParameters m_Parameters;
        [InjectContext] private IDependencyData m_DependencyData;
        [InjectContext(ContextUsage.In)] private IDependencyCallback m_Callback;

        public ReturnCode Run()
        {
            return m_Callback.PostDependency(m_Parameters, m_DependencyData);
        }
    }
}
