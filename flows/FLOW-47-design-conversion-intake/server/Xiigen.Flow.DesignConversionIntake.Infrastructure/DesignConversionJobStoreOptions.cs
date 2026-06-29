namespace Xiigen.Flow.DesignConversionIntake.Infrastructure;

public sealed class DesignConversionJobStoreOptions
{
    public const string SectionName = "DesignConversionIntake:JobStore";

    public string? DirectoryPath { get; set; }
    public string? FileName { get; set; }
}
