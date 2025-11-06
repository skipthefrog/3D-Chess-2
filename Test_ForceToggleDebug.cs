using UnityEngine;
using System.Collections;

/// <summary>
/// Force test the multi-player toggle interface by directly calling methods
/// </summary>
public class Test_ForceToggleDebug : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🚀 Test_ForceToggleDebug: Starting forced toggle interface test");
        StartCoroutine(RunForcedToggleTest());
    }
    
    IEnumerator RunForcedToggleTest()
    {
        yield return new WaitForSeconds(3f); // Wait for scene initialization
        
        Debug.Log("🚀 === FORCED MULTI-PLAYER TOGGLE TEST ===" );
        
        // Find MainMenuController
        MainMenuController controller = FindObjectOfType<MainMenuController>();
        if (controller == null)
        {
            Debug.LogError("🚀 ❌ MainMenuController not found!");
            yield break;
        }
        
        Debug.Log("🚀 ✅ MainMenuController found - setting up test configuration");
        
        // Access private fields using reflection to set up test state
        try
        {
            // Get currentConfig field
            var configField = typeof(MainMenuController).GetField("currentConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var sideSelectionPanelField = typeof(MainMenuController).GetField("sideSelectionPanel", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (configField == null || sideSelectionPanelField == null)
            {
                Debug.LogError("🚀 ❌ Could not access required fields via reflection");
                yield break;
            }
            
            // Create test configuration (4 players, 3 AI - needs side selection)
            var config = System.Activator.CreateInstance(configField.FieldType, 4, 3, 
                System.Enum.Parse(typeof(BoardSize), "Medium6x6x6"));
            configField.SetValue(controller, config);
            
            Debug.Log("🚀 ✅ Test configuration created: 4 players, 3 AI");
            
            // Get or create side selection panel
            GameObject sidePanel = sideSelectionPanelField.GetValue(controller) as GameObject;
            if (sidePanel == null)
            {
                Debug.Log("🚀 Creating side selection panel for test...");
                // Create a minimal panel
                sidePanel = new GameObject("SideSelectionPanel");
                sidePanel.transform.SetParent(controller.transform, false);
                var rectTransform = sidePanel.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                sideSelectionPanelField.SetValue(controller, sidePanel);
                Debug.Log("🚀 ✅ Side selection panel created");
            }
            
            yield return new WaitForSeconds(0.5f);
            
            // Now directly call OnManualPositionSelection to test the debugging
            Debug.Log("🚀 === CALLING OnManualPositionSelection() DIRECTLY ===");
            
            var onManualMethod = typeof(MainMenuController).GetMethod("OnManualPositionSelection", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (onManualMethod != null)
            {
                Debug.Log("🚀 🎯 Invoking OnManualPositionSelection method...");
                onManualMethod.Invoke(controller, null);
                Debug.Log("🚀 🎯 OnManualPositionSelection method invoked!");
            }
            else
            {
                Debug.LogError("🚀 ❌ OnManualPositionSelection method not found!");
            }
            
            yield return new WaitForSeconds(1f);
            
            Debug.Log("🚀 === TEST RESULTS ===");
            Debug.Log("🚀 If you saw 🎯🔥 logs above, button click handler works!");
            Debug.Log("🚀 If you saw 🎮🔥 logs above, toggle creation works!");
            Debug.Log("🚀 If no debug logs appeared, there's a deeper issue.");
            
        }
        catch (System.Exception e)
        {
            Debug.LogError($"🚀 ❌ Exception during forced test: {e.Message}");
            Debug.LogError($"🚀 Stack trace: {e.StackTrace}");
        }
    }
}