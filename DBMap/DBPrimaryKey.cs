using System.Reflection;

namespace DBFrame.DBMap
{
    public class DBPrimaryKey : DBColumn
    {
        private string _equenceName;

        /// <summary>
        /// 主键生成方式
        /// </summary>
        public DBPrimaryType DBPrimaryType { get; set; }

        /// <summary>
        /// 如果是自动增长，对应的序列名称
        /// </summary>
        public string SequenceName { get { return _equenceName; } private set { _equenceName = value.ToUpper(); } }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <param name="fieldName"></param>
        /// <param name="identity"></param>
        public DBPrimaryKey(MemberInfo memberInfo, string fieldName, string dbType, bool notNull, string defaultVal, DBPrimaryType dBPrimaryType, string seqName)
            : base(memberInfo, fieldName, dbType, notNull, defaultVal)
        {
            SequenceName = seqName;
            DBPrimaryType = dBPrimaryType;
        }
    }
}
