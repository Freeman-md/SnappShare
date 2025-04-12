using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Moq;

namespace api.tests.Services;

public partial class ServiceBusServiceTests
{
    [Fact]
    public async Task SendAsync_ShouldSerializePayloadAndSendSuccessfully_WhenPayloadIsValid()
    {
        var payload = new { Id = 123, Name = "Test" };
        var expectedJson = JsonSerializer.Serialize(payload);

        _serviceBusSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        await _serviceBusService.SendAsync(payload);


        _serviceBusSender.Verify(s =>
            s.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
                msg.ContentType == "application/json" &&
                Encoding.UTF8.GetString(msg.Body) == expectedJson
            ),
            It.IsAny<CancellationToken>()),
            Times.Once
        );
    }


    [Fact]
    public async Task SendAsync_ShouldThrowException_WhenServiceBusClientFails()
    {
        var payload = new { Message = "Fail me" };

        _serviceBusSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ServiceBusException("Service Bus send failed", ServiceBusFailureReason.GeneralError));


        await Assert.ThrowsAsync<ServiceBusException>(() =>
            _serviceBusService.SendAsync(payload));
    }

    [Fact]
    public async Task SendAsync_ShouldThrowException_WhenSerializationFails()
    {
        var obj = new Dictionary<string, object>();
        obj["self"] = obj;


        await Assert.ThrowsAsync<JsonException>(() =>
            _serviceBusService.SendAsync(obj));
    }

    [Fact]
    public async Task SendAsync_ShouldHandleNullPayload_Gracefully()
    {
        object payload = null;

        _serviceBusSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        await _serviceBusService.SendAsync(payload);


        _serviceBusSender.Verify(s =>
            s.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
                Encoding.UTF8.GetString(msg.Body) == "null"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldSendEmptyJson_WhenPayloadIsEmptyObject()
    {
        var payload = new { };

        _serviceBusSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);


        await _serviceBusService.SendAsync(payload);


        _serviceBusSender.Verify(s =>
            s.SendMessageAsync(It.Is<ServiceBusMessage>(msg =>
                Encoding.UTF8.GetString(msg.Body) == "{}"
            ), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_ShouldRespectCancellationToken_WhenAlreadyCancelled()
    {
        var payload = new { Task = "CancelledBeforeSend" };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _serviceBusSender
            .Setup(s => s.SendMessageAsync(It.IsAny<ServiceBusMessage>(), cts.Token))
            .ThrowsAsync(new TaskCanceledException());


        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            _serviceBusService.SendAsync(payload, cts.Token));
    }





}
