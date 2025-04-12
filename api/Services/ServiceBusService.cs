using System.Text;
using System.Text.Json;
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
    public async Task SendAsync<T>(T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ContentType = "application/json"
            };

            await _serviceBusSender.SendMessageAsync(message, cancellationToken);
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task ScheduleAsync<T>(T payload, DateTimeOffset scheduleAt)
    {
        if (scheduleAt <= DateTimeOffset.UtcNow)
        {
            throw new ArgumentException("Schedule time must be in the future.");
        }

        try
        {
            var json = JsonSerializer.Serialize(payload);
            var message = new ServiceBusMessage(Encoding.UTF8.GetBytes(json))
            {
                ContentType = "application/json"
            };

            await _serviceBusSender.ScheduleMessageAsync(message, scheduleAt.UtcDateTime);
        }
        catch (Exception)
        {
            throw;
        }
    }
}