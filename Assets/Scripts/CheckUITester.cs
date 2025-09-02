using UnityEngine;

/// <summary>
/// Simple testing component to manually trigger check UI for debugging
/// Press specific keys to test different UI systems
/// </summary>
public class CheckUITester : MonoBehaviour
{
    [Header("Test Controls")]
    public KeyCode testCheckIndicatorKey = KeyCode.T;
    public KeyCode testCheckStatusKey = KeyCode.Y;
    public KeyCode testUIManagerKey = KeyCode.U;
    
    private void Update()
    {
        // Test CheckIndicatorUI
        if (Input.GetKeyDown(testCheckIndicatorKey))
        {
            TestCheckIndicatorUI();
        }
        
        // Test CheckStatusUI  
        if (Input.GetKeyDown(testCheckStatusKey))
        {
            TestCheckStatusUI();
        }
        
        // Test UIManager
        if (Input.GetKeyDown(testUIManagerKey))
        {
            TestUIManager();
        }
    }
    
    private void TestCheckIndicatorUI()
    {
        Debug.Log("🧪 CheckUITester: Testing CheckIndicatorUI directly");
        
        if (CheckIndicatorUI.Instance != null)
        {
            Debug.Log("CheckUITester: CheckIndicatorUI.Instance found, calling TestCheckIndicator");
            CheckIndicatorUI.Instance.TestCheckIndicator(PieceColor.Black, false);
        }
        else
        {
            Debug.LogError("CheckUITester: CheckIndicatorUI.Instance is NULL!");
            
            // Try to find it manually
            CheckIndicatorUI[] indicators = FindObjectsByType<CheckIndicatorUI>(FindObjectsSortMode.None);
            Debug.Log($"CheckUITester: Found {indicators.Length} CheckIndicatorUI components in scene");
            
            if (indicators.Length > 0)
            {
                Debug.Log("CheckUITester: Testing first found CheckIndicatorUI component");
                indicators[0].TestCheckIndicator(PieceColor.Black, false);
            }
        }
    }
    
    private void TestCheckStatusUI()
    {
        Debug.Log("🧪 CheckUITester: Testing CheckStatusUI directly");
        
        if (CheckStatusUI.Instance != null)
        {
            Debug.Log("CheckUITester: CheckStatusUI.Instance found, calling TestCheckUI");
            CheckStatusUI.Instance.TestCheckUI();
        }
        else
        {
            Debug.LogError("CheckUITester: CheckStatusUI.Instance is NULL!");
            
            CheckStatusUI[] statusUIs = FindObjectsByType<CheckStatusUI>(FindObjectsSortMode.None);
            Debug.Log($"CheckUITester: Found {statusUIs.Length} CheckStatusUI components in scene");
            
            if (statusUIs.Length > 0)
            {
                Debug.Log("CheckUITester: Testing first found CheckStatusUI component");
                statusUIs[0].TestCheckUI();
            }
        }
    }
    
    private void TestUIManager()
    {
        Debug.Log("🧪 CheckUITester: Testing UIManager");
        
        if (UIManager.Instance != null)
        {
            Debug.Log("CheckUITester: UIManager.Instance found, calling TestCheckUI");
            UIManager.Instance.TestCheckUI();
        }
        else
        {
            Debug.LogError("CheckUITester: UIManager.Instance is NULL!");
            
            UIManager[] managers = FindObjectsByType<UIManager>(FindObjectsSortMode.None);
            Debug.Log($"CheckUITester: Found {managers.Length} UIManager components in scene");
            
            if (managers.Length > 0)
            {
                Debug.Log("CheckUITester: Testing first found UIManager component");
                managers[0].TestCheckUI();
            }
        }
    }
    
    // OnGUI method removed to prevent debug text from appearing in bottom left corner
    // The testing functionality via keyboard shortcuts is still available through Update()
    // private void OnGUI()
    // {
    //     // Draw instructions on screen
    //     GUIStyle style = new GUIStyle();
    //     style.fontSize = 16;
    //     style.normal.textColor = Color.white;
    //     
    //     string instructions = $"CHECK UI TESTER:\nPress {testCheckIndicatorKey} = Test CheckIndicatorUI\nPress {testCheckStatusKey} = Test CheckStatusUI\nPress {testUIManagerKey} = Test UIManager";
    //     
    //     GUI.Label(new Rect(10, Screen.height - 100, 300, 100), instructions, style);
    // }
}