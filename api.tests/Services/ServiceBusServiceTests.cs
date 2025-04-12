using System;
using api.Configs;
using api.Services;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Moq;

namespace api.tests.Services;

public partial class ServiceBusServiceTests
{
    private readonly Mock<ServiceBusClient> _serviceBusClient;
    private readonly Mock<ServiceBusSender> _serviceBusSender;
    private readonly Mock<IConfiguration> _configuration;

    private readonly Mock<IConfigurationSection> _serviceBusSection;

    private readonly ServiceBusService _serviceBusService;

    public ServiceBusServiceTests()
    {
        _serviceBusClient = new Mock<ServiceBusClient>();
        _serviceBusSender = new Mock<ServiceBusSender>();
        _configuration = new Mock<IConfiguration>();
        _serviceBusSection = new Mock<IConfigurationSection>();

        var options = new ServiceBusOptions
        {
            QueueName = "test-queue",
            NamespaceName = "test-namespace",
        };

        _configuration
            .Setup(c => c.GetSection(ServiceBusOptions.Key))
            .Returns(_serviceBusSection.Object);

        _serviceBusSection
            .Setup(s => s.Get<ServiceBusOptions>())
            .Returns(options);

        _serviceBusClient
            .Setup(c => c.CreateSender(options.QueueName))
            .Returns(_serviceBusSender.Object);

        _serviceBusService = new ServiceBusService(_serviceBusClient.Object, _configuration.Object);
    }
}
