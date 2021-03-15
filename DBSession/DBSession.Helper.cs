using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Data;
using System.Xml;
using DBFrame.DBMap;
using System.Reflection;

namespace DBFrame
{
    /// <summary>
    /// 数据库连接池
    /// </summary>
    public abstract partial class DBSession
    {
        /// <summary>
        /// 格式化sql语句参数,有的数据库参数前缀是 @ 有的是 :
        /// </summary>
        /// <param name="p">参数名称</param>
        /// <returns>合法的参数名称</returns>
        protected virtual string FormatParameterName(string p)
        {
            return "@" + p;
        }

        /// <summary>
        /// Guid转字符串
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        protected virtual string GuidToString(Guid guid)
        {
            return guid.ToString();
        }

        #region Get Or Set persistent Column Value

        /// <summary>
        /// 设置映射对象相应属性值
        /// </summary>
        /// <param name="column">对应数据库中的列</param>
        /// <param name="persistent">映射对象</param>
        /// <param name="value">设置的值</param>
        protected void SetValue(DBColumn column, object persistent, object value)
        {
            if (column.SetHandler != null)
            {
                if (column.Type == typeof(bool) && !(value is bool))
                {
                    value = (int)value == 1;
                }
                else if (column.ColumnType == DBColumnType.TimeSpan)
                {
                    value = new TimeSpan((long)value);
                }
                else if (column.ColumnType == DBColumnType.Guid && !(value is Guid))
                {
                    value = new Guid((string)value);
                }
                else if (column.ColumnType == DBColumnType.Xml && !(value is XmlDocument))
                {
                    var xml = (new XmlDocument());
                    xml.LoadXml((string)value);
                    value = xml;
                }

                column.SetHandler(persistent, value);
            }
        }

        /// <summary>
        /// 获取对象对应映射属性的值
        /// </summary>
        /// <param name="column">对应数据库中的列</param>
        /// <param name="persistent">映射对象</param>
        /// <returns>返回的值</returns>
        protected object GetValue(DBColumn column, object persistent)
        {
            if (column.GetHandler != null)
            {
                object value = column.GetHandler(persistent);

                if (value != null)
                {
                    if (value is bool)
                    {
                        //布尔需要转换为0和1才能插入到数据库中
                        return ((bool)value) ? 1 : 0;
                    }
                    else if (column.Type.IsEnum)
                    {
                        //枚举需要转换为Int32才能插入到数据库中
                        return (int)value;
                    }
                    else if (value is Guid)
                    {
                        //Guid处理成为字符串返回,以便插入到数据库中
                        if (value == null) return value;
                        return GuidToString(new Guid(value.ToString()));
                    }
                    else if (value is XmlDocument)
                    {
                        return ((XmlDocument)value).InnerXml;
                    }
                }
                return value;
            }
            return null;
        }

        #endregion

        /// <summary>
        /// 获取合法的where语句
        /// </summary>
        /// <param name="dbsql">属性表</param>
        /// <param name="sql">原始的where</param>
        /// <returns>处理后合法的where语句</returns>
        protected string FormatWhereOrder(DBTable table, string sql)
        {
            List<string> list = new List<string>();
            Regex _regexField = new Regex("(?<={).*?(?=})", RegexOptions.None);
            MatchCollection mc = _regexField.Matches(sql);
            foreach (Match ma in mc)
            {
            LabelForeach:
                string value = ma.Value;
                bool notFound = true;
                foreach (string str in list)
                {
                    if (str == value)
                    {
                        notFound = false;
                        break;
                    }
                }
                if (notFound)
                {
                    list.Add(value);
                    bool isPrim = false;
                    foreach (var item in table.PrimaryKey)
                    {
                        if (value == item.AliasName)
                        {
                            sql = sql.Replace("{" + value + "}", item.Name);
                            isPrim = true;
                        }
                    }

                    if (!isPrim)
                    {
                        DBTable tbl = table;
                        foreach (DBColumn col in tbl.ColumnList)
                        {
                            if (value == col.AliasName)
                            {
                                sql = sql.Replace("{" + value + "}", col.Name);
                                goto LabelForeach;
                            }
                        }
                    }
                }
            }
            return FormatSqlForParameter(sql);
        }


        /// <summary>
        /// 格式化SQL语句中的参数部分
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>格式化后的SQL语句</returns>
        protected string FormatSqlForParameter(string sql)
        {
            Regex _regexParameter = new Regex("\\?", RegexOptions.None);
            MatchCollection mc = _regexParameter.Matches(sql);
            for (int i = mc.Count - 1; i >= 0; i--)
            {
                int idx = mc[i].Index;
                sql = sql
                    .Remove(idx, 1)
                    .Insert(idx, FormatParameterName("p" + i.ToString()));
            }
            return sql;
        }

        /// <summary>
        /// 格式化sql语句
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="paras">参数列表</param>
        /// <returns>格式化后SQL语句</returns>
        protected string PrepareCustomSelect(string sql, object[] paras)
        {
            PrepareSelectParameter(paras);
            return FormatSqlForParameter(sql);
        }

        /// <summary>
        /// 为执行select sql语句准备输入参数
        /// </summary>
        /// <param name="paras">参数</param>
        protected void PrepareSelectParameter(object[] paras)
        {
            Command.Parameters.Clear();
            if (paras == null) return;
            int i = 0;
            foreach (object obj in paras)
            {
                Command.Parameters.Add(
                    CreateParameter(
                        FormatParameterName("p" + (i++).ToString()),
                        ParameterDirection.Input,
                        obj
                    )
                );
            }
        }

        /// <summary>
        /// 辅助函数,通过AssemblyName,以及TypeName获取相应的Type
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="typeName"></param>
        /// <returns></returns>
        protected static Type GetType(string assemblyName, string typeName)
        {
            Assembly assembly = Assembly.Load(assemblyName);
            if (assembly == null)
            {
                string name = assemblyName + ",";
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (a.FullName.StartsWith(name))
                    {
                        assembly = a;
                        break;
                    }
                }
            }
            return assembly == null ? null : assembly.GetType(typeName);
        }
    }
}
