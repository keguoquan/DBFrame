using System;
using System.Threading;
using System.Collections.Generic;

namespace DBFrame
{
    /// <summary>
    /// 主键生成器
    /// </summary>
    public class MyIdMake
    {
        /// <summary>
        /// 最大序号
        /// </summary>
        private static UInt32 _maxSerNum = 99999;
        /// <summary>
        /// 按当前时间生成时，当前序号，一秒钟内归零
        /// </summary>
        private static UInt32 _currSerNum = 0;
        /// <summary>
        /// 按当前时间生成时，序号清0时间
        /// </summary>
        private static DateTime _currClearTime = DateTime.MinValue;
        /// <summary>
        /// 指定时间生成时序号，从0开始直到最大值时才归零
        /// </summary>
        private static UInt32 _pointSerNum = 0;
        /// <summary>
        /// 指定时间生成时，序号上次清0时间
        /// </summary>
        private static DateTime _pointClearTime = DateTime.MinValue;

        /// <summary>
        /// 线程锁
        /// </summary>
        private static object lock_obj = new object();

        private static UInt16 _myIdNo = 999;
        /// <summary>
        /// 用于MyIdMake生成主键，进行标示不同程序，不同服务器之间生成的唯一性
        /// 如果需要用到MyId主键策略，则必须设置该值，值范围0~49
        /// 50~99为指定时间标识，因此应用程序不能设置为50~99
        /// </summary>
        public static UInt16 MyIdNo
        {
            get
            {
                if (_myIdNo < 0 || _myIdNo > 49) throw new MyDBException("MyId生成Id必须先设置MyIdNo，值范围0~49");
                return _myIdNo;
            }
            set
            {

                if (value < 0 || value > 49) throw new MyDBException("MyId生成Id必须先设置MyIdNo，值范围0~49");
                _myIdNo = value;
            }
        }

        #region 生成主键

        /// <summary>
        /// 根据当前时间生成一个新的主键
        /// </summary>
        public static long NewId
        {
            get
            {
                DateTime idTime;
                return CreateCurrentTime(out idTime);
            }
        }

        /// <summary>
        /// 根据当前时间生成一个新的主键
        /// </summary>
        /// <param name="idTime">返回主键中对应的时间</param>
        /// <returns></returns>
        public static long New(out DateTime idTime)
        {
            return CreateCurrentTime(out idTime);
        }

        /// <summary>
        /// 根据当前时间生成一个新的主键
        /// </summary>
        /// <param name="time">指定主键中的时间，格式：yyyy-MM-dd，如果时间为当前时间(同一天)则根据当前时间生成</param>
        /// <returns></returns>
        public static long New()
        {
            DateTime idTime;
            return CreateCurrentTime(out idTime);
        }
        
        /// <summary>
        /// 根据指定时间生成一个新的主键
        /// </summary>
        /// <param name="pointTime">指定主键中的时间，格式：yyyy-MM-dd。为null或者时间为当前时间(同一天)则根据当前时间生成</param>
        /// <returns></returns>
        public static long New(DateTime pointTime)
        {
            if (!DateTime.Now.ToString("yyyyMMdd").Equals(pointTime.ToString("yyyyMMdd")))
            {
                //指定时间不为null，并且时间不为当天，则根据指定时间生成
                return CreatePointTime(pointTime);
            }
            else
            {
                //根据当前时间生成
                DateTime idTime;
                return CreateCurrentTime(out idTime);
            }
        }

        /// <summary>
        /// 根据当前时间进行生成
        /// </summary>
        /// <param name="now">输出Id中的时间</param>
        /// <returns></returns>
        private static long CreateCurrentTime(out DateTime now)
        {
            lock (lock_obj)
            {
                now = DateTime.Now;
                TimeSpan ts = now - _currClearTime;
                //超过一秒，序号归零
                if ((now.Second != _currClearTime.Second) || ts.Seconds > 0)
                {
                    _currSerNum = 0;
                    _currClearTime = now;
                }
                else
                {
                    //1秒以内 超过最大序号，等待下一秒再生成，以防止出现重复
                    if (_currSerNum > _maxSerNum)
                    {
                        Thread.Sleep(1000 - ts.Milliseconds + 1);
                        return CreateCurrentTime(out now);//此处用递归加锁（在c#中lock只会生效于多线程，对于单线程，自身已经是锁的所有者，并不会出现死锁）
                    }
                }
                long id = ToLong(now, _currSerNum, MyIdNo);
                _currSerNum++;
                return id;
            }
        }

