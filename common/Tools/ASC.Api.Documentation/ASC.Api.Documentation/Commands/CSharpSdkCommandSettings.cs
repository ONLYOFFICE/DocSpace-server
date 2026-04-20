namespace ASC.Api.Documentation.Commands;

public sealed class CSharpSdkCommandSettings : CommandSettings
{
    [CommandOption("-c|--configuration <CONFIGURATION>")]
    [Description("Build configuration used for dotnet build and package lookup.")]
    public string Configuration { get; set; } = "Debug";

    public override ValidationResult Validate()
    {
        Configuration = Configuration?.Trim() ?? string.Empty;

        return string.IsNullOrWhiteSpace(Configuration)
            ? ValidationResult.Error("Configuration cannot be empty.")
            : ValidationResult.Success();
    }
}
