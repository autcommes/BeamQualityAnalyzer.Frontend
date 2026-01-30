using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BeamQualityAnalyzer.WpfClient.Models;

/// <summary>
/// 应用程序配置历史记录
/// 用于审计和回滚功能
/// </summary>
[Table("AppSettingsHistory")]
public class AppSettingsHistory
{
    /// <summary>
    /// 主键
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    /// <summary>
    /// 配置快照（JSON 格式）
    /// </summary>
    [Required]
    public string SettingsSnapshot { get; set; } = string.Empty;
    
    /// <summary>
    /// 修改人
    /// </summary>
    [MaxLength(100)]
    public string? ModifiedBy { get; set; }
    
    /// <summary>
    /// 修改时间
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
    
    /// <summary>
    /// 变更描述
    /// </summary>
    [MaxLength(500)]
    public string? ChangeDescription { get; set; }
}
