namespace api.Configs;

public class ServiceBusOptions {
    public const string Key = "ServiceBus";

    public string NamespaceName { get; set; } = string.Empty;
    public string QueueName { get; set; } = string.Empty;
}