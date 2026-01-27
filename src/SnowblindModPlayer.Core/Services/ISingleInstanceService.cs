using System;

namespace SnowblindModPlayer.Core.Services;

/// <summary>
/// Ensures single instance with inter-process signaling.
/// </summary>
public interface ISingleInstanceService : IDisposable
{
    /// <summary>Attempt to acquire primary instance mutex.</summary>
    bool TryAcquirePrimary();

    /// <summary>Start listening for commands from secondary instances.</summary>
    void StartListening(Action onShowRequested);

    /// <summary>Notify primary instance to show/focus.</summary>
    void NotifyPrimaryInstance(string command = "SHOW");
}
