namespace Xcaciv.Command.Interface;

/// <summary>
/// Configuration options for pipeline execution behavior.
/// </summary>
public class PipelineOptions
{
    /// <summary>
    /// Configuration section name for binding from appsettings.json.
    /// </summary>
    public const string SectionName = "Xcaciv:Command:Pipeline";

    /// <summary>
    /// Maximum size of the channel queue between pipeline stages.
    /// Default: 100
    /// </summary>
    public int MaxChannelQueueSize { get; set; } = 100;

    /// <summary>
    /// Backpressure mode when the channel queue is full.
    /// Options: "Wait", "DropOldest", "DropNewest"
    /// Default: "Wait"
    /// </summary>
    public string BackpressureMode { get; set; } = "Wait";

    /// <summary>
    /// Converts the BackpressureMode string to the enum value.
    /// </summary>
    /// <returns>
    /// The corresponding PipelineBackpressureMode enum value.
    /// Defaults to Block (the safest option) for any unrecognized values.
    /// </returns>
    /// <remarks>
    /// This method implements the Sensible Defaults principle by defaulting to Block
    /// when an invalid or unrecognized value is provided. Block is the most dependable
    /// and predictable option because it prevents data loss through backpressure.
    /// 
    /// Data loss modes (DropOldest, DropNewest) MUST be explicitly configured and 
    /// should never be applied as a default. This design choice prioritizes data 
    /// integrity over failing fast on configuration errors.
    /// 
    /// Valid values (case-insensitive):
    /// - "Wait" or "Block": Blocks/waits for consumer (prevents data loss)
    /// - "DropOldest": Drops oldest items when buffer is full (explicit data loss)
    /// - "DropNewest": Drops newest items when buffer is full (explicit data loss)
    /// </remarks>
    public PipelineBackpressureMode GetBackpressureMode()
    {
        return BackpressureMode.ToUpperInvariant() switch
        {
            "WAIT" or "BLOCK" => PipelineBackpressureMode.Block,
            "DROPOLDEST" => PipelineBackpressureMode.DropOldest,
            "DROPNEWEST" => PipelineBackpressureMode.DropNewest,
            _ => PipelineBackpressureMode.Block // Sensible default: prevent data loss
        };
    }
}
