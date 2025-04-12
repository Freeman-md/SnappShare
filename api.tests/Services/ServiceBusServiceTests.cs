using System;
using api.Configs;
using api.Interfaces.Services;
using api.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Moq;

namespace api.tests.Services;

public partial class ServiceBusServiceTests
{
    private readonly Mock<ServiceBusClient> _serviceBusClient;
    private readonly Mock<ServiceBusSender> _serviceBusSender;
    private readonly IMessageService _serviceBusService;

    public ServiceBusServiceTests()
    {
        _serviceBusClient = new Mock<ServiceBusClient>();
        _serviceBusSender = new Mock<ServiceBusSender>();

        var options = new ServiceBusOptions
        {
            QueueName = "test-queue",
            NamespaceName = "test-namespace",
        };

        var inMemorySettings = new Dictionary<string, string>
    {
        { "ServiceBus:QueueName", options.QueueName },
        { "ServiceBus:NamespaceName", options.NamespaceName }
    };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings.Cast<KeyValuePair<string, string?>>())
            .Build();

        _serviceBusService = new ServiceBusService(_serviceBusClient.Object, configuration);
    }
}
