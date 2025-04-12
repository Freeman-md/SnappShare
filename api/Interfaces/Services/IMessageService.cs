namespace api.Interfaces.Services;

public interface IMessageService {
    public Task SendAsync<T>(T payload, CancellationToken cancellationToken = default);
    public Task ScheduleAsync<T>(T payload, DateTimeOffset scheduleAt);
}