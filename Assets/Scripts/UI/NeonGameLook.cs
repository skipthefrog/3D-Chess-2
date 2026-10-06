using UnityEngine;

/// <summary>
/// Gives the game scene the neon look: deep violet backdrop instead of the sky,
/// cyan wireframe and tinted floor squares. Runs once the board has been built.
/// </summary>
public class NeonGameLook : MonoBehaviour
{
    private EmergencyChessBoard board;
    private int styledFloors;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("Neon Game Look");
        go.AddComponent<NeonGameLook>();
        DontDestroyOnLoad(go);
    }

    private void LateUpdate()
    {
        if (board == null)
        {
            board = FindFirstObjectByType<EmergencyChessBoard>();
            styledFloors = 0;
            if (board == null) return;
        }

        Camera cam = Camera.main;
        if (cam != null && cam.clearFlags != CameraClearFlags.SolidColor)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = NeonTheme.Ground;
        }

        // The board is built a frame or two after the scene starts; style it once it exists
        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
        int floors = 0;
        foreach (MeshRenderer r in renderers) if (r.name.StartsWith("Floor_")) floors++;
        if (floors == 0 || floors == styledFloors) return;
        styledFloors = floors;

        foreach (MeshRenderer r in renderers)
        {
            if (!r.name.StartsWith("Floor_")) continue;
            string[] parts = r.name.Split('_');
            if (parts.Length != 4) continue;
            int.TryParse(parts[1], out int x);
            int.TryParse(parts[2], out int y);
            int.TryParse(parts[3], out int z);
            bool light = (x + y + z) % 2 == 0;
            Color c = light ? NeonTheme.Cyan : NeonTheme.Pink;
            c.a = light ? 0.22f : 0.14f;
            r.material.color = c;
        }

        foreach (LineRenderer line in FindObjectsByType<LineRenderer>(FindObjectsSortMode.None))
        {
            line.material.color = NeonTheme.Cyan;
            line.startColor = NeonTheme.Cyan;
            line.endColor = NeonTheme.Cyan;
        }
    }
}
