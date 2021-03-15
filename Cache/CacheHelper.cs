using System;
using DBFrame.DBMap;
using System.Collections.Generic;

namespace DBFrame.Cache
{
    /// <summary>
    /// Cache帮助类
    /// </summary>
    public class CacheHelper
    {
        internal static BaseCache _instanse;
        /// <summary>
        /// 缓存操作-单例对象。如果需要使用自定义的缓存对象， 这在外部设置该单例对象
        /// </summary>
        internal static BaseCache Instanse
        {
            get
            {
                //默认使用DotNetCache的缓存
                if (_instanse == null)
                {
                    _instanse = new DotNetCache();
                }
                return CacheHelper._instanse;
            }
            set { CacheHelper._instanse = value; }
        }


        #region 对外缓存操作 公共方法

        /// <summary>
        /// 根据数据Id获取缓存对象，如果在缓存中没有获取到，则自动从数据库中加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public static T GetCacheObject<T>(object id)
        {
            T t = Instanse.GetCache<T>(id);
            DBTable table = MapHelper.GetDBTable(typeof(T));
            if (table == null) throw new Exception("非映射类，不能获取缓存！");
            if (table.CacheType != CacheType.Object) throw new Exception("该映射类，没有设置缓存！");

            //如果为nul，则从数据库获取
            if (t == null)
            {
                using (DBSession session = DBSession.TryGet())
                {
                    t = session.GetById<T>(id);
                }
            }
            return t;
        }

        /// <summary>
        /// 获取缓存List，如果在缓存中没有获取到，则自动从数据库中加载
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static List<T> GetCacheList<T>()
        {
            return Instanse.TryGetList<T>();
        }

        /// <summary>
        /// 从缓存Lis集合中， 根据Lamb表达式获取缓存对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="match"></param>
        /// <returns></returns>
        public static T GetListObj<T>(Predicate<T> match)
        {
            return Instanse.TryGetListObj<T>(match);
        }

        #endregion
    }
}
