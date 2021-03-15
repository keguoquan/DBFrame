using System;
using DBFrame;

namespace DBFrame.DBMapAttr
{
    /// <summary>
    /// 数据库表映射属性,使用映射类上
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class DBTableAttribute : Attribute
    {
        /// <summary>
        /// 数据库表名称
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 该表数据拆分方式，此值不为None时，必须设置CreateSql，并且该表的Id生成方式必须为MyIdMake
        /// </summary>
        public SeparateType SeparateType;
        
        /// <summary>
        /// 该数据库表的创建SQL语句，当表的数据拆分方式不为None时，必须设置此值
        /// </summary>
        public string CreateSql { get; set; }

        /// <summary>
        /// 数据缓存方式
        /// </summary>
        public CacheType CacheType { get; set; }

        /// <summary>
        /// 该数据对象缓存时间（秒数），默认为10分钟
        /// </summary>
        public int CacheSeconds { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">数据库表名称</param>
        public DBTableAttribute(string name)
            : this(name, CacheType.None, 0, SeparateType.None, string.Empty)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">数据库表名称</param>
        /// <param name="cacheType">数据缓存方式</param>
        /// <param name="cacheSeconds">该数据对象缓存时间（秒数）</param>
        public DBTableAttribute(string name, CacheType cacheType, int cacheSeconds)
            : this(name, cacheType, cacheSeconds, SeparateType.None, string.Empty)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">数据库表名称</param>
        /// <param name="separateType">表数据拆分方式</param>
        public DBTableAttribute(string name, SeparateType separateType, string createSql)
            : this(name, CacheType.None, 10, separateType, createSql)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name">数据库表名称</param>
        /// <param name="cacheType">数据缓存方式</param>
        /// <param name="cacheSeconds">该数据对象缓存时间（秒数）, 默认为10分钟</param>
        /// <param name="separateType">表数据拆分方式</param>
        public DBTableAttribute(string name, CacheType cacheType, int cacheSeconds, SeparateType separateType, string createSql)
        {
            Name = name;
            this.CacheType = cacheType;
            CacheSeconds = cacheSeconds;
            SeparateType = separateType;
            this.CreateSql = createSql;
        }
    }
}
