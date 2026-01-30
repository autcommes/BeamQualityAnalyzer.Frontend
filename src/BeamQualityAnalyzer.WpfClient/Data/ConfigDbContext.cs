using BeamQualityAnalyzer.WpfClient.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace BeamQualityAnalyzer.WpfClient.Data;

/// <summary>
/// 配置数据库上下文
/// 管理应用程序配置的持久化
/// </summary>
/// <remarks>
/// 数据库位置：%APPDATA%\BeamQualityAnalyzer\config.db
/// </remarks>
public class ConfigDbContext : DbContext
{
    private readonly string _dbPath;
    
    /// <summary>
    /// 测试数据库路径（仅用于单元测试）
    /// </summary>
    public static string? TestDatabasePath { get; set; }
    
    /// <summary>
    /// 应用程序配置表
    /// </summary>
    public DbSet<AppSettings> AppSettings { get; set; } = null!;
    
    /// <summary>
    /// 配置历史记录表
    /// </summary>
    public DbSet<AppSettingsHistory> AppSettingsHistory { get; set; } = null!;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigDbContext()
    {
        // 如果设置了测试数据库路径，使用测试路径
        if (!string.IsNullOrEmpty(TestDatabasePath))
        {
            _dbPath = TestDatabasePath;
            
            var directory = Path.GetDirectoryName(_dbPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
        else
        {
            // 生产环境使用 AppData 路径
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var directory = Path.Combine(appDataPath, "BeamQualityAnalyzer");
            
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            _dbPath = Path.Combine(directory, "config.db");
        }
    }
    
    /// <summary>
    /// 构造函数（用于测试）
    /// </summary>
    /// <param name="dbPath">数据库文件路径</param>
    public ConfigDbContext(string dbPath)
    {
        _dbPath = dbPath;
        
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
    
    /// <summary>
    /// 配置数据库连接
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
        }
    }
    
    /// <summary>
    /// 配置数据库模型
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置 AppSettings 表（单行表）
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            // 添加检查约束：只允许 Id = 1
            entity.ToTable(t => t.HasCheckConstraint("CK_AppSettings_SingleRow", "Id = 1"));
            
            // 设置默认值
            entity.Property(e => e.LastModified)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
        
        // 配置 AppSettingsHistory 表
        modelBuilder.Entity<AppSettingsHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.ModifiedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            
            // 创建索引
            entity.HasIndex(e => e.ModifiedAt);
        });
    }
}
