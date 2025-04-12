namespace api.Interfaces.Services;

public interface IServiceBusService {
    public Task SendAsync<T>(T payload, CancellationToken cancellationToken = default);
    public Task ScheduleAsync<T>(T payload, DateTimeOffset scheduleAt);
}