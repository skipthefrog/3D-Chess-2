using UnityEngine;

public enum CameraMode
{
    Overview,    // Far view for full board assessment
    Interior     // Close view for interior navigation
}

public class CameraController : MonoBehaviour
{
    [Header("Orbit Settings")]
    public Transform target;
    public float distance = 20f;
    public float minDistance = 3f; // Reduced from 12f to allow interior grid navigation
    public float maxDistance = 35f;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 2f;
    public float minVerticalAngle = -89.9f; // Nearly full 180° vertical rotation - avoids gimbal lock
    public float maxVerticalAngle = 89.9f;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 2f;
    public bool invertZoom = false;
    public AnimationCurve zoomCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f); // Slower zoom when close
    
    [Header("Collision Settings")]
    public bool enableCollisionDetection = true;
    public LayerMask overviewCollisionLayers = -1; // All layers for overview mode
    public LayerMask interiorCollisionLayers = 0; // No collision layers for interior mode (disable aggressive collision)
    public float collisionBuffer = 0.5f; // Minimum distance from obstacles
    
    // Collision state tracking to prevent infinite loops
    private Collider lastCollisionObject = null;
    private const float COLLISION_CACHE_DURATION = 0.1f; // Cache results for 0.1 seconds
    private const float COLLISION_DISTANCE_THRESHOLD = 0.2f; // Only log if distance changes by > 0.2 units
    
    [Header("Interior Mode Override")]
    private bool forceDisableCollision = false; // Complete collision bypass for Interior mode
    
    [Header("Camera Mode")]
    public CameraMode currentMode = CameraMode.Overview;
    
    [Header("Touch Settings")]
    public float touchSensitivity = 1f;
    public bool enableTouchControls = true;
    
    private float currentHorizontalAngle = 0f;
    private float currentVerticalAngle = 30f;
    private Vector3 targetPosition;
    
    private void Start()
    {
        // FORCE correct constraints (override Unity Inspector values)
        // Use 89.9° instead of 90° to prevent gimbal lock and quaternion normalization issues
        minVerticalAngle = -89.9f; // Nearly full vertical rotation - can see all the way through
        maxVerticalAngle = 89.9f; // Nearly full vertical rotation - complete freedom
        
        // FORCE correct distance constraints for unified zoom range
        minDistance = 3f; // Interior zoom capability
        maxDistance = 35f; // Far overview distance
        
        // Camera setup complete - constraints and distance range configured
        
        // Find the chess board if no target is set
        if (target == null && ChessBoard.Instance != null)
        {
            target = ChessBoard.Instance.transform;
        }
        
        // If still no target, create a default one at the center
        if (target == null)
        {
            GameObject targetObject = new GameObject("Camera Target");
            target = targetObject.transform;
            target.position = Vector3.zero;
        }
        
        targetPosition = target.position;
        UpdateCameraPosition();
    }
    
    private void Update()
    {
        HandleInput();
        UpdateCameraPosition();
    }
    
    private void HandleInput()
    {
        if (!enableTouchControls) return;
        
        // Handle touch input for iOS using InputHelper
        if (InputHelper.TouchCount == 1)
        {
            HandleSingleTouch();
        }
        else if (InputHelper.TouchCount == 2)
        {
            HandlePinchZoom();
        }
        
        // Handle mouse input (desktop equivalent of touch controls)
        HandleMouseInput();
    }
    
    private void HandleSingleTouch()
    {
        Touch touch = InputHelper.GetTouch(0);
        
        if (touch.phase == TouchPhase.Moved)
        {
            Vector2 deltaPosition = touch.deltaPosition * touchSensitivity * Time.unscaledDeltaTime;
            
            currentHorizontalAngle += deltaPosition.x * rotationSpeed;
            currentVerticalAngle -= deltaPosition.y * rotationSpeed;
            
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }
    }
    
    private void HandlePinchZoom()
    {
        Touch touch1 = InputHelper.GetTouch(0);
        Touch touch2 = InputHelper.GetTouch(1);
        
        Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
        Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;
        
        float prevTouchDeltaMag = (touch1PrevPos - touch2PrevPos).magnitude;
        float touchDeltaMag = (touch1.position - touch2.position).magnitude;
        
        float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
        
        // Apply zoom curve for more precise control at close distances
        float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
        float curveMultiplier = zoomCurve.Evaluate(normalizedDistance);
        
        float zoomDelta = deltaMagnitudeDiff * zoomSpeed * curveMultiplier * Time.unscaledDeltaTime;
        if (invertZoom) zoomDelta = -zoomDelta;
        
        AdjustDistance(zoomDelta);
    }
    
    private void HandleMouseInput()
    {
        // Mouse drag for rotation using InputHelper - RIGHT MOUSE BUTTON to avoid conflict with piece selection
        if (InputHelper.GetMouseButton(1))
        {
            float mouseX = InputHelper.GetAxis("Mouse X");
            float mouseY = InputHelper.GetAxis("Mouse Y");
            
            currentHorizontalAngle += mouseX * rotationSpeed;
            currentVerticalAngle -= mouseY * rotationSpeed;
            
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }
        
        // Mouse scroll for zoom with enhanced curve
        float scroll = InputHelper.GetAxis("Mouse ScrollWheel");
        
        if (scroll != 0)
        {
            // Apply zoom curve for more precise control
            float normalizedDistance = Mathf.InverseLerp(minDistance, maxDistance, distance);
            float curveMultiplier = zoomCurve.Evaluate(normalizedDistance);
            
            float zoomDelta = -scroll * zoomSpeed * curveMultiplier;
            
            // Apply minimum threshold to prevent floating-point precision issues
            float minZoomThreshold = 0.01f;
            if (Mathf.Abs(zoomDelta) < minZoomThreshold)
            {
                zoomDelta = Mathf.Sign(zoomDelta) * minZoomThreshold;
            }
            
            AdjustDistance(zoomDelta);
        }
        
        // Keyboard zoom fallback for testing
        if (InputHelper.GetKey(KeyCode.PageUp))
        {
            AdjustDistance(-0.5f); // Zoom in
        }
        if (InputHelper.GetKey(KeyCode.PageDown))
        {
            AdjustDistance(0.5f); // Zoom out
        }
    }
    
    /// <summary>
    /// Adjust camera distance with automatic distance-based behavior switching
    /// </summary>
    private void AdjustDistance(float deltaDistance)
    {
        float newDistance = distance + deltaDistance;
        
        // UNIFIED DISTANCE RANGE: 3-35 units with no gaps
        float effectiveMinDistance = minDistance;
        float effectiveMaxDistance = maxDistance;
        
        // COLLISION DETECTION DISABLED: Preventing zoom trapping behavior
        float oldDistance = distance;
        distance = Mathf.Clamp(newDistance, effectiveMinDistance, effectiveMaxDistance);
        
        // Update current mode based on distance for compatibility
        CameraMode oldMode = currentMode;
        currentMode = distance < 10f ? CameraMode.Interior : CameraMode.Overview;
        
        // Log mode transitions only (important for debugging camera behavior)
        if (oldMode != currentMode)
        {
            // Clear collision state on mode transition to prevent stale data
            ClearCollisionState();
        }
    }
    
    /// <summary>
    /// Clear collision state to prevent stale collision data
    /// </summary>
    private void ClearCollisionState()
    {
        lastCollisionObject = null;
    }
    
    /// <summary>
    /// Smart auto-tilt: DISABLED - was interfering with manual camera control for bottom view
    /// </summary>
    private void ApplySmartAutoTilt()
    {
        // DISABLED: Auto-tilt was interfering with manual camera control for bottom view
        // The continuous zoom system with full -90° to 90° rotation makes auto-tilt unnecessary
        return;
    }
    
    private void UpdateCameraPosition()
    {
        if (target == null) return;
        
        // Update target position smoothly if the target has moved
        targetPosition = Vector3.Lerp(targetPosition, target.position, Time.deltaTime * 2f);
        
        // Calculate the desired position based on angles and distance
        // Normalize angles to prevent quaternion issues
        float normalizedVertical = Mathf.Clamp(currentVerticalAngle, -89.9f, 89.9f); // Prevent gimbal lock at ±90°
        float normalizedHorizontal = currentHorizontalAngle % 360f; // Keep horizontal in 0-360 range
        
        Quaternion rotation = Quaternion.Euler(normalizedVertical, normalizedHorizontal, 0f);
        Vector3 direction = rotation * Vector3.back;
        Vector3 desiredPosition = targetPosition + direction * distance;
        
        // COLLISION DETECTION DISABLED: AdjustDistance() already handles collision detection
        // Removing duplicate collision detection to prevent zoom trapping conflicts
        // Close distance: Free movement through all objects
        
        // Update camera position and rotation
        transform.position = desiredPosition;
        
        // Safe LookAt with distance check to prevent quaternion normalization errors
        Vector3 lookDirection = targetPosition - desiredPosition;
        if (lookDirection.sqrMagnitude > 0.001f) // Only look if there's sufficient distance
        {
            transform.LookAt(targetPosition);
        }
        else
        {
            // If too close, use manual rotation with normalized angles
            transform.rotation = Quaternion.Euler(normalizedVertical, normalizedHorizontal, 0f);
        }
    }
    
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        if (target != null)
        {
            targetPosition = target.position;
        }
    }
    
    public void ResetToDefaultView()
    {
        currentHorizontalAngle = 0f;
        currentVerticalAngle = 30f;
        distance = 20f;
        
        // Auto-determine behavior based on distance
        currentMode = distance < 10f ? CameraMode.Interior : CameraMode.Overview;
        enableCollisionDetection = true; // Re-enable collision detection
        forceDisableCollision = false; // Clear force disable override
        
        // Camera reset to default view
    }
    
    public void FocusOnBoard()
    {
        if (ChessBoard.Instance != null)
        {
            SetTarget(ChessBoard.Instance.transform);
        }
    }
    
    /// <summary>
    /// Focus camera on a specific board position for interior navigation
    /// </summary>
    public void FocusOnPosition(BoardPosition boardPos)
    {
        if (ChessBoard.Instance != null)
        {
            Vector3 worldPos = ChessBoard.Instance.BoardToWorld(boardPos);
            
            // Create or update a temporary target at this position
            if (target == null || target.name != "Temp Camera Target")
            {
                GameObject tempTarget = new GameObject("Temp Camera Target");
                SetTarget(tempTarget.transform);
            }
            
            target.position = worldPos;
            targetPosition = worldPos;
        }
    }
    
    /// <summary>
    /// Set camera to a specific zoom distance (replaces mode switching)
    /// </summary>
    public void SetZoomPreset(float targetDistance)
    {
        // Clamp to valid range
        targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        
        distance = targetDistance;
        
        // Auto-determine behavior based on distance
        currentMode = distance < 10f ? CameraMode.Interior : CameraMode.Overview;
        bool isCloseZoom = distance < 10f;
        
        Debug.Log($"🎥 ZOOM PRESET: Set distance to {distance:F1} units ({(isCloseZoom ? "Interior-style" : "Overview-style")} behavior)");
    }
    
    /// <summary>
    /// Get current camera info for debugging
    /// </summary>
    public string GetCameraInfo()
    {
        string collisionStatus = forceDisableCollision ? "FORCE DISABLED" : (enableCollisionDetection ? "Enabled" : "DISABLED");
        return $"Mode: {currentMode} | Distance: {distance:F1} | Angles: ({currentHorizontalAngle:F0}°, {currentVerticalAngle:F0}°) | Collision: {collisionStatus} | Target: {(target != null ? target.position.ToString("F1") : "None")}";
    }
}