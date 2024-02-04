using System;
using System.Collections.Generic;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// The importer data about a sprite asset.
    /// </summary>
    [Serializable]
    public class SpriteImporterData
    {
        /// <summary>
        /// 该资源是否由 sprite packer 打包
        /// </summary>
        public bool PackedSprite { get; set; }

        /// <summary>
        /// 该 Sprite 的原始纹理资源
        /// </summary>
        public ObjectIdentifier SourceTexture { get; set; }
    }
    
    public interface IBuildSpriteData : IContextObject
    {
        Dictionary<GUID, SpriteImporterData> ImporterData { get; }
    }
}
