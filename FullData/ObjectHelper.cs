using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DBFrame.FullData
{
    /// <summary>
    /// 对象操作帮助类
    /// </summary>
    public class ObjectHelper
    {
        /// <summary>
        /// 对象类型
        /// </summary>
        static readonly Type objType = typeof(object);

        #region 创建GetValue方法

        /// <summary>
        /// 创建GetValue方法
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="fieldOrPropertyName">字段或者属性名称</param>
        /// <returns>GetValue委托</returns>
        public DegGetValue CreateDegGetValue(Type type, string fieldOrPropertyName)
        {
            //根据字段或者属性获取其属性
            PropertyInfo propertyInfo = type.GetProperty(
                fieldOrPropertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (propertyInfo != null)
            {
                //根据数据创建GetValue方法
                return CreateDegGetValueByPrty(propertyInfo);
            }

            //根据字段创建动态方法
            FieldInfo fieldInfo = type.GetField(
                fieldOrPropertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fieldInfo != null)
            {
                return CreateDegGetValueByField(fieldInfo);
            }

            throw new ArgumentException(string.Format("在类型[{0}]中没有找到[{1}]属性{{Get}}/字段",
                type.FullName, fieldOrPropertyName));
        }

        /// <summary>
        /// 根据属性创建动态的Get方法
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private DegGetValue CreateDegGetValueByPrty(PropertyInfo propertyInfo)
        {
            //创建动态方法
            DynamicMethod dynGetValue = CreateGetValueDynamicMethod(objType, objType);
            //获取属性的Get方法
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
            ILGenerator getGenerator = dynGetValue.GetILGenerator();

            if (!getMethodInfo.IsStatic)
            {
                getGenerator.Emit(OpCodes.Ldarg_0);
                if (objType != propertyInfo.DeclaringType)
                {
                    Unbox(propertyInfo.DeclaringType, getGenerator);
                }
            }
            getGenerator.Emit(OpCodes.Call, getMethodInfo);
            if (objType != propertyInfo.PropertyType)
            {
                Box(getMethodInfo.ReturnType, getGenerator);
            }

            getGenerator.Emit(OpCodes.Ret);

            return (DegGetValue)dynGetValue.CreateDelegate(typeof(DegGetValue));
        }

        /// <summary>
        /// 根据字段创建动态的Get方法
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        private DegGetValue CreateDegGetValueByField(FieldInfo fieldInfo)
        {
            DynamicMethod dynGetValue = CreateGetValueDynamicMethod(objType, objType);
            ILGenerator getGenerator = dynGetValue.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                getGenerator.Emit(OpCodes.Ldarg_0);
                if (objType != fieldInfo.DeclaringType)
                {
                    Unbox(fieldInfo.DeclaringType, getGenerator);
                }
            }
            getGenerator.Emit(OpCodes.Ldfld, fieldInfo);
            if (objType != fieldInfo.FieldType)
            {
                Box(fieldInfo.FieldType, getGenerator);
            }
            getGenerator.Emit(OpCodes.Ret);

            return (DegGetValue)dynGetValue.CreateDelegate(typeof(DegGetValue));
        }

        /// <summary>
        /// 创建动态方法
        /// </summary>
        /// <param name="name">动态方法名称</param>
        /// <param name="type">对象类型</param>
        /// <param name="returnType">方法返回值类型</param>
        /// <returns>创建的方法</returns>
        private DynamicMethod CreateGetValueDynamicMethod(Type type, Type returnType)
        {
            DynamicMethod dynamicGet = new DynamicMethod(
                "GetValue",
                MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard,
                returnType,
                new Type[] { type },
                type,
                true);
            dynamicGet.InitLocals = false;
            return dynamicGet;
        }

        #endregion

        #region 创建SetValue方法

        /// <summary>
        /// 创建SetValue方法
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="fieldOrPropertyName">字段或者属性名称</param>
        /// <returns>SetValue委托</returns>
        public DegSetValue CreateDegSetValue(Type type, string fieldOrPropertyName)
        {
            //根据字段或者属性获取其属性
            PropertyInfo propertyInfo = type.GetProperty(
                fieldOrPropertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (propertyInfo != null)
            {
                //根据数据创建GetValue方法
                return CreateDegSetValueByPrty(propertyInfo);
            }

            //根据字段创建动态方法
            FieldInfo fieldInfo = type.GetField(
                fieldOrPropertyName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fieldInfo != null)
            {
                return CreateDegSetValueByField(fieldInfo);
            }

            throw new ArgumentException(string.Format("在类型[{0}]中没有找到[{1}]属性{{Get}}/字段",
                type.FullName, fieldOrPropertyName));
        }

        /// <summary>
        /// 根据属性创建动态的Set方法
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns></returns>
        private DegSetValue CreateDegSetValueByPrty(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
            DynamicMethod dynGetValue = CreateSetValueDynamicMethod(objType, objType);
            ILGenerator setGenerator = dynGetValue.GetILGenerator();

            if (!setMethodInfo.IsStatic)
            {
                setGenerator.Emit(OpCodes.Ldarg_0);
                if (objType != propertyInfo.DeclaringType)
                {
                    Unbox(propertyInfo.DeclaringType, setGenerator);
                }
            }
            setGenerator.Emit(OpCodes.Ldarg_1);
            if (objType != propertyInfo.PropertyType)
            {
                UnboxAny(setMethodInfo.GetParameters()[0].ParameterType, setGenerator);
            }
            setGenerator.Emit(OpCodes.Call, setMethodInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (DegSetValue)dynGetValue.CreateDelegate(typeof(DegSetValue));
        }

        /// <summary>
        /// 根据字段创建动态的Set方法
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        private DegSetValue CreateDegSetValueByField(FieldInfo fieldInfo)
        {
            DynamicMethod dynGetValue = CreateSetValueDynamicMethod(objType, objType);
            ILGenerator setGenerator = dynGetValue.GetILGenerator();

            if (!fieldInfo.IsStatic)
            {
                setGenerator.Emit(OpCodes.Ldarg_0);
                if (objType != fieldInfo.DeclaringType)
                {
                    Unbox(fieldInfo.DeclaringType, setGenerator);
                }
            }
            setGenerator.Emit(OpCodes.Ldarg_1);
            if (objType != fieldInfo.FieldType)
            {
                UnboxAny(fieldInfo.FieldType, setGenerator);
            }
            setGenerator.Emit(OpCodes.Stfld, fieldInfo);
            setGenerator.Emit(OpCodes.Ret);

            return (DegSetValue)dynGetValue.CreateDelegate(typeof(DegSetValue));
        }

        /// <summary>
        /// 创建动态方法
        /// </summary>
        /// <param name="name">动态方法名称</param>
        /// <param name="type">对象类型</param>
        /// <param name="returnType">方法返回值类型</param>
        /// <returns>创建的方法</returns>
        private DynamicMethod CreateSetValueDynamicMethod(Type type, Type valueType)
        {
            DynamicMethod dynamicSet = new DynamicMethod(
                "SetHandler",
                typeof(void),
                new Type[] { type, valueType },
                type,
                true);
            dynamicSet.InitLocals = false;
            return dynamicSet;
        }

        #endregion

        #region Box UnboxAny

        /// <summary>
        /// 类型装箱
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="generator">ILGenerator对象</param>
        private void Box(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Box, type);
            }
        }

        /// <summary>
        /// 类型拆箱
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="generator">ILGenerator对象</param>
        private void Unbox(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Unbox, type);
            }
        }

        /// <summary>
        /// 类型拆箱
        /// </summary>
        /// <param name="type">对象类型</param>
        /// <param name="generator">ILGenerator对象</param>
        private void UnboxAny(Type type, ILGenerator generator)
        {
            if (type.IsValueType)
            {
                generator.Emit(OpCodes.Unbox_Any, type);
            }
        }

        #endregion
    }
}
