using System;

namespace api.Configs;

public class StorageOptions
{
    public const string Key = "Storage";

    public string AccountName { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}
