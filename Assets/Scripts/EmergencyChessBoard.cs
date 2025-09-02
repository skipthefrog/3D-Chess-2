using UnityEngine;

// Updated: Adding atmospheric haze cubes for better cell visualization
public class EmergencyChessBoard : MonoBehaviour
{
    void Start()
    {
        Debug.Log("*** EMERGENCY CHESS BOARD STARTING - FIXED COORDINATE SYSTEM ***");
        CreateSimpleBoard();
    }
    
    void CreateSimpleBoard()
    {
        Debug.Log("Creating simplified emergency chess board...");
        
        // Create rotatable parent for the entire board
        GameObject rotationParent = new GameObject("Board Rotator");
        BoardRotator rotator = rotationParent.AddComponent<BoardRotator>();
        
        // Create a child object for board organization
        GameObject boardParent = new GameObject("Emergency Chess Board");
        boardParent.transform.SetParent(rotationParent.transform);
        
        // Create a container for pieces that will rotate with the board
        GameObject piecesContainer = new GameObject("Pieces Container");
        piecesContainer.transform.SetParent(rotationParent.transform);
        piecesContainer.transform.localPosition = new Vector3(-4.2f, -4.2f, -4.2f);
        piecesContainer.transform.localRotation = Quaternion.identity;
        
        // Create floor planes for all 4 Y levels (0, 1, 2, 3)
        for (int x = 0; x < 4; x++)
        {
            for (int z = 0; z < 4; z++)
            {
                for (int y = 0; y < 4; y++)
                {
                    GameObject floorPlane = CreateFloorPlane(x, y, z);
                    floorPlane.transform.SetParent(boardParent.transform);
                }
            }
        }
        
        // Create essential 3D wireframe structure
        CreateEssentialWireframe(boardParent);
        
        // Center the board
        boardParent.transform.localPosition = new Vector3(-4.2f, -4.2f, -4.2f);
        rotationParent.transform.position = Vector3.zero;
        
        Debug.Log("3D emergency chess board created with 64 floor planes and essential wireframe structure");
    }
    
    void CreateEssentialWireframe(GameObject boardParent)
    {
        GameObject wireframeContainer = new GameObject("Essential 3D Wireframe");
        wireframeContainer.transform.SetParent(boardParent.transform);
        
        // Cell boundary positions
        float[] cellBoundaries = { -1.4f, 1.4f, 4.2f, 7.0f, 9.8f };
        int lineCount = 0;
        
        // 1. Outer boundary box (12 lines)
        float minPos = cellBoundaries[0];
        float maxPos = cellBoundaries[4];
        
        // Bottom and top faces (8 lines)
        for (int face = 0; face < 2; face++)
        {
            float yPos = (face == 0) ? minPos : maxPos;
            CreateSimpleLine(wireframeContainer, new Vector3(minPos, yPos, minPos), new Vector3(maxPos, yPos, minPos));
            CreateSimpleLine(wireframeContainer, new Vector3(maxPos, yPos, minPos), new Vector3(maxPos, yPos, maxPos));
            CreateSimpleLine(wireframeContainer, new Vector3(maxPos, yPos, maxPos), new Vector3(minPos, yPos, maxPos));
            CreateSimpleLine(wireframeContainer, new Vector3(minPos, yPos, maxPos), new Vector3(minPos, yPos, minPos));
            lineCount += 4;
        }
        
        // Vertical edges (4 lines)
        CreateSimpleLine(wireframeContainer, new Vector3(minPos, minPos, minPos), new Vector3(minPos, maxPos, minPos));
        CreateSimpleLine(wireframeContainer, new Vector3(maxPos, minPos, minPos), new Vector3(maxPos, maxPos, minPos));
        CreateSimpleLine(wireframeContainer, new Vector3(maxPos, minPos, maxPos), new Vector3(maxPos, maxPos, maxPos));
        CreateSimpleLine(wireframeContainer, new Vector3(minPos, minPos, maxPos), new Vector3(minPos, maxPos, maxPos));
        lineCount += 4;
        
        // 2. Interior Y-level dividers (12 lines for 3 intermediate levels)
        for (int level = 1; level < 4; level++) // Y levels 1, 2, 3
        {
            float yPos = cellBoundaries[level];
            // Create rectangular frame at each Y level
            CreateSimpleLine(wireframeContainer, new Vector3(minPos, yPos, minPos), new Vector3(maxPos, yPos, minPos));
            CreateSimpleLine(wireframeContainer, new Vector3(maxPos, yPos, minPos), new Vector3(maxPos, yPos, maxPos));
            CreateSimpleLine(wireframeContainer, new Vector3(maxPos, yPos, maxPos), new Vector3(minPos, yPos, maxPos));
            CreateSimpleLine(wireframeContainer, new Vector3(minPos, yPos, maxPos), new Vector3(minPos, yPos, minPos));
            lineCount += 4;
        }
        
        // 3. Key vertical grid lines for X and Z divisions (8 lines)
        // Vertical lines at X boundaries
        for (int i = 1; i < 4; i++) // X positions 1, 2, 3 (skip 0 and 4 as they're outer edges)
        {
            float xPos = cellBoundaries[i];
            CreateSimpleLine(wireframeContainer, new Vector3(xPos, minPos, minPos), new Vector3(xPos, maxPos, minPos));
            CreateSimpleLine(wireframeContainer, new Vector3(xPos, minPos, maxPos), new Vector3(xPos, maxPos, maxPos));
            lineCount += 2;
        }
        
        // Vertical lines at Z boundaries  
        for (int i = 1; i < 4; i++) // Z positions 1, 2, 3 (skip 0 and 4 as they're outer edges)
        {
            float zPos = cellBoundaries[i];
            CreateSimpleLine(wireframeContainer, new Vector3(minPos, minPos, zPos), new Vector3(minPos, maxPos, zPos));
            CreateSimpleLine(wireframeContainer, new Vector3(maxPos, minPos, zPos), new Vector3(maxPos, maxPos, zPos));
            lineCount += 2;
        }
        
        Debug.Log($"Essential 3D wireframe created with {lineCount} lines");
    }
    
