// Lügen — GameManager.cs
// Top-level Unity entry point. Stick this on a single GameObject in your
// boot scene. It owns:
//
//   - The active RunManager (or null between runs)
//   - The active RoundController (or null between rounds)
//   - The persistent SaveData
//   - The Steam hooks
//
// This is the ONLY file in the port that depends on UnityEngine.
// Everything under Cards/ Affixes/ Round/ Run/ AI/ etc. is plain C# so it's
// testable from a console runner or unit test.

using Lugen.Run;
using Lugen.Save;
using Lugen.Steam;

#if UNITY_5_3_OR_NEWER
using UnityEngine;

namespace Lugen.UnityHooks
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunManager RunManager { get; private set; }
        public SaveData Save { get; private set; }
        public RoundController CurrentRound { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Save = SaveSystem.Load();
            // Hook up Steamworks here if the user wired in a real ISteamHooks impl.
            SteamHub.Hooks.Init();
        }

        private void Update()
        {
            // Steamworks needs RunCallbacks() ticked every frame.
            SteamHub.Hooks.Tick();
        }

        private void OnApplicationQuit()
        {
            SaveSystem.Save(Save);
            SteamHub.Hooks.Shutdown();
        }

        // ---- Run lifecycle wrappers ----

        public void StartNewRun(string characterId)
        {
            RunManager = new RunManager();
            RunManager.StartRun(characterId);
            CurrentRound = new RoundController(RunManager.State);
            CurrentRound.StartRound();
        }

        public void EndRun(bool victory)
        {
            if (RunManager?.State != null)
            {
                if (victory) Save.runWon = true;
                if (RunManager.State.currentFloor > Save.maxFloorReached)
                    Save.maxFloorReached = RunManager.State.currentFloor;
                Save.runHistory.Add(new RunHistoryEntry
                {
                    characterId = RunManager.State.characterId,
                    result = victory ? "won" : "lost",
                    maxFloor = RunManager.State.currentFloor,
                    hearts = RunManager.State.hearts,
                    gold = RunManager.State.gold,
                    seed = RunManager.State.seed,
                    timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                });
                SaveSystem.Save(Save);
            }
            RunManager = null;
            CurrentRound = null;
        }
    }
}
#else
namespace Lugen.UnityHooks
{
    // Stub for non-Unity builds. Lets the rest of the namespace compile in
    // tooling / unit tests without UnityEngine on the path.
    public class GameManager
    {
        public RunManager RunManager;
        public SaveData Save;
        public RoundController CurrentRound;
    }
}
#endif
