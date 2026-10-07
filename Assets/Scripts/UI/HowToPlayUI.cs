using UnityEngine;

/// <summary>
/// A "?" button on the game screen that opens a one-screen guide to playing.
/// The guide opens by itself the first time a game is started.
/// </summary>
public class HowToPlayUI : MonoBehaviour
{
    private const string SeenKey = "HowToPlaySeen";
    private bool showing;
    private EmergencyChessBoard board;
    private bool autoShownThisGame;

    // Two columns so the whole guide fits on a phone screen without scrolling
    private const string GuideLeft =
        "SETUP\n" +
        "Tap a piece in your tray, then a dot on the board. \"Auto-place\" places the rest for you. " +
        "When everyone is done, tap your \"Ready to Play\" button.\n\n" +
        "MOVING\n" +
        "Tap one of your pieces, then a dot. Pieces move left-right, front-back, and up-down between floors.\n\n" +
        "SEEING THE BOARD\n" +
        "Drag to turn. Pinch to zoom. The floor buttons on the right show one floor at a time.";

    private const string GuideRight =
        "PIECES\n" +
        "King: 1 step any direction, including up or down.\n" +
        "Queen: any distance, straight or diagonal.\n" +
        "Rook: any distance along one axis.\n" +
        "Bishop: any distance diagonally, across floors too.\n" +
        "Knight: an L (2 then 1) in any plane; jumps.\n" +
        "Pawn: forward to the far side, then promotes.\n\n" +
        "WINNING\n" +
        "Checkmate the other King. Lose your King and you're out.";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        GameObject go = new GameObject("How To Play UI");
        go.AddComponent<HowToPlayUI>();
        DontDestroyOnLoad(go);
    }

    private bool InGame()
    {
        if (board == null) board = FindFirstObjectByType<EmergencyChessBoard>();
        return board != null;
    }

    private void Update()
    {
        if (!InGame())
        {
            showing = false;
            autoShownThisGame = false;
            return;
        }
        if (!autoShownThisGame)
        {
            autoShownThisGame = true;
            if (PlayerPrefs.GetInt(SeenKey, 0) == 0)
            {
                showing = true;
                PlayerPrefs.SetInt(SeenKey, 1);
                PlayerPrefs.Save();
            }
        }
    }

    private void OnGUI()
    {
        if (!InGame()) return;

        Vector2 ui = TouchGUI.Begin();
        GUI.depth = -10; // draw above the other game UI

        if (!showing)
        {
            // Sits just left of the level buttons
            // Top right, above the level buttons
            if (TouchGUI.NeonButton(new Rect(ui.x - 82f, 16f, 72f, 44f), "?", NeonTheme.Yellow, NeonTheme.Pink, fontSize: 22))
            {
                showing = true;
            }
            TouchGUI.End();
            return;
        }

        Rect full = new Rect(0, 0, ui.x, ui.y);
        TouchGUI.Block(full);

        float margin = 30f;
        Rect panel = new Rect(margin, 15f, ui.x - margin * 2, ui.y - 30f);
        NeonTheme.GUIPanel(panel, NeonTheme.Cyan, 0.96f);

        GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width, 30f), "HOW TO PLAY", NeonTheme.GUILabelStyle(NeonTheme.Lime, 22, true, TextAnchor.MiddleCenter, display: true));

        GUIStyle body = NeonTheme.GUILabelStyle(NeonTheme.White, 12, false, TextAnchor.UpperLeft);
        float columnWidth = (panel.width - 48f) / 2f;
        float top = panel.y + 40f;
        float columnHeight = panel.height - 100f;
        GUI.Label(new Rect(panel.x + 16f, top, columnWidth, columnHeight), GuideLeft, body);
        GUI.Label(new Rect(panel.x + 32f + columnWidth, top, columnWidth, columnHeight), GuideRight, body);

        if (NeonTheme.GUIButton(new Rect(panel.x + panel.width / 2f - 70f, panel.yMax - 52f, 140f, 42f), "Got it!", NeonTheme.Pink, NeonTheme.Cyan, false, 17))
        {
            showing = false;
        }

        TouchGUI.End();
    }
}