    void CreateSimpleLine(GameObject parent, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("WireframeLine");
        lineObj.transform.SetParent(parent.transform);
        
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        
        // Find shader
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }
        
        Material lineMaterial = new Material(shader);
        lineMaterial.color = new Color(0.8f, 0.8f, 0.8f, 1.0f);
        
        line.material = lineMaterial;
        line.positionCount = 2;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.useWorldSpace = false;
        line.enabled = true;
        
        line.SetPosition(0, start);
        line.SetPosition(1, end);
    }
    
    GameObject CreateFloorPlane(int x, int y, int z)
    {
        GameObject floorPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floorPlane.name = $"Floor_{x}_{y}_{z}";
        
        // Position plane at actual cell bottom
        // Each cell spans 2.8 units, so bottom of cell is at y*2.8 - 1.4
        floorPlane.transform.position = new Vector3(x * 2.8f, (y * 2.8f) - 1.4f, z * 2.8f);
        
        // Scale plane to fit cell area (planes are 10x10 units by default) 
        floorPlane.transform.localScale = new Vector3(0.26f, 1f, 0.26f); // Slightly smaller for clear boundaries
        
        // Create translucent material
        Renderer renderer = floorPlane.GetComponent<Renderer>();
        
        // Try URP Unlit first (more reliable for transparency)
        Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlitShader == null)
        {
            unlitShader = Shader.Find("Unlit/Transparent");
        }
        if (unlitShader == null)
        {
            unlitShader = Shader.Find("Legacy Shaders/Transparent/Diffuse");
        }
        
        Material floorMaterial = new Material(unlitShader);
        
        // Set subtle translucent colors for move planning without affecting piece colors
        if ((x + y + z) % 2 == 0)
        {
            // Light squares - minimal opacity to prevent color bleeding
            floorMaterial.color = new Color(1f, 1f, 1f, 0.12f);
        }
        else
        {
            // Dark squares - minimal opacity to prevent color bleeding
            floorMaterial.color = new Color(0.7f, 0.7f, 0.7f, 0.12f);
        }
        
        // Set up transparency
        try
        {
            if (unlitShader.name.Contains("Universal Render Pipeline"))
            {
                // URP Unlit transparency with depth-aware rendering
                floorMaterial.SetFloat("_Surface", 1); // Transparent surface type
                floorMaterial.SetFloat("_Blend", 0);   // Alpha blend mode
                floorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                floorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                floorMaterial.SetInt("_ZWrite", 0);
                floorMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Greater); // Only render behind pieces
                floorMaterial.renderQueue = 3000;
            }
            else
            {
                // Legacy transparency with depth-aware rendering
                floorMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                floorMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                floorMaterial.SetInt("_ZWrite", 0);
                floorMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Greater); // Only render behind pieces
                floorMaterial.renderQueue = 3000;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Transparency setup failed for floor ({x},{y},{z}): {e.Message}");
        }
        
        renderer.material = floorMaterial;
        
        // Remove collider for mobile-first design - only pieces should be clickable
        Collider planeCollider = floorPlane.GetComponent<Collider>();
        if (planeCollider != null)
        {
            DestroyImmediate(planeCollider);
        }
        
        return floorPlane;
    }
    
}