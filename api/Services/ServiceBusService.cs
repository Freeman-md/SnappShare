using api.Configs;
using api.Interfaces.Services;
using Azure.Messaging.ServiceBus;

namespace api.Services;

public class ServiceBusService : IMessageService
{
    private readonly ServiceBusSender _serviceBusSender;

    public ServiceBusService(ServiceBusClient serviceBusClient, IConfiguration config)
    {
        var options = config.GetSection(ServiceBusOptions.Key).Get<ServiceBusOptions>();

        _serviceBusSender = serviceBusClient.CreateSender(options.QueueName);
    }
    public Task ScheduleAsync<T>(T payload, DateTimeOffset scheduleAt)
    {
        throw new NotImplementedException();
    }

    public Task SendAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}