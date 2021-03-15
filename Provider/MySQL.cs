using System;
using System.Data.Common;
using DBFrame.DBMap;
using System.Data;

namespace DBFrame.Provider
{
    public class MySQL : DBSession
    {
        public const string ProviderTypeString = "MySql.Data";

        protected override DbProviderFactory CreateDbProviderFactory()
        {
            Type type = GetType(ProviderTypeString, "MySql.Data.MySqlClient.MySqlClientFactory");
            return (DbProviderFactory)Activator.CreateInstance(type, true);
        }

        protected override string FormatParameterName(string p)
        {
            return "@" + p;
        }

        protected override string GuidToString(Guid guid)
        {
            return guid.ToString("N").ToUpper();
        }

        /// <summary>
        /// 重写父类的新增--在父类的基础上增加了 返回新增的ID
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="table"></param>
        /// <param name="insertSql"></param>
        protected override void Insert(object instance, object primaryValue, DBTable table, string insertSql)
        {
            insertSql = string.Format("{0}select @@identity;", insertSql);
            Command.CommandText = insertSql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            //添加主键参数
            if (table.PrimaryKey.Count == 1)
            {
                if (table.PrimaryKey[0].DBPrimaryType != DBPrimaryType.Identity)
                {
                    AddParameter(FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.InputOutput, primaryValue);
                }
            }
            else
            {
                foreach (var item in table.PrimaryKey)
                {
                    AddParameter(FormatParameterName(item.AliasName), ParameterDirection.InputOutput, this.GetValue(item, instance));
                }
            }
            //添加其他属性参数
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    object value = this.GetValue(col, instance);
                    if (col.ColumnType == DBColumnType.Guid)
                    {
                        Guid vl = new Guid(value.ToString());
                        if (vl == Guid.Empty) value = null;
                    }

                    if (value == null)
                        AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value, col.ColumnType);
                    else
                        AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value);
                }
            }
            //执行命令
            object obj = Command.ExecuteScalar();
            if (table.PrimaryKey[0].DBPrimaryType == DBPrimaryType.Identity)
            {
                this.SetValue(table.PrimaryKey[0], instance, Convert.ChangeType(obj, table.PrimaryKey[0].Type));
            }
        }

        /// <summary>
        /// 分页Sql,页码从1开始
        /// </summary>
        /// <param name="pageIndex">分页索引，以1开始</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>分页的SQL语句</returns>
        protected override string PrepareCustomSelectPaging(int pageIndex, int pageSize, string fields, string from, string where, string group, string order, object[] paras)
        {
            PrepareSelectParameter(paras);

            return FormatSqlForParameter(string.Format(@"		
			select {0}
			  from {1}
				   {2}
				   {3}
                   {4}
            limit {5},{6}",
                    fields,
                    from,
                    string.IsNullOrEmpty(where) ? "" : "where " + where,
                    string.IsNullOrEmpty(group) ? "" : "group by " + group,
                    string.IsNullOrEmpty(order) ? "" : "order by " + order,
                    (pageIndex - 1) * pageSize,
                    pageSize
                )
            );
        }
    }
}
