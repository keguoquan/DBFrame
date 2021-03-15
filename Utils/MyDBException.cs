using System;

namespace DBFrame
{
    public class MyDBException : Exception
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public MyDBException() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="message">错误信息</param>
        public MyDBException(string message)
            : base(message)
        {
            
        }
    }
}
