using System.Threading.Tasks;

namespace v2
{
    public interface IShortUrlRepository
    {
        /// <summary>
        /// 根据短码获取原始URL信息
        /// </summary>
        /// <param name="alias">短码</param>
        /// <returns>包含ID、URL和过期时间的元组</returns>
        Task<(long id, string url, long expire)> GetUrlByAliasAsync(string alias);

        /// <summary>
        /// 创建短链接
        /// </summary>
        /// <param name="url">原始URL</param>
        /// <param name="expireSeconds">过期时间（秒），null表示永不过期</param>
        /// <returns>包含ID和短码的元组</returns>
        (long id, string alias) CreateShortLink(string url, int? expireSeconds);

        /// <summary>
        /// 获取当前最大ID（用于监控和调试）
        /// </summary>
        /// <returns>当前最大ID</returns>
        long GetCurrentMaxId();
    }
}
