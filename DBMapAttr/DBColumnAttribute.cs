using System;

namespace DBFrame.DBMapAttr
{
    /// <summary>
    /// 数据库列属性映射,用在映射类的Field,Property上,不允许重复使用
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DBColumnAttribute : Attribute
    {
        /// <summary>
        /// 对应数据库的字段名称
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// 数据库字段类型
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// 是否必须 true必须
        /// </summary>
        public bool NotNull { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public string Default { get; set; }

        /// <summary>
        /// 字段操作类型
        /// </summary>
        public DBColumnOpType DBColumnOpType { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">数据库字段名称</param>
        public DBColumnAttribute(string fieldName)
            : this(fieldName, DBColumnOpType.All)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">数据库字段名称</param>
        public DBColumnAttribute(string fieldName, string dataType)
            : this(fieldName, dataType, string.Empty, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">数据库字段名称</param>
        public DBColumnAttribute(string fieldName, string dataType, string defaultVal)
            : this(fieldName, dataType, defaultVal, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName">数据库字段名称</param>
        /// <param name="isIdentity">字段操作类型</param>
        public DBColumnAttribute(string fieldName, DBColumnOpType dbColOpType)
            : this(fieldName, string.Empty, string.Empty, false)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="dataType"></param>
        /// <param name="defaultVal"></param>
        /// <param name="notNull"></param>
        /// <param name="dbColOpType"></param>
        public DBColumnAttribute(string fieldName, string dataType, string defaultVal, bool notNull)
        {
            this.FieldName = fieldName;
            this.DataType = dataType;
            this.NotNull = notNull;
            this.Default = defaultVal;
        }
    }
}
