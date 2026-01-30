namespace BeamQualityAnalyzer.WpfClient.Helpers;

/// <summary>
/// 数据降采样辅助类
/// 用于减少图表显示的数据点数量，提高渲染性能
/// </summary>
/// <remarks>
/// Requirement 17.3: 2D 曲线图降采样（最多 1000 点）
/// 
/// 使用场景：
/// - 当原始数据点超过 1000 个时，进行降采样
/// - 保留数据的整体趋势和关键特征
/// - 提高图表渲染性能
/// </remarks>
public static class DataDownsamplingHelper
{
    /// <summary>
    /// 默认最大数据点数
    /// </summary>
    public const int DefaultMaxPoints = 1000;
    
    /// <summary>
    /// 对数据点进行降采样
    /// </summary>
    /// <typeparam name="T">数据点类型</typeparam>
    /// <param name="dataPoints">原始数据点</param>
    /// <param name="maxPoints">最大数据点数</param>
    /// <returns>降采样后的数据点</returns>
    /// <remarks>
    /// 使用均匀采样策略：
    /// - 如果数据点数量 <= maxPoints，直接返回原始数据
    /// - 否则，按固定间隔采样，保留首尾数据点
    /// </remarks>
    public static List<T> Downsample<T>(IEnumerable<T> dataPoints, int maxPoints = DefaultMaxPoints)
    {
        if (dataPoints == null)
            throw new ArgumentNullException(nameof(dataPoints));
        
        if (maxPoints <= 0)
            throw new ArgumentException("最大数据点数必须大于 0", nameof(maxPoints));
        
        var list = dataPoints.ToList();
        
        // 如果数据点数量不超过最大值，直接返回
        if (list.Count <= maxPoints)
            return list;
        
        // 计算采样间隔
        double interval = (double)list.Count / maxPoints;
        var result = new List<T>(maxPoints);
        
        // 始终包含第一个点
        result.Add(list[0]);
        
        // 按间隔采样
        for (int i = 1; i < maxPoints - 1; i++)
        {
            int index = (int)Math.Round(i * interval);
            if (index < list.Count)
            {
                result.Add(list[index]);
            }
        }
        
        // 始终包含最后一个点
        if (list.Count > 1)
        {
            result.Add(list[list.Count - 1]);
        }
        
        return result;
    }
    
    /// <summary>
    /// 对 2D 矩阵进行降采样（用于热力图和 3D 可视化）
    /// </summary>
    /// <param name="matrix">原始矩阵</param>
    /// <param name="targetRows">目标行数</param>
    /// <param name="targetCols">目标列数</param>
    /// <returns>降采样后的矩阵</returns>
    /// <remarks>
    /// 使用最近邻插值策略：
    /// - 如果原始矩阵尺寸 <= 目标尺寸，直接返回原始矩阵
    /// - 否则，按比例缩小矩阵
    /// </remarks>
    public static double[,] DownsampleMatrix(double[,] matrix, int targetRows, int targetCols)
    {
        if (matrix == null)
            throw new ArgumentNullException(nameof(matrix));
        
        if (targetRows <= 0 || targetCols <= 0)
            throw new ArgumentException("目标尺寸必须大于 0");
        
        int sourceRows = matrix.GetLength(0);
        int sourceCols = matrix.GetLength(1);
        
        // 如果原始矩阵尺寸不超过目标尺寸，直接返回
        if (sourceRows <= targetRows && sourceCols <= targetCols)
            return matrix;
        
        // 创建目标矩阵
        var result = new double[targetRows, targetCols];
        
        // 计算缩放比例
        double rowRatio = (double)sourceRows / targetRows;
        double colRatio = (double)sourceCols / targetCols;
        
        // 最近邻插值
        for (int i = 0; i < targetRows; i++)
        {
            for (int j = 0; j < targetCols; j++)
            {
                int sourceRow = (int)Math.Round(i * rowRatio);
                int sourceCol = (int)Math.Round(j * colRatio);
                
                // 边界检查
                sourceRow = Math.Min(sourceRow, sourceRows - 1);
                sourceCol = Math.Min(sourceCol, sourceCols - 1);
                
                result[i, j] = matrix[sourceRow, sourceCol];
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 对 2D 矩阵进行降采样（使用平均值策略）
    /// </summary>
    /// <param name="matrix">原始矩阵</param>
    /// <param name="targetRows">目标行数</param>
    /// <param name="targetCols">目标列数</param>
    /// <returns>降采样后的矩阵</returns>
    /// <remarks>
    /// 使用平均值策略：
    /// - 将原始矩阵划分为多个区域
    /// - 每个区域的平均值作为目标矩阵的一个元素
    /// - 比最近邻插值更平滑，但计算量稍大
    /// </remarks>
    public static double[,] DownsampleMatrixAverage(double[,] matrix, int targetRows, int targetCols)
    {
        if (matrix == null)
            throw new ArgumentNullException(nameof(matrix));
        
        if (targetRows <= 0 || targetCols <= 0)
            throw new ArgumentException("目标尺寸必须大于 0");
        
        int sourceRows = matrix.GetLength(0);
        int sourceCols = matrix.GetLength(1);
        
        // 如果原始矩阵尺寸不超过目标尺寸，直接返回
        if (sourceRows <= targetRows && sourceCols <= targetCols)
            return matrix;
        
        // 创建目标矩阵
        var result = new double[targetRows, targetCols];
        
        // 计算每个目标像素对应的源区域大小
        double rowBlockSize = (double)sourceRows / targetRows;
        double colBlockSize = (double)sourceCols / targetCols;
        
        // 对每个目标像素计算平均值
        for (int i = 0; i < targetRows; i++)
        {
            for (int j = 0; j < targetCols; j++)
            {
                // 计算源区域边界
                int rowStart = (int)(i * rowBlockSize);
                int rowEnd = (int)((i + 1) * rowBlockSize);
                int colStart = (int)(j * colBlockSize);
                int colEnd = (int)((j + 1) * colBlockSize);
                
                // 边界检查
                rowEnd = Math.Min(rowEnd, sourceRows);
                colEnd = Math.Min(colEnd, sourceCols);
                
                // 计算区域平均值
                double sum = 0;
                int count = 0;
                
                for (int r = rowStart; r < rowEnd; r++)
                {
                    for (int c = colStart; c < colEnd; c++)
                    {
                        sum += matrix[r, c];
                        count++;
                    }
                }
                
                result[i, j] = count > 0 ? sum / count : 0;
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// 计算降采样后的数据点数量
    /// </summary>
    /// <param name="originalCount">原始数据点数量</param>
    /// <param name="maxPoints">最大数据点数</param>
    /// <returns>降采样后的数据点数量</returns>
    public static int CalculateDownsampledCount(int originalCount, int maxPoints = DefaultMaxPoints)
    {
        return Math.Min(originalCount, maxPoints);
    }
    
    /// <summary>
    /// 判断是否需要降采样
    /// </summary>
    /// <param name="dataPointCount">数据点数量</param>
    /// <param name="maxPoints">最大数据点数</param>
    /// <returns>如果需要降采样返回 true，否则返回 false</returns>
    public static bool NeedsDownsampling(int dataPointCount, int maxPoints = DefaultMaxPoints)
    {
        return dataPointCount > maxPoints;
    }
}

