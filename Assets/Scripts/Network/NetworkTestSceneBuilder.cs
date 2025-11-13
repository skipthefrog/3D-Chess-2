/**
 * Network Test Scene Builder
 * Automatically creates the complete network test scene with all UI elements
 *
 * USAGE:
 * 1. Create a new empty scene in Unity
 * 2. Create an empty GameObject
 * 3. Attach this script to it
 * 4. Click the "Build Test Scene" button in the Inspector
 * 5. Delete this GameObject when done (optional)
 */

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;

[ExecuteInEditMode]
public class NetworkTestSceneBuilder : MonoBehaviour
{
    [Header("Click to Build Scene")]
    [SerializeField] private bool buildScene = false;

    private void Update()
    {
        if (buildScene)
        {
            buildScene = false;
            BuildTestScene();
        }
    }

    [ContextMenu("Build Test Scene")]
    public void BuildTestScene()
    {
        Debug.Log("🏗️ Building Network Test Scene...");

        // Step 1: Create NetworkManager
        GameObject networkManager = CreateNetworkManager();

        // Step 2: Create Canvas
        GameObject canvas = CreateCanvas();

        // Step 3: Create UI Elements
        CreateStatusPanel(canvas);
        var controlPanel = CreateControlPanel(canvas);
        var logPanel = CreateLogPanel(canvas);

        // Step 4: Create NetworkTestUI controller
        CreateNetworkTestController(canvas, controlPanel, logPanel);

        Debug.Log("✅ Network Test Scene built successfully!");
        Debug.Log("📝 Next step: Enter Play Mode and click 'Connect'");

        EditorUtility.SetDirty(gameObject.scene.GetRootGameObjects()[0]);
    }

    private GameObject CreateNetworkManager()
    {
        // Check if NetworkManager already exists
        var existing = FindObjectOfType<NetworkManager>();
        if (existing != null)
        {
            Debug.Log("✅ NetworkManager already exists");
            return existing.gameObject;
        }

        GameObject obj = new GameObject("NetworkManager");
        obj.AddComponent<NetworkManager>();

        Debug.Log("✅ Created NetworkManager");
        return obj;
    }

    private GameObject CreateCanvas()
    {
        GameObject canvas = new GameObject("Canvas");
        Canvas canvasComponent = canvas.AddComponent<Canvas>();
        canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvas.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvas.AddComponent<GraphicRaycaster>();

        // Add EventSystem if it doesn't exist
        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        Debug.Log("✅ Created Canvas");
        return canvas;
    }

    private void CreateStatusPanel(GameObject canvas)
    {
        // Status Panel
        GameObject statusPanel = CreateUIObject("StatusPanel", canvas.transform);
        RectTransform statusRect = statusPanel.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0, 1);
        statusRect.anchorMax = new Vector2(1, 1);
        statusRect.pivot = new Vector2(0.5f, 1);
        statusRect.anchoredPosition = new Vector2(0, 0);
        statusRect.sizeDelta = new Vector2(0, 100);

        Image statusImage = statusPanel.AddComponent<Image>();
        statusImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        // Status Text
        GameObject statusText = CreateUIObject("StatusText", statusPanel.transform);
        RectTransform statusTextRect = statusText.AddComponent<RectTransform>();
        statusTextRect.anchorMin = Vector2.zero;
        statusTextRect.anchorMax = Vector2.one;
        statusTextRect.offsetMin = Vector2.zero;
        statusTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = statusText.AddComponent<TextMeshProUGUI>();
        textComponent.text = "🔴 DISCONNECTED";
        textComponent.fontSize = 36;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.red;

