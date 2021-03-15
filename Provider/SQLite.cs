using System;
using System.Data.Common;

namespace DBFrame.Provider
{
    public class SQLite : DBSession
    {
        public const string ProviderTypeString = "System.Data.SQLite";

        protected override DbProviderFactory CreateDbProviderFactory()
        {
            Type type = GetType(ProviderTypeString, "System.Data.SQLite.SQLiteFactory");
            return (DbProviderFactory)Activator.CreateInstance(type, true);
        }

        protected override string PrepareCustomSelectPaging(int pageIndex, int pageSize, string fields, string from, string where, string group, string order, object[] paras)
        {
            throw new NotImplementedException();
        }
    }
}
