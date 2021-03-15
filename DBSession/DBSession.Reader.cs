using System.Collections.Generic;
using System.Data;
using DBFrame.DBMap;
using DBFrame.FullData;
using DBFrame.Cache;
using System;

namespace DBFrame
{
    public abstract partial class DBSession
    {
        #region 映射类Select方法

        /// <summary>
        /// 通过ID获取指定类型的对象，有缓存,支持分表数据的获取
        /// 只支持映射类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public T GetById<T>(object id)
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);

            if (table.PrimaryKey.Count > 1) throw new Exception("联合主建，不支持GetById方法");

            //获取对应的数据库表名
            string tbName = TableSeparate.GetTableName(table, id);

            string pName = this.FormatParameterName(table.PrimaryKey[0].AliasName);
            string sql = string.Format("select * from {0} where {1}={2}", tbName, table.PrimaryKey[0].Name, pName);

            Command.Parameters.Clear();
            //添加查询条件参数
            AddParameter(this.FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.Input, id);
            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;

            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullMapObj<T>(reader)(reader);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 通过ID获取指定类型的对象，支持分表数据的获取
        /// </summary>
        /// <param name="id"></param>
        /// <param name="type">返回对象类型</param>
        /// <returns></returns>
        public object GetById(object id, Type type)
        {
            DBTable table = MapHelper.GetDBTable(type);

            if (table.PrimaryKey.Count > 1) throw new Exception("联合主建，不支持GetById方法");

            //获取对应的数据库表名
            string tbName = TableSeparate.GetTableName(table, id);

            string pName = this.FormatParameterName(table.PrimaryKey[0].AliasName);
            string sql = string.Format("select * from {0} where {1}={2}", tbName, table.PrimaryKey[0].Name, pName);

            Command.Parameters.Clear();
            //添加查询条件参数
            AddParameter(this.FormatParameterName(table.PrimaryKey[0].AliasName), ParameterDirection.Input, id);
            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;

            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullMapObj(reader, type)(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据条件查询对象，不支持分表数据的查询
        /// 只支持映射类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public T GetObject<T>(string where, params object[] paras)
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);

            string sql = string.Format("select * from {0} {1}",
                table.Name,
                string.IsNullOrEmpty(where) ? "" : "where " + FormatWhereOrder(table, where));

            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullMapObj<T>(reader)(reader);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 根据条件查询对象，不支持分表数据的查询
        /// </summary>
        /// <param name="where"></param>
        /// <param name="type">返回对象类型</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public object GetObject(string where, Type type, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTable(type);

            string sql = string.Format("select * from {0} {1}",
                table.Name,
                string.IsNullOrEmpty(where) ? "" : "where " + FormatWhereOrder(table, where));

            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullMapObj(reader, type)(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// 根据SQL语句查询对象，不支持分表数据的查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="where"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public T GetObjectBySQL<T>(string sql, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTableExist(typeof(T));
            if (table != null)
            {
                Command.CommandText = FormatWhereOrder(table, sql);
                Command.CommandType = CommandType.Text;
                Command.Parameters.Clear();
                int i = 0;
                foreach (object obj in paras)
                {
                    AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
                }
                using (IDataReader reader = Command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return FullDataReader.CreateDegFullMapObj<T>(reader)(reader);
                    }
                }
                return default(T);
            }
            else
            {
                return GetCustomerObject<T>(sql, paras);
            }
        }

