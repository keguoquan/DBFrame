using System;
using System.Linq;
using System.Collections.Concurrent;
using DBFrame.DBMap;

namespace DBFrame
{
    /// <summary>
    /// 表拆分-获取拆分表的当前表
    /// 拆分表注意事项：
    /// 1：主键必须为MyDBId
    /// 2：Id中的时间必须对应其写入表的时间。比如按年拆分的表，Id中的年份与写入表的年份必须一致。这样才能通过GetById等方法查询到是某张表
    /// 3：拆分表中的CreateTime时间最好与Id中的时间一致
    /// </summary>
    public class TableSeparate
    {
        /// <summary>
        /// 已验证表集合
        /// </summary>
        private static ConcurrentDictionary<string, ConcurrentBag<string>> _cdCheckDate = new ConcurrentDictionary<string, ConcurrentBag<string>>();

        /// <summary>
        /// 获取按时间拆分的表名
        /// </summary>
        /// <param name="type">映射类型，比如：typeof(SMSendRecord)</param>
        /// <param name="date">时间</param>
        /// <returns>返回拆分的表名</returns>
        public static string GetTableName(Type type, DateTime date)
        {
            DBTable table = MapHelper.GetDBTable(type);
            if (table.SeparateType == SeparateType.None) return table.Name;
            return GetTableName(table, date);
        }

        /// <summary>
        /// 根据MyId的主键获取拆分表名
        /// </summary>
        /// <param name="type">映射类型，比如：typeof(SMSendRecord)</param>
        /// <param name="myId">MyId主键值</param>
        /// <returns></returns>
        public static string GetTableName(Type type, object myId)
        {
            DBTable table = MapHelper.GetDBTable(type);
            if (table.SeparateType == SeparateType.None) return table.Name;

            return GetTableName(table, MyIdMake.GetMyIdDate(myId));
        }

        /// <summary>
        /// 根据MyId的主键获取拆分表名
        /// </summary>
        /// <param name="type">对应的数据库映射信息</param>
        /// <param name="myId">MyId主键值</param>
        /// <returns></returns>
        internal static string GetTableName(DBMap.DBTable table, object myId)
        {
            if (table.SeparateType == SeparateType.None) return table.Name;

            return GetTableName(table, MyIdMake.GetMyIdDate(myId));
        }

        /// <summary>
        /// 根据时间获取对应的拆分表名
        /// </summary>
        /// <param name="table">对应的数据库映射信息</param>
        /// <param name="date">主键时间</param>
        /// <returns>返回拆分的表名</returns>
        internal static string GetTableName(DBMap.DBTable table, DateTime date)
        {
            if (table.SeparateType == SeparateType.None) return table.Name;

            //获取表名
            string tbName = GetFormatTableName(table, table.SeparateType, date);
            if (string.IsNullOrWhiteSpace(tbName)) return table.Name;
            if (tbName.Equals(table.Name)) return table.Name;

            //判断表是否已存在验证
            ConcurrentBag<string> cb = null;
            if (!_cdCheckDate.TryGetValue(table.MapType.FullName, out cb))
            {
                _cdCheckDate.TryAdd(table.MapType.FullName, cb = new ConcurrentBag<string>());
            }

            //如果不存在，则验证数据库中是否存在该表
            if (!cb.Contains(tbName))
            {
                //验证表在数据库中是否存在
                if (CheckTableAndCreate(table, tbName))
                {
                    cb.Add(tbName);
                }
                else
                {
                    //如果数据库中不存在该分表，则返回默认的表名
                    return table.Name;
                }
            }
            return tbName;
        }

        /// <summary>
        /// 获取表拆分的表名
        /// </summary>
        /// <param name="table"></param>
        /// <param name="sType"></param>
        /// <param name="date"></param>
        /// <param name="sign"></param>
        /// <returns></returns>
        private static string GetFormatTableName(DBMap.DBTable table, SeparateType sType, DateTime date)
        {
            switch (sType)
            {
                case SeparateType.Year:
                    return string.Format("{0}_{1}", table.Name, date.Year);
                case SeparateType.JiDu:
                    return string.Format("{0}_{1}", table.Name, GetJiDu(date));
                case SeparateType.Mouth:
                    return string.Format("{0}_{1}", table.Name, date.ToString("yyyyMM"));
                case SeparateType.Day:
                    return string.Format("{0}_{1}", table.Name, date.ToString("yyyyMMdd"));
            }
            return table.Name;
        }

        /// <summary>
        /// 验证数据库表是否存在，如果不存在则创建表
        /// </summary>
        /// <param name="table"></param>
        /// <param name="tbName"></param>
        /// <returns></returns>
        private static bool CheckTableAndCreate(DBTable table, string tbName)
        {
            bool isExist = false;
            using (DBSession session = DBSession.TryGet())
            {
                try
                {
                    session.ExecuteScalar<int>(string.Format("select count(1) from {0}", tbName));
                    isExist = true;
                }
                catch (Exception)
                {
                    isExist = false;
                }

                //创建表
                if (!isExist)
                {
                    try
                    {
                        string createSql = table.CreateSql.ToUpper().
                            Replace(table.Name.ToUpper(), tbName.ToUpper());

                        string[] sqls = createSql.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        session.BeginTransaction();
                        foreach (string item in sqls)
                        {
                            if (!string.IsNullOrWhiteSpace(item.Trim()))
                                session.ExecuteNonQuery(item);
                        }
                        session.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        session.Rollback();
                        throw ex;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 扩展方法，获取日期所在季度
        /// </summary>
        /// <param name="date"></param>
        /// <returns>年+季度：20151,20152,20153,20154</returns>
        private static string GetJiDu(DateTime date)
        {
            if (date.Month >= 1 && date.Month < 4) return string.Format("{0}1", date.Year);
            else if (date.Month >= 4 && date.Month < 7) return string.Format("{0}2", date.Year);
            else if (date.Month >= 7 && date.Month < 10) return string.Format("{0}3", date.Year);
            else return string.Format("{0}4", date.Year);
        }
    }
}
