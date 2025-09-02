using UnityEngine;

/// <summary>
/// Test script to verify tray positioning is correct and symmetric
/// Can be added to a GameObject in the scene for debugging
/// </summary>
public class TrayPositionTest : MonoBehaviour
{
    [Header("Debug Visualization")]
    public bool showDebugLines = true;
    public Color boardBoundsColor = Color.yellow;
    public Color trayPositionColor = Color.green;
    public Color symmetryLineColor = Color.blue;
    
    private void Start()
    {
        Debug.Log("=== TRAY POSITION TEST STARTED ===");
        VerifyTrayPositions();
    }
    
    private void VerifyTrayPositions()
    {
        // Find the trays
        PieceTray whiteTray = PieceTray.WhiteTray;
        PieceTray blackTray = PieceTray.BlackTray;
        
        if (whiteTray == null || blackTray == null)
        {
            Debug.LogWarning("TrayPositionTest: Trays not found! Searching manually...");
            
            // Manual search for trays
            PieceTray[] allTrays = FindObjectsByType<PieceTray>(FindObjectsSortMode.None);
            Debug.Log($"Found {allTrays.Length} tray objects in scene");
            
            foreach (PieceTray tray in allTrays)
            {
                Debug.Log($"Tray: {tray.name}, Color: {tray.trayColor}, Position: {tray.transform.position}");
                
                if (tray.trayColor == PieceColor.White && whiteTray == null)
                    whiteTray = tray;
                else if (tray.trayColor == PieceColor.Black && blackTray == null)
                    blackTray = tray;
            }
        }
        
        if (whiteTray != null && blackTray != null)
        {
            Vector3 whitePos = whiteTray.transform.position;
            Vector3 blackPos = blackTray.transform.position;
            
            Debug.Log("=== TRAY POSITION VERIFICATION ===");
            Debug.Log($"White Tray Position: {whitePos}");
            Debug.Log($"Black Tray Position: {blackPos}");
            
            // Check symmetry (CORRECTED)
            float boardCenterX = 0f; // Board is actually centered at world origin
            float whiteDistanceFromCenter = Mathf.Abs(whitePos.x - boardCenterX);
            float blackDistanceFromCenter = Mathf.Abs(blackPos.x - boardCenterX);
            
            Debug.Log($"Board Center X: {boardCenterX}");
            Debug.Log($"White Tray Distance from Center: {whiteDistanceFromCenter}");
            Debug.Log($"Black Tray Distance from Center: {blackDistanceFromCenter}");
            
            float symmetryDifference = Mathf.Abs(whiteDistanceFromCenter - blackDistanceFromCenter);
            Debug.Log($"Symmetry Difference: {symmetryDifference}");
            
            if (symmetryDifference < 0.1f)
            {
                Debug.Log("✅ SYMMETRY TEST PASSED - Trays are positioned symmetrically");
            }
            else
            {
                Debug.LogWarning($"❌ SYMMETRY TEST FAILED - Difference of {symmetryDifference} units");
            }
            
            // Check expected positions (MAXIMUM SPACING)
            float expectedWhiteX = -12.2f;  // Left edge (-4.2) - spacing (8.0)
            float expectedBlackX = 12.2f;   // Right edge (4.2) + spacing (8.0)
            
            float whitePosError = Mathf.Abs(whitePos.x - expectedWhiteX);
            float blackPosError = Mathf.Abs(blackPos.x - expectedBlackX);
            
            Debug.Log($"Expected White X: {expectedWhiteX}, Actual: {whitePos.x}, Error: {whitePosError}");
            Debug.Log($"Expected Black X: {expectedBlackX}, Actual: {blackPos.x}, Error: {blackPosError}");
            
            if (whitePosError < 0.1f && blackPosError < 0.1f)
            {
                Debug.Log("✅ POSITION TEST PASSED - Trays are at expected coordinates");
            }
            else
            {
                Debug.LogWarning($"❌ POSITION TEST FAILED - White error: {whitePosError}, Black error: {blackPosError}");
            }
            
            // Check Y alignment (CORRECTED)
            float expectedY = -1.0f; // Visible height above board center
            float whiteYError = Mathf.Abs(whitePos.y - expectedY);
            float blackYError = Mathf.Abs(blackPos.y - expectedY);
            
            Debug.Log($"Expected Y: {expectedY}, White Y: {whitePos.y} (error: {whiteYError}), Black Y: {blackPos.y} (error: {blackYError})");
            
            if (whiteYError < 0.1f && blackYError < 0.1f)
            {
                Debug.Log("✅ Y-ALIGNMENT TEST PASSED - Trays are at board center height");
            }
            else
            {
                Debug.LogWarning($"❌ Y-ALIGNMENT TEST FAILED - Y positioning may need adjustment");
            }
        }
        else
        {
            Debug.LogError("❌ CRITICAL: Cannot find both trays for verification");
        }
        
        Debug.Log("=== TRAY POSITION TEST COMPLETED ===");
    }
    
    private void OnDrawGizmos()
    {
        if (!showDebugLines) return;
        
        // Draw board bounds (CORRECTED)
        Gizmos.color = boardBoundsColor;
        Vector3 boardCenter = new Vector3(0f, -4.2f, 0f); // Actual world center
        Vector3 boardSize = new Vector3(8.4f, 11.2f, 8.4f); // Board total size
        Gizmos.DrawWireCube(boardCenter, boardSize);
        
        // Draw tray positions if they exist
        PieceTray whiteTray = PieceTray.WhiteTray;
        PieceTray blackTray = PieceTray.BlackTray;
        
        if (whiteTray != null)
        {
            Gizmos.color = trayPositionColor;
            Gizmos.DrawWireSphere(whiteTray.transform.position, 1.0f);
            Gizmos.DrawLine(whiteTray.transform.position, whiteTray.transform.position + Vector3.up * 2f);
        }
        
        if (blackTray != null)
        {
            Gizmos.color = trayPositionColor;
            Gizmos.DrawWireSphere(blackTray.transform.position, 1.0f);
            Gizmos.DrawLine(blackTray.transform.position, blackTray.transform.position + Vector3.up * 2f);
        }
        
        // Draw symmetry line through board center (CORRECTED)
        Gizmos.color = symmetryLineColor;
        Vector3 symmetryStart = new Vector3(0f, -10f, 0f); // Through world origin
        Vector3 symmetryEnd = new Vector3(0f, 5f, 0f);
        Gizmos.DrawLine(symmetryStart, symmetryEnd);
    }
}