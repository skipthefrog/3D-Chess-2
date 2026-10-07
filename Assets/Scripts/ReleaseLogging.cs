using UnityEngine;

/// <summary>
/// The game logs heavily (several messages every frame during placement). That is useful
/// in the editor and development builds but slows phones down, so release builds turn
/// logging off except for errors and exceptions.
/// </summary>
public static class ReleaseLogging
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Configure()
    {
        if (!Debug.isDebugBuild)
        {
            Debug.unityLogger.filterLogType = LogType.Error;
        }
    }
}
