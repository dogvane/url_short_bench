using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace url_short.common
{
    /// <summary>
    /// URL短链生成和解析接口
    /// 用于将长URL的ID编码为短代码，以及将短代码解码回原始ID
    /// </summary>
    public interface IShortCodeGen
    {
        /// <summary>
        /// 将数字ID编码为短代码
        /// </summary>
        /// <param name="number">要编码的数字ID</param>
        /// <returns>生成的短代码字符串</returns>
        public string Encode(long number);
        
        /// <summary>
        /// 将短代码解码为原始数字ID
        /// </summary>
        /// <param name="code">要解码的短代码</param>
        /// <returns>原始的数字ID</returns>
        public long Decode(string code);
    }
}