using BeamQualityAnalyzer.Contracts.Dtos;
using BeamQualityAnalyzer.Contracts.Messages;

namespace BeamQualityAnalyzer.ApiClient.Extensions;

/// <summary>
/// DTO 映射扩展方法
/// </summary>
public static class DtoMappingExtensions
{
    // ==================== RawDataPoint 映射 ====================
    
    /// <summary>
    /// 将 RawDataPointDto 转换为领域模型（如果需要）
    /// </summary>
    public static RawDataPoint ToModel(this RawDataPointDto dto)
    {
        return new RawDataPoint
        {
            DetectorPosition = dto.DetectorPosition,
            BeamDiameterX = dto.BeamDiameterX,
            BeamDiameterY = dto.BeamDiameterY,
            Timestamp = dto.Timestamp
        };
    }
    
    /// <summary>
    /// 将领域模型转换为 RawDataPointDto
    /// </summary>
    public static RawDataPointDto ToDto(this RawDataPoint model)
    {
        return new RawDataPointDto
        {
            DetectorPosition = model.DetectorPosition,
            BeamDiameterX = model.BeamDiameterX,
            BeamDiameterY = model.BeamDiameterY,
            Timestamp = model.Timestamp
        };
    }
    
    // ==================== BeamAnalysisResult 映射 ====================
    
    /// <summary>
    /// 将 BeamAnalysisResultDto 转换为领域模型
    /// </summary>
    public static BeamAnalysisResult ToModel(this BeamAnalysisResultDto dto)
    {
        return new BeamAnalysisResult
        {
            MSquaredX = dto.MSquaredX,
            MSquaredY = dto.MSquaredY,
            MSquaredGlobal = dto.MSquaredGlobal,
            BeamWaistPositionX = dto.BeamWaistPositionX,
            BeamWaistPositionY = dto.BeamWaistPositionY,
            BeamWaistDiameterX = dto.BeamWaistDiameterX,
            BeamWaistDiameterY = dto.BeamWaistDiameterY,
            PeakPositionX = dto.PeakPositionX,
            PeakPositionY = dto.PeakPositionY,
            FittedCurveX = dto.FittedCurveX,
            FittedCurveY = dto.FittedCurveY
        };
    }
    
    /// <summary>
    /// 将领域模型转换为 BeamAnalysisResultDto
    /// </summary>
    public static BeamAnalysisResultDto ToDto(this BeamAnalysisResult model)
    {
        return new BeamAnalysisResultDto
        {
            MSquaredX = model.MSquaredX,
            MSquaredY = model.MSquaredY,
            MSquaredGlobal = model.MSquaredGlobal,
            BeamWaistPositionX = model.BeamWaistPositionX,
            BeamWaistPositionY = model.BeamWaistPositionY,
            BeamWaistDiameterX = model.BeamWaistDiameterX,
            BeamWaistDiameterY = model.BeamWaistDiameterY,
            PeakPositionX = model.PeakPositionX,
            PeakPositionY = model.PeakPositionY,
            FittedCurveX = model.FittedCurveX,
            FittedCurveY = model.FittedCurveY
        };
    }
    
    // ==================== MeasurementRecord 映射 ====================
    
    /// <summary>
    /// 将 MeasurementRecordDto 转换为领域模型
    /// </summary>
    public static MeasurementRecord ToModel(this MeasurementRecordDto dto)
    {
        return new MeasurementRecord
        {
            Id = dto.Id,
            MeasurementTime = dto.MeasurementTime,
            DeviceInfo = dto.DeviceInfo,
            Status = dto.Status,
            Notes = dto.Notes,
            RawDataPoints = dto.RawDataPoints?.Select(p => p.ToModel()).ToList(),
            AnalysisResult = dto.AnalysisResult?.ToModel(),
            CreatedAt = dto.CreatedAt
        };
    }
    
    /// <summary>
    /// 将领域模型转换为 MeasurementRecordDto
    /// </summary>
    public static MeasurementRecordDto ToDto(this MeasurementRecord model)
    {
        return new MeasurementRecordDto
        {
            Id = model.Id,
            MeasurementTime = model.MeasurementTime,
            DeviceInfo = model.DeviceInfo,
            Status = model.Status,
            Notes = model.Notes,
            RawDataPoints = model.RawDataPoints?.Select(p => p.ToDto()).ToList(),
            AnalysisResult = model.AnalysisResult?.ToDto(),
            CreatedAt = model.CreatedAt
        };
    }
    
    // ==================== AnalysisParameters 映射 ====================
    
    /// <summary>
    /// 将 AnalysisParametersDto 转换为领域模型
    /// </summary>
    public static AnalysisParameters ToModel(this AnalysisParametersDto dto)
    {
        return new AnalysisParameters
        {
            Magnification = dto.Magnification,
            Line86Result = dto.Line86Result,
            SecondOrderFitResult = dto.SecondOrderFitResult,
            Wavelength = dto.Wavelength,
            MinDataPoints = dto.MinDataPoints,
            FitTolerance = dto.FitTolerance
        };
    }
    
    /// <summary>
    /// 将领域模型转换为 AnalysisParametersDto
    /// </summary>
    public static AnalysisParametersDto ToDto(this AnalysisParameters model)
    {
        return new AnalysisParametersDto
        {
            Magnification = model.Magnification,
            Line86Result = model.Line86Result,
            SecondOrderFitResult = model.SecondOrderFitResult,
            Wavelength = model.Wavelength,
            MinDataPoints = model.MinDataPoints,
            FitTolerance = model.FitTolerance
        };
    }
}

// ==================== 领域模型定义（客户端侧） ====================

/// <summary>
/// 原始数据点（客户端领域模型）
/// </summary>
public class RawDataPoint
{
    public double DetectorPosition { get; set; }
    public double BeamDiameterX { get; set; }
    public double BeamDiameterY { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// 光束分析结果（客户端领域模型）
/// </summary>
public class BeamAnalysisResult
{
    public double MSquaredX { get; set; }
    public double MSquaredY { get; set; }
    public double MSquaredGlobal { get; set; }
    public double BeamWaistPositionX { get; set; }
    public double BeamWaistPositionY { get; set; }
    public double BeamWaistDiameterX { get; set; }
    public double BeamWaistDiameterY { get; set; }
    public double PeakPositionX { get; set; }
    public double PeakPositionY { get; set; }
    public double[]? FittedCurveX { get; set; }
    public double[]? FittedCurveY { get; set; }
}

/// <summary>
/// 测量记录（客户端领域模型）
/// </summary>
public class MeasurementRecord
{
    public int Id { get; set; }
    public DateTime MeasurementTime { get; set; }
    public string? DeviceInfo { get; set; }
    public string Status { get; set; } = "Complete";
    public string? Notes { get; set; }
    public List<RawDataPoint>? RawDataPoints { get; set; }
    public BeamAnalysisResult? AnalysisResult { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 分析参数（客户端领域模型）
/// </summary>
public class AnalysisParameters
{
    public double Magnification { get; set; } = 1.0;
    public double Line86Result { get; set; }
    public double SecondOrderFitResult { get; set; }
    public double Wavelength { get; set; } = 632.8;
    public int MinDataPoints { get; set; } = 10;
    public double FitTolerance { get; set; } = 0.001;
}
