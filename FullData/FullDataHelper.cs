using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Data;
using DBFrame.DBMap;
using System.Reflection;
using System.Xml;

namespace DBFrame.FullData
{
    /// <summary>
    /// 数据填充帮助类，注意该类中都为静态方法，在多线程下，外部调用应使用lock
    /// </summary>
    sealed class FullDataHelper
    {
        public static string HexStringFromByteArray(byte[] bytes)
        {
            string hex = "";

            foreach (byte b in bytes)
            {
                hex += b.ToString("X2");
            }

            return hex;
        }

        #region Static Readonly Fields

        private static readonly MethodInfo hsfba = typeof(FullDataHelper).GetMethod("HexStringFromByteArray");

        public static readonly MethodInfo readMethod = typeof(IDataReader).GetMethod("Read");
        private static readonly MethodInfo getValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private static readonly MethodInfo getIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull", new Type[] { typeof(int) });

        private static readonly MethodInfo getBoolean = typeof(IDataRecord).GetMethod("GetBoolean", new Type[] { typeof(int) });
        private static readonly MethodInfo getChar = typeof(IDataRecord).GetMethod("GetChar", new Type[] { typeof(int) });
        private static readonly MethodInfo getDateTime = typeof(IDataRecord).GetMethod("GetDateTime", new Type[] { typeof(int) });
        private static readonly MethodInfo getDecimal = typeof(IDataRecord).GetMethod("GetDecimal", new Type[] { typeof(int) });
        private static readonly MethodInfo getDouble = typeof(IDataRecord).GetMethod("GetDouble", new Type[] { typeof(int) });
        private static readonly MethodInfo getFloat = typeof(IDataRecord).GetMethod("GetFloat", new Type[] { typeof(int) });
        private static readonly MethodInfo getGuid = typeof(IDataRecord).GetMethod("GetGuid", new Type[] { typeof(int) });
        private static readonly MethodInfo getByte = typeof(IDataRecord).GetMethod("GetByte", new Type[] { typeof(int) });
        private static readonly MethodInfo getInt16 = typeof(IDataRecord).GetMethod("GetInt16", new Type[] { typeof(int) });
        private static readonly MethodInfo getInt32 = typeof(IDataRecord).GetMethod("GetInt32", new Type[] { typeof(int) });
        private static readonly MethodInfo getInt64 = typeof(IDataRecord).GetMethod("GetInt64", new Type[] { typeof(int) });
        private static readonly MethodInfo getString = typeof(IDataRecord).GetMethod("GetString", new Type[] { typeof(int) });

