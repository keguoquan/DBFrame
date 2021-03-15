using System;
using System.Linq;
using System.Collections.Concurrent;
using DBFrame.DBMap;
using System.Collections.Generic;
using System.Text;

namespace DBFrame
{
    /// <summary>
    /// 初始化时-检查表机构
    /// 注意：
    /// 1：目前仅支持Oracle，SQL Server数据库
    /// 2：根据Model层进行检测，只有在模型层上配置了DBColumn 属性FieldType的字段才会检查
    /// 3：检测内容：表是否存在，字段是否存在等内容。不会检测索引，字段数据类型，主键，外键,是否允许为null,字段是否是自增长的变化
    /// </summary>
    public class TableInitCheck
    {
        /// <summary>
        /// 开始检测
        /// </summary>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        public static void BeginCheck(DBContext dbContext)
        {
            if (!dbContext.InitCheckDB) return;

            if (dbContext.DataType == DataBaseType.Oracle)
            {
                CheckTable(ReaderOracle(dbContext), dbContext);
            }
            else if (dbContext.DataType == DataBaseType.SQLServer)
            {
                CheckTable(ReaderSQLServer(dbContext), dbContext);
            }
            else
                throw new Exception("DBFrame框架目前仅支持Oracle，SQL Server数据库表结构检查");
        }

        /// <summary>
        /// 检查表
        /// </summary>
        /// <param name="list"></param>
        /// <param name="dbContext"></param>
        private static void CheckTable(List<TBField> list, DBContext dbContext)
        {
            //对list按表名分组
            List<IGrouping<string, TBField>> groups = list.GroupBy((x) => { return x.TableName; }).ToList();

            using (DBSession session = DBSession.TryGet(dbContext.DbKey))
            {
                foreach (DBTable dbTable in MapHelper.TableDictionary.Values)
                {
                    //判断表是否存在
                    TBField table = list.FirstOrDefault(x => x.TableName.ToUpper() == dbTable.Name.ToUpper());
                    if (table == null)
                    {
                        //表不存在，则创建表
                        CreateTable(session, dbContext, dbTable);
                    }

                    //验证字段
                    if (table != null || dbTable.SeparateType != SeparateType.None)
                    {
                        foreach (DBColumn column in dbTable.ColumnList)
                        {
                            TBField field = list.FirstOrDefault(x => (x.TableName.ToUpper() == dbTable.Name.ToUpper() && x.Name.ToUpper() == column.Name.ToUpper()));
                            if (field == null && !string.IsNullOrWhiteSpace(column.DataType))
                            {
                                //为表添加字段
                                if (dbTable.SeparateType != SeparateType.None)
                                {
                                    List<IGrouping<string, TBField>> sepTbs = groups.FindAll((x) => x.Key.ToUpper().StartsWith(dbTable.Name.ToUpper()));
                                    foreach (var item in sepTbs)
                                    {
                                        CreateColumn(session, item.Key, column);
                                    }
                                }
                                else
                                {
                                    CreateColumn(session, dbTable.Name, column);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 创建表
        /// </summary>
        /// <param name="dbTable"></param>
        private static void CreateTable(DBSession session, DBContext dbContext, DBTable dbTable)
        {
            StringBuilder sql_colums = new StringBuilder();
            StringBuilder sql_pri = new StringBuilder();
            //添加主键字段
            foreach (var item in dbTable.PrimaryKey)
            {
                sql_colums.AppendFormat(",{0} {1} NOT NULL", item.Name, item.DataType);
                sql_pri.AppendFormat(",{0}", item.Name);
            }
            //添加字段
            foreach (var item in dbTable.ColumnList)
            {
                sql_colums.AppendFormat(",{0} {1}", item.Name, item.DataType);
                if (!string.IsNullOrWhiteSpace(item.Default))
                {
                    sql_colums.AppendFormat(" default {0}", item.Default);
                }
                if (item.NotNull)
                {
                    sql_colums.Append(" NOT NULL");
                }
            }
            if (dbContext.DataType == DataBaseType.SQLServer)//添加sql server主键
            {
                sql_colums.AppendFormat(",constraint PK_{0} primary key ({1})", dbTable.Name, sql_pri.ToString().TrimStart(','));
            }
            //创建表
            session.ExecuteNonQuery(string.Format("create table {0} ({1})", dbTable.Name, sql_colums.ToString().TrimStart(',')));

            if (dbContext.DataType == DataBaseType.Oracle)//添加Oracle主键
            {
                session.ExecuteNonQuery(string.Format("alter table {0} add constraint PK_{0} primary key ({1})",
                    dbTable.Name, sql_pri.ToString().TrimStart(',')));
            }
        }

        /// <summary>
        /// 创建列
        /// </summary>
        /// <param name="table"></param>
        /// <param name="column"></param>
        private static void CreateColumn(DBSession session, string tbName, DBColumn column)
        {
            string sql = string.Format("alter table {0} add {1} {2} {3} {4}",
                tbName, column.Name, column.DataType,
                string.IsNullOrWhiteSpace(column.Default) ? "" : "default " + column.Default,
                column.NotNull ? "NOT NULL" : "");
            session.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// 读取Oracle
        /// </summary>
        /// <returns></returns>
        private static List<TBField> ReaderOracle(DBContext dbContext)
        {
            using (DBSession db = DBSession.TryGet(dbContext.DbKey))
            {
                //获取所有table
                return db.GetCustomerList<TBField>("select TABLE_NAME TableName,COLUMN_NAME Name,data_type DataType from user_tab_cols");
            }
        }

        /// <summary>
        /// 读取SQL Server
        /// </summary>
        /// <returns></returns>
        private static List<TBField> ReaderSQLServer(DBContext dbContext)
        {
            #region sql

            /// <summary>
            /// 获取字段信息以及注释等sql语句
            /// </summary>
            string sql = @"select sysobjects.name as TableName,syscolumns.name as Name,systypes.name as DataType,
                            ispk= case when 
                            exists(select 1 from sysobjects where xtype='PK' and parent_obj=syscolumns.id and name in
                             (select name from sysindexes where indid 
                            in(select indid from sysindexkeys where id = syscolumns.id AND colid=syscolumns.colid)))
                             then 1 else 0 end,columndescription= isnull(sys.extended_properties.[value],''),
                            tabledescription=case when (select count(*) from sys.extended_properties 
                            where major_id=sysobjects.id and minor_id=0)=1 then 
                            (select [value] from sys.extended_properties where major_id=sysobjects.id and minor_id=0) else '' end 
                            from sysobjects inner join syscolumns on sysobjects.id = syscolumns.id 
                            left join systypes  on syscolumns.xtype = systypes.xusertype  
                            left join sys.extended_properties on syscolumns.id = sys.extended_properties.major_id 
                            and syscolumns.colid = sys.extended_properties.minor_id 
                            where sysobjects.xtype='U' and sysobjects.name <>'sysdiagrams'";

            #endregion

            using (DBSession db = DBSession.TryGet(dbContext.DbKey))
            {
                return db.GetCustomerList<TBField>(sql);
            }
        }
    }

    /// <summary>
    /// 表字段信息
    /// </summary>
    public class TBField
    {
        /// <summary>
        /// 表名称
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// 字段名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 字段数据类型
        /// </summary>
        public string DataType { get; set; }
    }
}
