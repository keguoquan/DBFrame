using System;
using System.Web;
using System.Collections;

namespace DBFrame.Cache
{
    /// <summary>
    /// 基于.Net System.Web.Caching.Cache类实现的缓存
    /// </summary>
    internal class DotNetCache : BaseCache
    {
        protected override object GetCache(Type type, object dataId)
        {
            return HttpRuntime.Cache.Get(GetCacheKey(type, dataId));
        }

        internal override void UpdateCache(Type type, object dataId, object objObject)
        {
            HttpRuntime.Cache[GetCacheKey(type, dataId)] = objObject;
        }

        internal override void SetCache(Type type, object dataId, object objObject, int seconds)
        {
            HttpRuntime.Cache.Insert(GetCacheKey(type, dataId), objObject, null, DateTime.Now.AddSeconds(seconds), TimeSpan.Zero);
        }

        internal override void RemoveOneCache(Type type, object dataId)
        {
            HttpRuntime.Cache.Remove(GetCacheKey(type, dataId));
        }

        protected override void RemoveAllCache()
        {
            System.Web.Caching.Cache _cache = HttpRuntime.Cache;
            IDictionaryEnumerator CacheEnum = _cache.GetEnumerator();
            if (_cache.Count > 0)
            {
                ArrayList al = new ArrayList();
                while (CacheEnum.MoveNext())
                {
                    al.Add(CacheEnum.Key);
                }
                foreach (string key in al)
                {
                    _cache.Remove(key);
                }
            }
        }
    }
}
