using BeamQualityAnalyzer.WpfClient.Data;
using BeamQualityAnalyzer.WpfClient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeamQualityAnalyzer.WpfClient.Services;

/// <summary>
/// 配置服务实现
/// 使用 SQLite 数据库存储应用程序配置
/// </summary>
/// <remarks>
/// Requirements:
/// - 13.6: 配置持久化到本地数据库
/// - 13.7: 应用程序启动时加载配置
/// </remarks>
public class SettingsService : ISettingsService
{
    private readonly ILogger<SettingsService> _logger;
    private AppSettings? _cachedSettings;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志记录器</param>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
    
    /// <summary>
    /// 加载应用配置
    /// </summary>
    public async Task<AppSettings> LoadSettingsAsync()
    {
        try
        {
            // 如果已缓存，直接返回
            if (_cachedSettings != null)
            {
                _logger.LogDebug("从缓存加载配置");
                return _cachedSettings;
            }
            
            using var db = new ConfigDbContext();
            
            // 确保数据库已创建
            await db.Database.EnsureCreatedAsync();
            
            // 查询配置
            var settings = await db.AppSettings.FirstOrDefaultAsync();
            
            if (settings == null)
            {
                // 首次运行，创建默认配置
                _logger.LogInformation("首次运行，创建默认配置");
                
                settings = new AppSettings();
                db.AppSettings.Add(settings);
                await db.SaveChangesAsync();
                
                // 记录初始配置历史
                await SaveHistoryAsync(db, settings, "初始配置");
            }
            
            _cachedSettings = settings;
            _logger.LogInformation("配置加载成功");
            
            return settings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "加载配置失败");
            
            // 返回默认配置
            return new AppSettings();
        }
    }
    
    /// <summary>
    /// 保存应用配置
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings, string? changeDescription = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }
        
        try
        {
            using var db = new ConfigDbContext();
            
            // 更新最后修改时间
            settings.LastModified = DateTime.Now;
            
            // 更新配置
            db.AppSettings.Update(settings);
            await db.SaveChangesAsync();
            
            // 记录配置历史
            await SaveHistoryAsync(db, settings, changeDescription ?? "配置更新");
            
            // 更新缓存
            _cachedSettings = settings;
            
            _logger.LogInformation("配置保存成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置失败");
            throw;
        }
    }
    
    /// <summary>
    /// 测试远程数据库连接
    /// </summary>
    public async Task<bool> TestRemoteDatabaseConnectionAsync(string connectionString, string databaseType)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }
        
        try
        {
            _logger.LogInformation("测试 {DatabaseType} 数据库连接", databaseType);
            
            if (databaseType == "MySQL")
            {
                // 测试 MySQL 连接
                var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
                
                using var context = new DbContext(optionsBuilder.Options);
                await context.Database.CanConnectAsync();
                
                _logger.LogInformation("MySQL 数据库连接成功");
                return true;
            }
            else if (databaseType == "SqlServer")
            {
                // 测试 SQL Server 连接
                var optionsBuilder = new DbContextOptionsBuilder<DbContext>();
                optionsBuilder.UseSqlServer(connectionString);
                
                using var context = new DbContext(optionsBuilder.Options);
                await context.Database.CanConnectAsync();
                
                _logger.LogInformation("SQL Server 数据库连接成功");
                return true;
            }
            else
            {
                _logger.LogWarning("不支持的数据库类型: {DatabaseType}", databaseType);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "数据库连接测试失败");
            return false;
        }
    }
    
    /// <summary>
    /// 获取配置历史记录
    /// </summary>
    public async Task<List<AppSettingsHistory>> GetSettingsHistoryAsync(int count = 10)
    {
        try
        {
            using var db = new ConfigDbContext();
            
            var history = await db.AppSettingsHistory
                .OrderByDescending(h => h.ModifiedAt)
                .Take(count)
                .ToListAsync();
            
            _logger.LogInformation("获取到 {Count} 条配置历史记录", history.Count);
            
            return history;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取配置历史失败");
            return new List<AppSettingsHistory>();
        }
    }
    
    /// <summary>
    /// 回滚到历史配置
    /// </summary>
    public async Task<bool> RollbackToHistoryAsync(int historyId)
    {
        try
        {
            using var db = new ConfigDbContext();
            
            // 查找历史记录
            var history = await db.AppSettingsHistory.FindAsync(historyId);
            
            if (history == null)
            {
                _logger.LogWarning("历史记录不存在: {HistoryId}", historyId);
                return false;
            }
            
            // 反序列化配置快照
            var settings = JsonSerializer.Deserialize<AppSettings>(history.SettingsSnapshot);
            
            if (settings == null)
            {
                _logger.LogError("配置快照反序列化失败");
                return false;
            }
            
            // 保存配置（会自动记录新的历史）
            await SaveSettingsAsync(settings, $"回滚到历史记录 #{historyId}");
            
            _logger.LogInformation("成功回滚到历史记录 #{HistoryId}", historyId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "回滚配置失败");
            return false;
        }
    }
    
    /// <summary>
    /// 保存配置历史记录
    /// </summary>
    private async Task SaveHistoryAsync(ConfigDbContext db, AppSettings settings, string? description)
    {
        try
        {
            var snapshot = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            var history = new AppSettingsHistory
            {
                SettingsSnapshot = snapshot,
                ModifiedBy = Environment.UserName,
                ModifiedAt = DateTime.Now,
                ChangeDescription = description
            };
            
            db.AppSettingsHistory.Add(history);
            await db.SaveChangesAsync();
            
            _logger.LogDebug("配置历史记录已保存: {Description}", description);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存配置历史失败");
            // 不抛出异常，避免影响主流程
        }
    }
}
