using System.Collections.Generic;
using UnityEditor.Build.Content;

namespace UnityEditor.Build.Pipeline.Interfaces
{
    /// <summary>
    /// 写数据容器的基本接口.
    /// </summary>
    public interface IWriteData : IContextObject
    {
        /// <summary>
        /// 资产到文件依赖关系的映射.
        /// 列表中的第一个依赖项是 asset 的主文件.
        /// </summary>
        Dictionary<GUID, List<string>> AssetToFiles { get; }

        /// <summary>
        /// Map of file to list of objects in that file
        /// </summary>
        Dictionary<string, List<ObjectIdentifier>> FileToObjects { get; }

        /// <summary>
        /// 将数据序列化到磁盘的所有写操作的列表
        /// </summary>
        List<IWriteOperation> WriteOperations { get; }
    }

    /// <summary>
    /// Extended interface for Asset Bundle write data container.
    /// </summary>
    public interface IBundleWriteData : IWriteData
    {
        /// <summary>
        /// Map of file name to bundle name
        /// </summary>
        Dictionary<string, string> FileToBundle { get; }

        /// <summary>
        /// Map of file name to calculated usage set
        /// </summary>
        Dictionary<string, BuildUsageTagSet> FileToUsageSet { get; }

        /// <summary>
        /// Map of file name to calculated object references
        /// </summary>
        Dictionary<string, BuildReferenceMap> FileToReferenceMap { get; }
    }
}
