using System;
using DBFrame.DBMap;
using System.Data;
using System.Data.Common;
using System.Collections.Generic;
using System.Text;
using System.Data.SqlClient;

namespace DBFrame.Provider
{
    public class SQLServer : DBSession
    {
        /// <summary>
        /// 设置父类 连接工厂
        /// </summary>
        /// <returns></returns>
        protected override DbProviderFactory CreateDbProviderFactory()
        {
            return System.Data.SqlClient.SqlClientFactory.Instance;
        }

        #region override

        protected override string GuidToString(Guid guid)
        {
            return guid.ToString();
        }

        /// <summary>
        /// 重写父类  批量新增sql
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="table"></param>
        /// <param name="tbName"></param>
        /// <returns></returns>
        protected override string GetInsertBatchSql<T>(List<T> list, DBTable table, string tbName)
        {
            //sql格式
            //insert Test(id, name)
            //select 1, '广州1'
            //union all
            //select 2, '广州2'
            //union all

            StringBuilder fileds = new StringBuilder();
            foreach (var item in table.PrimaryKey)
            {
                if (item.DBPrimaryType != DBPrimaryType.Identity)
                {
                    fileds.AppendFormat(",{0}", item.Name);
                }
                
            }
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    fileds.AppendFormat(",{0}", col.Name);
                }
            }

            StringBuilder sql = new StringBuilder();
            int paraIdx = 0;
            foreach (T instance in list)
            {                
                sql.AppendFormat(" union all select ");
                //添加主键参数
                foreach (var item in table.PrimaryKey)
                {
                    if (item.DBPrimaryType != DBPrimaryType.Identity)
                    {
                        string paraName = this.FormatParameterName(string.Format("{0}_{1}", item.AliasName, paraIdx));
                        paraIdx++;
                        object primaryVal = this.GetValue(item, instance);
                        if (primaryVal == null) throw new MyDBException("新增对象，非自增长表主键不能为空");
                        sql.AppendFormat("{0},", paraName);
                        AddParameter(paraName, ParameterDirection.InputOutput, primaryVal);
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
                        string paraName = this.FormatParameterName(string.Format("{0}_{1}", col.AliasName, paraIdx));
                        paraIdx++;
                        sql.AppendFormat("{0},", paraName);
                        AddParameter(paraName, ParameterDirection.Input, value);
                    }
                }
                sql = sql.Remove(sql.Length - 1, 1);
                sql.AppendLine();
            }
            sql = sql.Remove(0, 11);
            return $"insert into {tbName}({fileds.ToString().TrimStart(',')}){sql.ToString()}";
        }

        /// <summary>
        /// 重写父类 批量新增
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="date"></param>
        public override void InsertBatchBulkCopy(DBTable table, DataTable dt, DateTime? date = default(DateTime?))
        {
            SqlConnection sqlcon = this.Connection as SqlConnection;
            if (sqlcon == null) throw new MyDBException("连接类型不是SqlConnection，不能使用SqlBulkCopy方式批量新增");
            SqlTransaction trans = null;
            if (this.Command.Transaction != null)
                trans = this.Command.Transaction as SqlTransaction;
            
            //取表名。如果是拆分表,则获取拆分表名
            string tbName = table.Name;
            if (table.SeparateType != SeparateType.None)
            {
                //如果传入时间为空
                if (!date.HasValue || date.Value == DateTime.MinValue)
                {
                    throw new Exception("分区表批量新增时，必须指定表的拆分日期");
                }
                //获取数据库表名
                tbName = TableSeparate.GetTableName(table, date.Value);
            }

            SqlBulkCopy bulkCopy = null;
            if (trans != null)
            {
                bulkCopy = new SqlBulkCopy(sqlcon, SqlBulkCopyOptions.Default, trans);
            }
            else
            {
                bulkCopy = new SqlBulkCopy(sqlcon);
            }
            try
            {
                bulkCopy.DestinationTableName = tbName;
                bulkCopy.BatchSize = dt.Rows.Count;
                bulkCopy.WriteToServer(dt);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                if (bulkCopy != null)
                {
                    bulkCopy.Close();
                }
            }
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
                    AddParameter(FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.InputOutput, primaryValue, table.PrimaryKey[0]);
                }
            }
            else
            {
                foreach (var item in table.PrimaryKey)
                {
                    AddParameter(FormatParameterName(item.AliasName), ParameterDirection.InputOutput, this.GetValue(item, instance), item);
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
                        AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value, col.ColumnType, col);
                    else
                        AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value, col);
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
            if (string.IsNullOrEmpty(order))
            {
                throw new Exception("无法对没有排序的SQL语句进行分页查询!");
            }

            if (pageIndex == 1)
            {
                return PrepareCustomSelectTopN(pageSize, fields, from, where, group, order, paras);
            }

            PrepareSelectParameter(paras);

            return FormatSqlForParameter(string.Format(@"
select top {0} * from (
	select	row_number() over (order by {1}
			) as r__n,
			{2}
	from	{3}
			{4}
			{5}
	) a
where r__n > {6}",
                 pageSize,
                 order,
                 fields,
                 from,
                 string.IsNullOrEmpty(where) ? "" : "where " + where,
                 string.IsNullOrEmpty(group) ? "" : "group by " + group,
                 (pageIndex - 1) * pageSize)
            );
        }

        /// <summary>
        /// select topN dbSql
        /// </summary>
        /// <param name="topN">指定获取记录条数</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>格式后的sql语句</returns>
        private string PrepareCustomSelectTopN(int topN, string fields, string from, string where, string group, string order, object[] paras)
        {
            PrepareSelectParameter(paras);

            return FormatSqlForParameter(string.Format(@"
select 	{0}
		{1}
from	{2}
		{3}
		{4}
		{5}",
                topN > 0 ? string.Format("top {0}", topN) : "",
                fields,
                from,
                string.IsNullOrEmpty(where) ? "" : "where " + where,
                string.IsNullOrEmpty(group) ? "" : "group by " + group,
                string.IsNullOrEmpty(order) ? "" : "order by " + order)
            );
        }

        #endregion
    }
}