        private static readonly MethodInfo decimalToByte = typeof(decimal).GetMethod("ToByte");
        private static readonly MethodInfo decimalToSByte = typeof(decimal).GetMethod("ToSByte");
        private static readonly MethodInfo decimalToUInt16 = typeof(decimal).GetMethod("ToUInt16");
        private static readonly MethodInfo decimalToUInt32 = typeof(decimal).GetMethod("ToUInt32");
        private static readonly MethodInfo decimalToUInt64 = typeof(decimal).GetMethod("ToUInt64");
        private static readonly MethodInfo decimalToInt16 = typeof(decimal).GetMethod("ToInt16");
        private static readonly MethodInfo decimalToInt32 = typeof(decimal).GetMethod("FCallToInt32", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly MethodInfo decimalToInt64 = typeof(decimal).GetMethod("ToInt64");
        private static readonly MethodInfo decimalToSingle = typeof(decimal).GetMethod("ToSingle");
        private static readonly MethodInfo decimalToDouble = typeof(decimal).GetMethod("ToDouble");

        private static readonly MethodInfo toString = typeof(string).GetMethod("Format", new Type[] { typeof(string), typeof(object) });
        private static readonly MethodInfo toCharArray = typeof(string).GetMethod("ToCharArray", new Type[] { });

        #endregion

        #region 创建填充方法体

        /// <summary>
        /// 创建填充对象方法
        /// </summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="type">映射类型</param>
        /// <param name="isMap">是否从映射类中填充</param>
        public static DynamicMethod CreateFullObjMethod(IDataReader reader, Type type, bool isMap, bool returnTypeIsT = true)
        {
            //创建动态方法
            Type returnType= type;
            if (!returnTypeIsT)
            {
                returnType = typeof(object);
            }
            DynamicMethod method = new DynamicMethod("Create",
               returnType,
               new Type[] { typeof(IDataReader) },
               type,
               true);

            ILGenerator il = method.GetILGenerator();

            //声明将要返回的对象
            LocalBuilder item = il.DeclareLocal(type);

            //如果有对象中有XML字段,那声明xml变量,但是在编译过程中,如果判读没有使用
            //这个变量,编译器会优化,将这个声明语句去除
            LocalBuilder xml = il.DeclareLocal(typeof(XmlDocument));

            CreateBuilder(isMap, il, item, xml, reader, type);

            //将声明的对象item推到堆栈上
            il.Emit(OpCodes.Ldloc_S, item);

            //返回堆栈上的item
            il.Emit(OpCodes.Ret);

            return method;
        }

        /// <summary>
        /// 创建填充集合方法
        /// </summary>
        /// <param name="reader">IDataReader</param>
        /// <param name="type">映射类型</param>
        /// <param name="isMap">是否从映射类中填充</param>
        public static DynamicMethod CreateFullListMethod(IDataReader reader, Type type, bool isMap, bool returnTypeIsT = true)
        {
            Type returnType;
            if (returnTypeIsT)
            {
                returnType = typeof(List<>).MakeGenericType(type);
            }
            else
            {
                returnType = typeof(List<object>);
            }
            DynamicMethod method = new DynamicMethod("Create",
                returnType,
                new Type[] { typeof(IDataRecord) },
                type,
                true);
            ILGenerator il = method.GetILGenerator();

            Type listType = typeof(List<>).MakeGenericType(type);
            //声明将要返回的对象list,类型为List<T>
            LocalBuilder list = il.DeclareLocal(listType);

            //声明局部变量item
            LocalBuilder item = il.DeclareLocal(type);

            //如果有对象中有XML字段,那声明xml变量,但是在编译过程中,如果判读没有使用
            //这个变量,编译器会优化,将这个声明语句去除
            LocalBuilder xml = il.DeclareLocal(typeof(XmlDocument));

            //退出标签
            Label exit = il.DefineLabel();

            //声明标签,用于循环从dataReader中获取数据
            Label loop = il.DefineLabel();

            //调用List<T>的构造函数,list对象初始化创建
            il.Emit(OpCodes.Newobj, listType.GetConstructor(Type.EmptyTypes));

            //将list对象放在堆栈上
            il.Emit(OpCodes.Stloc_S, list);

            //循环开始
            il.MarkLabel(loop);

            //将函数的第一个参数加载到堆栈上,这个地方实际上是一个IDataReader
            il.Emit(OpCodes.Ldarg_0);

            //调用第一个参数(IDataReader类型)的readMethod方法
            il.Emit(OpCodes.Call, readMethod);

            //如果第一个参数(IDataReader类型)的readMethod方法返回false
            //直接跳到退出(exit)标签,退出循环
            il.Emit(OpCodes.Brfalse, exit);

            //从第一个参数中,读取数据来填充item对象
            CreateBuilder(isMap, il, item, xml, reader, type);

            //将list对象放在堆栈上
            il.Emit(OpCodes.Ldloc_S, list);

            //将item对象放在堆栈上
            il.Emit(OpCodes.Ldloc_S, item);

            //调用list对象的Add方法,将item增加到list列表中
            il.Emit(OpCodes.Call, listType.GetMethod("Add"));

            //跳到loop标签,进行循环
            il.Emit(OpCodes.Br, loop);

            //写上exit标签
            il.MarkLabel(exit);

            //将list对象放在堆栈上
            il.Emit(OpCodes.Ldloc_S, list);

            //将list返回
            il.Emit(OpCodes.Ret);

            return method;
        }
        
        #endregion

        #region 创建单个对象 Private方法

        /// <summary>
        /// 创建单个对象
        /// </summary>
        /// <param name="isMap">是否是映射对象</param>
        /// <param name="il"></param>
        /// <param name="item"></param>
        /// <param name="xml"></param>
        /// <param name="dataReader"></param>
        /// <param name="type">返回的类型</param>
        private static void CreateBuilder(bool isMap, ILGenerator il, LocalBuilder item, LocalBuilder xml, IDataReader dataReader, Type type)
        {
            //调用T对象的构造函数,并赋予item
            il.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes));

