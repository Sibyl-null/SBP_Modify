using UnityEditor.Build.Pipeline.Injector;
using UnityEditor.Build.Pipeline.Interfaces;
using UnityEditor.Sprites;
using UnityEditor.U2D;

namespace UnityEditor.Build.Pipeline.Tasks
{
    /// <summary>
    /// Builds the cache data for all sprite atlases.
    /// 为所有 SpriteAtlases 构建缓存数据。
    /// </summary>
    public class RebuildSpriteAtlasCache : IBuildTask
    {
        public int Version => 1;

#pragma warning disable 649
        [InjectContext(ContextUsage.In)]
        IBuildParameters m_Parameters;
#pragma warning restore 649

        public ReturnCode Run()
        {
            SpriteAtlasUtility.PackAllAtlases(m_Parameters.Target);
            return ReturnCode.Success;
        }
    }
}
