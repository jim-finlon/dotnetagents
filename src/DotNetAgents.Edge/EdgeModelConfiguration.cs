namespace DotNetAgents.Edge;

/// <summary>
/// Configuration for edge-optimized models.
/// </summary>
public class EdgeModelConfiguration
{
    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public EdgeModelType ModelType { get; set; } = EdgeModelType.Quantized;

    /// <summary>
    /// Gets or sets the maximum model size in MB.
    /// </summary>
    public int MaxModelSizeMB { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to use GPU acceleration.
    /// </summary>
    public bool UseGpuAcceleration { get; set; }

    /// <summary>
    /// Gets or sets the quantization level.
    /// </summary>
    public QuantizationLevel Quantization { get; set; } = QuantizationLevel.Q4;

    /// <summary>
    /// Gets or sets the maximum context length.
    /// </summary>
    public int MaxContextLength { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the model path or identifier.
    /// </summary>
    public string ModelPath { get; set; } = string.Empty;
}

/// <summary>
/// Types of edge models.
/// </summary>
public enum EdgeModelType
{
    /// <summary>
    /// Quantized model (smaller size, lower precision).
    /// </summary>
    Quantized,

    /// <summary>
    /// Pruned model (removed parameters).
    /// </summary>
    Pruned,

    /// <summary>
    /// Distilled model (knowledge distillation).
    /// </summary>
    Distilled,

    /// <summary>
    /// Custom edge model.
    /// </summary>
    Custom
}

/// <summary>
/// Quantization levels for edge models.
/// </summary>
public enum QuantizationLevel
{
    /// <summary>
    /// 8-bit quantization.
    /// </summary>
    Q8,

    /// <summary>
    /// 4-bit quantization.
    /// </summary>
    Q4,

    /// <summary>
    /// 2-bit quantization.
    /// </summary>
    Q2
}
