using UnityEngine;

/// <summary>
/// Cross-platform input helper that works with both Legacy Input and New Input System
/// Provides fallback support when Input System package conflicts occur
/// </summary>
public static class InputHelper
{
    private static bool _useNewInputSystem = false;
    private static bool _inputSystemChecked = false;
    
    /// <summary>
    /// Check if we should use new input system or legacy input
    /// </summary>
    private static bool UseNewInputSystem()
    {
        if (!_inputSystemChecked)
        {
            // For now, always use legacy input approach to avoid conflicts
            // This can be expanded later to detect and use the new Input System when available
            _useNewInputSystem = false;
            _inputSystemChecked = true;
        }
        return _useNewInputSystem;
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetKeyDown that handles Input System conflicts
    /// </summary>
    public static bool GetKeyDown(KeyCode keyCode)
    {
        try
        {
            return Input.GetKeyDown(keyCode);
        }
        catch (System.InvalidOperationException)
        {
            // Input System conflict - return false for now
            // In production, this would use the new Input System
            return false;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetKey that handles Input System conflicts
    /// </summary>
    public static bool GetKey(KeyCode keyCode)
    {
        try
        {
            return Input.GetKey(keyCode);
        }
        catch (System.InvalidOperationException)
        {
            // Input System conflict - return false for now
            // In production, this would use the new Input System
            return false;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetMouseButton that handles Input System conflicts
    /// </summary>
    public static bool GetMouseButton(int button)
    {
        try
        {
            return Input.GetMouseButton(button);
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetMouseButtonDown that handles Input System conflicts
    /// </summary>
    public static bool GetMouseButtonDown(int button)
    {
        try
        {
            bool result = Input.GetMouseButtonDown(button);
            if (result && button == 0) // Log left mouse button clicks
            {
                Debug.Log($"InputHelper.GetMouseButtonDown: LEFT CLICK detected");
            }
            return result;
        }
        catch (System.InvalidOperationException e)
        {
            Debug.LogError($"InputHelper.GetMouseButtonDown: Input System Exception - {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetAxis that handles Input System conflicts
    /// </summary>
    public static float GetAxis(string axisName)
    {
        try
        {
            float result = Input.GetAxis(axisName);
            
            // DEBUG: Log scroll wheel input specifically
            if (axisName == "Mouse ScrollWheel" && result != 0)
            {
                Debug.Log($"🖱️ InputHelper.GetAxis: ScrollWheel detected! value={result:F4}");
            }
            
            return result;
        }
        catch (System.InvalidOperationException)
        {
            if (axisName == "Mouse ScrollWheel")
            {
                Debug.LogError("🖱️ InputHelper.GetAxis: ScrollWheel FAILED - Input System conflict!");
            }
            return 0f;
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.mousePosition that handles Input System conflicts
    /// </summary>
    public static Vector3 MousePosition
    {
        get
        {
            try
            {
                return Input.mousePosition;
            }
            catch (System.InvalidOperationException)
            {
                return Vector3.zero;
            }
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.touchCount that handles Input System conflicts
    /// </summary>
    public static int TouchCount
    {
        get
        {
            try
            {
                return Input.touchCount;
            }
            catch (System.InvalidOperationException)
            {
                return 0;
            }
        }
    }
    
    /// <summary>
    /// Safe wrapper for Input.GetTouch that handles Input System conflicts
    /// </summary>
    public static Touch GetTouch(int index)
    {
        try
        {
            return Input.GetTouch(index);
        }
        catch (System.InvalidOperationException)
        {
            // Return a default touch structure
            return new Touch();
        }
    }
    
    /// <summary>
    /// Check if touch input is available and working
    /// </summary>
    public static bool IsTouchSupported()
    {
        try
        {
            return Input.touchSupported && TouchCount >= 0;
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if mouse input is available and working
    /// </summary>
    public static bool IsMouseSupported()
    {
        try
        {
            Vector3 pos = Input.mousePosition;
            return true;
        }
        catch (System.InvalidOperationException)
        {
            return false;
        }
    }
    
    /// <summary>
    /// Get primary input position (mouse or first touch)
    /// </summary>
    public static Vector2 GetPrimaryInputPosition()
    {
        // Try touch first (mobile-first approach)
        if (IsTouchSupported() && TouchCount > 0)
        {
            return GetTouch(0).position;
        }
        
        // Fallback to mouse
        if (IsMouseSupported())
        {
            return MousePosition;
        }
        
        return Vector2.zero;
    }
    
    /// <summary>
    /// Check if primary input is pressed (touch began or mouse down)
    /// </summary>
    public static bool GetPrimaryInputDown()
    {
        // Try touch first
        if (IsTouchSupported() && TouchCount > 0)
        {
            return GetTouch(0).phase == TouchPhase.Began;
        }
        
        // Fallback to mouse
        return GetMouseButtonDown(0);
    }
    
    /// <summary>
    /// Check if primary input is being held (touch stationary/moved or mouse held)
    /// </summary>
    public static bool GetPrimaryInput()
    {
        // Try touch first
        if (IsTouchSupported() && TouchCount > 0)
        {
            TouchPhase phase = GetTouch(0).phase;
            return phase == TouchPhase.Stationary || phase == TouchPhase.Moved;
        }
        
        // Fallback to mouse
        return GetMouseButton(0);
    }
}