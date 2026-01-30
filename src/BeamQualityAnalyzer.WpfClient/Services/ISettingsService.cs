using BeamQualityAnalyzer.WpfClient.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BeamQualityAnalyzer.WpfClient.Services;

/// <summary>
/// 配置服务接口
/// 管理应用程序配置的加载、保存和历史记录
/// </summary>
/// <remarks>
/// Requirements:
/// - 13.6: 配置持久化到本地数据库
/// - 13.7: 应用程序启动时加载配置
/// </remarks>
public interface ISettingsService
{
    /// <summary>
    /// 加载应用配置
    /// </summary>
    /// <returns>应用配置对象</returns>
    /// <remarks>
    /// 如果配置不存在，将创建默认配置
    /// </remarks>
    Task<AppSettings> LoadSettingsAsync();
    
    /// <summary>
    /// 保存应用配置
    /// </summary>
    /// <param name="settings">要保存的配置</param>
    /// <param name="changeDescription">变更描述（可选）</param>
    /// <remarks>
    /// 保存时会自动记录配置历史
    /// </remarks>
    Task SaveSettingsAsync(AppSettings settings, string? changeDescription = null);
    
    /// <summary>
    /// 测试远程数据库连接
    /// </summary>
    /// <param name="connectionString">连接字符串</param>
    /// <param name="databaseType">数据库类型（MySQL, SqlServer）</param>
    /// <returns>连接是否成功</returns>
    Task<bool> TestRemoteDatabaseConnectionAsync(string connectionString, string databaseType);
    
    /// <summary>
    /// 获取配置历史记录
    /// </summary>
    /// <param name="count">获取的记录数量</param>
    /// <returns>配置历史记录列表</returns>
    Task<List<AppSettingsHistory>> GetSettingsHistoryAsync(int count = 10);
    
    /// <summary>
    /// 回滚到历史配置
    /// </summary>
    /// <param name="historyId">历史记录 ID</param>
    /// <returns>回滚是否成功</returns>
    Task<bool> RollbackToHistoryAsync(int historyId);
}
