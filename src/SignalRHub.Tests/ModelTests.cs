using SignalRHub.Models;

namespace SignalRHub.Tests;

/// <summary>Unit tests for the <see cref="NotificationMessage"/> model.</summary>
public class NotificationMessageTests
{
    [Fact]
    public void DefaultTimestamp_IsRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var msg = new NotificationMessage();
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(msg.Timestamp, before, after);
    }

    [Fact]
    public void DefaultChannel_IsEmptyString()
    {
        var msg = new NotificationMessage();
        Assert.Equal(string.Empty, msg.Channel);
    }

    [Fact]
    public void DefaultTargetUserId_IsNull()
    {
        var msg = new NotificationMessage();
        Assert.Null(msg.TargetUserId);
    }

    [Fact]
    public void CanSetAllProperties()
    {
        var payload = new { id = 1 };
        var msg = new NotificationMessage
        {
            Channel = "document-upload",
            EventType = "upload-success",
            Message = "File uploaded.",
            Payload = payload,
            TargetUserId = "user-1"
        };

        Assert.Equal("document-upload", msg.Channel);
        Assert.Equal("upload-success", msg.EventType);
        Assert.Equal("File uploaded.", msg.Message);
        Assert.Equal(payload, msg.Payload);
        Assert.Equal("user-1", msg.TargetUserId);
    }
}

/// <summary>Unit tests for the <see cref="HubResponse"/> model.</summary>
public class HubResponseTests
{
    [Fact]
    public void Ok_SetsSuccessTrue_And_Message()
    {
        var response = HubResponse.Ok("Published", new { count = 1 });

        Assert.True(response.Success);
        Assert.Equal("Published", response.Message);
        Assert.NotNull(response.Data);
    }

    [Fact]
    public void Fail_SetsSuccessFalse_And_Message()
    {
        var response = HubResponse.Fail("Bad request.");

        Assert.False(response.Success);
        Assert.Equal("Bad request.", response.Message);
        Assert.Null(response.Data);
    }
}
