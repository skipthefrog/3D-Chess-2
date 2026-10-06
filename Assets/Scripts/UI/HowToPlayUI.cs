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
    private Vector2 scroll;

    private const string Guide =
        "SETUP\n" +
        "Tap a piece in your tray, then tap a dot on the board to place it. " +
        "Tap \"Auto-place\" to place the rest for you. When every player is done, tap your \"Ready to Play\" button.\n\n" +
        "MOVING\n" +
        "Tap one of your pieces to see where it can go, then tap a dot. " +
        "Pieces move in three dimensions: left-right, front-back, and up-down between levels.\n\n" +
        "PIECES\n" +
        "King: 1 step in any direction, including diagonals and up or down.\n" +
        "Queen: any distance in a straight line or diagonal, in any direction.\n" +
        "Rook: any distance straight along one axis, including straight up or down.\n" +
        "Bishop: any distance diagonally, including diagonals that change level.\n" +
        "Knight: an L shape (2 then 1) in any plane, jumping over pieces.\n" +
        "Pawn: forward toward the far side of the cube; promotes on reaching it.\n\n" +
        "SEEING THE BOARD\n" +
        "Drag to turn the view. Pinch to zoom. " +
        "Use the Level buttons on the right to show one level at a time, and \"All\" to see the whole cube.\n\n" +
        "WINNING\n" +
        "Checkmate the other King. A captured King knocks that player out.";

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
            GUI.skin.button.fontSize = 18;
            if (TouchGUI.Button(new Rect(ui.x - 150f, 20f, 50f, 40f), "?"))
            {
                showing = true;
            }
            TouchGUI.End();
            return;
        }

        Rect full = new Rect(0, 0, ui.x, ui.y);
        TouchGUI.Block(full);
        GUI.Box(full, "");
        GUI.Box(full, "");

        float margin = 30f;
        Rect panel = new Rect(margin, 15f, ui.x - margin * 2, ui.y - 30f);
        GUI.Box(panel, "");

        GUIStyle title = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(panel.x, panel.y + 6f, panel.width, 30f), "How to Play", title);

        GUIStyle body = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        Rect view = new Rect(panel.x + 16f, panel.y + 40f, panel.width - 32f, panel.height - 95f);
        float contentHeight = body.CalcHeight(new GUIContent(Guide), view.width - 20f);
        scroll = GUI.BeginScrollView(view, scroll, new Rect(0, 0, view.width - 20f, contentHeight), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        GUI.Label(new Rect(0, 0, view.width - 20f, contentHeight), Guide, body);
        GUI.EndScrollView();

        GUI.skin.button.fontSize = 16;
        if (GUI.Button(new Rect(panel.x + panel.width / 2f - 70f, panel.yMax - 50f, 140f, 40f), "Got it"))
        {
            showing = false;
        }

        TouchGUI.End();
    }
}
