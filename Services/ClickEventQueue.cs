using System.Threading.Channels;

namespace UrlShortener.Services;

public record ClickEvent(
    string UrlId,
    DateTime ClickedAt,
    string? ReferrerHost,
    string? Country,
    string DeviceType,
    string? Browser);

/// <summary>
/// Bounded in-memory queue the redirect hot path writes to. Never blocks the
/// caller: once full, new events are dropped rather than slowing the redirect.
/// </summary>
public interface IClickEventQueue
{
    void Enqueue(ClickEvent clickEvent);
    ChannelReader<ClickEvent> Reader { get; }
}

public class ClickEventQueue : IClickEventQueue
{
    private readonly Channel<ClickEvent> _channel = Channel.CreateBounded<ClickEvent>(
        new BoundedChannelOptions(capacity: 2000)
        {
            FullMode = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        });

    public ChannelReader<ClickEvent> Reader => _channel.Reader;

    public void Enqueue(ClickEvent clickEvent) => _channel.Writer.TryWrite(clickEvent);
}
