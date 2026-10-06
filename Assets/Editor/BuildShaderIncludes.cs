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
        "Skybox/Panoramic", // space backdrop
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
        AssetDatabase.SaveAssets();
    }
}
