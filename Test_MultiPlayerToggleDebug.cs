using UnityEngine;
using System.Collections;

/// <summary>
/// Debug test script to trace multi-player toggle interface issues
/// </summary>
public class Test_MultiPlayerToggleDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔍 Test_MultiPlayerToggleDebug: Starting debug trace");
        StartCoroutine(RunDebugTrace());
    }
    
    IEnumerator RunDebugTrace()
    {
        yield return new WaitForSeconds(5f); // Wait longer for UI to fully load
        
        Debug.Log("🔍 === MULTI-PLAYER TOGGLE DEBUG TRACE ===");
        
        // Test 1: Find and analyze MainMenuController
        AnalyzeMainMenuController();
        
        yield return new WaitForSeconds(1f);
        
        // Test 2: Check side selection panel state
        CheckSideSelectionPanel();
        
        yield return new WaitForSeconds(1f);
        
        // Test 3: Look for existing UI elements
        AnalyzeExistingUIElements();
        
        yield return new WaitForSeconds(1f);
        
        // Test 4: Monitor for button clicks
        MonitorButtonClickDetection();
        
        Debug.Log("🔍 === MULTI-PLAYER TOGGLE DEBUG TRACE COMPLETE ===");
        LogDebugSummary();
    }
    
    void AnalyzeMainMenuController()
    {
        Debug.Log("🔍 Analyzing MainMenuController state...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            Debug.Log("  ✅ MainMenuController found");
            
            // Check current step
            var stepField = typeof(MainMenuController).GetField("currentStep", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (stepField != null)
            {
                var currentStep = stepField.GetValue(controller);
                Debug.Log($"  📊 Current step: {currentStep}");
            }
            
            // Check configuration
            var configField = typeof(MainMenuController).GetField("currentConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                var config = configField.GetValue(controller);
                if (config != null)
                {
                    var playerCountProp = config.GetType().GetProperty("playerCount");
                    var aiPlayerCountProp = config.GetType().GetProperty("aiPlayerCount");
                    
                    if (playerCountProp != null && aiPlayerCountProp != null)
                    {
                        Debug.Log($"  📊 Configuration: {playerCountProp.GetValue(config)} players, {aiPlayerCountProp.GetValue(config)} AI");
                    }
                }
            }
        }
        else
        {
            Debug.LogError("  ❌ MainMenuController not found!");
        }
    }
    
    void CheckSideSelectionPanel()
    {
        Debug.Log("🔍 Checking side selection panel state...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            var panelField = typeof(MainMenuController).GetField("sideSelectionPanel", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (panelField != null)
            {
                GameObject panel = panelField.GetValue(controller) as GameObject;
                if (panel != null)
                {
                    Debug.Log($"  ✅ Side selection panel found");
                    Debug.Log($"  📊 Panel active: {panel.activeInHierarchy}");
                    Debug.Log($"  📊 Panel enabled: {panel.activeSelf}");
                    Debug.Log($"  📊 Panel child count: {panel.transform.childCount}");
                    
                    // List all child objects
                    for (int i = 0; i < panel.transform.childCount; i++)
                    {
                        Transform child = panel.transform.GetChild(i);
                        Debug.Log($"    Child {i}: {child.name} (active: {child.gameObject.activeInHierarchy})");
                    }
                }
                else
                {
                    Debug.LogError("  ❌ Side selection panel is null!");
                }
            }
        }
    }
    
    void AnalyzeExistingUIElements()
    {
        Debug.Log("🔍 Analyzing existing UI elements...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            var panelField = typeof(MainMenuController).GetField("sideSelectionPanel", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (panelField != null)
            {
                GameObject panel = panelField.GetValue(controller) as GameObject;
                if (panel != null)
                {
                    // Look for specific elements
                    Transform chooseButton = panel.transform.Find("ChoosePositionButton");
                    Transform randomButton = panel.transform.Find("RandomAssignButton");
                    Transform assignmentDetails = panel.transform.Find("AssignmentDetails");
                    
                    Debug.Log($"  📊 ChoosePositionButton: {(chooseButton != null ? "EXISTS" : "NULL")}");
                    if (chooseButton != null)
                    {
                        Debug.Log($"    Active: {chooseButton.gameObject.activeInHierarchy}");
                        var button = chooseButton.GetComponent<UnityEngine.UI.Button>();
                        Debug.Log($"    Interactable: {button?.interactable}");
                    }
                    
                    Debug.Log($"  📊 RandomAssignButton: {(randomButton != null ? "EXISTS" : "NULL")}");
                    if (randomButton != null)
                    {
                        Debug.Log($"    Active: {randomButton.gameObject.activeInHierarchy}");
                    }
                    
                    Debug.Log($"  📊 AssignmentDetails: {(assignmentDetails != null ? "EXISTS" : "NULL")}");
                    if (assignmentDetails != null)
                    {
                        var text = assignmentDetails.GetComponent<TMPro.TextMeshProUGUI>();
                        if (text != null)
                        {
                            Debug.Log($"    Text content: '{text.text}'");
                        }
                    }
                    
                    // Look for toggle buttons
                    for (int i = 0; i < 6; i++)
                    {
                        Transform toggle = panel.transform.Find($"PlayerToggle{i}");
                        Debug.Log($"  📊 PlayerToggle{i}: {(toggle != null ? "EXISTS" : "NULL")}");
                        if (toggle != null)
                        {
                            Debug.Log($"    Active: {toggle.gameObject.activeInHierarchy}");
                        }
                    }
                }
            }
        }
    }
    
    void MonitorButtonClickDetection()
    {
        Debug.Log("🔍 Monitoring button click detection...");
        
        Debug.Log("  ℹ️  Instructions for manual testing:");
        Debug.Log("    1. Navigate to Side Selection step (Step 3)");
        Debug.Log("    2. Look for [Choose My Position] button");
        Debug.Log("    3. Click the button");
        Debug.Log("    4. Watch for debug logs starting with 🎯🔥");
        Debug.Log("    5. If no logs appear, button click handler isn't connected");
        Debug.Log("    6. If logs appear but no toggles show, UI creation issue");
        Debug.Log("    7. If toggles show but aren't clickable, toggle handler issue");
    }
    
    void LogDebugSummary()
    {
        Debug.Log("🔍 MULTI-PLAYER TOGGLE DEBUG SUMMARY:");
        Debug.Log("");
        Debug.Log("🔍 DEBUGGING FEATURES ADDED:");
        Debug.Log("  ✅ Comprehensive logging in OnManualPositionSelection");
        Debug.Log("  ✅ Detailed toggle creation tracing in ShowMultiPlayerPositionSelection");
        Debug.Log("  ✅ Button click handler debugging in OnPlayerTypeToggleClicked");
        Debug.Log("  ✅ Layout positioning fixes to prevent overlap");
        Debug.Log("  ✅ Error handling and null checks");
        
        Debug.Log("");
        Debug.Log("🔍 DEBUG LOG MARKERS TO WATCH FOR:");
        Debug.Log("  🎯🔥 OnManualPositionSelection: BUTTON CLICKED! <- Button click detected");
        Debug.Log("  🎮🔥 ShowMultiPlayerPositionSelection: ENTRY <- Toggle creation started");  
        Debug.Log("  🎮🔥 ✅ Successfully created toggle X <- Each toggle created");
        Debug.Log("  🎮🔥 OnPlayerTypeToggleClicked: TOGGLE X CLICKED! <- Toggle clicked");
        
        Debug.Log("");
        Debug.Log("🔍 TROUBLESHOOTING GUIDE:");
        Debug.Log("  ❌ No 🎯🔥 logs → Button click not detected (UI connection issue)");
        Debug.Log("  ✅ 🎯🔥 but no 🎮🔥 logs → Toggle creation failing");
        Debug.Log("  ✅ 🎮🔥 creation but no toggles visible → Layout/positioning issue");
        Debug.Log("  ✅ Toggles visible but not clickable → Click handler connection issue");
        
        Debug.Log("");
        Debug.Log("🔍 NEXT STEPS: Click 'Choose My Position' and monitor debug output!");
    }
}