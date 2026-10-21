using Ttw.Services.Usb;

namespace Ttw.Tests;

public class WatchProtocolTests
{
    [Fact]
    public void BuildRequestFramesThePayload()
    {
        byte[] packet = WatchProtocol.BuildRequest(
            WatchProtocol.OpenFileRead,
            [0x00, 0x00, 0x91, 0x00],
            counter: 7,
            packetSize: 64);

        Assert.Equal(64, packet.Length);
        Assert.Equal(0x09, packet[0]);
        Assert.Equal(6, packet[1]);
        Assert.Equal(7, packet[2]);
        Assert.Equal(WatchProtocol.OpenFileRead, packet[3]);
        Assert.Equal([0x00, 0x00, 0x91, 0x00], packet[4..8]);
        Assert.All(packet[8..], value => Assert.Equal(0, value));
    }

    [Fact]
    public void BuildRequestWithoutPayload()
    {
        byte[] packet = WatchProtocol.BuildRequest(WatchProtocol.FindClose, [], counter: 0, packetSize: 256);

        Assert.Equal(0x09, packet[0]);
        Assert.Equal(2, packet[1]);
        Assert.Equal(0, packet[2]);
        Assert.Equal(WatchProtocol.FindClose, packet[3]);
    }

    [Fact]
    public void TryParseResponseAcceptsAWellFormedResponse()
    {
        byte[] buffer = [0x01, 0x06, 0x03, WatchProtocol.GetFileSize, 0xAA, 0xBB, 0xCC, 0xDD];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 4,
            out byte[] payload,
            out string? error);

        Assert.True(valid);
        Assert.Null(error);
        Assert.Equal([0xAA, 0xBB, 0xCC, 0xDD], payload);
    }

    [Fact]
    public void TryParseResponseAcceptsAVariableLengthResponse()
    {
        byte[] buffer = [0x01, 0x04, 0x00, WatchProtocol.ResetGpsProcessor, 0x11, 0x22];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 0,
            expectedMessageType: WatchProtocol.ResetGpsProcessor,
            expectedPayloadLength: null,
            out byte[] payload,
            out string? error);

        Assert.True(valid);
        Assert.Null(error);
        Assert.Equal([0x11, 0x22], payload);
    }

    [Fact]
    public void TryParseResponseRejectsAWrongMarker()
    {
        byte[] buffer = [0x02, 0x06, 0x03, WatchProtocol.GetFileSize, 0xAA, 0xBB, 0xCC, 0xDD];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 4,
            out _,
            out string? error);

        Assert.False(valid);
        Assert.Equal("USB: Invalid response", error);
    }

    [Fact]
    public void TryParseResponseRejectsAShortResponse()
    {
        byte[] buffer = [0x01, 0x06, 0x03];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 4,
            out _,
            out string? error);

        Assert.False(valid);
        Assert.Equal("USB: Invalid response", error);
    }

    [Fact]
    public void TryParseResponseRejectsAWrongLength()
    {
        byte[] buffer = [0x01, 0x06, 0x03, WatchProtocol.GetFileSize, 0xAA, 0xBB, 0xCC, 0xDD];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 20,
            out _,
            out string? error);

        Assert.False(valid);
        Assert.Equal("USB: Invalid response length", error);
    }

    [Fact]
    public void TryParseResponseRejectsAWrongCounter()
    {
        byte[] buffer = [0x01, 0x06, 0x04, WatchProtocol.GetFileSize, 0xAA, 0xBB, 0xCC, 0xDD];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 4,
            out _,
            out string? error);

        Assert.False(valid);
        Assert.Equal("USB: Response not synchonized with the request", error);
    }

    [Fact]
    public void TryParseResponseRejectsAWrongMessageType()
    {
        byte[] buffer = [0x01, 0x06, 0x03, WatchProtocol.OpenFileRead, 0xAA, 0xBB, 0xCC, 0xDD];

        bool valid = WatchProtocol.TryParseResponse(
            buffer,
            transferred: buffer.Length,
            expectedCounter: 3,
            expectedMessageType: WatchProtocol.GetFileSize,
            expectedPayloadLength: 4,
            out _,
            out string? error);

        Assert.False(valid);
        Assert.Equal("USB: Wrong response type", error);
    }
}