        Debug.Log("✅ Created Status Panel");
    }

    private GameObject CreateControlPanel(GameObject canvas)
    {
        // Control Panel
        GameObject controlPanel = CreateUIObject("ControlPanel", canvas.transform);
        RectTransform controlRect = controlPanel.AddComponent<RectTransform>();
        controlRect.anchorMin = new Vector2(0, 0.3f);
        controlRect.anchorMax = new Vector2(1, 0.9f);
        controlRect.offsetMin = new Vector2(20, 0);
        controlRect.offsetMax = new Vector2(-20, -120);

        Image controlImage = controlPanel.AddComponent<Image>();
        controlImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);

        VerticalLayoutGroup layout = controlPanel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 15;
        layout.childControlHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        // Create UI elements
        CreateLabel("Network Test Controls", controlPanel.transform, 28);
        CreateInputField("ServerUrlInput", "Server URL", "http://localhost:3000", controlPanel.transform);
        CreateInputField("PlayerNameInput", "Player Name", "TestPlayer_" + Random.Range(1000, 9999), controlPanel.transform);
        CreateInputField("RoomCodeInput", "Room Code", "", controlPanel.transform);

        CreateButton("ConnectButton", "Connect to Server", controlPanel.transform);
        CreateButton("DisconnectButton", "Disconnect", controlPanel.transform);
        CreateButton("CreateRoomButton", "Create Room", controlPanel.transform);
        CreateButton("JoinRoomButton", "Join Room", controlPanel.transform);

        Debug.Log("✅ Created Control Panel");
        return controlPanel;
    }

    private GameObject CreateLogPanel(GameObject canvas)
    {
        // Log Panel
        GameObject logPanel = CreateUIObject("LogPanel", canvas.transform);
        RectTransform logRect = logPanel.AddComponent<RectTransform>();
        logRect.anchorMin = new Vector2(0, 0);
        logRect.anchorMax = new Vector2(1, 0.3f);
        logRect.offsetMin = new Vector2(20, 20);
        logRect.offsetMax = new Vector2(-20, 0);

        Image logImage = logPanel.AddComponent<Image>();
        logImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // Scroll View
        GameObject scrollView = CreateUIObject("ScrollView", logPanel.transform);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -10);

        ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
        scrollComponent.horizontal = false;
        scrollComponent.vertical = true;
        scrollComponent.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        // Content
        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0, 1);
        contentRect.sizeDelta = new Vector2(0, 0);

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;

        // Log Text
        GameObject logText = CreateUIObject("LogText", content.transform);
        RectTransform logTextRect = logText.AddComponent<RectTransform>();

        TextMeshProUGUI textComponent = logText.AddComponent<TextMeshProUGUI>();
        textComponent.text = "Network Test Log\n";
        textComponent.fontSize = 14;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.color = Color.white;
        textComponent.enableWordWrapping = true;
        textComponent.richText = true;

        scrollComponent.viewport = viewportRect;
        scrollComponent.content = contentRect;

        Debug.Log("✅ Created Log Panel");
        return logPanel;
    }

    private void CreateNetworkTestController(GameObject canvas, GameObject controlPanel, GameObject logPanel)
    {
        GameObject controller = new GameObject("NetworkTestController");
        NetworkTestUI testUI = controller.AddComponent<NetworkTestUI>();

        // Use reflection to set private fields
        var type = typeof(NetworkTestUI);

        // Find and assign UI elements
        SetField(testUI, "statusText", GameObject.Find("StatusText")?.GetComponent<TextMeshProUGUI>());
        SetField(testUI, "logText", GameObject.Find("LogText")?.GetComponent<TextMeshProUGUI>());
        SetField(testUI, "serverUrlInput", GameObject.Find("ServerUrlInput")?.GetComponent<TMP_InputField>());
        SetField(testUI, "playerNameInput", GameObject.Find("PlayerNameInput")?.GetComponent<TMP_InputField>());
        SetField(testUI, "roomCodeInput", GameObject.Find("RoomCodeInput")?.GetComponent<TMP_InputField>());
        SetField(testUI, "connectButton", GameObject.Find("ConnectButton")?.GetComponent<Button>());
        SetField(testUI, "disconnectButton", GameObject.Find("DisconnectButton")?.GetComponent<Button>());
        SetField(testUI, "createRoomButton", GameObject.Find("CreateRoomButton")?.GetComponent<Button>());
        SetField(testUI, "joinRoomButton", GameObject.Find("JoinRoomButton")?.GetComponent<Button>());
        SetField(testUI, "logScrollRect", GameObject.Find("ScrollView")?.GetComponent<ScrollRect>());

        EditorUtility.SetDirty(controller);

        Debug.Log("✅ Created NetworkTestController");
        Debug.Log("⚠️ Note: Some fields may need manual assignment in Inspector");
    }

    private void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

        if (field != null && value != null)
        {
            field.SetValue(target, value);
        }
    }

    // Helper methods
    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private void CreateLabel(string text, Transform parent, int fontSize = 18)
    {
        GameObject label = CreateUIObject("Label_" + text, parent);
        RectTransform rect = label.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, fontSize + 10);

        TextMeshProUGUI textComponent = label.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
    }

    private void CreateInputField(string name, string placeholder, string defaultValue, Transform parent)
    {
        GameObject inputObj = CreateUIObject(name, parent);
        RectTransform rect = inputObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 40);

        Image image = inputObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject textArea = CreateUIObject("TextArea", inputObj.transform);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 0);
        textAreaRect.offsetMax = new Vector2(-10, 0);

        GameObject text = CreateUIObject("Text", textArea.transform);
        RectTransform textRect = text.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 16;
        textComponent.color = Color.white;

        GameObject placeholderObj = CreateUIObject("Placeholder", textArea.transform);
        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        TextMeshProUGUI placeholderText = placeholderObj.AddComponent<TextMeshProUGUI>();
        placeholderText.text = placeholder;
        placeholderText.fontSize = 16;
        placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
        placeholderText.fontStyle = FontStyles.Italic;

        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        inputField.text = defaultValue;
    }

    private void CreateButton(string name, string text, Transform parent)
    {
        GameObject button = CreateUIObject(name, parent);
        RectTransform rect = button.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0, 50);

        Image image = button.AddComponent<Image>();
        image.color = new Color(0.2f, 0.4f, 0.8f, 1f);

        Button buttonComponent = button.AddComponent<Button>();
        buttonComponent.targetGraphic = image;

        var colors = buttonComponent.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.5f, 0.9f, 1f);
        colors.pressedColor = new Color(0.15f, 0.3f, 0.6f, 1f);
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        buttonComponent.colors = colors;

        GameObject textObj = CreateUIObject("Text", button.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = 20;
        textComponent.alignment = TextAlignmentOptions.Center;
        textComponent.color = Color.white;
    }
}
#endif