        /// <summary>
        /// 根据指定时间生成
        /// 指定时间，只能指定年月日，时分秒以当前时间为准。防止同一天内程序重启序号归0后导致重复
        /// 不是同一天中的同一时间点（秒），同时序号一样时，会导致生成的Id存在重复，基于出现这种情况的概念非常非常低(百亿分之一)，则不处理。即使出现这种情况，数据库Insert也会失败
        /// </summary>
        /// <param name="pointTime"></param>
        /// <returns></returns>
        private static long CreatePointTime(DateTime pointTime)
        {
            lock (lock_obj)
            {
                DateTime now = DateTime.Now;
                //序号超过最大序号时，从下一秒开始生成，并将序号归零
                if (_pointSerNum > _maxSerNum)
                {
                    TimeSpan ts = now - _pointClearTime;
                    //两次序号清0时间间隔在同1秒内，则暂停至下一秒再开始生成
                    if (now.Second != _pointClearTime.Second || ts.Seconds < 1 || _pointClearTime == DateTime.MinValue)
                    {
                        Thread.Sleep(1000 - ts.Milliseconds + 1);
                        now = DateTime.Now;
                    }
                    _pointSerNum = 0;
                    _pointClearTime = now;
                }
                //替换指定时间中的，时分秒为当前时间的时分秒                
                DateTime createTime = new DateTime(pointTime.Year, pointTime.Month, pointTime.Day, now.Hour, now.Minute, now.Second);

                //生成Id
                long id = ToLong(createTime, _pointSerNum, (ushort)(MyIdNo + 50));//通过50~99标识与按时间顺序生成的进行区别
                _pointSerNum++;
                return id;
            }
        }

        /// <summary>
        /// 生成long
        /// </summary>
        /// <param name="myId">MyId对象</param>
        /// <returns></returns>
        private static long ToLong(DateTime time, UInt32 serNum, UInt16 myIdNo)
        {
            //距离当天起始时间秒数
            int totalSeconds = (int)(time - time.Date).TotalSeconds;

            string val = string.Format("{0}{1}{2}{3}",
                time.ToString("yyMMdd"),
                totalSeconds.ToString("00000"),
                serNum.ToString("00000"),
                myIdNo.ToString("00"));
            return long.Parse(val);
        }

        #endregion

        #region 解析主键

        /// <summary>
        /// 获取MyId中的时间。如果Id错误，会抛异常
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime GetMyIdDate(long id)
        {
            return GetMyIdDate(id.ToString());
        }

        /// <summary>
        /// 获取MyId中的时间。如果Id错误，会抛异常
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime GetMyIdDate(object val)
        {
            if (val == null) throw new MyDBException("请传入正确的Id值");
            return GetMyIdDate(val.ToString());
        }

        /// <summary>
        /// 获取MyId中的时间。如果Id错误，会抛异常
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static DateTime GetMyIdDate(string val)
        {
            if (val.Length != 18) throw new MyDBException("请传入正确的Id值");

            try
            {
                int year = 2000 + int.Parse(val.Substring(0, 2));
                int mouth = int.Parse(val.Substring(2, 2));
                int day = int.Parse(val.Substring(4, 2));
                //总共秒数
                int totalSec = int.Parse(val.Substring(6, 5));
                return new DateTime(year, mouth, day, 0, 0, 0).AddSeconds(totalSec);
            }
            catch (Exception)
            {
                throw new MyDBException("请传入正确的Id值");
            }
        }

        #endregion

        #region 时间点Id

        /// <summary>
        /// 获取 某个时间点最小的Id，用于将Id作为时间查询的值
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long GetTimeQueryId(DateTime time)
        {
            return ToLong(time, 0, 0);
        }

        /// <summary>
        /// 获取 某个时间点最小的Id，用于将Id作为时间查询的值
        /// </summary>
        /// <param name="time">格式：yyyy-MM-dd</param>
        /// <returns></returns>
        public static long GetTimeQueryId(string time, Func<DateTime, DateTime> execTime = null)
        {
            DateTime dTime = DateTime.MinValue;
            if (DateTime.TryParse(time, out dTime))
            {
                if (execTime != null)
                {
                    dTime = execTime(dTime);
                }
                return ToLong(dTime, 0, 0);
            }
            throw new MyDBException(string.Format("[{0}]不是正确的时间格式", time));
        }

        #endregion

        /// <summary>
        /// 获取一个Guid的主键
        /// </summary>
        public static string NewGuid
        {
            get
            {
                return Guid.NewGuid().ToString("N").ToUpper();
            }
        }
    }
}
