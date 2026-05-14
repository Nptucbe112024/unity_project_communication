using UnityEngine;

public static class MonsterDebugger
{
    public static bool enabled = true;

    static readonly string[] stateTag = {
        "<color=#00ff88>[PATROL]</color>",
        "<color=#ffaa00>[ALERT]</color>",
        "<color=#ff4444>[CHASE]</color>",
    };

    public static void Log(string name, SoundMonsterAI.State state, string msg)
    {
        if (!enabled) return;
        Debug.Log($"{stateTag[(int)state]} {name} — {msg}");
    }

    public static void LogSound(string source, float intensity, Vector3 pos)
    {
        if (!enabled) return;
        Debug.Log($"<color=#aaaaff>[SOUND]</color> {source} " +
                  $"intensity:{intensity:F1} @ {pos}");
    }
}