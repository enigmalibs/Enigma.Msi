using System;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// A clock that moves only when a test says so, so a time window can be crossed without waiting for it.
/// </summary>
internal sealed class ManualTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => _timestamp;

    public void Advance(TimeSpan by) => _timestamp += by.Ticks;
}
