using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    void Start()
    {
        Debug.Log("*** HELLO WORLD - SCRIPTS ARE WORKING! ***");
        Debug.Log("SimpleTest: Unity is running scripts successfully!");
        Debug.Log($"GameObject name: {gameObject.name}");
        Debug.Log($"Time: {System.DateTime.Now}");
    }
    
    void Update()
    {
        // Flash a message every 2 seconds to prove Update is running (proper frame-based check)
        if (Time.frameCount % 120 == 0) // Every 120 frames = 2 seconds at 60fps
        {
            Debug.Log($"SimpleTest: Update running at time {Time.time:F1}");
        }
    }
}