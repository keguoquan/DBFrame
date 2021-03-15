using System;
using System.Collections.Generic;
using System.Reflection;
using DBFrame.DBMapAttr;

namespace DBFrame.DBMap
{
    /// <summary>
    /// 数据库表映射实体
    /// </summary>
    public class DBTable
    {
        /// <summary>
        /// 映射类型
        /// </summary>
        public Type MapType { get; set; }

        /// <summary>
        /// 映射类名
        /// </summary>
        public string AliasName { get; set; }

        /// <summary>
        /// 对应的数据库表名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 主键
        /// </summary>
        public List<DBPrimaryKey> PrimaryKey { get; private set; }

        /// <summary>
        /// 字段列表(不包含主键)
        /// </summary>
        public List<DBColumn> ColumnList { get; private set; }

        /// <summary>
        /// 数据缓存方式
        /// </summary>
        public CacheType CacheType { get; set; }

        /// <summary>
        /// 该表数据拆分方式，此值不为None时，必须设置CreateTBSql
        /// </summary>
        public SeparateType SeparateType;

        /// <summary>
        /// 该数据库表的创建SQL语句，当表的数据拆分方式不为None时，必须设置此值
        /// </summary>
        public string CreateSql { get; set; }

        /// <summary>
        /// 该数据对象缓存时间（秒数）。0表示不缓存
        /// </summary>
        public int CacheSeconds { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="type">映射类型</param>
        /// <param name="tbName">表名</param>
        /// <param name="cacheType">表数据缓存方式</param>
        /// <param name="separateType">数据库表拆分方式</param>
        /// <param name="createSql">创建表的sql语句</param>
        /// <param name="separateFieldName">数据表拆分字段</param>
        /// <param name="separateIDHashNum"></param>
        /// <param name="cacheSeconds">是否缓存</param>
        public DBTable(Type type, string tbName, DBTableAttribute attr)
        {
            this.MapType = type;
            this.AliasName = type.Name;
            this.Name = tbName;
            this.CacheType = attr.CacheType;
            this.SeparateType = attr.SeparateType;

            this.CreateSql = attr.CreateSql;
            if (this.SeparateType != SeparateType.None && string.IsNullOrWhiteSpace(this.CreateSql))
                throw new MyDBException(string.Format("表{0}没有配置创建SQL语句", tbName));
            this.CacheSeconds = attr.CacheSeconds;

            PrimaryKey = new List<DBPrimaryKey>();
            ColumnList = new List<DBColumn>();

            //初始化字段列表
            GetFieldList(type);

            if (PrimaryKey.Count == 0) throw new Exception(string.Format("表{0}未映射主键", this.Name));
            if (PrimaryKey.Count > 1 && PrimaryKey[0].DBPrimaryType == DBPrimaryType.Identity)
                throw new Exception(string.Format("表{0}联合主键不支持自增长", this.Name));
            if (PrimaryKey.Count > 1 && this.SeparateType != DBFrame.SeparateType.None)
                throw new Exception(string.Format("表{0}联合主键不支持表拆分", this.Name));
        }

        private void GetFieldList(Type type)
        {
            foreach (MemberInfo mi in type.FindMembers(MemberTypes.Property | MemberTypes.Field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, null))
            {
                string field_name = "";
                foreach (DBPrimaryKeyAttribute pk in mi.GetCustomAttributes(typeof(DBPrimaryKeyAttribute), false))
                {
                    //如果没有指定字段名称,使用MemberInfo的Name作为字段名称
                    field_name = string.IsNullOrEmpty(pk.FieldName) ? mi.Name : pk.FieldName;

                    PrimaryKey.Add(new DBPrimaryKey(mi, field_name, pk.DataType, pk.NotNull, pk.Default, pk.DBPrimaryType, string.Format("SEQ_{0}", this.AliasName)));
                }

                //如果是其他普通字段,只需要处理自己内部的属性
                if (mi.DeclaringType != type)
                {
                    continue;
                }
                foreach (DBColumnAttribute ca in mi.GetCustomAttributes(typeof(DBColumnAttribute), false))
                {
                    if (field_name == "")
                    {
                        //如果没有指定字段名称,使用MemberInfo的Name作为字段名称
                        field_name = string.IsNullOrEmpty(ca.FieldName) ? mi.Name : ca.FieldName;
                        ColumnList.Add(new DBColumn(mi, field_name, ca.DataType, ca.NotNull, ca.Default, ca.DBColumnOpType));
                    }
                }
            }
        }
    }
}
