using System;
using System.Text;
using System.Data;
using DBFrame.DBMap;
using DBFrame.Cache;
using System.Collections.Generic;

namespace DBFrame
{
    public abstract partial class DBSession
    {
        #region insert

        /// <summary>
        /// 设置指定表的Insert语句
        /// </summary>
        /// <param name="table">属性表</param>
        /// <param name="tbName">新增的表名</param>
        /// <returns>新增的SQL语句</returns>
        protected virtual string GetInsertSql(DBTable table, string tbName)
        {
            StringBuilder fields = new StringBuilder();
            StringBuilder values = new StringBuilder();
            if (table.PrimaryKey[0].DBPrimaryType != DBPrimaryType.Identity)//如果不是自增长
            {
                foreach (var item in table.PrimaryKey)
                {
                    fields.AppendFormat(",{0}", item.Name);
                    values.AppendFormat(",{0}", this.FormatParameterName(item.AliasName));
                }
            }
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    fields.AppendFormat(",{0}", col.Name);
                    values.AppendFormat(",{0}", this.FormatParameterName(col.AliasName));
                }
            }
            return string.Format("insert into {0}({1})values({2});", tbName, fields.ToString().TrimStart(','), values.ToString().TrimStart(','));
        }

        /// <summary>
        /// 插入记录
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        protected virtual void Insert(object instance, object primaryValue, DBTable table, string insertSql)
        {
            Command.CommandText = insertSql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            //添加主键参数
            if (table.PrimaryKey.Count == 1)
            {
                IDataParameter pId = AddParameter(FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.InputOutput, primaryValue, table.PrimaryKey[0]);
            }
            else
            {
                foreach (var item in table.PrimaryKey)
                {
                    IDataParameter pId = AddParameter(FormatParameterName(item.AliasName), ParameterDirection.InputOutput, this.GetValue(item, instance), item);
                }
            }

            //添加其他属性参数
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, this.GetValue(col, instance), col);
                }
            }
            //执行命令
            Command.ExecuteNonQuery();
        }

        /// <summary>
        /// 新增对象，如果是拆分对象，则采用Id中的日期进行拆分
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public void Insert<T>(T instance)
        {
            Insert<T>(instance, DateTime.MinValue);
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="type">映射表类型</param>
        public void Insert(object instance, Type type)
        {
            Insert(instance, type, DateTime.MinValue);
        }

        /// <summary>
        /// 向指定日期拆分表中插入数据
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        public void Insert<T>(T instance, DateTime date)
        {
            Insert(instance, typeof(T), date);
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="type">映射表类型</param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        public void Insert(object instance, Type type, DateTime date)
        {
            DBTable table = MapHelper.GetDBTable(type);

            //取主键值
            object primaryVal = null;
            if (table.PrimaryKey[0].DBPrimaryType != DBPrimaryType.Identity)
            {
                primaryVal = this.GetValue(table.PrimaryKey[0], instance);
                if (primaryVal == null) throw new MyDBException("新增对象，非自增长表主键不能为空");
            }

            //取表名。如果是拆分表,则获取拆分表名
            string tbName = table.Name;
            if (table.SeparateType != SeparateType.None)
            {
                //如果传入时间为空，则取myId中的时间
                if (date == DateTime.MinValue)
                {
                    date = MyIdMake.GetMyIdDate(primaryVal);
                }

                //获取数据库表名
                tbName = TableSeparate.GetTableName(table, date);
            }

            //获取该数据库表 对应的DBSql中的Insert语句
            DBSql dbsql = DBSqlHelper.GetDBSql(tbName, _dbContext.DataType);
            if (string.IsNullOrEmpty(dbsql.InsertSql))//如果该表的新增语句为空，则生成该表的Insert语句
            {
                dbsql.InsertSql = GetInsertSql(table, tbName);
            }

            //将数据写入数据库
            Insert(instance, primaryVal, table, dbsql.InsertSql);
        }

        /// <summary>
        /// 批量新增
        /// 目前仅支持Oracle Sql Server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="pageSize">批量新增时单次写入的页大小</param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        public virtual int InsertBatch<T>(List<T> list, int pageSize = 100, DateTime? date = null)
        {
            if (list.Count < 1) return 0;
            if (list.Count > pageSize && this.Command.Transaction == null)
                throw new Exception(string.Format("批量新增条数大于{0}时必须开启事务", pageSize));

            DBTable table = MapHelper.GetDBTable(typeof(T));

            //取表名。如果是拆分表,则获取拆分表名
            string tbName = table.Name;
            if (table.SeparateType != SeparateType.None)
            {
                //如果传入时间为空，则取myId中的时间
                if (!date.HasValue || date.Value == DateTime.MinValue)
                {
                    throw new Exception("分区表批量新增时，必须指定表的拆分日期");
                }
                //获取数据库表名
                tbName = TableSeparate.GetTableName(table, date.Value);
            }

            if (list.Count > 100)
            {
                int total = list.Count;
                int totalPage = (int)Math.Ceiling(total / (double)pageSize);
                int resCont = 0;
                for (int i = 0; i < totalPage; i++)
                {
                    int beginIndex = i * pageSize;
                    int getCont = pageSize;
                    if (total - beginIndex < pageSize)
                        getCont = total - beginIndex;
                    List<T> nList = list.GetRange(beginIndex, getCont);

                    Command.Parameters.Clear();
                    string sql = GetInsertBatchSql(nList, table, tbName);
                    Command.CommandText = sql;
                    Command.CommandType = CommandType.Text;
                    resCont += Command.ExecuteNonQuery();
                }
                return resCont;
            }
            else
            {
                Command.Parameters.Clear();
                string sql = GetInsertBatchSql(list, table, tbName);
                Command.CommandText = sql;
                Command.CommandType = CommandType.Text;
                return Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 批量新增
        /// 目前仅支持Oracle Sql Server
        /// </summary>
        /// <param name="list"></param>
        /// <param name="type">映射类型</param>
        /// <param name="pageSize">批量新增时单次写入的页大小</param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        /// <returns></returns>
        public virtual int InsertBatch(List<object> list, Type type, int pageSize = 100, DateTime? date = null)
        {
            if (list.Count < 1) return 0;
            if (list.Count > pageSize && this.Command.Transaction == null)
                throw new Exception(string.Format("批量新增条数大于{0}时必须开启事务", pageSize));

            DBTable table = MapHelper.GetDBTable(type);

            //取表名。如果是拆分表,则获取拆分表名
            string tbName = table.Name;
            if (table.SeparateType != SeparateType.None)
            {
                //如果传入时间为空，则取myId中的时间
                if (!date.HasValue || date.Value == DateTime.MinValue)
                {
                    throw new Exception("分区表批量新增时，必须指定表的拆分日期");
                }
                //获取数据库表名
                tbName = TableSeparate.GetTableName(table, date.Value);
            }

            if (list.Count > 100)
            {
                int total = list.Count;
                int totalPage = (int)Math.Ceiling(total / (double)pageSize);
                int resCont = 0;
                for (int i = 0; i < totalPage; i++)
                {
                    int beginIndex = i * pageSize;
                    int getCont = pageSize;
                    if (total - beginIndex < pageSize)
                        getCont = total - beginIndex;
                    List<object> nList = list.GetRange(beginIndex, getCont);

                    Command.Parameters.Clear();
                    string sql = GetInsertBatchSql(nList, table, tbName);
                    Command.CommandText = sql;
                    Command.CommandType = CommandType.Text;
                    resCont += Command.ExecuteNonQuery();
                }
                return resCont;
            }
            else
            {
                Command.Parameters.Clear();
                string sql = GetInsertBatchSql(list, table, tbName);
                Command.CommandText = sql;
                Command.CommandType = CommandType.Text;
                return Command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 批量新增 采用SqlBulkCopy方式批量新增
        /// 利用SqlBulkCopy一次性把Table中的数据插入到数据库
        /// 目前仅支持Sql Server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        /// <returns></returns>
        public virtual void InsertBatchBulkCopy<T>(List<T> list, DateTime? date = null)
        {
            DBTable table = MapHelper.GetDBTable(typeof(T));

            //创建DataTable表和列
            DataTable dt = new DataTable();
            foreach (var item in table.PrimaryKey)
            {
                if (item.DBPrimaryType != DBPrimaryType.Identity)
                {
                    dt.Columns.Add(item.Name);
                }

            }
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    dt.Columns.Add(col.Name);
                }
            }

            //转换数据
            foreach (object instance in list)
            {
                DataRow row = dt.NewRow();
                //添加主键参数
                foreach (var prim in table.PrimaryKey)
                {
                    if (prim.DBPrimaryType != DBPrimaryType.Identity)
                    {
                        object primaryVal = this.GetValue(prim, instance);
                        if (primaryVal == null) throw new MyDBException("新增对象，非自增长表主键不能为空");
                        row[prim.Name] = primaryVal;
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
                        row[col.Name] = value;
                    }
                }
                dt.Rows.Add(row);
            }
            InsertBatchBulkCopy(table, dt, date);
        }

        /// <summary>
        /// 批量新增 采用SqlBulkCopy方式批量新增
        /// 利用SqlBulkCopy一次性把Table中的数据插入到数据库
        /// 目前仅支持Sql Server
        /// </summary>
        /// <param name="list"></param>
        /// <param name="type">映射类型</param>
        /// <param name="date">要写入表的拆分日期。注意该日期必须与Id中的日期对应。比如按年拆分的表，Id中的年份与该日期中的年份必须相同</param>
        public virtual void InsertBatchBulkCopy(List<object> list, Type type, DateTime? date = null)
        {
            DBTable table = MapHelper.GetDBTable(type);

            //创建DataTable表和列
            DataTable dt = new DataTable();
            foreach (var item in table.PrimaryKey)
            {
                if (item.DBPrimaryType != DBPrimaryType.Identity)
                {
                    dt.Columns.Add(item.Name);
                }

            }
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoInsert))
                {
                    dt.Columns.Add(col.Name);
                }
            }

            //转换数据
            foreach (object instance in list)
            {
                DataRow row = dt.NewRow();
                //添加主键参数
                foreach (var prim in table.PrimaryKey)
                {
                    if (prim.DBPrimaryType != DBPrimaryType.Identity)
                    {
                        object primaryVal = this.GetValue(prim, instance);
                        if (primaryVal == null) throw new MyDBException("新增对象，非自增长表主键不能为空");
                        row[prim.Name] = primaryVal;
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
                        row[col.Name] = value;
                    }
                }
                dt.Rows.Add(row);
            }
            InsertBatchBulkCopy(table, dt, date);
        }

        /// <summary>
        /// 批量新增 采用SqlBulkCopy方式批量新增
        /// 利用SqlBulkCopy一次性把Table中的数据插入到数据库
        /// 目前仅支持Sql Server
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dt"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public virtual void InsertBatchBulkCopy(DBTable table, DataTable dt, DateTime? date = null)
        {
            throw new Exception(string.Format("数据库{0}未实现SqlBulkCopy方式批量新增", this._dbContext.DataType.ToString()));
        }

        /// <summary>
        /// 获取批量新增的sql语句
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="table"></param>
        /// <param name="tbName"></param>
        /// <returns></returns>
        protected virtual string GetInsertBatchSql<T>(List<T> list, DBTable table, string tbName)
        {
            throw new Exception(string.Format("数据库{0}未实现批量新增的操作", this._dbContext.DataType.ToString()));
        }

        #endregion

        #region update

        /// <summary>
        /// 设置指定表的Update语句
        /// </summary>
        /// <param name="table">属性表</param>
        /// <param name="tbName">表名</param>
        /// <returns></returns>
        protected virtual string GetUpdateSql(DBTable table, string tbName)
        {
            StringBuilder sets = new StringBuilder();
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoUpdate))
                {
                    sets.AppendFormat(",{0}={1}", col.Name, this.FormatParameterName(col.AliasName));
                }
            }

            StringBuilder where = new StringBuilder();
            foreach (var item in table.PrimaryKey)
            {
                where.AppendFormat(" and {0}={1}", item.Name, this.FormatParameterName(item.AliasName));
            }

            return string.Format("update {0} set {1} where {2}",
                                    tbName,
                                    sets.ToString().TrimStart(','),
                                    where.ToString().Remove(0, 4));
        }

        /// <summary>
        /// 保存一个指定类型的对象
        /// </summary>
        /// <typeparam name="T">类型</typeparam>
        /// <param name="instance">实例</param>
        /// <param name="table"></param>
        /// <param name="updateSql"></param>
        /// <param name="primaryVal">主键值</param>
        /// <returns>受影响的行数</returns>
        protected virtual int Update<T>(T instance, DBTable table, string updateSql, object primaryVal)
        {
            Command.Parameters.Clear();
            //添加其他属性参数
            foreach (DBColumn col in table.ColumnList)
            {
                if (!col.DBColumnOpType.HasFlag(DBColumnOpType.NoUpdate))
                {
                    object value = this.GetValue(col, instance);
                    if (col.ColumnType == DBColumnType.Guid)
                    {
                        Guid vl = new Guid(value.ToString());
                        if (vl == Guid.Empty) value = null;
                    }

                    IDataParameter param = AddParameter(this.FormatParameterName(col.AliasName), ParameterDirection.Input, value, col);
                    if (value == null && col.ColumnType == DBColumnType.ByteArray)
                    {
                        param.DbType = DbType.Binary;
                    }
                }
            }
            //添加主键参数
            if (table.PrimaryKey.Count == 1)
            {
                AddParameter(this.FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.Input, primaryVal, table.PrimaryKey[0]);
            }
            else
            {
                foreach (var item in table.PrimaryKey)
                {
                    AddParameter(this.FormatParameterName(item.AliasName), ParameterDirection.Input, this.GetValue(item, instance), item);
                }
            }

            Command.CommandType = CommandType.Text;
            Command.CommandText = updateSql;
            return Command.ExecuteNonQuery();
        }

        /// <summary>
        /// 修改对象，支持分表数据的修改
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns>受影响的行数</returns>
        public int Update<T>(T instance)
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);

            //获取修改对象的主键
            object primaryVal = this.GetValue(table.PrimaryKey[0], instance);

            //获取主键对应的数据库表名
            string tbName = TableSeparate.GetTableName(table, primaryVal);

            DBSql dbsql = DBSqlHelper.GetDBSql(tbName, _dbContext.DataType);
            if (string.IsNullOrEmpty(dbsql.UpdateSql))//如果该表的修改语句为空，则生成该表的update语句
            {
                dbsql.UpdateSql = GetUpdateSql(table, tbName);
            }

            //修改数据
            int retVal = Update<T>(instance, table, dbsql.UpdateSql, primaryVal);
            return retVal;
        }

        #endregion

        #region delete

        /// <summary>
        /// 设置指定表的删除语句
        /// </summary>
        /// <param name="table">属性表</param>
        protected string GetDeleteSql(DBTable table, string tbName)
        {
            return string.Format("delete from {0} where {1}={2}",
                tbName,
                table.PrimaryKey[0].Name,
                this.FormatParameterName(table.PrimaryKey[0].AliasName));
        }

        /// <summary>
        /// 执行删除
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="IdT"></typeparam>
        /// <param name="id"></param>
        /// <param name="table"></param>
        /// <param name="deleteSql"></param>
        /// <returns></returns>
        protected int Delete<T, IdT>(IdT id, DBTable table, string deleteSql)
        {
            Command.Parameters.Clear();
            AddParameter(this.FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.Input, id);

            Command.CommandType = CommandType.Text;
            Command.CommandText = deleteSql;

            return Command.ExecuteNonQuery();
        }

        /// <summary>
        /// 根据主键删除，支持分表数据的删除
        /// </summary>
        /// <typeparam name="T">要删除的类型</typeparam>
        /// <typeparam name="IdT">主键类型</typeparam>
        /// <param name="id">主键值</param>
        /// <returns>影响行数</returns>
        public int Delete<T, IdT>(IdT id)
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);
            if (table.PrimaryKey.Count > 1)
                throw new Exception("联合主键表，不支持根据主键删除");

            //获取对应的数据库表名
            string tbName = TableSeparate.GetTableName(table, id);

            DBSql dbsql = DBSqlHelper.GetDBSql(tbName, _dbContext.DataType);
            if (string.IsNullOrEmpty(dbsql.DeleteSql))//如果该表的修改语句为空，则生成该表的update语句
            {
                dbsql.DeleteSql = GetDeleteSql(table, tbName);
            }

            int retVal = Delete<T, IdT>(id, table, dbsql.DeleteSql);
            return retVal;
        }

        /// <summary>
        /// 根据条件删除，此删除方法，不支持分表数据的删除
        /// </summary>
        /// <typeparam name="T">要删除的类型</typeparam>
        /// <param name="where">删除条件</param>
        /// <param name="paras">参数</param>
        /// <returns>影响行数</returns>
        public int Delete<T>(string where, params object[] paras)
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);
            DBSql dbsql = DBSqlHelper.GetDBSql(table.Name, _dbContext.DataType);

            where = FormatWhereOrder(table, where);
            Command.CommandText = string.Format("delete from {0} {1}", table.Name, where.Length > 0 ? " where " + where : "");
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            return Command.ExecuteNonQuery();
        }

        #endregion
    }
}
