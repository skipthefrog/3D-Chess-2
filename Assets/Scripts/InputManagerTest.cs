using UnityEngine;

// Simple test to verify Unity can compile and run a basic input test
public class InputManagerTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("🧪 InputManagerTest.Start: HELLO - This script is working!");
        Debug.Log($"InputManagerTest: GameObject = {gameObject.name}");
        Debug.Log($"InputManagerTest: Time = {Time.time}");
    }
    
    void Update()
    {
        // DISABLED: Test scripts should not run during normal gameplay
        // This was causing performance issues and excessive logging
        
        /*
        // Test basic input detection every second
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"🧪 InputManagerTest.Update: Frame {Time.frameCount}, Time {Time.time:F1}");
            
            // Test mouse input
            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log("🧪 InputManagerTest: LEFT MOUSE CLICK DETECTED!");
                Vector3 mousePos = Input.mousePosition;
                Debug.Log($"🧪 Mouse position: {mousePos}");
            }
        }
        
        // Test click detection
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🧪 InputManagerTest: MOUSE CLICK!");
            Debug.Log($"🧪 Mouse pos: {Input.mousePosition}");
        }
        */
    }
}