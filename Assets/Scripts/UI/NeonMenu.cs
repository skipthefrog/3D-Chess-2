using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Neon-styled main menu and one-screen game setup. Replaces the older multi-step
/// MainMenuController UI (which stays in the scene but is hidden).
/// </summary>
public class NeonMenu : MonoBehaviour
{
    private static readonly string[] ColorNames = { "White", "Black", "Green", "Purple", "Yellow", "Orange" };

    private RectTransform safeRoot;
    private GameObject menuScreen;
    private GameObject setupScreen;
    private TextMeshProUGUI toastText;
    private Rect lastSafeArea;

    // Setup state
    private int playerCount = 2;
    private BoardSize boardSize = BoardSize.Small4x4x4;
    private AIDifficulty difficulty = AIDifficulty.Medium;
    private readonly List<PlayerType> playerTypes = new List<PlayerType> { PlayerType.Human, PlayerType.Computer };
    private bool chaosMode;
    private bool timedPlay;

    // Setup controls that change with state
    private readonly List<(Image image, TextMeshProUGUI label, System.Func<bool> selected, System.Func<bool> enabled, Color color)> chips =
        new List<(Image, TextMeshProUGUI, System.Func<bool>, System.Func<bool>, Color)>();
    private RectTransform playerGrid;
    private Image chaosSwitch, timedSwitch;
    private RectTransform chaosKnob, timedKnob;
    private TextMeshProUGUI chaosNote, timedNote;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded += (scene, mode) => TryCreate();
        TryCreate();
    }

    private static void TryCreate()
    {
        if (FindFirstObjectByType<MainMenuController>() == null) return;
        if (FindFirstObjectByType<NeonMenu>() != null) return;
        new GameObject("Neon Menu").AddComponent<NeonMenu>();
    }

    private IEnumerator Start()
    {
        BuildCanvas();
        BuildMenuScreen();
        BuildSetupScreen();
        BuildCustomizeScreen();
        BuildOnlineScreen();
        ShowMenu();

        // Coming back to the menu from an online game ends the session
        if (OnlineSession.IsActive && OnlineClient.Instance != null) OnlineClient.Instance.LeaveOnline();

        // The old menu builds its UI in its own Start; hide it once it exists
        yield return null;
        yield return null;
        HideOldMenu();
    }

    private void HideOldMenu()
    {
        Canvas mine = GetComponent<Canvas>();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c != mine && c.isRootCanvas) c.enabled = false;
        }
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }

    private void Update()
    {
        if (Screen.safeArea != lastSafeArea) ApplySafeArea();
    }

    // ───────────────────────── Canvas ─────────────────────────

    private void BuildCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(844, 390);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();

        // Background fills the whole screen, including behind the notch
        Image bg = NewImage("Background", transform, NeonTheme.Ground);
        Stretch(bg.rectTransform);
        Decoration(bg.transform);

        safeRoot = NewRect("Safe Area", transform);
        ApplySafeArea();
    }

    private void ApplySafeArea()
    {
        lastSafeArea = Screen.safeArea;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        safeRoot.anchorMin = new Vector2(lastSafeArea.xMin / Screen.width, lastSafeArea.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(lastSafeArea.xMax / Screen.width, lastSafeArea.yMax / Screen.height);
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
    }

    private void Decoration(Transform parent)
    {
        Image ring = NewImage("Pink Ring", parent, new Color(NeonTheme.Pink.r, NeonTheme.Pink.g, NeonTheme.Pink.b, 0.3f));
        ring.sprite = NeonTheme.RoundedOutline;
        ring.type = Image.Type.Sliced;
        Place(ring.rectTransform, new Vector2(0.45f, 1f), new Vector2(220, 220), new Vector2(0, -40));
        ring.rectTransform.localEulerAngles = new Vector3(0, 0, 12);

        Image square = NewImage("Lime Square", parent, new Color(NeonTheme.Lime.r, NeonTheme.Lime.g, NeonTheme.Lime.b, 0.22f));
        square.sprite = NeonTheme.RoundedOutline;
        square.type = Image.Type.Sliced;
        Place(square.rectTransform, new Vector2(0.08f, 0f), new Vector2(160, 160), new Vector2(0, 30));
        square.rectTransform.localEulerAngles = new Vector3(0, 0, 18);
    }

    // ───────────────────────── Main menu ─────────────────────────

    private void BuildMenuScreen()
    {
        menuScreen = NewRect("Menu Screen", safeRoot).gameObject;
        Stretch(menuScreen.GetComponent<RectTransform>());

        // Left: a little neon cube
        RectTransform cube = NewRect("Cube", menuScreen.transform);
        cube.anchorMin = cube.anchorMax = new Vector2(0f, 0.5f);
        cube.pivot = new Vector2(0.5f, 0.5f);
        cube.anchoredPosition = new Vector2(150, 0);
        cube.sizeDelta = new Vector2(200, 300);
        cube.localEulerAngles = new Vector3(0, 0, -6);
        BuildCube(cube);

        // Right: title and buttons
        RectTransform column = NewRect("Column", menuScreen.transform);
        column.anchorMin = new Vector2(0f, 0f);
        column.anchorMax = new Vector2(1f, 1f);
        column.offsetMin = new Vector2(300, 20);
        column.offsetMax = new Vector2(-30, -20);
        var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 14;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // Title with a hard pink drop
        RectTransform titleBox = NewRect("Title", column);
        titleBox.sizeDelta = new Vector2(420, 58);
        TextMeshProUGUI titleShadow = NewText("Shadow", titleBox, "3D CHESS", 54, NeonTheme.Pink, TextAlignmentOptions.Left);
        NeonTheme.ApplyFont(titleShadow, display: true);
        Stretch(titleShadow.rectTransform, new Vector2(4, -4));
        TextMeshProUGUI title = NewText("Text", titleBox, "3D CHESS", 54, NeonTheme.Lime, TextAlignmentOptions.Left);
        NeonTheme.ApplyFont(title, display: true);
        Stretch(title.rectTransform);
        titleBox.localEulerAngles = new Vector3(0, 0, -2);

        TextMeshProUGUI tagline = NewText("Tagline", column, "Up, down, sideways. Go wild.", 18, NeonTheme.Cyan, TextAlignmentOptions.Left);
        tagline.rectTransform.sizeDelta = new Vector2(420, 26);

        NeonButton(column, "Quick Play vs Computer!", new Vector2(370, 58), NeonTheme.Pink, NeonTheme.Cyan, -1.5f, 21, StartQuickPlay);

        RectTransform row = NewRect("Row", column);
        row.sizeDelta = new Vector2(370, 52);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 14;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        NeonButton(row, "Local Game", new Vector2(178, 52), NeonTheme.Lime, NeonTheme.Pink, 1f, 18, ShowSetup);
        NeonButton(row, "Online Game", new Vector2(178, 52), NeonTheme.Yellow, NeonTheme.Cyan, -1f, 18, ShowOnline);

        NeonButton(column, "Customize: pieces & backdrops", new Vector2(370, 44), NeonTheme.Cyan, NeonTheme.Pink, 0.5f, 16, ShowCustomize);

        toastText = NewText("Toast", menuScreen.transform, "", 18, NeonTheme.Yellow, TextAlignmentOptions.Center);
        toastText.rectTransform.anchorMin = new Vector2(0, 0);
        toastText.rectTransform.anchorMax = new Vector2(1, 0);
        toastText.rectTransform.sizeDelta = new Vector2(0, 30);
        toastText.rectTransform.anchoredPosition = new Vector2(0, 16);
    }

    private void BuildCube(RectTransform parent)
    {
        // Four stacked levels, each a square seen at an angle
        float[] heights = { -114, -38, 38, 114 };
        for (int level = 0; level < 4; level++)
        {
            RectTransform squash = NewRect($"Level {level + 1}", parent);
            squash.anchorMin = squash.anchorMax = new Vector2(0.5f, 0.5f);
            squash.anchoredPosition = new Vector2(0, heights[level]);
            squash.sizeDelta = Vector2.zero;
            squash.localScale = new Vector3(1f, 0.58f, 1f);

            Image fill = NewImage("Fill", squash, new Color(NeonTheme.Cyan.r, NeonTheme.Cyan.g, NeonTheme.Cyan.b, 0.08f));
            fill.rectTransform.sizeDelta = new Vector2(104, 104);
            fill.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
            Image edge = NewImage("Edge", squash, NeonTheme.Cyan);
            edge.sprite = NeonTheme.SquareOutline;
            edge.type = Image.Type.Sliced;
            edge.rectTransform.sizeDelta = new Vector2(104, 104);
            edge.rectTransform.localEulerAngles = new Vector3(0, 0, 45);
        }

        // A few pieces
        (float x, float y, Color c)[] pieces =
        {
            (-30, -105, NeonTheme.Lime), (25, -95, NeonTheme.Lime), (40, -30, NeonTheme.Lime),
            (-35, 40, NeonTheme.Pink), (10, 110, NeonTheme.Pink), (-45, 100, NeonTheme.Pink)
        };
        foreach (var p in pieces)
        {
            Image piece = NewImage("Piece", parent, p.c);
            piece.sprite = NeonTheme.Rounded;
            piece.type = Image.Type.Sliced;
            Place(piece.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(13, 22), new Vector2(p.x, p.y + 10));
        }
        (float x, float y)[] dots = { (0, -40), (30, -40), (-28, -28) };
        foreach (var d in dots)
        {
            Image dot = NewImage("Dot", parent, NeonTheme.Yellow);
            dot.sprite = NeonTheme.Circle;
            Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(12, 12), new Vector2(d.x, d.y));
        }
    }

    // ───────────────────────── Setup ─────────────────────────

    private void BuildSetupScreen()
    {
        setupScreen = NewRect("Setup Screen", safeRoot).gameObject;
        RectTransform root = setupScreen.GetComponent<RectTransform>();
        Stretch(root);

        // Header
        RectTransform header = NewRect("Header", root);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.offsetMin = new Vector2(16, -64);
        header.offsetMax = new Vector2(-24, -12);

        Button back = NeonButton(header, "<", new Vector2(48, 48), NeonTheme.Cyan, NeonTheme.Pink, 0f, 24, ShowMenu);
        Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(28, 0));
        back.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        TextMeshProUGUI title = NewText("Title", header, "PICK YOUR CHAOS", 30, NeonTheme.Lime, TextAlignmentOptions.Left);
        NeonTheme.ApplyFont(title, display: true);
        title.rectTransform.anchorMin = new Vector2(0, 0);
        title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(70, 0);
        title.rectTransform.offsetMax = new Vector2(-180, 0);
        title.rectTransform.localEulerAngles = new Vector3(0, 0, -1);

        Button go = NeonButton(header, "Let's go!", new Vector2(150, 50), NeonTheme.Pink, NeonTheme.Cyan, 2f, 20, StartFromSetup);
        Place((RectTransform)go.transform, new Vector2(1, 0.5f), new Vector2(150, 50), new Vector2(-75, 0));

        // Two cards
        RectTransform left = Card(root, "Left Card", NeonTheme.Cyan, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(16, 14), new Vector2(-8, -74));
        RectTransform right = Card(root, "Right Card", NeonTheme.Pink, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(8, 14), new Vector2(-24, -74));

        SectionLabel(left, "How many players?", NeonTheme.Cyan);
        ChipRow(left, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            ("2", () => SetPlayers(2), () => playerCount == 2, () => true),
            ("4", () => SetPlayers(4), () => playerCount == 4, () => true),
            ("6", () => SetPlayers(6), () => playerCount == 6, () => true),
        }, NeonTheme.Lime, 20);

        SectionLabel(left, "How big a cube?", NeonTheme.Cyan);
        ChipRow(left, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            ("Snack 4³", () => SetBoard(BoardSize.Small4x4x4), () => boardSize == BoardSize.Small4x4x4, () => playerCount == 2),
            ("Meal 6³", () => SetBoard(BoardSize.Medium6x6x6), () => boardSize == BoardSize.Medium6x6x6, () => playerCount <= 4),
            ("Feast 8³", () => SetBoard(BoardSize.Large8x8x8), () => boardSize == BoardSize.Large8x8x8, () => true),
        }, NeonTheme.Lime, 16);

        SectionLabel(left, "Tap a player to swap You / Robot", NeonTheme.Lavender);
        playerGrid = NewRect("Players", left);
        var rows = playerGrid.gameObject.AddComponent<VerticalLayoutGroup>();
        rows.spacing = 6;
        rows.childControlWidth = true;
        rows.childControlHeight = true;
        rows.childForceExpandWidth = true;
        rows.childForceExpandHeight = false;
        var gridSize = playerGrid.gameObject.AddComponent<LayoutElement>();
        gridSize.preferredHeight = 74;
        gridSize.flexibleHeight = 0;

        SectionLabel(right, "Robot brain size", NeonTheme.PinkSoft);
        ChipRow(right, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            ("Pea", () => SetDifficulty(AIDifficulty.Easy), () => difficulty == AIDifficulty.Easy, () => true),
            ("Walnut", () => SetDifficulty(AIDifficulty.Medium), () => difficulty == AIDifficulty.Medium, () => true),
            ("Galaxy", () => SetDifficulty(AIDifficulty.Hard), () => difficulty == AIDifficulty.Hard, () => true),
        }, NeonTheme.Yellow, 16);

        SwitchRow(right, "Chaos Mode", out chaosNote, out chaosSwitch, out chaosKnob, () => { chaosMode = !chaosMode; Refresh(); });
        SwitchRow(right, "Speed round", out timedNote, out timedSwitch, out timedKnob, () => { timedPlay = !timedPlay; Refresh(); });

        Refresh();
    }

    private RectTransform Card(RectTransform parent, string name, Color border, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        Image card = NewImage(name, parent, border);
        card.sprite = NeonTheme.RoundedOutline;
        card.type = Image.Type.Sliced;
        RectTransform rt = card.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 12, 12);
        layout.spacing = 6;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        return rt;
    }

    private void SectionLabel(RectTransform parent, string text, Color color)
    {
        TextMeshProUGUI label = NewText("Label", parent, text, 15, color, TextAlignmentOptions.Left);
        label.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;
    }

    private void ChipRow(RectTransform parent, (string text, System.Action onClick, System.Func<bool> selected, System.Func<bool> enabled)[] items, Color color, float fontSize)
    {
        RectTransform row = NewRect("Chips", parent);
        var rowSize = row.gameObject.AddComponent<LayoutElement>();
        rowSize.preferredHeight = 44;
        rowSize.flexibleHeight = 0; // don't soak up the card's spare height
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        foreach (var item in items)
        {
            var captured = item;
            Image image = NewImage(item.text, row, color);
            Button button = image.gameObject.AddComponent<Button>();
            NeonTheme.NeutralTint(button);
            button.onClick.AddListener(() =>
            {
                if (!captured.enabled()) return;
                captured.onClick();
                Refresh();
            });
            TextMeshProUGUI label = NewText("Text", image.transform, item.text, fontSize, color, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
            chips.Add((image, label, item.selected, item.enabled, color));
        }
    }

    private void SwitchRow(RectTransform parent, string title, out TextMeshProUGUI note, out Image track, out RectTransform knob, System.Action onToggle)
    {
        RectTransform row = NewRect(title, parent);
        var rowSize = row.gameObject.AddComponent<LayoutElement>();
        rowSize.preferredHeight = 54;
        rowSize.flexibleHeight = 0;

        TextMeshProUGUI titleText = NewText("Title", row, title, 18, NeonTheme.White, TextAlignmentOptions.TopLeft);
        titleText.rectTransform.anchorMin = new Vector2(0, 0.5f);
        titleText.rectTransform.anchorMax = new Vector2(1, 1);
        titleText.rectTransform.offsetMin = Vector2.zero;
        titleText.rectTransform.offsetMax = new Vector2(-80, 0);

        note = NewText("Note", row, "", 14, NeonTheme.Lavender, TextAlignmentOptions.TopLeft);
        note.fontStyle = FontStyles.Normal;
        note.rectTransform.anchorMin = new Vector2(0, 0);
        note.rectTransform.anchorMax = new Vector2(1, 0.5f);
        note.rectTransform.offsetMin = Vector2.zero;
        note.rectTransform.offsetMax = new Vector2(-80, 0);

        track = NewImage("Switch", row, NeonTheme.Lime);
        track.sprite = NeonTheme.Rounded;
        track.type = Image.Type.Sliced;
        Place(track.rectTransform, new Vector2(1, 0.5f), new Vector2(62, 36), new Vector2(-34, 0));
        Button button = track.gameObject.AddComponent<Button>();
        NeonTheme.NeutralTint(button);
        button.onClick.AddListener(() => onToggle());

        Image knobImage = NewImage("Knob", track.transform, NeonTheme.Ground);
        knobImage.sprite = NeonTheme.Circle;
        knob = knobImage.rectTransform;
        knob.sizeDelta = new Vector2(26, 26);
    }

    private void SetPlayers(int count)
    {
        playerCount = count;
        if (count == 4 && boardSize == BoardSize.Small4x4x4) boardSize = BoardSize.Medium6x6x6;
        if (count == 6) boardSize = BoardSize.Large8x8x8;
        while (playerTypes.Count < count) playerTypes.Add(PlayerType.Computer);
        while (playerTypes.Count > count) playerTypes.RemoveAt(playerTypes.Count - 1);
    }

    private void SetBoard(BoardSize size) => boardSize = size;
    private void SetDifficulty(AIDifficulty level) => difficulty = level;

    private void Refresh()
    {
        foreach (var chip in chips)
        {
            bool on = chip.selected();
            bool usable = chip.enabled();
            if (on)
            {
                NeonTheme.StyleFilled(chip.image, chip.color, NeonTheme.Pink);
                chip.label.color = NeonTheme.Ground;
            }
            else
            {
                NeonTheme.StyleOutline(chip.image, usable ? chip.color : NeonTheme.Muted);
                chip.label.color = usable ? chip.color : NeonTheme.Muted;
            }
        }

        // Player buttons: rows of three that share the card's width
        for (int i = playerGrid.childCount - 1; i >= 0; i--) Destroy(playerGrid.GetChild(i).gameObject);
        RectTransform row = null;
        for (int i = 0; i < playerCount; i++)
        {
            if (i % 3 == 0)
            {
                row = NewRect("Row", playerGrid);
                var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 8;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = true;
                rowLayout.childForceExpandHeight = true;
                var rowSize = row.gameObject.AddComponent<LayoutElement>();
                rowSize.preferredHeight = 34;
                rowSize.flexibleHeight = 0;
            }

            int index = i;
            bool human = playerTypes[i] == PlayerType.Human;
            Color c = human ? NeonTheme.White : NeonTheme.PinkSoft;
            Image image = NewImage(ColorNames[i], row, c);
            NeonTheme.StyleOutline(image, c);
            Button button = image.gameObject.AddComponent<Button>();
            NeonTheme.NeutralTint(button);
            button.onClick.AddListener(() =>
            {
                playerTypes[index] = playerTypes[index] == PlayerType.Human ? PlayerType.Computer : PlayerType.Human;
                Refresh();
            });
            TextMeshProUGUI label = NewText("Text", image.transform, $"{ColorNames[i]}: {(human ? "You" : "Robot")}", 14, c, TextAlignmentOptions.Center);
            label.enableAutoSizing = true;
            label.fontSizeMin = 10;
            label.fontSizeMax = 14;
            Stretch(label.rectTransform, Vector2.zero);
            label.rectTransform.offsetMin = new Vector2(6, 0);
            label.rectTransform.offsetMax = new Vector2(-6, 0);
        }
        // Keep two-player buttons the same width as the others
        if (row != null)
        {
            for (int pad = playerCount % 3; pad != 0 && pad < 3; pad++) NewRect("Spacer", row);
        }

        RefreshCustomize();
        SetSwitch(chaosSwitch, chaosKnob, chaosMode);
        chaosNote.text = chaosMode ? "Cube spins every 9 turns" : "Off";
        SetSwitch(timedSwitch, timedKnob, timedPlay);
        timedNote.text = timedPlay ? "10 minutes each" : "No clock";
    }

    private void SetSwitch(Image track, RectTransform knob, bool on)
    {
        if (on) NeonTheme.StyleFilled(track, NeonTheme.Lime, NeonTheme.Pink);
        else NeonTheme.StyleOutline(track, NeonTheme.Lavender);
        knob.anchorMin = knob.anchorMax = new Vector2(on ? 1f : 0f, 0.5f);
        knob.anchoredPosition = new Vector2(on ? -18f : 18f, 0f);
        knob.GetComponent<Image>().color = on ? NeonTheme.Ground : NeonTheme.Lavender;
    }

    // ───────────────────────── Customize ─────────────────────────

    private GameObject customizeScreen;
    private TextMeshProUGUI pieceSetNote, backdropNote;
    private Image[] swatches;

    private void BuildCustomizeScreen()
    {
        customizeScreen = NewRect("Customize Screen", safeRoot).gameObject;
        RectTransform root = customizeScreen.GetComponent<RectTransform>();
        Stretch(root);

        RectTransform header = NewRect("Header", root);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.offsetMin = new Vector2(16, -64);
        header.offsetMax = new Vector2(-24, -12);

        Button back = NeonButton(header, "<", new Vector2(48, 48), NeonTheme.Cyan, NeonTheme.Pink, 0f, 24, ShowMenu);
        Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(28, 0));

        TextMeshProUGUI title = NewText("Title", header, "MAKE IT YOURS", 30, NeonTheme.Lime, TextAlignmentOptions.Left);
        NeonTheme.ApplyFont(title, display: true);
        title.rectTransform.anchorMin = new Vector2(0, 0);
        title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(70, 0);
        title.rectTransform.offsetMax = new Vector2(-180, 0);
        title.rectTransform.localEulerAngles = new Vector3(0, 0, -1);

        Button done = NeonButton(header, "Done!", new Vector2(150, 50), NeonTheme.Pink, NeonTheme.Cyan, 2f, 20, ShowMenu);
        Place((RectTransform)done.transform, new Vector2(1, 0.5f), new Vector2(150, 50), new Vector2(-75, 0));

        RectTransform left = Card(root, "Pieces Card", NeonTheme.Cyan, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(16, 14), new Vector2(-8, -74));
        RectTransform right = Card(root, "Backdrop Card", NeonTheme.Pink, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(8, 14), new Vector2(-24, -74));

        SectionLabel(left, "Piece set", NeonTheme.Cyan);
        ChipRow(left, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            PieceSetChip(PieceSets.Kind.NeonGlow),
            PieceSetChip(PieceSets.Kind.Chrome),
        }, NeonTheme.Lime, 16);
        ChipRow(left, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            PieceSetChip(PieceSets.Kind.Crystal),
            PieceSetChip(PieceSets.Kind.Classic),
        }, NeonTheme.Lime, 16);
        pieceSetNote = NewText("Note", left, "", 15, NeonTheme.Lavender, TextAlignmentOptions.Left);
        pieceSetNote.fontStyle = FontStyles.Normal;
        pieceSetNote.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

        // Colour swatches for the six sides in the chosen set
        RectTransform swatchRow = NewRect("Swatches", left);
        var swatchSize = swatchRow.gameObject.AddComponent<LayoutElement>();
        swatchSize.preferredHeight = 30;
        swatchSize.flexibleHeight = 0;
        var swatchLayout = swatchRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        swatchLayout.spacing = 10;
        swatchLayout.childControlWidth = false;
        swatchLayout.childControlHeight = false;
        swatchLayout.childForceExpandWidth = false;
        swatchLayout.childAlignment = TextAnchor.MiddleLeft;
        swatches = new Image[6];
        for (int i = 0; i < 6; i++)
        {
            swatches[i] = NewImage(ColorNames[i], swatchRow, Color.white);
            swatches[i].sprite = NeonTheme.Circle;
            swatches[i].rectTransform.sizeDelta = new Vector2(26, 26);
        }

        SectionLabel(right, "Backdrop the board floats in", NeonTheme.PinkSoft);
        ChipRow(right, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            BackdropChip(Backdrops.Kind.DeepSpace),
            BackdropChip(Backdrops.Kind.NeonDesert),
        }, NeonTheme.Cyan, 16);
        ChipRow(right, new (string, System.Action, System.Func<bool>, System.Func<bool>)[]
        {
            BackdropChip(Backdrops.Kind.NeonGlow),
        }, NeonTheme.Cyan, 16);
        backdropNote = NewText("Note", right, "", 15, NeonTheme.Lavender, TextAlignmentOptions.TopLeft);
        backdropNote.fontStyle = FontStyles.Normal;
        backdropNote.enableWordWrapping = true;
        backdropNote.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;

        TextMeshProUGUI more = NewText("More", right, "More backdrops and piece sets on the way.", 14, NeonTheme.Muted, TextAlignmentOptions.TopLeft);
        more.fontStyle = FontStyles.Normal;
        more.enableWordWrapping = true;
        more.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
    }

    private (string, System.Action, System.Func<bool>, System.Func<bool>) BackdropChip(Backdrops.Kind kind)
    {
        return (Backdrops.DisplayName(kind), () => Backdrops.Current = kind, () => Backdrops.Current == kind, () => true);
    }

    private (string, System.Action, System.Func<bool>, System.Func<bool>) PieceSetChip(PieceSets.Kind kind)
    {
        return (PieceSets.DisplayName(kind), () => PieceSets.Current = kind, () => PieceSets.Current == kind, () => true);
    }

    private void RefreshCustomize()
    {
        if (pieceSetNote == null) return;
        pieceSetNote.text = PieceSets.Description(PieceSets.Current);
        PieceColor[] sides = { PieceColor.White, PieceColor.Black, PieceColor.Green, PieceColor.Purple, PieceColor.Yellow, PieceColor.Orange };
        for (int i = 0; i < swatches.Length; i++) swatches[i].color = PieceSets.Tint(PieceSets.Current, sides[i]);
        backdropNote.text = Backdrops.Description(Backdrops.Current);
    }

    // ───────────────────────── Online ─────────────────────────

    private GameObject onlineScreen;
    private TMP_InputField nameInput, codeInput;
    private TextMeshProUGUI onlineStatus;
    private bool onlineBusy;
    private bool onlineStarting;
    private GameObject matchButtonRow, searchingPanel;
    private TextMeshProUGUI searchingLabel;

    private void BuildOnlineScreen()
    {
        onlineScreen = NewRect("Online Screen", safeRoot).gameObject;
        RectTransform root = onlineScreen.GetComponent<RectTransform>();
        Stretch(root);

        RectTransform header = NewRect("Header", root);
        header.anchorMin = new Vector2(0, 1);
        header.anchorMax = new Vector2(1, 1);
        header.pivot = new Vector2(0.5f, 1);
        header.offsetMin = new Vector2(16, -64);
        header.offsetMax = new Vector2(-24, -12);

        Button back = NeonButton(header, "<", new Vector2(48, 48), NeonTheme.Cyan, NeonTheme.Pink, 0f, 24, LeaveOnlineScreen);
        Place((RectTransform)back.transform, new Vector2(0, 0.5f), new Vector2(48, 48), new Vector2(28, 0));

        TextMeshProUGUI title = NewText("Title", header, "PLAY ONLINE", 30, NeonTheme.Lime, TextAlignmentOptions.Left);
        NeonTheme.ApplyFont(title, display: true);
        title.rectTransform.anchorMin = new Vector2(0, 0);
        title.rectTransform.anchorMax = new Vector2(1, 1);
        title.rectTransform.offsetMin = new Vector2(70, 0);
        title.rectTransform.offsetMax = new Vector2(-20, 0);

        RectTransform left = Card(root, "Friend Card", NeonTheme.Cyan, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(16, 60), new Vector2(-8, -74));
        RectTransform right = Card(root, "Match Card", NeonTheme.Pink, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(8, 60), new Vector2(-24, -74));

        SectionLabel(left, "Your name", NeonTheme.Cyan);
        nameInput = InputField(left, "Name", PlayerPrefs.GetString("OnlineName", ""), 20);
        SectionLabel(left, "Play a friend", NeonTheme.Cyan);
        RectTransform createRow = Row(left);
        NeonButton(createRow, "Create game", new Vector2(0, 44), NeonTheme.Lime, NeonTheme.Pink, 0f, 17, CreateOnlineGame);
        RectTransform joinRow = Row(left);
        codeInput = InputField(joinRow, "CODE", "", 6);
        codeInput.characterValidation = TMP_InputField.CharacterValidation.Alphanumeric;
        codeInput.onValidateInput += (text, index, ch) => char.ToUpperInvariant(ch);
        NeonButton(joinRow, "Join", new Vector2(0, 44), NeonTheme.Yellow, NeonTheme.Cyan, 0f, 17, JoinOnlineGame);

        SectionLabel(right, "Play anyone", NeonTheme.PinkSoft);
        TextMeshProUGUI blurb = NewText("Blurb", right, "Get paired with the next player looking for a 4x4x4 game.", 15, NeonTheme.Lavender, TextAlignmentOptions.TopLeft);
        blurb.fontStyle = FontStyles.Normal;
        blurb.enableWordWrapping = true;
        blurb.gameObject.AddComponent<LayoutElement>().preferredHeight = 44;
        RectTransform matchRow = Row(right);
        NeonButton(matchRow, "Find match!", new Vector2(0, 50), NeonTheme.Pink, NeonTheme.Cyan, -1f, 19, FindOnlineMatch);
        matchButtonRow = matchRow.gameObject;
        BuildSearchingPanel(right);

        onlineStatus = NewText("Status", root, "", 17, NeonTheme.Yellow, TextAlignmentOptions.Center);
        onlineStatus.enableWordWrapping = true;
        onlineStatus.rectTransform.anchorMin = new Vector2(0, 0);
        onlineStatus.rectTransform.anchorMax = new Vector2(1, 0);
        onlineStatus.rectTransform.pivot = new Vector2(0.5f, 0);
        onlineStatus.rectTransform.sizeDelta = new Vector2(-40, 48);
        onlineStatus.rectTransform.anchoredPosition = new Vector2(0, 6);
    }

    // Shown in place of the Find match button while looking for an opponent
    private void BuildSearchingPanel(RectTransform parent)
    {
        RectTransform panel = NewRect("Searching", parent);
        var size = panel.gameObject.AddComponent<LayoutElement>();
        size.preferredHeight = 96;
        size.flexibleHeight = 0;

        NeonSpinner spinner = NeonSpinner.Create(panel, 84);
        Place((RectTransform)spinner.transform, new Vector2(0, 0.5f), new Vector2(84, 84), new Vector2(46, 0));

        searchingLabel = NewText("Label", panel, "", 17, NeonTheme.Yellow, TextAlignmentOptions.Left);
        searchingLabel.rectTransform.anchorMin = new Vector2(0, 0.5f);
        searchingLabel.rectTransform.anchorMax = new Vector2(1, 1);
        searchingLabel.rectTransform.offsetMin = new Vector2(100, 0);
        searchingLabel.rectTransform.offsetMax = new Vector2(0, -4);

        Button cancel = NeonButton(panel, "Cancel", new Vector2(120, 38), NeonTheme.Cyan, NeonTheme.Pink, 0f, 15, CancelFindMatch);
        RectTransform cancelRect = (RectTransform)cancel.transform;
        cancelRect.anchorMin = cancelRect.anchorMax = new Vector2(0, 0);
        cancelRect.pivot = new Vector2(0, 0);
        cancelRect.sizeDelta = new Vector2(120, 38);
        cancelRect.anchoredPosition = new Vector2(100, 6);

        searchingPanel = panel.gameObject;
        searchingPanel.SetActive(false);
    }

    private void SetSearching(string label)
    {
        bool searching = label != null;
        searchingPanel.SetActive(searching);
        matchButtonRow.SetActive(!searching);
        if (searching)
        {
            searchingLabel.text = label;
            SetOnlineStatus("");
        }
    }

    private void CancelFindMatch()
    {
        if (OnlineClient.Instance != null) OnlineClient.Instance.LeaveOnline();
        onlineBusy = false;
        SetSearching(null);
    }

    private RectTransform Row(RectTransform parent)
    {
        RectTransform row = NewRect("Row", parent);
        var size = row.gameObject.AddComponent<LayoutElement>();
        size.preferredHeight = 50;
        size.flexibleHeight = 0;
        var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        return row;
    }

    private TMP_InputField InputField(RectTransform parent, string placeholder, string value, int limit)
    {
        Image box = NewImage("Input", parent, NeonTheme.Cyan);
        box.sprite = NeonTheme.RoundedOutline;
        box.type = Image.Type.Sliced;
        var size = box.gameObject.AddComponent<LayoutElement>();
        size.preferredHeight = 44;
        size.flexibleHeight = 0;

        RectTransform area = NewRect("Text Area", box.transform);
        Stretch(area);
        area.offsetMin = new Vector2(14, 4);
        area.offsetMax = new Vector2(-14, -4);
        area.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI hint = NewText("Placeholder", area, placeholder, 18, NeonTheme.Muted, TextAlignmentOptions.Left);
        Stretch(hint.rectTransform);
        TextMeshProUGUI text = NewText("Text", area, "", 18, NeonTheme.White, TextAlignmentOptions.Left);
        Stretch(text.rectTransform);

        var input = box.gameObject.AddComponent<TMP_InputField>();
        input.textViewport = area;
        input.textComponent = text;
        input.placeholder = hint;
        input.characterLimit = limit;
        input.fontAsset = text.font;
        input.pointSize = 18;
        input.caretColor = NeonTheme.Lime;
        input.text = value;
        return input;
    }

    private void ShowOnline()
    {
        menuScreen.SetActive(false);
        setupScreen.SetActive(false);
        customizeScreen.SetActive(false);
        onlineScreen.SetActive(true);
        onlineStatus.text = "";
        SetSearching(null);
        OnlineClient.GetOrCreate();
    }

    private void LeaveOnlineScreen()
    {
        if (OnlineClient.Instance != null) OnlineClient.Instance.LeaveOnline();
        onlineBusy = false;
        ShowMenu();
    }

    private void SetOnlineStatus(string text) => onlineStatus.text = text;

    private string ChosenName()
    {
        string name = nameInput.text.Trim();
        return string.IsNullOrEmpty(name) ? "Player" : name;
    }

    private void CreateOnlineGame() => StartCoroutine(CreateRoutine());
    private void JoinOnlineGame() => StartCoroutine(JoinRoutine(codeInput.text));
    private void FindOnlineMatch() => StartCoroutine(MatchRoutine());

    private IEnumerator SignIn(Action<bool> done)
    {
        var client = OnlineClient.GetOrCreate();
        bool ok = false;
        yield return client.EnsureSignedIn(ChosenName(), (success, error) =>
        {
            ok = success;
            if (!success) SetOnlineStatus(error);
        });
        done(ok);
    }

    private IEnumerator CreateRoutine()
    {
        if (onlineBusy) yield break;
        onlineBusy = true;
        SetOnlineStatus("Connecting…");
        bool signedIn = false;
        yield return SignIn(ok => signedIn = ok);
        if (!signedIn) { onlineBusy = false; yield break; }

        string code = null;
        yield return OnlineClient.Instance.CreateRoom("4x4x4", (c, error) =>
        {
            code = c;
            if (c == null) SetOnlineStatus(error);
        });
        if (code == null) { onlineBusy = false; yield break; }

        ListenForStart();
        OnlineClient.Instance.JoinRoom(code);
        SetOnlineStatus($"Your game code is {code}. Send it to a friend; the game starts when they join.");
    }

    private IEnumerator JoinRoutine(string code)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        if (code.Length != 6) { SetOnlineStatus("Game codes are 6 letters and numbers"); yield break; }
        if (onlineBusy) yield break;
        onlineBusy = true;
        SetOnlineStatus("Joining…");
        bool signedIn = false;
        yield return SignIn(ok => signedIn = ok);
        if (!signedIn) { onlineBusy = false; yield break; }
        ListenForStart();
        OnlineClient.Instance.JoinRoom(code);
        // If the room is full or missing, the socket closes and OnClosed reports it
    }

    private IEnumerator MatchRoutine()
    {
        if (onlineBusy) yield break;
        onlineBusy = true;
        SetSearching("Connecting…");
        bool signedIn = false;
        yield return SignIn(ok => signedIn = ok);
        if (!signedIn) { SetSearching(null); onlineBusy = false; yield break; }
        if (!onlineBusy) yield break;  // cancelled while signing in
        ListenForStart();
        SetSearching("Finding you an opponent…");
        OnlineClient.Instance.FindMatch("4x4x4",
            code => { SetSearching("Opponent found!"); OnlineClient.Instance.JoinRoom(code); },
            error => { SetSearching(null); SetOnlineStatus(error); onlineBusy = false; });
    }

    private void ListenForStart()
    {
        var client = OnlineClient.Instance;
        client.OnMessage -= OnOnlineMessage;
        client.OnMessage += OnOnlineMessage;
        client.OnClosed -= OnOnlineClosed;
        client.OnClosed += OnOnlineClosed;
    }

    private void OnOnlineClosed(string reason)
    {
        if (onlineStarting || !onlineScreen.activeInHierarchy) return;
        SetOnlineStatus("Couldn't join that game. Check the code, or it may already be full.");
        onlineBusy = false;
    }

    private void OnOnlineMessage(ServerMessage msg)
    {
        if (onlineStarting) return;
        // Both seats filled: the room moves to piece placement and the game begins
        if ((msg.type == "room:joined" || msg.type == "room:playerJoined") && msg.state != null && msg.state.phase == "placement")
        {
            StartOnlineGame(msg.state);
        }
        else if (msg.type == "room:joined")
        {
            SetOnlineStatus($"Your game code is {msg.roomCode}. Waiting for your opponent to join…");
        }
    }

    private void StartOnlineGame(NetState state)
    {
        onlineStarting = true;
        var client = OnlineClient.Instance;
        client.OnMessage -= OnOnlineMessage;
        client.OnClosed -= OnOnlineClosed;

        OnlineSession.Begin(client.LocalColor);
        OnlineGameBridge.Begin();

        BoardSize size = state.config != null && state.config.boardSize == "8x8x8" ? BoardSize.Large8x8x8
                       : state.config != null && state.config.boardSize == "6x6x6" ? BoardSize.Medium6x6x6
                       : BoardSize.Small4x4x4;
        var config = new GameConfiguration(2, 0, size, AIDifficulty.Medium);
        config.playerTypes = new List<PlayerType> { PlayerType.Human, PlayerType.Human };
        config.enableChaosMode = false;
        config.enableTimedPlay = false;
        config.isOnlineGame = true;
        config.roomCode = client.RoomCode;
        config.ApplyToLegacyFields();
        SceneController.Instance.LoadGameScene(config);
    }

    // ───────────────────────── Navigation ─────────────────────────

    private void ShowMenu()
    {
        menuScreen.SetActive(true);
        setupScreen.SetActive(false);
        customizeScreen.SetActive(false);
        onlineScreen.SetActive(false);
    }

    private void ShowCustomize()
    {
        menuScreen.SetActive(false);
        setupScreen.SetActive(false);
        onlineScreen.SetActive(false);
        customizeScreen.SetActive(true);
        Refresh();
    }

    private void ShowSetup()
    {
        menuScreen.SetActive(false);
        customizeScreen.SetActive(false);
        onlineScreen.SetActive(false);
        setupScreen.SetActive(true);
        Refresh();
    }

    private void ShowToast(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        toastText.text = message;
        yield return new WaitForSeconds(2.5f);
        toastText.text = "";
    }

    private void StartQuickPlay()
    {
        playerCount = 2;
        boardSize = BoardSize.Small4x4x4;
        difficulty = AIDifficulty.Medium;
        playerTypes.Clear();
        playerTypes.Add(PlayerType.Human);
        playerTypes.Add(PlayerType.Computer);
        chaosMode = false;
        timedPlay = false;
        StartFromSetup();
    }

    private void StartFromSetup()
    {
        var config = new GameConfiguration();
        config.playerCount = playerCount;
        config.boardSize = boardSize;
        config.aiDifficulty = difficulty;
        config.playerTypes = new List<PlayerType>(playerTypes);
        int robots = 0;
        foreach (PlayerType t in playerTypes) if (t == PlayerType.Computer) robots++;
        config.aiPlayerCount = robots;
        config.enableChaosMode = chaosMode;
        config.chaosTurnInterval = 9;
        config.enableTimedPlay = timedPlay;
        config.timePerPlayerMinutes = 10;
        if (playerCount == 2) config.ApplyToLegacyFields();

        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadGameScene(config);
        }
        else
        {
            Debug.LogError("NeonMenu: SceneController.Instance not found");
            ShowToast("Couldn't start the game");
        }
    }

    // ───────────────────────── Building blocks ─────────────────────────

    private Button NeonButton(Transform parent, string text, Vector2 size, Color fill, Color shadow, float tilt, float fontSize, System.Action onClick)
    {
        Image image = NewImage(text, parent, fill);
        image.rectTransform.sizeDelta = size;
        NeonTheme.StyleFilled(image, fill, shadow, tilt);
        Button button = image.gameObject.AddComponent<Button>();
        NeonTheme.NeutralTint(button);
        button.onClick.AddListener(() => onClick());
        TextMeshProUGUI label = NewText("Text", image.transform, text, fontSize, NeonTheme.Ground, TextAlignmentOptions.Center);
        Stretch(label.rectTransform);
        return button;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        RectTransform rt = NewRect(name, parent);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        RectTransform rt = NewRect(name, parent);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        NeonTheme.ApplyFont(tmp);
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = false;
        return tmp;
    }

    private static void Stretch(RectTransform rt, Vector2 offset = default)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = offset;
        rt.offsetMax = offset;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 size, Vector2 position)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = position;
    }
}
