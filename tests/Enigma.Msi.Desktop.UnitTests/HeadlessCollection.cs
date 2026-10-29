using Xunit;

namespace Enigma.Msi.Desktop.UnitTests;

/// <summary>
/// The collection every headless UI test belongs to, so they share the one Avalonia platform
/// <see cref="HeadlessAvaloniaFixture"/> sets up and never run concurrently on it.
/// </summary>
[CollectionDefinition(Name)]
public sealed class HeadlessCollection : ICollectionFixture<HeadlessAvaloniaFixture>
{
    /// <summary>The collection's name.</summary>
    public const string Name = "headless-avalonia";
}
