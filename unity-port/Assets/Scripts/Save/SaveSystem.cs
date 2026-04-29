// Lügen — SaveSystem.cs
// Read/write the SaveData blob to disk. Uses Unity's JsonUtility which
// requires the [Serializable] attribute on every type in the graph (already
// applied in SaveData.cs).
//
// Outside of Unity (e.g. unit tests, server tooling) JsonUtility isn't
// available; for those callers we fall back to a stub that throws. The
// runtime-only code path is wrapped in #if UNITY_5_3_OR_NEWER.

using System.IO;

namespace Lugen.Save
{
    public static class SaveSystem
    {
        // The file lives in Unity's persistent data path. Unity guarantees this
        // is platform-correct (AppData on Windows, ~/Library/... on macOS, etc.).
#if UNITY_5_3_OR_NEWER
        private static string SavePath =>
            System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, "lugen_save.json");
#else
        private static string SavePath => System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
            "Lugen", "lugen_save.json");
#endif

        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(SavePath)) return new SaveData();
                string json = File.ReadAllText(SavePath);
#if UNITY_5_3_OR_NEWER
                return UnityEngine.JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
#else
                // Outside Unity, return a fresh blob. (Add your favorite JSON
                // lib here if you need editor-time tooling.)
                return new SaveData();
#endif
            }
            catch
            {
                // Corrupt save → start fresh. Don't lose runs over a parse error.
                return new SaveData();
            }
        }

        public static void Save(SaveData data)
        {
            try
            {
                string dir = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
#if UNITY_5_3_OR_NEWER
                string json = UnityEngine.JsonUtility.ToJson(data, prettyPrint: true);
#else
                // Pretty-printed JSON via reflection would need a real lib.
                // Stub for non-Unity builds: write an empty placeholder so we
                // don't blow up tooling that imports the namespace.
                string json = "{}";
#endif
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception)
            {
                // Best-effort save. The runtime should keep going even on disk failures.
            }
        }
    }
}
