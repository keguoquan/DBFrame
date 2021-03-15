
using System;
namespace DBFrame
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DataBaseType
    {
        SQLServer = 1,
        Oracle = 2,
        MySQL = 3,
        SQLite = 4
    }

    /// <summary>
    /// 数据库操作类型
    /// </summary>
    [Flags]
    public enum DBColumnOpType
    {
        /// <summary>
        /// 默认All所有操作，都不忽略
        /// </summary>
        All = 1,
        /// <summary>
        /// Insert时忽略此字段
        /// </summary>
        NoInsert = 2,
        /// <summary>
        /// Update时忽略此字段
        /// </summary>
        NoUpdate = 4
    }

    /// <summary>
    /// 数据缓存方式
    /// </summary>
    public enum CacheType
    {
        /// <summary>
        /// 不缓存
        /// </summary>
        None = 1,
        /// <summary>
        /// 缓存单个数据对象
        /// </summary>
        Object = 2,
        /// <summary>
        /// 缓存整个表数据， 该种方式只限数据量较小的表，读取频繁的数据
        /// </summary>
        List = 3
    }

    /// <summary>
    /// 表数据拆分方式，按时间拆分的表，在数据较多的时候可以更改时间拆分方式，但是Field，IdHash，时间三种方式不能相互更换。
    /// 注意：根据MyId生成策略，此枚举允许的最大值为7
    /// </summary>
    public enum SeparateType
    {
        /// <summary>
        /// 不拆分
        /// </summary>
        None = 0,
        /// <summary>
        /// 按日期年进行分隔，即每年产生一张表
        /// </summary>
        Year = 1,
        /// <summary>
        /// 按季度进行分隔，即每季度产生一张表
        /// </summary>
        JiDu = 2,
        /// <summary>
        /// 按日期月进行分隔，即每月产生一张表
        /// </summary>
        Mouth = 3,
        /// <summary>
        /// 按天进行分隔，即每天产生一张表
        /// </summary>
        Day = 4
    }

    /// <summary>
    /// 主键生成方式
    /// </summary>
    public enum DBPrimaryType
    {
        /// <summary>
        /// 不生成，由开发人员赋值
        /// </summary>
        None = 0,
        /// <summary>
        /// 数据库中的自增长
        /// </summary>
        Identity = 1
    }

    /// <summary>
    /// 数据库字段类型
    /// </summary>
    public enum DBColumnType
    {
        Boolean,
        Byte,
        SByte,
        Char,
        Decimal,
        Double,
        Single,
        Int32,
        UInt32,
        Int16,
        UInt16,
        Int64,
        UInt64,
        String,
        DateTime,
        Guid,
        TimeSpan,
        ByteArray,
        Xml
    }
}
