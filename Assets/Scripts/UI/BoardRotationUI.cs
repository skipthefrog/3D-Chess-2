using UnityEngine;

public class BoardRotationUI : MonoBehaviour
{
    [Header("UI Settings")]
    public float buttonSize = 60f;
    public float screenEdgeOffset = 20f;
    public bool showRotationButtons = false; // Disabled by default - users can use Q/E keyboard shortcuts instead
    public bool showRotationStatus = false; // Hide status display to make room for check status
    
    [Header("Colors")]
    public Color buttonColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    public Color buttonHoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
    public Color textColor = Color.white;
    
    private Rect leftButtonRect;
    private Rect rightButtonRect;
    private Rect statusRect;
    
    private bool leftButtonHover = false;
    private bool rightButtonHover = false;
    
    private GUIStyle buttonStyle;
    private GUIStyle textStyle;
    private GUIStyle statusStyle;
    
    private void Start()
    {
        SetupGUI();
        UpdateButtonPositions();
    }
    
    private void SetupGUI()
    {
        // Button style
        buttonStyle = new GUIStyle();
        buttonStyle.fontSize = 24;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.normal.textColor = textColor;
        
        // Text style
        textStyle = new GUIStyle();
        textStyle.fontSize = 16;
        textStyle.alignment = TextAnchor.MiddleCenter;
        textStyle.normal.textColor = textColor;
        
        // Status style
        statusStyle = new GUIStyle();
        statusStyle.fontSize = 14;
        statusStyle.alignment = TextAnchor.UpperLeft;
        statusStyle.normal.textColor = textColor;
    }
    
    private void UpdateButtonPositions()
    {
        float centerY = Screen.height / 2f - buttonSize / 2f;
        
        // Left rotation button (bottom left)
        leftButtonRect = new Rect(
            screenEdgeOffset,
            Screen.height - buttonSize - screenEdgeOffset,
            buttonSize,
            buttonSize
        );
        
        // Right rotation button (bottom right)
        rightButtonRect = new Rect(
            Screen.width - buttonSize - screenEdgeOffset,
            Screen.height - buttonSize - screenEdgeOffset,
            buttonSize,
            buttonSize
        );
        
        // Status display (top center)
        statusRect = new Rect(
            Screen.width / 2f - 100f,
            screenEdgeOffset,
            200f,
            60f
        );
    }
    
    private void OnGUI()
    {
        if (!showRotationButtons) return;
        
        // Only show rotation UI in Overview mode
        if (!ShouldShowRotationUI()) return;
        
        UpdateButtonPositions(); // Update for screen size changes
        
        // Draw rotation status
        DrawRotationStatus();
        
        // Draw rotation buttons
        DrawRotationButtons();
        
        // Handle input
        HandleGUIInput();
    }
    
    private void DrawRotationStatus()
    {
        // Status display is disabled to make room for check status UI
        if (!showRotationStatus)
        {
            return;
        }
        
        GUI.Box(statusRect, "", GUI.skin.box);
        
        string statusText = "Board Rotation\\n";
        if (BoardRotator.Instance != null)
        {
            float currentRotation = BoardRotator.Instance.GetCurrentRotation();
            statusText += $"{currentRotation:F0}°";
            
            if (BoardRotator.Instance.IsRotating())
            {
                statusText += " (Rotating...)";
            }
        }
        else
        {
            statusText += "N/A";
        }
        
        GUI.Label(statusRect, statusText, statusStyle);
    }
    
