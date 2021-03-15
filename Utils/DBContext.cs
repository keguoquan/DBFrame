using System.Data.Common;

namespace DBFrame
{
    /// <summary>
    /// 连接的数据库信息
    /// </summary>
    public class DBContext
    {
        /// <summary>
        /// 连接数据库自定义名称，唯一
        /// </summary>
        public string DbKey { get; set; }

        /// <summary>
        /// 数据库类型
        /// </summary>
        public DataBaseType DataType { get; set; }

        /// <summary>
        /// 连接对象池大小
        /// 0则不使用对象连接池
        /// </summary>
        public int ConnPoolNum { get; set; }

        /// <summary>
        /// 连接字符串
        /// </summary>
        public string Connectstring { get; set; }

        /// <summary>
        /// 系统初始化时，是否根据Model检查表结构
        /// </summary>
        public bool InitCheckDB { get; set; }

        /// <summary>
        /// 驱动工厂
        /// </summary>
        public DbProviderFactory Factory { get; set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public DBContext() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbkey">连接数据库自定义唯一名称</param>
        /// <param name="dataType">数据库类型</param>
        /// <param name="conString">连接字符串</param>
        public DBContext(string dbkey, DataBaseType dataType, string conString)
            : this(dbkey, dataType, conString, false, 0)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbkey">连接数据库自定义唯一名称</param>
        /// <param name="dataType">数据库类型</param>
        /// <param name="conString">连接字符串</param>
        /// <param name="conString">连接池大小，0则不使用对象连接池</param>
        public DBContext(string dbkey, DataBaseType dataType, string conString, int conPoolNum)
            : this(dbkey, dataType, conString, false, 0)
        {

        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dbkey">连接数据库自定义唯一名称</param>
        /// <param name="dataType">数据库类型</param>
        /// <param name="conString">连接字符串</param>
        /// <param name="initCheckDB">系统初始化时，是否检查表结构</param>
        /// <param name="conPoolNum">连接池大小，0则不使用对象连接池</param>
        public DBContext(string dbkey, DataBaseType dataType, string conString, bool initCheckDB, int conPoolNum = 0)
        {
            this.DbKey = dbkey;
            this.DataType = dataType;
            this.Connectstring = conString;
            this.ConnPoolNum = conPoolNum;
            this.InitCheckDB = initCheckDB;
        }
    }
}