        /// <summary>
        /// 根据SQL语句查询对象，不支持分表数据的查询
        /// </summary>
        /// <param name="where"></param>
        /// <param name="type">返回对象类型</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public object GetObjectBySQL(string sql, Type type, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTableExist(type);
            if (table != null)
            {
                Command.CommandText = FormatWhereOrder(table, sql);
                Command.CommandType = CommandType.Text;
                Command.Parameters.Clear();
                int i = 0;
                foreach (object obj in paras)
                {
                    AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
                }
                using (IDataReader reader = Command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        return FullDataReader.CreateDegFullMapObj(reader, type)(reader);
                    }
                }
                return null;
            }
            else
            {
                return GetCustomerObject(sql, type, paras);
            }
        }

        /// <summary>
        /// 根据SQL语句查询列表，不支持分表数据的查询
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public List<T> GetListBySQL<T>(string sql, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTableExist(typeof(T));
            if (table != null)
            {
                Command.CommandText = FormatWhereOrder(table, sql);
                Command.CommandType = CommandType.Text;
                Command.Parameters.Clear();
                int i = 0;
                foreach (object obj in paras)
                {
                    AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
                }
                using (IDataReader reader = Command.ExecuteReader())
                {
                    return FullDataReader.CreateDegFullMapList<T>(reader)(reader);
                }
            }
            else
            {
                return GetCustomerList<T>(sql, paras);
            }
        }

        /// <summary>
        /// 根据SQL语句查询列表，不支持分表数据的查询
        /// </summary>        
        /// <param name="sql"></param>
        /// <param name="type">返回对象类型</param>
        /// <returns></returns>
        public List<object> GetListBySQL(string sql, Type type, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTableExist(type);
            if (table != null)
            {
                Command.CommandText = FormatWhereOrder(table, sql);
                Command.CommandType = CommandType.Text;
                Command.Parameters.Clear();
                int i = 0;
                foreach (object obj in paras)
                {
                    AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
                }
                using (IDataReader reader = Command.ExecuteReader())
                {
                    return FullDataReader.CreateDegFullMapList(reader, type)(reader);
                }
            }
            else
            {
                return GetCustomerList(sql, type, paras);
            }
        }

        /// <summary>
        /// 根据条件获取列表，不支持分表数据的查询
        /// 只支持映射类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql"></param>
        /// <returns></returns>
        public List<T> GetList<T>(string where, string order, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTable(typeof(T));

            string sql = string.Format("select * from {0} {1} {2}",
                table.Name,
                string.IsNullOrEmpty(where) ? "" : "where " + FormatWhereOrder(table, where),
                string.IsNullOrEmpty(order) ? "" : string.Format(" order by {0}", order));

            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullMapList<T>(reader)(reader);
            }
        }

        /// <summary>
        /// 根据条件获取列表，不支持分表数据的查询
        /// </summary>        
        /// <param name="sql"></param>
        /// <param name="type">返回对象类型</param>
        /// <returns></returns>
        public List<object> GetList(string where, string order, Type type, params object[] paras)
        {
            DBTable table = MapHelper.GetDBTable(type);

            string sql = string.Format("select * from {0} {1} {2}",
                table.Name,
                string.IsNullOrEmpty(where) ? "" : "where " + FormatWhereOrder(table, where),
                string.IsNullOrEmpty(order) ? "" : string.Format(" order by {0}", order));

            Command.CommandText = sql;
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();

            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullMapList(reader, type)(reader);
            }
        }

        #endregion

        #region 自定义类型Select方法

        /// <summary>
        /// 获取自定义的对象列表列表
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public List<T> GetCustomerList<T>(string sql, params object[] paras)
        {
            Command.CommandText = FormatSqlForParameter(sql);
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }

            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullCustomList<T>(reader)(reader);
            }
        }

        /// <summary>
        /// 获取自定义的对象列表列表
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">返回对象类型</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public List<object> GetCustomerList(string sql, Type type, params object[] paras)
        {
            Command.CommandText = FormatSqlForParameter(sql);
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }

            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullCustomList(reader, type)(reader);
            }
        }

        /// <summary>
        /// 根据SQL语句查询对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public T GetCustomerObject<T>(string sql, params object[] paras)
        {
            Command.CommandText = FormatSqlForParameter(sql);
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullCustomObj<T>(reader)(reader);
                }
            }
            return default(T);
        }

        /// <summary>
        /// 根据SQL语句查询对象
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">返回对象类型</param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public object GetCustomerObject(string sql, Type type, params object[] paras)
        {
            Command.CommandText = FormatSqlForParameter(sql);
            Command.CommandType = CommandType.Text;
            Command.Parameters.Clear();
            int i = 0;
            foreach (object obj in paras)
            {
                AddParameter(FormatParameterName("p" + (i++).ToString()), ParameterDirection.Input, obj);
            }
            using (IDataReader reader = Command.ExecuteReader())
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegFullCustomObj(reader, type)(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// 获取自定义对象列表,自己给定SQL语句
        /// 通过字段名称与属性名称匹配来进行填充(不区分大小写)
        /// </summary>
        /// <typeparam name="T">返回的对象类型</typeparam>
        /// <param name="pageIndex">分页索引，以1开始</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="before">加在SQL语句最前面</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>List</returns>
        public List<T> GetCustomPagingList<T>(
            int pageIndex,
            int pageSize,
            string before,
            string fields,
            string from,
            string where,
            string group,
            string order,
            params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            string sql = PrepareCustomSelectPaging(pageIndex, pageSize, fields, from, where, group, order, paras);
            Command.CommandText = string.Format("{0} {1}", before, sql);
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullCustomList<T>(reader)(reader);
            }
        }

        /// <summary>
        /// 获取自定义对象列表,自己给定SQL语句
        /// 通过字段名称与属性名称匹配来进行填充(不区分大小写)
        /// </summary>
        /// <typeparam name="T">返回的对象类型</typeparam>
        /// <param name="pageIndex">分页索引，以1开始</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="before">加在SQL语句最前面</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>List</returns>
        public List<object> GetCustomPagingList(Type type,
            int pageIndex,
            int pageSize,
            string before,
            string fields,
            string from,
            string where,
            string group,
            string order,
            params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            string sql = PrepareCustomSelectPaging(pageIndex, pageSize, fields, from, where, group, order, paras);
            Command.CommandText = string.Format("{0} {1}", before, sql);
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullCustomList(reader, type)(reader);
            }
        }

        #endregion

        #region 动态类Select方法

        /// <summary>
        /// 获取自定义对象列表,自己给定SQL语句
        /// 通过字段名称与属性名称匹配来进行填充(不区分大小写)
        /// </summary>
        /// <param name="sql">sql语句,其中参数以?代替</param>
        /// <param name="paras">传入的参数</param>
        /// <returns>List</returns>
        public object GetDynamicList(string sql, params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelect(sql, paras);
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullDynamicList(reader)(reader);
            }
        }

        /// <summary>
        /// 获取符合条件的第一条记录
        /// 给定SQL语句，用于获取自定义对象
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="paras">参数列表</param>
        /// <returns>T</returns>
        public object GetDynamicObject(string sql, params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelect(sql, paras);
            using (IDataReader reader = Command.ExecuteReader(CommandBehavior.SingleRow))
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegDynamicFullObj(reader)(reader);
                }
            }
            return null;
        }

        /// <summary>
        /// 获取自定义对象列表,自己给定SQL语句
        /// 通过字段名称与属性名称匹配来进行填充(不区分大小写)
        /// </summary>
        /// <param name="pageIndex">分页索引，以1开始</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>List</returns>
        public object GetDynamicPagingList(
            int pageIndex,
            int pageSize,
            string fields,
            string from,
            string where,
            string group,
            string order,
            params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelectPaging(pageIndex, pageSize, fields, from, where, group, order, paras);
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegFullDynamicList(reader)(reader);
            }
        }

        /// <summary>
        /// 动态可扩展集合对象 List&lt;System.Dynamic.ExpandoObject&gt;
        /// 返回对象与列名一致,返回对象可动态扩展
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public object GetExpandoDynamicList(string sql, params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelect(sql, paras);
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegExpandoDynamicFullList(reader)(reader);
            }
        }
        public object GetExpandoDynamicObject(string sql, params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelect(sql, paras);
            using (IDataReader reader = Command.ExecuteReader(CommandBehavior.SingleRow))
            {
                while (reader.Read())
                {
                    return FullDataReader.CreateDegExpandoDynamicFullObj(reader)(reader);
                }
            }
            return null;
        }
        public object GetExpandoDynamicPagingList(
            int pageIndex,
            int pageSize,
            string before,
            string fields,
            string from,
            string where,
            string group,
            string order,
            params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = string.Format("{0} {1}", before, PrepareCustomSelectPaging(pageIndex, pageSize, fields, from, where, group, order, paras));
            using (IDataReader reader = Command.ExecuteReader())
            {
                return FullDataReader.CreateDegExpandoDynamicFullList(reader)(reader);
            }
        }

        #endregion

        /// <summary>
        /// 获取自定义对象列表,自己给定SQL语句
        /// 通过字段名称与属性名称匹配来进行填充(不区分大小写)
        /// </summary>
        /// <param name="pageIndex">分页索引，以1开始</param>
        /// <param name="pageSize">分页大小</param>
        /// <param name="fields">字段列表，以“,”分隔</param>
        /// <param name="from">表名称,比如t_a left join t_b on t_a.id=t_b.id</param>
        /// <param name="where">Where 条件，参数用？代替</param>
        /// <param name="group">Group by 子句</param>
        /// <param name="order">排序方式,不包含"order by"</param>
        /// <param name="paras">条件参数</param>
        /// <returns>DataTable</returns>
        public DataTable GetDataTablePaging(
            int pageIndex,
            int pageSize,
            string fields,
            string from,
            string where,
            string group,
            string order,
            params object[] paras)
        {
            Command.CommandType = CommandType.Text;
            Command.CommandText = PrepareCustomSelectPaging(pageIndex, pageSize, fields, from, where, group, order, paras);
            DataTable dt = new DataTable();
            dt.Load(Command.ExecuteReader());
            return dt;
        }

        #region 子类重写分页SQL语句

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
        protected abstract string PrepareCustomSelectPaging(int pageIndex, int pageSize, string fields, string from, string where, string group, string order, object[] paras);

        #endregion
    }
}