    private void DrawRotationButtons()
    {
        // Left rotation button
        Color leftColor = leftButtonHover ? buttonHoverColor : buttonColor;
        DrawButton(leftButtonRect, "↺", leftColor, () => {
            if (BoardRotator.Instance != null)
            {
                BoardRotator.Instance.RotateLeft();
                Debug.Log("UI: Board rotated left");
            }
        });
        
        // Right rotation button  
        Color rightColor = rightButtonHover ? buttonHoverColor : buttonColor;
        DrawButton(rightButtonRect, "↻", rightColor, () => {
            if (BoardRotator.Instance != null)
            {
                BoardRotator.Instance.RotateRight();
                Debug.Log("UI: Board rotated right");
            }
        });
        
        // Add labels
        Rect leftLabelRect = new Rect(leftButtonRect.x, leftButtonRect.y - 25f, leftButtonRect.width, 20f);
        Rect rightLabelRect = new Rect(rightButtonRect.x, rightButtonRect.y - 25f, rightButtonRect.width, 20f);
        
        GUI.Label(leftLabelRect, "Rotate\\nLeft", textStyle);
        GUI.Label(rightLabelRect, "Rotate\\nRight", textStyle);
    }
    
    private void DrawButton(Rect rect, string text, Color color, System.Action onClick)
    {
        // Draw button background
        Color oldColor = GUI.backgroundColor;
        GUI.backgroundColor = color;
        
        bool clicked = GUI.Button(rect, text, buttonStyle);
        
        GUI.backgroundColor = oldColor;
        
        if (clicked)
        {
            onClick?.Invoke();
        }
    }
    
    private void HandleGUIInput()
    {
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y; // Flip Y coordinate for GUI
        
        // Check button hover states
        leftButtonHover = leftButtonRect.Contains(mousePos);
        rightButtonHover = rightButtonRect.Contains(mousePos);
    }
    
    private void Update()
    {
        // Only handle input in Overview mode
        if (!ShouldShowRotationUI()) return;
        
        // Alternative keyboard controls (backup for when Q/E don't work)
        if (InputHelper.GetKeyDown(KeyCode.A) || InputHelper.GetKeyDown(KeyCode.LeftArrow))
        {
            if (BoardRotator.Instance != null)
            {
                BoardRotator.Instance.RotateLeft();
                Debug.Log("Keyboard: Board rotated left (A/←)");
            }
        }
        else if (InputHelper.GetKeyDown(KeyCode.D) || InputHelper.GetKeyDown(KeyCode.RightArrow))
        {
            if (BoardRotator.Instance != null)
            {
                BoardRotator.Instance.RotateRight();
                Debug.Log("Keyboard: Board rotated right (D/→)");
            }
        }
        
        // (Tapping the screen edges used to rotate the board a quarter turn. The edges hold the
        // floor and player view buttons, so that is off; drag to orbit or use the view buttons.)
    }
    
    private void HandleTouchRotation()
    {
        Touch touch = InputHelper.GetTouch(0);
        
        if (touch.phase == TouchPhase.Began)
        {
            Vector2 normalizedPos = new Vector2(
                touch.position.x / Screen.width,
                touch.position.y / Screen.height
            );
            
            // Expanded edge zones for easier touch control
            float edgeZone = 0.25f; // 25% of screen width
            
            if (normalizedPos.x < edgeZone) // Left edge - rotate left
            {
                if (BoardRotator.Instance != null)
                {
                    BoardRotator.Instance.RotateLeft();
                    Debug.Log("Touch: Board rotated left (screen edge)");
                }
            }
            else if (normalizedPos.x > (1f - edgeZone)) // Right edge - rotate right
            {
                if (BoardRotator.Instance != null)
                {
                    BoardRotator.Instance.RotateRight();
                    Debug.Log("Touch: Board rotated right (screen edge)");
                }
            }
        }
    }
    
    public void SetButtonsVisible(bool visible)
    {
        showRotationButtons = visible;
    }
    
    public void ToggleButtons()
    {
        showRotationButtons = !showRotationButtons;
        Debug.Log($"BoardRotationUI: Buttons {(showRotationButtons ? "shown" : "hidden")}");
    }
    
    /// <summary>
    /// Check if rotation UI should be shown (only in Overview mode)
    /// </summary>
    private bool ShouldShowRotationUI()
    {
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController == null) return true; // Default to showing UI if no camera controller
        
        // Only show rotation controls in Overview mode
        return cameraController.currentMode == CameraMode.Overview;
    }
}