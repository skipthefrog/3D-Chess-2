using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Comprehensive diagnostic test for multi-player toggle interface issues
/// </summary>
public class Test_ComprehensiveToggleDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔬 Test_ComprehensiveToggleDebug: Starting comprehensive diagnostics");
        StartCoroutine(RunComprehensiveDiagnostics());
    }
    
    IEnumerator RunComprehensiveDiagnostics()
    {
        yield return new WaitForSeconds(2f);
        
        Debug.Log("🔬 === COMPREHENSIVE MULTI-PLAYER TOGGLE DIAGNOSTICS ===");
        
        // Test 1: Find MainMenuController
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller == null)
        {
            Debug.LogError("🔬 ❌ CRITICAL: MainMenuController not found!");
            yield break;
        }
        Debug.Log("🔬 ✅ MainMenuController found");
        
        yield return new WaitForSeconds(0.5f);
        
        // Test 2: Check for compilation errors by testing method existence
        TestMethodExistence(controller);
        
        yield return new WaitForSeconds(0.5f);
        
        // Test 3: Check side selection panel existence
        TestSideSelectionPanel(controller);
        
        yield return new WaitForSeconds(0.5f);
        
        // Test 4: Test button creation and setup
        TestButtonCreationAndSetup(controller);
        
        yield return new WaitForSeconds(0.5f);
        
        // Test 5: Force test the debugging by creating a test scenario
        yield return StartCoroutine(ForceTestToggleInterface(controller));
        
        Debug.Log("🔬 === DIAGNOSTICS COMPLETE ===");
        LogDiagnosticSummary();
    }
    
    void TestMethodExistence(MainMenuController controller)
    {
        Debug.Log("🔬 Testing method existence...");
        
        var onManualMethod = typeof(MainMenuController).GetMethod("OnManualPositionSelection", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Debug.Log($"🔬 OnManualPositionSelection method: {(onManualMethod != null ? "EXISTS ✅" : "MISSING ❌")}");
        
        var showMultiMethod = typeof(MainMenuController).GetMethod("ShowMultiPlayerPositionSelection", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Debug.Log($"🔬 ShowMultiPlayerPositionSelection method: {(showMultiMethod != null ? "EXISTS ✅" : "MISSING ❌")}");
        
        var createButtonMethod = typeof(MainMenuController).GetMethod("CreateUIButton", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Debug.Log($"🔬 CreateUIButton method: {(createButtonMethod != null ? "EXISTS ✅" : "MISSING ❌")}");
    }
    
    void TestSideSelectionPanel(MainMenuController controller)
    {
        Debug.Log("🔬 Testing side selection panel...");
        
        var panelField = typeof(MainMenuController).GetField("sideSelectionPanel", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        
        if (panelField != null)
        {
            GameObject panel = panelField.GetValue(controller) as GameObject;
            Debug.Log($"🔬 Side selection panel: {(panel != null ? "EXISTS ✅" : "NULL ❌")}");
            
            if (panel != null)
            {
                Debug.Log($"🔬 Panel active: {panel.activeInHierarchy}");
                Debug.Log($"🔬 Panel child count: {panel.transform.childCount}");
                
                // Look for the Choose My Position button
                Transform chooseButton = panel.transform.Find("ChoosePositionButton");
                Debug.Log($"🔬 ChoosePositionButton: {(chooseButton != null ? "EXISTS ✅" : "NOT FOUND ❌")}");
                
                if (chooseButton != null)
                {
                    Button buttonComponent = chooseButton.GetComponent<Button>();
                    Debug.Log($"🔬 Button component: {(buttonComponent != null ? "EXISTS ✅" : "MISSING ❌")}");
                    Debug.Log($"🔬 Button interactable: {buttonComponent?.interactable}");
                    Debug.Log($"🔬 Button click listener count: {buttonComponent?.onClick.GetPersistentEventCount()}");
                }
            }
        }
        else
        {
            Debug.LogError("🔬 ❌ sideSelectionPanel field not found!");
        }
    }
    
    void TestButtonCreationAndSetup(MainMenuController controller)
    {
        Debug.Log("🔬 Testing button creation method...");
        
        try
        {
            // Test if we can call CreateUIButton method via reflection
            var createButtonMethod = typeof(MainMenuController).GetMethod("CreateUIButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new System.Type[] { typeof(string), typeof(string), typeof(GameObject), 
                                   typeof(Vector2), typeof(Vector2), typeof(System.Action) },
                null);
            
            if (createButtonMethod != null)
            {
                Debug.Log("🔬 ✅ CreateUIButton method signature is correct");
                
                // Test lambda compilation by creating a simple test lambda
                System.Action testAction = () => {
                    Debug.Log("🔬 TEST: Lambda expression works correctly");
                };
                testAction(); // Execute to verify compilation
            }
            else
            {
                Debug.LogError("🔬 ❌ CreateUIButton method signature not found!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔬 ❌ Exception testing button creation: {e.Message}");
        }
    }
    
    IEnumerator ForceTestToggleInterface(MainMenuController controller)
    {
        Debug.Log("🔬 === FORCING TOGGLE INTERFACE TEST ===");
        
        try
        {
            // Set up test configuration
            var configField = typeof(MainMenuController).GetField("currentConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (configField != null)
            {
                // Create 4-player, 3-AI configuration
                var config = System.Activator.CreateInstance(configField.FieldType);
                
                // Set properties via reflection
                config.GetType().GetProperty("playerCount")?.SetValue(config, 4);
                config.GetType().GetProperty("aiPlayerCount")?.SetValue(config, 3);
                
                configField.SetValue(controller, config);
                
                Debug.Log("🔬 ✅ Test configuration set: 4 players, 3 AI");
                
                yield return new WaitForSeconds(0.5f);
                
                // Try to call OnManualPositionSelection directly
                var onManualMethod = typeof(MainMenuController).GetMethod("OnManualPositionSelection", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (onManualMethod != null)
                {
                    Debug.Log("🔬 🚀 INVOKING OnManualPositionSelection() DIRECTLY...");
                    onManualMethod.Invoke(controller, null);
                    Debug.Log("🔬 🚀 OnManualPositionSelection() invocation completed");
                }
                else
                {
                    Debug.LogError("🔬 ❌ OnManualPositionSelection method not accessible!");
                }
            }
            else
            {
                Debug.LogError("🔬 ❌ currentConfig field not accessible!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🔬 ❌ Exception during forced test: {e.Message}");
            Debug.LogError($"🔬 Stack trace: {e.StackTrace}");
        }
    }
    
    void LogDiagnosticSummary()
    {
        Debug.Log("🔬 === DIAGNOSTIC SUMMARY ===");
        Debug.Log("🔬 WHAT TO LOOK FOR IN THE LOGS ABOVE:");
        Debug.Log("  ✅ All methods should exist");
        Debug.Log("  ✅ Side selection panel should exist");
        Debug.Log("  ✅ ChoosePositionButton should exist when in side selection");
        Debug.Log("  🚀 Direct method invocation should show 🎯🔥 logs");
        Debug.Log("  🚀 If successful, should also show 🎮🔥 toggle creation logs");
        Debug.Log("");
        Debug.Log("🔬 TROUBLESHOOTING:");
        Debug.Log("  ❌ If methods are missing: Compilation error or code not saved");
        Debug.Log("  ❌ If panel is null: UI setup issue or wrong game state");
        Debug.Log("  ❌ If button missing: Button creation failed or wrong UI state");
        Debug.Log("  ❌ If direct invocation fails: Method signature issue");
        Debug.Log("  ❌ If no 🎯🔥 logs: Method not executing (compilation issue)");
        Debug.Log("  ❌ If 🎯🔥 but no 🎮🔥: ShowMultiPlayerPositionSelection not working");
        Debug.Log("");
        Debug.Log("🔬 NEXT STEPS IF ISSUES FOUND:");
        Debug.Log("  1. Check Unity console for compilation errors");
        Debug.Log("  2. Verify MainMenuController.cs was saved properly");
        Debug.Log("  3. Restart Unity editor if needed");
        Debug.Log("  4. Ensure correct scene is loaded");
    }
}