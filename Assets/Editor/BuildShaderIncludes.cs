using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Game scripts create materials at runtime with Shader.Find(). Player builds only
/// ship shaders that are referenced by assets or listed under Always Included Shaders,
/// so these lookups return null on device (pink pieces, ChessBoard failing to initialize).
/// This adds every shader the scripts look up by name to Always Included Shaders.
/// </summary>
public static class BuildShaderIncludes
{
    private static readonly string[] RuntimeShaders =
    {
        "Standard",
        "Diffuse",
        "Legacy Shaders/Diffuse",
        "Transparent/Diffuse",
        "Legacy Shaders/Transparent/Diffuse",
        "Unlit/Color",
        "Sprites/Default",
        "UI/Default",
        "UI/Unlit/Transparent",
        "Skybox/Panoramic", // backdrops
        "Unlit/Texture",    // desert grid floor
    };

    [MenuItem("Build/Include Runtime Shaders")]
    public static void EnsureIncluded()
    {
        var graphicsSettings = AssetDatabase.LoadAssetAtPath<GraphicsSettings>("ProjectSettings/GraphicsSettings.asset");
        var serialized = new SerializedObject(graphicsSettings);
        var list = serialized.FindProperty("m_AlwaysIncludedShaders");

        foreach (string name in RuntimeShaders)
        {
            Shader shader = Shader.Find(name);
            if (shader == null)
            {
                Debug.LogWarning($"BuildShaderIncludes: shader '{name}' not found in editor");
                continue;
            }

            bool present = false;
            for (int i = 0; i < list.arraySize; i++)
            {
                if (list.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                {
                    present = true;
                    break;
                }
            }

            if (!present)
            {
                list.InsertArrayElementAtIndex(list.arraySize);
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = shader;
                Debug.Log($"BuildShaderIncludes: added '{name}' to Always Included Shaders");
            }
        }

        serialized.ApplyModifiedProperties();
        EnsurePieceSetMaterials();
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// Template materials for PieceSets. Shipping them keeps the Standard shader variants
    /// the sets need (emission, transparency) from being stripped out of builds.
    /// </summary>
    private static void EnsurePieceSetMaterials()
    {
        const string folder = "Assets/Resources/PieceSets";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Resources", "PieceSets");
        Shader standard = Shader.Find("Standard");

        if (AssetDatabase.LoadAssetAtPath<Material>(folder + "/Glow.mat") == null)
        {
            var glow = new Material(standard);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor", Color.white);
            glow.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            AssetDatabase.CreateAsset(glow, folder + "/Glow.mat");
        }

        if (AssetDatabase.LoadAssetAtPath<Material>(folder + "/Chrome.mat") == null)
        {
            var chrome = new Material(standard);
            chrome.SetFloat("_Metallic", 1f);
            chrome.SetFloat("_Glossiness", 0.9f);
            AssetDatabase.CreateAsset(chrome, folder + "/Chrome.mat");
        }

        if (AssetDatabase.LoadAssetAtPath<Material>(folder + "/Crystal.mat") == null)
        {
            // Standard shader in Fade mode
            var crystal = new Material(standard);
            crystal.SetFloat("_Mode", 2f);
            crystal.SetOverrideTag("RenderType", "Transparent");
            crystal.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            crystal.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            crystal.SetInt("_ZWrite", 0);
            crystal.DisableKeyword("_ALPHATEST_ON");
            crystal.EnableKeyword("_ALPHABLEND_ON");
            crystal.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            crystal.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            crystal.color = new Color(1f, 1f, 1f, 0.55f);
            AssetDatabase.CreateAsset(crystal, folder + "/Crystal.mat");
        }
    }
}
