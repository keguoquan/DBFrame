using System;
using System.Collections.Generic;
using System.Reflection;
using DBFrame.DBMapAttr;
using System.Collections.Concurrent;

namespace DBFrame.DBMap
{
    public class MapHelper
    {
        /// <summary>
        /// 映射表信息
        /// </summary>
        public static IDictionary<string, DBTable> TableDictionary = new Dictionary<string, DBTable>();

        /// <summary>
        /// 初始化实体类
        /// </summary>
        /// <param name="assemblies">待初始化的程序集</param>
        public static void InitDBMap(params Assembly[] assemblies)
        {
            if (assemblies == null) return;

            lock (TableDictionary)
            {
                foreach (Assembly assembly in assemblies)
                {
                    Type[] types = assembly.GetTypes();

                    foreach (Type type in types)
                    {
                        string tbName = string.Empty;
                        DBTableAttribute attr = null;

                        //初始化表信息
                        foreach (DBTableAttribute attribute in type.GetCustomAttributes(typeof(DBTableAttribute), false))
                        {
                            tbName = string.IsNullOrEmpty(attribute.Name) ? type.Name : attribute.Name;
                            attr = attribute;
                        }
                        //表示该类型未映射位 实体类
                        if (string.IsNullOrWhiteSpace(tbName)) continue;
                        if (attr == null) continue;

                        //所有映射表只添加一次
                        string tableKey = type.FullName;
                        DBTable table = null;
                        if (!TableDictionary.ContainsKey(tableKey))
                        {
                            table = new DBTable(type, tbName, attr);
                            TableDictionary.Add(tableKey, table);
                        }
                        else
                        {
                            table = TableDictionary[tableKey];
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 根据映射类型，获取DBTable
        /// 不存在 则抛异常
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DBTable GetDBTable(Type type)
        {
            string key = type.FullName;
            if (TableDictionary.ContainsKey(key))
            {
                return TableDictionary[key];
            }
            throw new ArgumentException("[" + type.FullName + "]是一个无效的映射类!");
        }

        /// <summary>
        /// 根据映射类型，获取DBTable
        /// 不存在 返回null
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static DBTable GetDBTableExist(Type type)
        {
            string key = type.FullName;
            if (TableDictionary.ContainsKey(key))
            {
                return TableDictionary[key];
            }
            return null;
        }
    }

    public class DBSqlHelper
    {
        /// <summary>
        /// 映射字段字典
        /// key	  -- [数据库表名 + DataBaseType]
        /// value -- [DBSql]
        /// </summary>
        private static ConcurrentDictionary<string, DBSql> _sqlDict = new ConcurrentDictionary<string, DBSql>();

        /// <summary>
        /// 获取DBSql
        /// </summary>
        /// <param name="tbName">表名</param>
        /// <returns></returns>
        public static DBSql GetDBSql(string tbName, DataBaseType dbType)
        {
            string key = string.Format("{0}_{1}", tbName, dbType);

            DBSql dbsql;
            if (!_sqlDict.TryGetValue(tbName, out dbsql))
            {
                dbsql = new DBSql();
                _sqlDict.TryAdd(tbName, dbsql);
            }
            return dbsql;
        }
    }

    public class DBSql
    {
        /// <summary>
        /// Insert SQL Statement
        /// </summary>
        public string InsertSql { get; internal set; }

        /// <summary>
        /// Update SQL Statement
        /// </summary>
        public string UpdateSql { get; internal set; }

        /// <summary>
        /// Delete SQL Statement
        /// </summary>
        public string DeleteSql { get; internal set; }
    }
}
