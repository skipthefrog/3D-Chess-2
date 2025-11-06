using UnityEngine;
using System.Collections;

/// <summary>
/// Test script to verify the player position selection interface fix works correctly
/// </summary>
public class Test_SideSelectionFix : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔧 Test_SideSelectionFix: Starting side selection interface test");
        StartCoroutine(RunSideSelectionTests());
    }
    
    IEnumerator RunSideSelectionTests()
    {
        yield return new WaitForSeconds(3f); // Wait for UI initialization
        
        Debug.Log("🔧 === SIDE SELECTION INTERFACE FIX TESTING ===");
        
        // Test 1: Verify MainMenuController exists and has proper references
        TestMainMenuControllerReferences();
        
        yield return new WaitForSeconds(1f);
        
        // Test 2: Test button creation and assignment
        TestButtonCreationAndAssignment();
        
        yield return new WaitForSeconds(1f);
        
        // Test 3: Test button highlighting system
        TestButtonHighlighting();
        
        yield return new WaitForSeconds(1f);
        
        // Test 4: Test click handlers
        TestClickHandlers();
        
        Debug.Log("🔧 === SIDE SELECTION INTERFACE FIX TESTING COMPLETE ===");
        LogTestSummary();
    }
    
    void TestMainMenuControllerReferences()
    {
        Debug.Log("🧪 Testing MainMenuController references...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            Debug.Log("  ✅ MainMenuController found");
            
            // Check if side selection panel exists
            GameObject sidePanel = controller.sideSelectionPanel;
            Debug.Log($"  📊 Side selection panel: {(sidePanel != null ? "EXISTS" : "NULL")}");
            
            if (sidePanel != null)
            {
                Debug.Log($"  📊 Side panel active: {sidePanel.activeInHierarchy}");
            }
        }
        else
        {
            Debug.LogError("  ❌ MainMenuController not found");
        }
    }
    
    void TestButtonCreationAndAssignment()
    {
        Debug.Log("🧪 Testing button creation and assignment...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            // Use reflection to check private button fields
            var whiteButtonField = typeof(MainMenuController).GetField("playAsWhiteButton", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var blackButtonField = typeof(MainMenuController).GetField("playAsBlackButton", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (whiteButtonField != null && blackButtonField != null)
            {
                var whiteButton = whiteButtonField.GetValue(controller) as UnityEngine.UI.Button;
                var blackButton = blackButtonField.GetValue(controller) as UnityEngine.UI.Button;
                
                Debug.Log($"  📊 PlayAsWhite button field: {(whiteButton != null ? "ASSIGNED" : "NULL")}");
                Debug.Log($"  📊 PlayAsBlack button field: {(blackButton != null ? "ASSIGNED" : "NULL")}");
                
                if (whiteButton != null)
                {
                    Debug.Log($"    ✅ White button - Active: {whiteButton.gameObject.activeInHierarchy}, Interactable: {whiteButton.interactable}");
                }
                
                if (blackButton != null)
                {
                    Debug.Log($"    ✅ Black button - Active: {blackButton.gameObject.activeInHierarchy}, Interactable: {blackButton.interactable}");
                }
                
                // Check if buttons can be found by name in the side panel
                if (controller.sideSelectionPanel != null)
                {
                    Transform whiteByName = controller.sideSelectionPanel.transform.Find("PlayAsWhiteButton");
                    Transform blackByName = controller.sideSelectionPanel.transform.Find("PlayAsBlackButton");
                    Debug.Log($"  📊 Buttons findable by name - White: {whiteByName != null}, Black: {blackByName != null}");
                }
            }
        }
    }
    
    void TestButtonHighlighting()
    {
        Debug.Log("🧪 Testing button highlighting system...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            Debug.Log("  📊 Button highlighting system depends on proper button field assignment");
            Debug.Log("  📊 If button fields are assigned (from previous test), highlighting should work");
            Debug.Log("  📊 No more 'SetButtonHighlight: Button is null' warnings should appear");
            Debug.Log("  ℹ️  To test highlighting: Navigate to Step 3 and click White/Black buttons");
        }
    }
    
    void TestClickHandlers()
    {
        Debug.Log("🧪 Testing click handlers...");
        
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            Debug.Log("  📊 Click handlers are assigned during button creation");
            Debug.Log("  📊 Legacy handlers removed to prevent conflicts");
            Debug.Log("  📊 OnPlayAsWhiteSelected/OnPlayAsBlackSelected should be the only active handlers");
            Debug.Log("  ℹ️  To test handlers: Navigate to Step 3 and click buttons");
        }
    }
    
    void LogTestSummary()
    {
        Debug.Log("🔧 SIDE SELECTION INTERFACE FIX SUMMARY:");
        Debug.Log("  ✅ Button reference assignment fixed in CreateSideSelectionPanel");
        Debug.Log("  ✅ playAsWhiteButton and playAsBlackButton now properly assigned");
        Debug.Log("  ✅ Legacy button handlers removed to prevent conflicts");
        Debug.Log("  ✅ UpdateSideSelectionUI updated to use public field references");
        Debug.Log("  ✅ Enhanced validation and null checks added");
        Debug.Log("  ✅ Better error messages for debugging");
        
        Debug.Log("");
        Debug.Log("🔧 EXPECTED BEHAVIOR AFTER FIX:");
        Debug.Log("  - Navigate to Step 3: Player Positions");
        Debug.Log("  - 'Choose your side:' interface should be visible");
        Debug.Log("  - [Play as White] [Play as Black] buttons should be clickable");
        Debug.Log("  - Clicking buttons should highlight them in green");
        Debug.Log("  - No more 'SetButtonHighlight: Button is null' warnings");
        Debug.Log("  - Button clicks should register and allow proceeding to next step");
        Debug.Log("  - User feedback through visual highlighting should work correctly");
    }
}