using System;

namespace DBFrame.DBMapAttr
{
    /// <summary>
    /// 标示列为主键字段,用在映射类的Field,Property上,不允许重复使用
    /// 并且不允许与DBColumnAttribute同时使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class DBPrimaryKeyAttribute : DBColumnAttribute
    {
        /// <summary>
        /// 主键生成方式
        /// </summary>
        public DBPrimaryType DBPrimaryType { get; set; }

        /// <summary>
        /// 主键名称
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        public DBPrimaryKeyAttribute(string fieldName)
            : this(fieldName, DBPrimaryType.None)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">数据库字段名称</param>
        public DBPrimaryKeyAttribute(string fieldName, string dataType)
            : this(fieldName, dataType, string.Empty, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <param name="isIdentity">是否自动增长</param>
        /// <param name="size">字段长度</param>
        public DBPrimaryKeyAttribute(string fieldName, DBPrimaryType dbPrimaryType)
            : this(fieldName, string.Empty, string.Empty, false)
        {
            DBPrimaryType = dbPrimaryType;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="dataType"></param>
        /// <param name="defaultVal"></param>
        /// <param name="notNull"></param>
        public DBPrimaryKeyAttribute(string fieldName, string dataType, string defaultVal, bool notNull)
            : base(fieldName, dataType, defaultVal, notNull)
        {
            this.FieldName = fieldName;
            this.DataType = dataType;
            this.NotNull = notNull;
            this.Default = defaultVal;
        }
    }
}
