using System;
using System.Text;
using System.Data.Common;
using DBFrame.DBMap;
using System.Data;
using System.Collections.Generic;
using System.Reflection;

namespace DBFrame.Provider
{
    public class Oracle : DBSession
    {
        public const string ProviderTypeString = "Oracle.ManagedDataAccess";

        protected override DbProviderFactory CreateDbProviderFactory()
        {
            Type type = GetType(ProviderTypeString, "Oracle.ManagedDataAccess.Client.OracleClientFactory");
            return (DbProviderFactory)Activator.CreateInstance(type, true);
        }

        #region override

        protected override string FormatParameterName(string p)
        {
            return ":" + p;
        }

        protected override string GuidToString(Guid guid)
        {
            return guid.ToString("N").ToUpper();
        }

        protected override string GetInsertSql(DBTable table, string tbName)
        {
            StringBuilder fields = new StringBuilder();
            StringBuilder values = new StringBuilder();

            //添加主键
            foreach (var item in table.PrimaryKey)
            {
                fields.AppendFormat(",{0}", item.Name);
                values.AppendFormat(",{0}", this.FormatParameterName(item.AliasName));
            }
            //添加其他属性
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    fields.AppendFormat(",{0}", col.Name);
                    values.AppendFormat(",{0}", this.FormatParameterName(col.AliasName));
                }
            }

            return string.Format("insert into {0}({1})values({2})", tbName, fields.ToString().TrimStart(','), values.ToString().TrimStart(','));
        }

        protected override string GetInsertBatchSql<T>(List<T> list, DBTable table, string tbName)
        {
            StringBuilder fileds = new StringBuilder();
            foreach (var item in table.PrimaryKey)
            {
                if (item.DBPrimaryType == DBPrimaryType.Identity)
                {
                    //因为此种写法  SEQ不会自增长
                    //INSERT ALL 
                    //INTO CodeItem(ID,VAL,NAME,CODE)VALUES(SEQ_CODEITEM.nextval,'1','aaa','12') 
                    //INTO CodeItem(ID,VAL,NAME,CODE)VALUES(SEQ_CODEITEM.nextval,'2','nnn','12') 
                    //INTO CodeItem(ID,VAL,NAME,CODE)VALUES(SEQ_CODEITEM.nextval,'3','ccc','14')
                    //SELECT 1 FROM DUAL
                    throw new MyDBException("主键自增长表不支持批量写入");
                }
                fileds.AppendFormat(",{0}", item.Name);
            }
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    fileds.AppendFormat(",{0}", col.Name);
                }
            }

            string intotb = string.Format("INTO {0}({1})", tbName, fileds.ToString().TrimStart(','));
            StringBuilder sql = new StringBuilder();
            sql.AppendFormat("INSERT ALL ");
            int paraIdx = 0;
            foreach (T instance in list)
            {
                sql.AppendLine();
                sql.AppendFormat("{0}VALUES(", intotb);
                //添加主键参数
                foreach (var item in table.PrimaryKey)
                {
                    string paraName = this.FormatParameterName(string.Format("{0}_{1}", item.AliasName, paraIdx));
                    paraIdx++;
                    object primaryVal = this.GetValue(item, instance);
                    if (primaryVal == null) throw new MyDBException("新增对象，非自增长表主键不能为空");
                    sql.AppendFormat("{0},", paraName);
                    AddParameter(paraName, ParameterDirection.InputOutput, primaryVal);
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
                sql.AppendFormat(") ");
            }
            sql.AppendLine();
            sql.AppendLine("SELECT 1 FROM DUAL");
            return sql.ToString();
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
            Command.CommandText = insertSql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            //获取seq 主键值
            if (table.PrimaryKey[0].DBPrimaryType == DBPrimaryType.Identity)
            {
                primaryValue = ExecuteScalar(string.Format("select {0}.nextval from dual", table.PrimaryKey[0].SequenceName));
                //添加主键参数
                AddParameter(FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.Input, primaryValue, table.PrimaryKey[0]);
                Command.CommandText = insertSql;
            }
            else
            {
                //添加主键参数
                if (table.PrimaryKey.Count == 1)
                {
                    AddParameter(FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.InputOutput, primaryValue, table.PrimaryKey[0]);
                }
                else
                {
                    foreach (var item in table.PrimaryKey)
                    {
                        AddParameter(FormatParameterName(item.AliasName), ParameterDirection.InputOutput, this.GetValue(item, instance), item);
                    }
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
                    AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value, col);
                }
            }
            //执行命令
            Command.ExecuteNonQuery();
            if (table.PrimaryKey[0].DBPrimaryType == DBPrimaryType.Identity)
            {
                this.SetValue(table.PrimaryKey[0], instance, Convert.ChangeType(primaryValue, table.PrimaryKey[0].Type));
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
select * from(
		select	a.*, rownum r__n from(
			select	{0}
			from	{1}
					{2}
					{3}
			        {4}
		) a
		where rownum <= {5}
	) b
where r__n > {6}",
                    fields,
                    from,
                    string.IsNullOrEmpty(where) ? "" : "where " + where,
                    string.IsNullOrEmpty(group) ? "" : "group by " + group,
                    string.IsNullOrEmpty(order) ? "" : "order by " + order,
                    pageIndex * pageSize,
                    (pageIndex - 1) * pageSize
                )
            );
        }

        protected override IDataParameter CreateParameter(string name, ParameterDirection direction, object value, DBColumn column = null)
        {
            IDataParameter parameter = Command.CreateParameter();
            if (value is Guid) value = GuidToString((Guid)value);

            parameter.ParameterName = name;
            parameter.Direction = direction;
            parameter.Value = value == null ? DBNull.Value : value;

            if (value != null && value is string && column != null && (!string.IsNullOrWhiteSpace(column.DataType)))
            {
                if (column.DataType.ToUpper() == "CLOB")
                {
                    PropertyInfo field = parameter.GetType().GetProperty("OracleDbType");
                    if (field != null)
                    {
                        field.SetValue(parameter, 105, null);//105 对应OracleDbType.Clob
                    }
                }
            }
            return parameter;
        }

        #endregion
    }
}
