using UnityEngine;
using System.Collections;

/// <summary>
/// Simple debug script to attach to a GameObject to verify multi-player toggle debugging
/// </summary>
public class Test_DebugToggleIssue : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🔍 Test_DebugToggleIssue: Debug script started");
        StartCoroutine(RunToggleDebugTest());
    }
    
    IEnumerator RunToggleDebugTest()
    {
        yield return new WaitForSeconds(2f); // Wait for scene initialization
        
        Debug.Log("🔍 === MULTI-PLAYER TOGGLE DEBUG TEST ===" );
        
        // Find MainMenuController
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller != null)
        {
            Debug.Log("🔍 ✅ MainMenuController found successfully");
            
            // Check current step via reflection
            var stepField = typeof(MainMenuController).GetField("currentStep", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (stepField != null)
            {
                var currentStep = stepField.GetValue(controller);
                Debug.Log($"🔍 Current menu step: {currentStep}");
            }
            
            // Check current configuration
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
                        Debug.Log($"🔍 Configuration: {playerCountProp.GetValue(config)} players, {aiPlayerCountProp.GetValue(config)} AI");
                    }
                }
            }
            
            Debug.Log("🔍 EXPECTED BEHAVIOR:");
            Debug.Log("  1. Navigate to Side Selection (Step 3)");
            Debug.Log("  2. Click 'Choose My Position' button");
            Debug.Log("  3. Watch for debug logs starting with 🎯🔥");
            Debug.Log("  4. Should see individual player toggles created with 🎮🔥 logs");
            
            Debug.Log("🔍 IF NO DEBUG LOGS APPEAR:");
            Debug.Log("  → Button click handler might not be properly connected");
            Debug.Log("  → Check that OnManualPositionSelection method is being called");
            Debug.Log("  → Verify button creation in ShowMultiPlayerSideSelection method");
        }
        else
        {
            Debug.LogError("🔍 ❌ MainMenuController NOT found - this is the problem!");
        }
        
        Debug.Log("🔍 === END TOGGLE DEBUG TEST ===");
    }
}