            //将item对象加载到堆栈上
            il.Emit(OpCodes.Stloc_S, item);

            //如果是映射对象
            if (isMap)
            {
                //获取映射对象对应的DBTable
                DBTable table = MapHelper.GetDBTable(type);

                //获取表主键
                foreach (var pk in table.PrimaryKey)
                {
                    //获取主键在参数dataReader中的顺序号
                    int fieldIndex = dataReader.GetOrdinal(pk.Name);

                    //如果找到,那将dataReader中的序号为fieldIndex的数据装载到item的相应属性或者中
                    if (fieldIndex > -1) LoadValueBuilder(il, item, xml, dataReader, fieldIndex, pk.PropertyInfo, pk.FieldInfo);
                }

                //循环处理表中的除主键外的其他字段
                foreach (DBColumn col in table.ColumnList)
                {
                    //获取字段在参数dataReader中的顺序号
                    int fieldIndex = dataReader.GetOrdinal(col.Name);

                    //如果找到,那将dataReader中的序号为fieldIndex的数据装载到item的相应属性或者
                    if (fieldIndex > -1) LoadValueBuilder(il, item, xml, dataReader, fieldIndex, col.PropertyInfo, col.FieldInfo);
                }
            }
            else
            {
                //如果是CustomSelect,那通过对象的字段属性或者字段名称来作为dataReader中的字段匹配来获取值

                //获取当前对象的所有字段或者属性
                MemberInfo[] members = type.FindMembers(
                    MemberTypes.Property | MemberTypes.Field,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance,
                    null, null);

                //循环处理所有属性或者字段方法
                foreach (MemberInfo mi in members)
                {
                    try
                    {
                        //获取字段在参数dataReader中的顺序号
                        int fieldIndex = dataReader.GetOrdinal(mi.Name);
                        if (fieldIndex > -1)
                        {
                            PropertyInfo propertyInfo = mi as PropertyInfo;
                            FieldInfo fieldInfo = mi as FieldInfo;

                            //如果找到,那将dataReader中的序号为fieldIndex的数据装载到item的相应属性或者
                            LoadValueBuilder(il, item, xml, dataReader, fieldIndex, propertyInfo, fieldInfo);
                        }
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// 将指定字段的值装载到item对象指定的属性或者字段中
        /// </summary>
        /// <param name="il"></param>
        /// <param name="item"></param>
        /// <param name="xml"></param>
        /// <param name="dataReader"></param>
        /// <param name="fieldIndex"></param>
        /// <param name="propertyInfo"></param>
        /// <param name="fieldInfo"></param>
        private static void LoadValueBuilder(
            ILGenerator il,
            LocalBuilder item,
            LocalBuilder xml,
            IDataReader dataReader,
            int fieldIndex,
            PropertyInfo propertyInfo,
            FieldInfo fieldInfo)
        {
            //获取dataReader中序号为fieldIndex的字段类型
            Type fieldType = dataReader.GetFieldType(fieldIndex);

            //判断item对象当前对应的属性或者字段类型
            Type type = propertyInfo == null ? fieldInfo.FieldType : propertyInfo.PropertyType;

            //如果是可空类型,那需要判断底层的数据类型
            Type underlyingType = type;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) underlyingType = Nullable.GetUnderlyingType(type);

            //定义endIf标签,用于控制当前dataReader中序号为fieldIndex的字段值是否为空,如果为空,那跳出endIf标签
            Label endIfLabel = il.DefineLabel();

            //将函数的第一个参数(本案中实际上为IDataReader)加载到堆栈上
            il.Emit(OpCodes.Ldarg_0);

            //将调用getIsDBNull所需要的参数,本案中数值为fieldIndex加载到
            Ldc_I4(il, fieldIndex);

            //调用getIsDBNull方法
            il.Emit(OpCodes.Call, getIsDBNull);

            //如果getIsDBNull方法调用结果方法true,那直接跳到endIf标签
            il.Emit(OpCodes.Brtrue, endIfLabel);

            //将对象item加载到堆栈中
            il.Emit(OpCodes.Ldloc_S, item);

            //如果当前对象item当前的属性或者字段类型为string
            //并且当前dataReader对应字段类型不是strng
            //意味着需要将从dataReader中当前字段读取的数据需要转换为string,通过调用对象的ToString放
            if (underlyingType == typeof(string) && fieldType != typeof(string))
            {
                //调用ToString方法的第一个参数为 "{0}"
                il.Emit(OpCodes.Ldstr, "{0}");
            }
            //如果当前对象item当前的属性或者字段类型为XmlDocument
            //意味着需要动态构造XmlDocument对象
            else if (underlyingType == typeof(XmlDocument))
            {
                //构造XmlDocument对象
                il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(Type.EmptyTypes));

                //将构造的xml对象存储到局部变量xml中
                il.Emit(OpCodes.Stloc_S, xml);

                //并且将xml对象加载的计算堆栈上
                il.Emit(OpCodes.Ldloc_S, xml);
            }

            //将函数的第一个参数0--dataReader加载到堆栈中
            il.Emit(OpCodes.Ldarg_0);

            //将fieldIndex加载到堆栈中
            Ldc_I4(il, fieldIndex);

            //调用获取数据方法dataReader.getXXX
            if (fieldType == typeof(bool)) il.Emit(OpCodes.Call, getBoolean);
            else if (fieldType == typeof(char)) GetCharValue(il, fieldType, underlyingType);
            else if (fieldType == typeof(DateTime)) il.Emit(OpCodes.Call, getDateTime);
            else if (fieldType == typeof(Guid)) il.Emit(OpCodes.Call, getGuid);
            else if (fieldType == typeof(string)) GetStringValue(il, fieldType, underlyingType, xml);
            else if (fieldType == typeof(byte)) GetByteValue(il, fieldType, underlyingType);
            else if (fieldType == typeof(short)) GetInt16Value(il, fieldType, underlyingType);
            else if (fieldType == typeof(int)) GetInt32Value(il, fieldType, underlyingType);
            else if (fieldType == typeof(long)) GetInt64Value(il, fieldType, underlyingType);
            else if (fieldType == typeof(float)) GetFloatValue(il, fieldType, underlyingType);
            else if (fieldType == typeof(double)) GetDoubleValue(il, fieldType, underlyingType);
            else if (fieldType == typeof(decimal)) GetDecimalValue(il, fieldType, underlyingType);
            else
            {
                //如果是其他类型,那通过GetValue方法获取object的值
                il.Emit(OpCodes.Call, getValue);

                if (typeof(Guid) == underlyingType && typeof(byte[]) == fieldType)
                {
                    //Guid数据库中有两种存放方式，char(32),byte[16]
                    //如果数据库内部类型是byte[],映射类型是Guid,那需要构造一个Guid
                    il.Emit(OpCodes.Call, hsfba);
                    il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { typeof(string) }));
                }
                else if (fieldType != underlyingType)
                {
                    //如果fieldType与underlyingType不同，那需要装箱为underlyingType
                    il.Emit(OpCodes.Unbox_Any, underlyingType);
                }
            }

