using System.Data;
using System.Collections.Generic;
using System;

namespace DBFrame.FullData
{
    #region 动态类填充委托

    /// <summary>
    /// 将IDataReader填充到动态类中
    /// </summary>
    /// <param name="reader">需要填充的IDataReader</param>
    /// <returns>填充后的动态对象</returns>
    public delegate object DegFullDynamicObj(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到动态类集合中
    /// </summary>
    /// <param name="reader">需要填充的IDataReader</param>
    /// <returns>填充后的动态对象</returns>
    public delegate object DegFullDynamicList(IDataReader reader);

    #endregion

    #region 动态类填充委托

    /// <summary>
    /// 将IDataReader填充到自定义类集合中
    /// </summary>
    /// <typeparam name="T">自定义类型</typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public delegate T DegFullCustomObj<T>(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到自定义类集合中
    /// </summary>
    /// <typeparam name="T">自定义类型</typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public delegate List<T> DegFullCustomList<T>(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到自定义类集合中
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate object DegFullCustomObjNoT(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到自定义类集合中
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate List<object> DegFullCustomListNoT(IDataReader reader);

    #endregion

    #region 映射类填充委托

    /// <summary>
    /// 将IDataReader填充到映射对象中
    /// </summary>
    /// <typeparam name="T">自定义类型</typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public delegate T DegFullMapObj<T>(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到映射类集合中
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="reader"></param>
    /// <returns></returns>
    public delegate List<T> DegFullMapList<T>(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到映射对象中
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate object DegFullMapObjNoT(IDataReader reader);

    /// <summary>
    /// 将IDataReader填充到映射类集合中
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public delegate List<object> DegFullMapListNoT(IDataReader reader);

    #endregion

    #region SetValue GetValue委托

    /// <summary>
    /// 获取对象属性值
    /// </summary>
    /// <param name="source">对象</param>
    /// <returns>属性值</returns>
    public delegate object DegGetValue(object source);

    /// <summary>
    /// 给对象指定属性或者字段赋值
    /// </summary>
    /// <param name="source">对象</param>
    /// <param name="value">值</param>
    public delegate void DegSetValue(object source, object value);

    #endregion
}
