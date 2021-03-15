using System.Data;
using System.Reflection.Emit;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DBFrame.FullData
{
    /// <summary>
    /// 填充数据
    /// </summary>
    public class FullDataReader
    {
        #region 填充自定义类

        /// <summary>
        /// 自定义类型填充，已经存在的填充动态方法
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> _idiFullCustomObj = new ConcurrentDictionary<string, Delegate>();
        /// <summary>
        /// 构造自定义类 填充Object委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullCustomObj<T> CreateDegFullCustomObj<T>(IDataReader reader)
        {
            string key = string.Format("{0}{1}", typeof(T).FullName, FullDataHelper.GetDataReaderInfo(reader));

            Delegate degMethod;
            if (_idiFullCustomObj.TryGetValue(key, out degMethod))
                return (DegFullCustomObj<T>)degMethod;

            DegFullCustomObj<T> deg = null;
            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullObjMethod(reader, typeof(T), false);

            //为动态方法构造调用委托
            deg = (DegFullCustomObj<T>)method.CreateDelegate(typeof(DegFullCustomObj<T>));
            if (!_idiFullCustomObj.ContainsKey(key))
            {
                _idiFullCustomObj.TryAdd(key, deg);
            }
            return deg;
        }

        /// <summary>
        /// 自定义类型填充，已经存在的填充动态方法
        /// </summary>
        private static Dictionary<string, Delegate> _idiFullCustomObj_noT = new Dictionary<string, Delegate>();
        /// <summary>
        /// 构造自定义类 填充Object委托
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type">要填充的类型</param>
        /// <returns></returns>
        public static DegFullCustomObjNoT CreateDegFullCustomObj(IDataReader reader, Type type)
        {
            string key = string.Format("{0}{1}", type.FullName, FullDataHelper.GetDataReaderInfo(reader));

            Delegate degMethod;
            if (_idiFullCustomObj_noT.TryGetValue(key, out degMethod))
                return (DegFullCustomObjNoT)degMethod;

            DegFullCustomObjNoT deg = null;
            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullObjMethod(reader, type, false, false);

            //为动态方法构造调用委托
            deg = (DegFullCustomObjNoT)method.CreateDelegate(typeof(DegFullCustomObjNoT));
            if (!_idiFullCustomObj_noT.ContainsKey(key))
            {
                _idiFullCustomObj_noT.Add(key, deg);
            }
            return deg;
        }

        /// <summary>
        /// 自定义类型填充，已经存在的填充动态方法
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> _idiFullCustomList = new ConcurrentDictionary<string, Delegate>();
        /// <summary>
        /// 构造自定义类 填充List委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullCustomList<T> CreateDegFullCustomList<T>(IDataReader reader)
        {
            string key = string.Format("{0}{1}", typeof(T).FullName, FullDataHelper.GetDataReaderInfo(reader));

            Delegate degMethod;
            if (_idiFullCustomList.TryGetValue(key, out degMethod))
                return (DegFullCustomList<T>)degMethod;

            DegFullCustomList<T> deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullListMethod(reader, typeof(T), false);

            //为动态方法构造调用委托
            deg = (DegFullCustomList<T>)method.CreateDelegate(typeof(DegFullCustomList<T>));
            if (!_idiFullCustomList.ContainsKey(key))
            {
                _idiFullCustomList.TryAdd(key, deg);
            }
            return deg;
        }

        /// <summary>
        /// 自定义类型填充，已经存在的填充动态方法
        /// </summary>
        private static Dictionary<string, Delegate> _idiFullCustomList_noT = new Dictionary<string, Delegate>();
        /// <summary>
        /// 构造自定义类 填充List委托
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type">要填充的类型</param>
        /// <returns></returns>
        public static DegFullCustomListNoT CreateDegFullCustomList(IDataReader reader, Type type)
        {
            string key = string.Format("{0}{1}", type.FullName, FullDataHelper.GetDataReaderInfo(reader));

            Delegate degMethod;
            if (_idiFullCustomList_noT.TryGetValue(key, out degMethod))
                return (DegFullCustomListNoT)degMethod;

            DegFullCustomListNoT deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullListMethod(reader, type, false, false);

            //为动态方法构造调用委托
            deg = (DegFullCustomListNoT)method.CreateDelegate(typeof(DegFullCustomListNoT));
            if (!_idiFullCustomList_noT.ContainsKey(key))
            {
                _idiFullCustomList_noT.Add(key, deg);
            }
            return deg;
        }

        #endregion

        #region 填充映射类型

        /// <summary>
        /// 填充映射类委托
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> _idiFullMapObj = new ConcurrentDictionary<string, Delegate>();
        /// <summary>
        /// 构造填充映射类对象 填充Object委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullMapObj<T> CreateDegFullMapObj<T>(IDataReader reader)
        {
            //判断是否已经存在该类型的动态方法
            Type type = typeof(T);
            Delegate degMethod;
            if (_idiFullMapObj.TryGetValue(type.FullName, out degMethod))
                return (DegFullMapObj<T>)degMethod;

            DegFullMapObj<T> deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullObjMethod(reader, typeof(T), true);

            //为动态方法构造调用委托
            deg = (DegFullMapObj<T>)method.CreateDelegate(typeof(DegFullMapObj<T>));
            if (!_idiFullMapObj.ContainsKey(type.FullName))
            {
                _idiFullMapObj.TryAdd(type.FullName, deg);
            }
            return deg;
        }

        /// <summary>
        /// 填充映射类委托
        /// </summary>
        private static Dictionary<string, Delegate> _idiFullMapObj_noT = new Dictionary<string, Delegate>();
        /// <summary>
        /// 构造填充映射类对象 填充Object委托
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type">要填充的类型</param>
        /// <returns></returns>
        public static DegFullMapObjNoT CreateDegFullMapObj(IDataReader reader, Type type)
        {
            //判断是否已经存在该类型的动态方法
            Delegate degMethod;
            if (_idiFullMapObj_noT.TryGetValue(type.FullName, out degMethod))
                return (DegFullMapObjNoT)degMethod;

            DegFullMapObjNoT deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullObjMethod(reader, type, true, false);

            //为动态方法构造调用委托
            deg = (DegFullMapObjNoT)method.CreateDelegate(typeof(DegFullMapObjNoT));
            if (!_idiFullMapObj_noT.ContainsKey(type.FullName))
            {
                _idiFullMapObj_noT.Add(type.FullName, deg);
            }
            return deg;
        }

        /// <summary>
        /// 填充映射类集合委托
        /// </summary>
        private static ConcurrentDictionary<string, Delegate> _idiFullMapList = new ConcurrentDictionary<string, Delegate>();
        /// <summary>
        /// 构造填充映射类集合 填充List委托
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullMapList<T> CreateDegFullMapList<T>(IDataReader reader)
        {
            //判断是否已经存在该类型的动态方法
            Type type = typeof(T);
            Delegate degMethod;
            if (_idiFullMapList.TryGetValue(type.FullName, out degMethod))
                return (DegFullMapList<T>)degMethod;

            DegFullMapList<T> deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullListMethod(reader, typeof(T), true);

            //为动态方法构造调用委托
            deg = (DegFullMapList<T>)method.CreateDelegate(typeof(DegFullMapList<T>));
            if (!_idiFullMapList.ContainsKey(type.FullName))
            {
                _idiFullMapList.TryAdd(type.FullName, deg);
            }
            return deg;
        }

        /// <summary>
        /// 填充映射类集合委托
        /// </summary>
        private static Dictionary<string, Delegate> _idiFullMapList_noT = new Dictionary<string, Delegate>();
        /// <summary>
        /// 构造填充映射类集合 填充List委托
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="type">要填充的类型</param>
        /// <returns></returns>
        public static DegFullMapListNoT CreateDegFullMapList(IDataReader reader, Type type)
        {
            //判断是否已经存在该类型的动态方法
            Delegate degMethod;
            if (_idiFullMapList_noT.TryGetValue(type.FullName, out degMethod))
                return (DegFullMapListNoT)degMethod;

            DegFullMapListNoT deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullListMethod(reader, type, true, false);

            //为动态方法构造调用委托
            deg = (DegFullMapListNoT)method.CreateDelegate(typeof(DegFullMapListNoT));
            if (!_idiFullMapList_noT.ContainsKey(type.FullName))
            {
                _idiFullMapList_noT.Add(type.FullName, deg);
            }
            return deg;
        }

        #endregion

        #region 填充动态类

        private static ConcurrentDictionary<string, DegFullDynamicObj> _idiFullDynamicObj = new ConcurrentDictionary<string, DegFullDynamicObj>();
        /// <summary>
        /// 构造动态类 填充Object委托
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullDynamicObj CreateDegDynamicFullObj(IDataReader reader)
        {
            Type type = FullDataHelper.CreateType(reader);

            DegFullDynamicObj degMethod;
            if (_idiFullDynamicObj.TryGetValue(type.FullName, out degMethod))
                return degMethod;

            DegFullDynamicObj deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullObjMethod(reader, type, false);

            //为动态方法构造调用委托
            deg = (DegFullDynamicObj)method.CreateDelegate(typeof(DegFullDynamicObj));
            if (!_idiFullDynamicObj.ContainsKey(type.FullName))
            {
                _idiFullDynamicObj.TryAdd(type.FullName, deg);
            }
            return deg;
        }


        private static ConcurrentDictionary<string, DegFullDynamicList> _idiFullDynamicList = new ConcurrentDictionary<string, DegFullDynamicList>();
        /// <summary>
        /// 构造动态类 填充List委托
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullDynamicList CreateDegFullDynamicList(IDataReader reader)
        {
            Type type = FullDataHelper.CreateType(reader);

            DegFullDynamicList degMethod;
            if (_idiFullDynamicList.TryGetValue(type.FullName, out degMethod))
                return degMethod;

            DegFullDynamicList deg = null;

            //构造填充方法
            DynamicMethod method = FullDataHelper.CreateFullListMethod(reader, type, false);

            //为动态方法构造调用委托
            deg = (DegFullDynamicList)method.CreateDelegate(typeof(DegFullDynamicList));
            if (!_idiFullDynamicList.ContainsKey(type.FullName))
            {
                _idiFullDynamicList.TryAdd(type.FullName, deg);
            }
            return deg;
        }

        #endregion

        #region 可扩展动态类

        /// <summary>
        /// 构造动态可扩展类,填充obj
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullDynamicObj CreateDegExpandoDynamicFullObj(IDataReader reader)
        {
            return new DegFullDynamicObj((x) =>
            {
                var obj = new System.Dynamic.ExpandoObject() as IDictionary<string, Object>;

                for (int i = 0; i < x.FieldCount; i++)
                {
                    obj[reader.GetName(i)] = x.GetValue(i);
                }
                return obj;
            });
        }

        /// <summary>
        ///构造动态可扩展类，填充list
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static DegFullDynamicList CreateDegExpandoDynamicFullList(IDataReader reader)
        {
            return new DegFullDynamicList((x) =>
            {
                var list = new List<System.Dynamic.ExpandoObject>();

                while (reader.Read())
                {
                    var obj = new System.Dynamic.ExpandoObject();
                    for (int f = 0; f < x.FieldCount; f++)
                    {
                        (obj as IDictionary<string, Object>)[reader.GetName(f)] = x.GetValue(f);
                    }
                    list.Add(obj);
                }
                return list;
            });
        }

        #endregion
    }
}