            //如果字段类型不等于当前item的当前处理的属性或者字段类型
            if (fieldType != underlyingType)
            {
                //如果当前item的当前处理的属性或者字段类型是timespan,那构造
                //if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { typeof(long) }));
                //else
                //处理bool类型,直接判断当前从dataReader中获取的值时候等于1
                if (typeof(bool) == underlyingType)
                {
                    //将数据1放在堆栈上
                    il.Emit(OpCodes.Ldc_I4_1);
                    //比较堆栈上的值是否相等
                    il.Emit(OpCodes.Ceq);
                }
                //如果当前item的当前属性或者字段类型为string,那需要调用ToString方法
                //然后放在item的当前属性或者字段中
                else if (typeof(string) == underlyingType)
                {
                    //将从dataReader中读取的值装箱,调用ToString方法，必须将对象首先装箱
                    il.Emit(OpCodes.Box, fieldType);
                    //然后调用ToString方法
                    il.Emit(OpCodes.Call, toString);
                }
            }

            //处理可空类型,那构造出这个可空类型
            if (type != underlyingType) il.Emit(OpCodes.Newobj, type.GetConstructor(new Type[] { underlyingType }));

            //将获取的值设置到相应的FieldInfo 或者 PropertyInfo上
            if (propertyInfo != null/* && propertyInfo.GetSetMethod(true) != null*/) il.Emit(OpCodes.Call, propertyInfo.GetSetMethod(true));
            else il.Emit(OpCodes.Stfld, fieldInfo);

