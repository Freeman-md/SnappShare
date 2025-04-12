using System;
using System.Text;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Moq;

namespace api.tests.Services;

public partial class ServiceBusServiceTests
{
    [Fact]
    public async Task ScheduleAsync_ShouldScheduleMessageSuccessfully_WhenPayloadAndTimeAreValid()
    {
        var payload = new { Event = "UploadComplete", FileId = "abc123" };
        var scheduleTime = DateTimeOffset.UtcNow.AddMinutes(10);
        var expectedJson = JsonSerializer.Serialize(payload);

        _serviceBusSender
            .Setup(s => s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), scheduleTime.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(123456L);


        await _serviceBusService.ScheduleAsync(payload, scheduleTime);


        _serviceBusSender.Verify(s =>
            s.ScheduleMessageAsync(It.Is<ServiceBusMessage>(msg =>
                msg.ContentType == "application/json" &&
                Encoding.UTF8.GetString(msg.Body) == expectedJson
            ),
            scheduleTime.UtcDateTime,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldThrowException_WhenScheduleFails()
    {
        var payload = new { Status = "Failure" };
        var scheduleTime = DateTimeOffset.UtcNow.AddMinutes(5);

        _serviceBusSender
            .Setup(s => s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), scheduleTime.UtcDateTime, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Scheduling failed", ServiceBusFailureReason.GeneralError));


        await Assert.ThrowsAsync<ServiceBusException>(() =>
            _serviceBusService.ScheduleAsync(payload, scheduleTime));
    }

    [Fact]
    public async Task ScheduleAsync_ShouldThrowException_WhenPayloadSerializationFails()
    {
        var payload = new Dictionary<string, object>();
        payload["self"] = payload;

        var scheduleTime = DateTimeOffset.UtcNow.AddMinutes(5);


        await Assert.ThrowsAsync<JsonException>(() =>
            _serviceBusService.ScheduleAsync(payload, scheduleTime));

        _serviceBusSender.Verify(s =>
            s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldThrow_WhenPayloadIsNull()
    {
        object payload = null!;
        var scheduleTime = DateTimeOffset.UtcNow.AddMinutes(10);

        _serviceBusSender
            .Setup(s => s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), scheduleTime.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);


        await _serviceBusService.ScheduleAsync(payload, scheduleTime);


        _serviceBusSender.Verify(s =>
            s.ScheduleMessageAsync(It.Is<ServiceBusMessage>(msg =>
                Encoding.UTF8.GetString(msg.Body) == "null"
            ),
            scheduleTime.UtcDateTime,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldScheduleMessage_WhenGivenFarFutureDate()
    {
        var payload = new { Action = "ExpireAfter" };
        var futureDate = DateTimeOffset.UtcNow.AddYears(10);

        _serviceBusSender
            .Setup(s => s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), futureDate.UtcDateTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(999L);


        await _serviceBusService.ScheduleAsync(payload, futureDate);


        _serviceBusSender.Verify(s =>
            s.ScheduleMessageAsync(It.IsAny<ServiceBusMessage>(), futureDate.UtcDateTime, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ScheduleAsync_ShouldThrowException_WhenScheduledTimeIsInThePast()
    {
        var payload = new { Event = "PastAttempt" };
        var pastTime = DateTimeOffset.UtcNow.AddMinutes(-10);


        await Assert.ThrowsAsync<ArgumentException>(() =>
            _serviceBusService.ScheduleAsync(payload, pastTime));
    }





}
