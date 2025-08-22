using UnityEngine;

public class BoardRotator : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // degrees per second
    public bool snapToQuarters = true; // Snap to 0°, 90°, 180°, 270°
    public float snapThreshold = 5f; // degrees from target angle to snap
    
    [Header("360-Degree Rotation (Overview Mode)")]
    public bool enable360Rotation = true; // Enable full rotation in Overview mode
    public float smoothRotationSpeed = 180f; // degrees per second for smooth rotation
    
    [Header("Touch Controls")]
    public bool enableTouchRotation = true;
    public float touchRotationSensitivity = 2f;
    public float screenEdgeZone = 0.15f; // 15% of screen width for edge zones
    
    [Header("Input Controls")]
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;
    
    private float currentRotationY = 0f;
    private float targetRotationY = 0f;
    private bool isRotating = false;
    
    public static BoardRotator Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Initialize rotation
        currentRotationY = transform.eulerAngles.y;
        targetRotationY = currentRotationY;
        
        Debug.Log($"BoardRotator: Initialized at rotation Y = {currentRotationY}°");
    }
    
    private void Update()
    {
        HandleInput();
        UpdateRotation();
    }
    
    private void HandleInput()
    {
        // Check if we should allow rotation based on camera mode
        bool allowRotation = ShouldAllowRotation();
        
        if (!allowRotation)
        {
            return; // Skip all rotation input when not in Overview mode
        }
        
        // Keyboard controls for testing using InputHelper
        if (InputHelper.GetKeyDown(rotateLeftKey))
        {
            RotateLeft();
        }
        else if (InputHelper.GetKeyDown(rotateRightKey))
        {
            RotateRight();
        }
        
        // Alternative WASD controls (backup method)
        if (InputHelper.GetKeyDown(KeyCode.A))
        {
            RotateLeft();
            Debug.Log("BoardRotator: Rotated left via A key");
        }
        else if (InputHelper.GetKeyDown(KeyCode.D))
        {
            RotateRight();
            Debug.Log("BoardRotator: Rotated right via D key");
        }
        
        // Touch controls for mobile (360° rotation in Overview mode)
        if (enableTouchRotation && InputHelper.TouchCount == 1)
        {
            HandleTouchRotation();
        }
        
        // Mouse controls for board rotation - DISABLED to prioritize camera rotation
        // Board rotation is now primarily controlled via Q/E keyboard keys
        // This prevents conflict with CameraController's right-click camera orbit
        if (false && Application.isEditor && enableTouchRotation)
        {
            HandleMouseRotation();
        }
    }
    
    private void HandleTouchRotation()
    {
        Touch touch = InputHelper.GetTouch(0);
        
        // Only handle touch if it started in screen edge zones
        if (touch.phase == TouchPhase.Began)
        {
            return; // Just register the touch start
        }
        
        if (touch.phase == TouchPhase.Moved)
        {
            Vector2 normalizedPos = new Vector2(
                touch.position.x / Screen.width,
                touch.position.y / Screen.height
            );
            
            // Check if touch is in left or right edge zone
            if (normalizedPos.x < screenEdgeZone) // Left edge - rotate left
            {
                float rotationDelta = -touch.deltaPosition.x * touchRotationSensitivity * Time.deltaTime;
                AddRotation(rotationDelta);
            }
            else if (normalizedPos.x > (1f - screenEdgeZone)) // Right edge - rotate right
            {
                float rotationDelta = -touch.deltaPosition.x * touchRotationSensitivity * Time.deltaTime;
                AddRotation(rotationDelta);
            }
            
            // Two-finger rotation gesture
            if (InputHelper.TouchCount == 2)
            {
                Touch touch2 = InputHelper.GetTouch(1);
                Vector2 touch1Delta = touch.deltaPosition;
                Vector2 touch2Delta = touch2.deltaPosition;
                
                // Calculate rotation from two finger movement
                float angle = Vector2.SignedAngle(touch1Delta, touch2Delta);
                AddRotation(angle * touchRotationSensitivity * Time.deltaTime);
            }
        }
    }
    
    private void HandleMouseRotation()
    {
        if (InputHelper.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = InputHelper.GetAxis("Mouse X");
            
            // Use smooth 360° rotation in Overview mode
            if (ShouldUse360Rotation())
            {
                AddSmoothRotation(mouseX * touchRotationSensitivity * 10f);
            }
            else
            {
                AddRotation(mouseX * touchRotationSensitivity * 10f); // Scale for mouse sensitivity
            }
        }
    }
    
    public void RotateLeft()
    {
        if (snapToQuarters)
        {
            targetRotationY -= 90f;
            NormalizeTargetRotation();
        }
        else
        {
            AddRotation(-90f);
        }
        
        Debug.Log($"BoardRotator: Rotating left to {targetRotationY}°");
    }
    
    public void RotateRight()
    {
        if (snapToQuarters)
        {
            targetRotationY += 90f;
            NormalizeTargetRotation();
        }
        else
        {
            AddRotation(90f);
        }
        
        Debug.Log($"BoardRotator: Rotating right to {targetRotationY}°");
    }
    
    private void AddRotation(float deltaRotation)
    {
        // Check if we should use 360° rotation or quarter snapping
        bool shouldSnap = snapToQuarters && !ShouldUse360Rotation();
        
        if (shouldSnap)
        {
            // Accumulate small rotations toward next quarter
            targetRotationY += deltaRotation;
            
            // Check if we should snap to nearest quarter
            float nearestQuarter = Mathf.Round(targetRotationY / 90f) * 90f;
            if (Mathf.Abs(targetRotationY - nearestQuarter) > snapThreshold)
            {
                // Continue free rotation
                currentRotationY = targetRotationY;
            }
            else
            {
                // Snap to quarter
                targetRotationY = nearestQuarter;
            }
        }
        else
        {
            targetRotationY += deltaRotation;
        }
        
        NormalizeTargetRotation();
    }
    
    private void NormalizeTargetRotation()
    {
        // Keep rotation within 0-360 degrees
        while (targetRotationY < 0f) targetRotationY += 360f;
        while (targetRotationY >= 360f) targetRotationY -= 360f;
    }
    
    private void UpdateRotation()
    {
        if (Mathf.Abs(currentRotationY - targetRotationY) > 0.1f)
        {
            isRotating = true;
            
            // Use different rotation speeds for different modes
            float rotationStep = ShouldUse360Rotation() ? smoothRotationSpeed * Time.deltaTime : rotationSpeed * Time.deltaTime;
            currentRotationY = Mathf.MoveTowardsAngle(currentRotationY, targetRotationY, rotationStep);
            
            // Apply rotation to transform
            transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
        }
        else
        {
            if (isRotating)
            {
                // Snap to exact target when close enough
                currentRotationY = targetRotationY;
                transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
                isRotating = false;
                
                Debug.Log($"BoardRotator: Rotation complete at {currentRotationY}°");
                
                // Debug: Count pieces that are rotating with the board
                Transform piecesContainer = transform.Find("Pieces Container");
                if (piecesContainer != null)
                {
                    int pieceCount = piecesContainer.childCount;
                    Debug.Log($"BoardRotator: {pieceCount} pieces are rotating with the board");
                }
            }
        }
    }
    
    public void SetRotation(float angleY)
    {
        targetRotationY = angleY;
        NormalizeTargetRotation();
        Debug.Log($"BoardRotator: Set target rotation to {targetRotationY}°");
    }
    
    public void ResetRotation()
    {
        SetRotation(0f);
        Debug.Log("BoardRotator: Reset to 0° rotation");
    }
    
    public float GetCurrentRotation()
    {
        return currentRotationY;
    }
    
    public bool IsRotating()
    {
        return isRotating;
    }
    
    /// <summary>
    /// Add smooth rotation for 360° mode (no snapping)
    /// </summary>
    private void AddSmoothRotation(float deltaRotation)
    {
        targetRotationY += deltaRotation;
        NormalizeTargetRotation();
        
        // For smooth rotation, update current rotation immediately for responsive feel
        currentRotationY = targetRotationY;
        transform.rotation = Quaternion.Euler(0, currentRotationY, 0);
    }
    
    /// <summary>
    /// Check if rotation should be allowed based on camera mode
    /// </summary>
    private bool ShouldAllowRotation()
    {
        // Find CameraController to check current mode
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController == null)
        {
            return true; // Default to allowing rotation if no camera controller found
        }
        
        // Only allow rotation in Overview mode
        return cameraController.currentMode == CameraMode.Overview;
    }
    
    /// <summary>
    /// Check if 360° rotation should be used (Overview mode with feature enabled)
    /// </summary>
    private bool ShouldUse360Rotation()
    {
        if (!enable360Rotation) return false;
        
        CameraController cameraController = FindFirstObjectByType<CameraController>();
        if (cameraController == null) return false;
        
        return cameraController.currentMode == CameraMode.Overview;
    }
}