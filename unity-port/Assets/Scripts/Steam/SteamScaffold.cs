// Lügen — SteamScaffold.cs
// Steamworks integration scaffold. Real Steam integration requires the
// Steamworks.NET package (https://steamworks.github.io/) which can't ship
// in this port — you have to add it yourself in Unity.
//
// The intent of this file: define a single seam (`ISteamHooks`) so the
// rest of the game doesn't have to know whether Steam is running or not.
// The default implementation is `NoOpSteamHooks` (safe in dev, in editor,
// and on non-Steam builds).
//
// Once you install Steamworks.NET, write a `SteamHooksSteamworks` class
// that calls SteamUserStats.SetAchievement / StoreStats / etc., and
// register it from your bootstrapper.

namespace Lugen.Steam
{
    public interface ISteamHooks
    {
        // Initialize the Steam API. Returns false if Steam isn't running
        // (the game should still launch — Steam features just won't work).
        bool Init();
        void Shutdown();

        // Achievements. Steam achievement IDs typically use SCREAMING_SNAKE_CASE
        // ("ACH_FIRST_RUN") — map your local achievement IDs to Steam IDs in
        // the implementation, not in the catalog.
        void GrantAchievement(string localId);

        // Cloud-save passthrough. The default Lügen save lives in
        // Application.persistentDataPath; if Steam Cloud is on, Steam will
        // sync that directory automatically, so these are typically no-ops.
        void OnSaveWritten();

        // Per-frame tick. Steamworks needs SteamAPI.RunCallbacks() each frame.
        void Tick();

        bool IsRunning { get; }
        string SteamId  { get; }
    }

    public class NoOpSteamHooks : ISteamHooks
    {
        public bool Init() => false;
        public void Shutdown() { }
        public void GrantAchievement(string localId) { }
        public void OnSaveWritten() { }
        public void Tick() { }
        public bool IsRunning => false;
        public string SteamId => null;
    }

    /// <summary>
    /// Single global accessor — set this once at startup from your bootstrapper.
    /// `SteamHub.Hooks ?? new NoOpSteamHooks()` is the safe access pattern.
    /// </summary>
    public static class SteamHub
    {
        public static ISteamHooks Hooks = new NoOpSteamHooks();
    }
}
