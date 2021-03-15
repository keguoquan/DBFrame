using System;
using System.Collections.Generic;
using DBFrame.DBMap;

namespace DBFrame.Cache
{
    /// <summary>
    /// 缓存基类，如果需要自定义缓存实现， 新建一个类，继承至该类，并实现抽象方法即可
    /// 缓存数据Key命名规则，通过父类的GetCacheKey方法进行获取
    /// </summary>
    public abstract class BaseCache
    {
        #region 子类需实现的 抽象方法

        /// <summary>
        /// 获取指定Key的缓存数据
        /// </summary>
        /// <param name="type">缓存数据类型</param>
        /// <param name="dataId">该类型数据唯一标识</param>
        /// <returns>缓存数据</returns>
        protected abstract object GetCache(Type type, object dataId);

        /// <summary>
        /// 更新缓存数据
        /// </summary>
        /// <param name="type">缓存数据类型</param>
        /// <param name="dataId">该类型数据唯一标识</param>
        /// <param name="objObject">缓存数据</param>
        internal abstract void UpdateCache(Type type, object dataId, object objObject);

        /// <summary>
        /// 设置缓存-按秒缓存
        /// </summary>
        /// <param name="type">缓存数据类型</param>
        /// <param name="dataId">该类型数据唯一标识</param>
        /// <param name="objObject">缓存数据</param>
        /// <param name="seconds">缓存时间（秒）</param>
        internal abstract void SetCache(Type type, object dataId, object objObject, int seconds);

        /// <summary>
        /// 根据Key清除缓存的数据
        /// </summary>
        /// <param name="type">缓存数据类型</param>
        /// <param name="dataId">该类型数据唯一标识</param>
        internal abstract void RemoveOneCache(Type type, object dataId);

        /// <summary>
        /// 根据缓存数据类型-清除所有缓存
        /// </summary>
        protected abstract void RemoveAllCache();

        #endregion

        /// <summary>
        /// 获取指定Key的缓存数据
        /// </summary>
        /// <param name="dataId">该类型数据唯一标识</param>
        /// <returns>缓存数据</returns>
        internal T GetCache<T>(object dataId)
        {
            object obj = GetCache(typeof(T), dataId);
            if (obj != null) return (T)obj;
            return default(T);
        }
        
        /// <summary>
        /// 根据缓存数据类型以及唯一数据标识，获取缓存Key值
        /// </summary>
        /// <param name="type">缓存数据类型</param>
        /// <param name="dataId">该类型数据唯一标识</param>
        /// <returns>缓存Key</returns>
        internal string GetCacheKey(Type type, object dataId)
        {
            return string.Format("{0}_{1}", type.FullName, dataId);
        }

        #region 缓存List

        /// <summary>
        /// 获取缓存的List集合
        /// </summary>
        /// <typeparam name="T">获取的数据类型</typeparam>
        /// <returns>该类型的所有缓存数据</returns>
        internal List<T> TryGetList<T>()
        {
            Type type = typeof(T);
            DBTable table = MapHelper.GetDBTable(type);
            if (table.CacheType != CacheType.List)
            {
                throw new Exception("该类型不是list缓存模式");
            }

            List<T> list = GetCache<List<T>>(type.FullName);
            if (list == null)//加载该集合
            {
                using (DBSession session = DBSession.TryGet())
                {
                    list = session.GetList<T>("", "");
                }
                SetCache(typeof(List<>).MakeGenericType(type), type.FullName, list, table.CacheSeconds);
            }
            return list;
        }

        /// <summary>
        /// 从缓存Lis集合中， 根据Lamb表达式获取缓存对象
        /// </summary>
        /// <typeparam name="T">要获取的数据类型</typeparam>
        /// <param name="match">匹配条件</param>
        /// <returns>获取到的数据对象</returns>
        internal T TryGetListObj<T>(Predicate<T> match)
        {
            List<T> list = TryGetList<T>();
            if (list != null)
            {
                return list.Find(match);
            }
            return default(T);
        }

        /// <summary>
        /// 从缓存Lis集合中， 根据主键值获取缓存对象
        /// </summary>
        /// <typeparam name="T">要获取的数据类型</typeparam>
        /// <param name="primVal">主键值</param>
        /// <returns>获取到的数据对象</returns>
        internal T TryGetListObj<T>(object primVal)
        {
            List<T> list = TryGetList<T>();
            if (list != null)
            {
                DBTable table = MapHelper.GetDBTable(typeof(T));
                foreach (T item in list)
                {
                    object curPrimVal = table.PrimaryKey[0].GetHandler(item);
                    if (primVal.Equals(curPrimVal))
                    {
                        return item;
                    }
                }
            }
            return default(T);
        }

        /// <summary>
        /// 新增一个Lis缓存模式的对象
        /// </summary>
        /// <param name="type">缓存的数据类型</param>
        /// <param name="cacheObj">缓存的数据对象</param>
        internal void AddCahceListObj<T>(Type type, T cacheObj)
        {
            List<T> list = GetCache<List<T>>(type.FullName);
            if (list != null)
            {
                list.Add(cacheObj);

                //更新缓存
                UpdateCache(typeof(List<T>), type.FullName, list);
            }
        }

        /// <summary>
        /// 更新一个List缓存模式的对象
        /// </summary>
        /// <param name="type">缓存的数据类型</param>
        /// <param name="cacheObj">缓存的数据对象</param>
        /// <param name="updatePrimVal">主键值</param>
        internal void UpdateCahceListObj<T>(Type type, T cacheObj, object updatePrimVal)
        {
            DBTable table = MapHelper.GetDBTable(type);
            if (table.CacheType != CacheType.List)
            {
                throw new Exception("该类型不是list缓存模式");
            }

            List<T> list = GetCache<List<T>>(type.FullName);
            if (list != null)
            {
                list.ForEach((item) =>
                {
                    object curPrimVal = table.PrimaryKey[0].GetHandler(item);
                    if (curPrimVal.Equals(updatePrimVal))
                    {
                        int index = list.IndexOf(item);
                        list.Remove(item);
                        list.Insert(index, cacheObj);
                        //更新缓存
                        UpdateCache(typeof(List<T>), type.FullName, list);
                        return;
                    }
                });
            }
        }

        /// <summary>
        /// 删除一个List缓存模式的对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="delPrimVal"></param>
        internal void DeleteCahceListObj<T>(Type type, object delPrimVal)
        {
            DBTable table = MapHelper.GetDBTable(type);
            if (table.CacheType != CacheType.List)
            {
                throw new Exception("该类型不是list缓存模式");
            }

            List<T> list = GetCache<List<T>>(type.FullName);
            if (list != null)
            {
                list.ForEach((item) =>
                {
                    object curPrimVal = table.PrimaryKey[0].GetHandler(item);
                    if (curPrimVal.Equals(delPrimVal))
                    {
                        list.Remove(item);

                        //更新缓存
                        UpdateCache(typeof(List<T>), type.FullName, list);
                        return;
                    }
                });
            }
        }

        #endregion
    }
}
