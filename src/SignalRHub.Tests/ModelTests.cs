using SignalRHub.Hubs;
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
    public void DefaultGroup_IsNull()
    {
        var msg = new NotificationMessage();
        Assert.Null(msg.Group);
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
            Group = "team-finance",
            TargetUserId = "user-1"
        };

        Assert.Equal("document-upload", msg.Channel);
        Assert.Equal("upload-success", msg.EventType);
        Assert.Equal("File uploaded.", msg.Message);
        Assert.Equal(payload, msg.Payload);
        Assert.Equal("team-finance", msg.Group);
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

/// <summary>Unit tests for <see cref="NotificationHub"/> helpers.</summary>
public class NotificationHubTests
{
    [Fact]
    public void BuildGroupKey_CombinesChannelAndGroup()
    {
        var key = NotificationHub.BuildGroupKey("document-upload", "team-finance");
        Assert.Equal("document-upload:team-finance", key);
    }

    [Theory]
    [InlineData(null, "group")]
    [InlineData("", "group")]
    [InlineData("   ", "group")]
    [InlineData("channel", null)]
    [InlineData("channel", "")]
    [InlineData("channel", "   ")]
    public void BuildGroupKey_NullOrEmptyInput_Throws(string? channel, string? group)
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationHub.BuildGroupKey(channel!, group!));
    }

    [Theory]
    [InlineData("chan:nel", "group")]
    [InlineData("channel", "gr:oup")]
    public void BuildGroupKey_ColonInInput_Throws(string channel, string group)
    {
        Assert.Throws<ArgumentException>(() =>
            NotificationHub.BuildGroupKey(channel, group));
    }
}
