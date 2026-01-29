namespace SnowblindModPlayer.Core.Services;

public enum ImportProgressStage
{
    Starting,
    Processing,
    Imported,
    Skipped,
    Failed,
    GeneratingThumbnails,
    Completed
}

public class ImportProgressEventArgs : EventArgs
{
    public int Total { get; init; }
    public int Processed { get; init; }
    public string? CurrentPath { get; init; }
    public ImportProgressStage Stage { get; init; }
    public string? Message { get; init; }
}