            //endIf标签
            il.MarkLabel(endIfLabel);
        }

        /// <summary>
        /// 从IDataReader中读取char
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetCharValue(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getChar);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Conv_I2);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));
            }
        }

        /// <summary>
        /// 从IDataReader中读取Decimal
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetDecimalValue(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getDecimal);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Call, decimalToByte);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Call, decimalToUInt16);
                else if (underlyingType == typeof(uint)) il.Emit(OpCodes.Call, decimalToUInt32);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Call, decimalToUInt64);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Call, decimalToSByte);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Call, decimalToInt16);
                else if (underlyingType == typeof(int) || underlyingType == typeof(bool) || underlyingType.IsEnum) il.Emit(OpCodes.Call, decimalToInt32);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Call, decimalToInt64);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Call, decimalToSingle);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8, decimalToDouble);
                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Call, decimalToInt64);
            }
        }

        /// <summary>
        /// 从IDataReader中读取String
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        /// <param name="xml">本地声明的XmlDocument变量</param>
        private static void GetStringValue(ILGenerator il, Type fieldType, Type underlyingType, LocalBuilder xml)
        {
            il.Emit(OpCodes.Call, getString);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(Guid))
                {
                    il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));
                }
                else if (underlyingType == typeof(char))
                {
                    il.Emit(OpCodes.Call, toCharArray);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ldelem_U2);
                }
                else if (underlyingType == typeof(XmlDocument))
                {
                    il.Emit(OpCodes.Call, underlyingType.GetMethod("LoadXml"));
                    il.Emit(OpCodes.Ldloc_S, xml);
                }
            }
        }

        /// <summary>
        /// 从IDataReader中读取Double
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetDoubleValue(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getDouble);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Conv_U2);
                else if (underlyingType == typeof(uint)) il.Emit(OpCodes.Conv_U4);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Conv_I2);
                else if (underlyingType == typeof(int) || underlyingType == typeof(bool) || underlyingType.IsEnum) il.Emit(OpCodes.Conv_I4);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));

                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Conv_I8);
            }
        }

        /// <summary>
        /// 从IDataReader中读取Float
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetFloatValue(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getFloat);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Conv_U2);
                else if (underlyingType == typeof(uint)) il.Emit(OpCodes.Conv_U4);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Conv_I2);
                else if (underlyingType == typeof(int) || underlyingType == typeof(bool) || underlyingType.IsEnum) il.Emit(OpCodes.Conv_I4);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));

                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Conv_I8);
            }
        }

        /// <summary>
        /// 从IDataReader中读取Int64
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetInt64Value(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getInt64);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Conv_U2);
                else if (underlyingType == typeof(uint)) il.Emit(OpCodes.Conv_U4);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Conv_I2);
                else if (underlyingType == typeof(int) || underlyingType == typeof(bool) || underlyingType.IsEnum) il.Emit(OpCodes.Conv_I4);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));
                //TimeSpan可以直接由int64赋值
            }
        }

        /// <summary>
        /// 从IDataReader中读取Int32
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetInt32Value(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getInt32);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Conv_U2);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(short)) il.Emit(OpCodes.Conv_I2);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));

                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Conv_I8);
            }
        }

        /// <summary>
        /// 从IDataReader中读取Int16
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetInt16Value(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getInt16);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(byte)) il.Emit(OpCodes.Conv_U1);
                else if (underlyingType == typeof(ushort) || underlyingType == typeof(char)) il.Emit(OpCodes.Conv_U2);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_I8);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));

                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Conv_I8);
            }
        }

        /// <summary>
        /// 从IDataReader中读取Byte
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="fieldType">属性类型</param>
        /// <param name="underlyingType">Nullable中的底层类型</param>
        private static void GetByteValue(ILGenerator il, Type fieldType, Type underlyingType)
        {
            il.Emit(OpCodes.Call, getByte);
            if (fieldType != underlyingType)
            {
                if (underlyingType == typeof(sbyte)) il.Emit(OpCodes.Conv_I1);
                else if (underlyingType == typeof(long)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(ulong)) il.Emit(OpCodes.Conv_U8);
                else if (underlyingType == typeof(float)) il.Emit(OpCodes.Conv_R4);
                else if (underlyingType == typeof(double)) il.Emit(OpCodes.Conv_R8);
                else if (underlyingType == typeof(decimal)) il.Emit(OpCodes.Newobj, underlyingType.GetConstructor(new Type[] { fieldType }));
                else if (underlyingType == typeof(TimeSpan)) il.Emit(OpCodes.Conv_I8);
            }
        }

        /// <summary>
        /// 从堆栈总读取本地变量
        /// </summary>
        /// <param name="il">ILGenerator</param>
        /// <param name="i">变量序号</param>
        private static void Ldc_I4(ILGenerator il, int i)
        {
            if (i == -1) il.Emit(OpCodes.Ldc_I4_M1);
            else if (i == 0) il.Emit(OpCodes.Ldc_I4_0);
            else if (i == 1) il.Emit(OpCodes.Ldc_I4_1);
            else if (i == 2) il.Emit(OpCodes.Ldc_I4_2);
            else if (i == 3) il.Emit(OpCodes.Ldc_I4_3);
            else if (i == 4) il.Emit(OpCodes.Ldc_I4_4);
            else if (i == 5) il.Emit(OpCodes.Ldc_I4_5);
            else if (i == 6) il.Emit(OpCodes.Ldc_I4_6);
            else if (i == 7) il.Emit(OpCodes.Ldc_I4_7);
            else if (i == 8) il.Emit(OpCodes.Ldc_I4_8);
            else if (i > -129 && i < 128) il.Emit(OpCodes.Ldc_I4_S, (sbyte)i);
            else il.Emit(OpCodes.Ldc_I4, i);
        }

        #endregion

        /// <summary>
        /// 获取DataReader的信息
        /// </summary>
        /// <param name="reader">IDataReader</param>
        /// <returns></returns>
        public static string GetDataReaderInfo(IDataReader reader)
        {
            string readerInfo = "RI_";
            int fieldCount = reader.FieldCount;
            for (int i = 0; i < fieldCount; i++)
            {
                readerInfo += string.Format("{0}{1}", reader.GetName(i).ToUpper(), reader.GetFieldType(i).Name);
            }
            return readerInfo;
        }

        /// <summary>
        /// 动态类字典集合
        /// </summary>
        private static Dictionary<string, Type> _idiDynamicType = new Dictionary<string, Type>();
        private static ModuleBuilder moduleBuilder = null;
        /// <summary>
        /// 通过特定的IDataReader动态创建相应数据类型
        /// </summary>
        /// <param name="reader">IDataReader对象</param>
        /// <returns>创建好的Type</returns>
        public static Type CreateType(IDataReader reader)
        {
            //获取reader的字段信息
            string readerInfo = GetDataReaderInfo(reader);

            Type type;
            //如果reader字段信息已经形成相应类型，那直接返回
            if (_idiDynamicType.TryGetValue(readerInfo, out type))
                return type;

            //否则创建相关类型
            if (moduleBuilder == null)
            {
                string assemblyName = "CreateTypeForDataReader";
                //创建程序集
                AssemblyBuilder assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName(assemblyName),
                    AssemblyBuilderAccess.Run);

                //创建模块
                moduleBuilder = assembly.DefineDynamicModule(assemblyName);
            }

            string typeName = string.Format("MyDB.Dynamic.DynamicObject{0}", _idiDynamicType.Count.ToString());
            TypeBuilder typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed);

            int fieldCount = reader.FieldCount;
            for (int i = 0; i < fieldCount; i++)
            {
                //必须设置为Property，才能够绑定到比如DataGridView子类的控件中进行显示
                CreateProperty(typeBuilder, reader.GetName(i).ToUpper(), reader.GetFieldType(i));
            }
            _idiDynamicType[readerInfo] = typeBuilder.CreateType();

            return _idiDynamicType[readerInfo];
        }

        private static void CreateProperty(TypeBuilder typeBuilder, string propertyName, Type propertyType)
        {
            // 定义内部私有字段
            FieldBuilder fieldBuilder = typeBuilder.DefineField(
                            "_" + propertyName,
                            propertyType,
                            FieldAttributes.Private);

            // 定义属性创建方法
            // 因为属性是没有参数的，所以最后一个参数设置为null（没有参数）
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty(
                            propertyName,
                            System.Reflection.PropertyAttributes.HasDefault,
                            propertyType,
                            null);

            // 定义属性set和get方法需要的attributes
            MethodAttributes getSetAttr = MethodAttributes.Public |
                                          MethodAttributes.SpecialName |
                                          MethodAttributes.HideBySig;

            // 定义get方法
            MethodBuilder getMethodBuilder = typeBuilder.DefineMethod(
                            "get_" + propertyName,
                            getSetAttr,
                            propertyType,
                            Type.EmptyTypes);

            ILGenerator ilGet = getMethodBuilder.GetILGenerator();

            ilGet.Emit(OpCodes.Ldarg_0);
            ilGet.Emit(OpCodes.Ldfld, fieldBuilder);
            ilGet.Emit(OpCodes.Ret);

            // 定义set方法
            MethodBuilder setMethodBuilder = typeBuilder.DefineMethod(
                            "set_" + propertyName,
                            getSetAttr,
                            null,
                            new Type[] { propertyType });

            ILGenerator ilSet = setMethodBuilder.GetILGenerator();

            ilSet.Emit(OpCodes.Ldarg_0);
            ilSet.Emit(OpCodes.Ldarg_1);
            ilSet.Emit(OpCodes.Stfld, fieldBuilder);
            ilSet.Emit(OpCodes.Ret);

            // 设置propertyBuilder的get与set方法 
            propertyBuilder.SetGetMethod(getMethodBuilder);
            propertyBuilder.SetSetMethod(setMethodBuilder);
        }
    }
}
