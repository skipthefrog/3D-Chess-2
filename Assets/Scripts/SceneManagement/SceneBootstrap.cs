using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Emergency bootstrap script that ensures we start with MainMenu scene.
/// This runs before any scene objects are created and provides a backup
/// in case the SceneController approach fails.
/// </summary>
public static class SceneBootstrap
{
    private const string MAIN_MENU_SCENE_NAME = "MainMenu";
    private const string GAME_SCENE_NAME = "SampleScene";
    
    /// <summary>
    /// This method runs automatically when Unity starts, before any scene objects are created.
    /// It provides a failsafe to ensure we always start with the MainMenu scene.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        Debug.Log("🚀 SceneBootstrap.InitializeOnLoad: CALLED - Bootstrap starting up!");
        Debug.Log($"🚀 SceneBootstrap: Unity time={Time.time}");
        
        try 
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            Debug.Log($"🚀 SceneBootstrap: Current scene at bootstrap: '{currentSceneName}'");
            
            // If we're not in the MainMenu scene, force load it
            if (currentSceneName != MAIN_MENU_SCENE_NAME)
            {
                Debug.Log($"🚨 SceneBootstrap: EMERGENCY REDIRECT! Not in MainMenu scene, redirecting from '{currentSceneName}' to '{MAIN_MENU_SCENE_NAME}'");
                
                // Force immediate scene load
                SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
                
                Debug.Log("🚨 SceneBootstrap: Emergency redirect initiated");
            }
            else
            {
                Debug.Log("✅ SceneBootstrap: Already in MainMenu scene, bootstrap complete");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ SceneBootstrap: Error during bootstrap: {e.Message}");
            Debug.LogError($"❌ SceneBootstrap: Stack trace: {e.StackTrace}");
            
            // Even if there's an error, try to load MainMenu as last resort
            try 
            {
                Debug.Log("🆘 SceneBootstrap: Attempting emergency MainMenu load as last resort");
                SceneManager.LoadScene(MAIN_MENU_SCENE_NAME);
            }
            catch (System.Exception e2)
            {
                Debug.LogError($"💀 SceneBootstrap: Even emergency load failed: {e2.Message}");
            }
        }
    }
    
    /// <summary>
    /// Alternative initialization that runs after scene load, as additional backup
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeAfterSceneLoad()
    {
        Debug.Log("🔄 SceneBootstrap.InitializeAfterSceneLoad: CALLED - Post-scene bootstrap check");
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"🔄 SceneBootstrap: Scene after load: '{currentSceneName}'");
        
        if (currentSceneName != MAIN_MENU_SCENE_NAME)
        {
            Debug.LogWarning($"⚠️ SceneBootstrap: Still not in MainMenu after bootstrap! Current: '{currentSceneName}'");
            Debug.LogWarning("⚠️ SceneBootstrap: This suggests a deeper issue with scene loading");
        }
        else
        {
            Debug.Log("✅ SceneBootstrap: Successfully in MainMenu scene after bootstrap");
        }
    }
}