using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class PaletteTextureGenerator : EditorWindow
{
    private Texture2D sourceTexture;
    private Texture2D paletteTexture;
    private int maxColors = 16;
    private string savePath = "Assets/GeneratedPalette.png";
    
    [MenuItem("Tools/Palette Texture Generator")]
    public static void ShowWindow()
    {
        GetWindow<PaletteTextureGenerator>("Palette Texture Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate and Save a Sorted Palette Texture", EditorStyles.boldLabel);
        
        sourceTexture = (Texture2D)EditorGUILayout.ObjectField("Source Texture", sourceTexture, typeof(Texture2D), false);
        maxColors = EditorGUILayout.IntSlider("Max Colors", maxColors, 2, 256);
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        
        if (GUILayout.Button("Generate & Sort Palette"))
        {
            GenerateSortedPalette();
        }

        if (paletteTexture != null)
        {
            GUILayout.Label("Preview:", EditorStyles.boldLabel);
            GUILayout.Label(paletteTexture, GUILayout.Width(256), GUILayout.Height(32));

            if (GUILayout.Button("Save Palette Texture"))
            {
                SaveTexture();
            }
        }
    }

    private void GenerateSortedPalette()
    {
        if (sourceTexture == null)
        {
            Debug.LogError("No source texture selected!");
            return;
        }

        // Get unique colors from the source texture
        Color[] extractedColors = ExtractUniqueColors(sourceTexture, maxColors);

        // Sort colors using perceptual Lab sorting
        List<Color> sortedColors = extractedColors.OrderBy(c => RGBToLab(c).x).ThenBy(c => RGBToLab(c).y).ToList();

        // Create and assign sorted palette texture
        paletteTexture = new Texture2D(sortedColors.Count, 1, TextureFormat.RGBA32, false);
        for (int i = 0; i < sortedColors.Count; i++)
        {
            paletteTexture.SetPixel(i, 0, sortedColors[i]);
        }

        paletteTexture.Apply();
        Debug.Log("Palette Texture Generated!");
    }

    private void SaveTexture()
    {
        if (paletteTexture == null)
        {
            Debug.LogError("No palette texture to save!");
            return;
        }

        byte[] bytes = paletteTexture.EncodeToPNG();
        File.WriteAllBytes(savePath, bytes);
        AssetDatabase.Refresh();

        Debug.Log("Palette texture saved at: " + savePath);
    }

    private Color[] ExtractUniqueColors(Texture2D texture, int maxColors)
    {
        HashSet<Color> uniqueColors = new HashSet<Color>();

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                Color pixelColor = texture.GetPixel(x, y);
                if (!uniqueColors.Contains(pixelColor))
                {
                    uniqueColors.Add(pixelColor);
                    if (uniqueColors.Count >= maxColors) break;
                }
            }
            if (uniqueColors.Count >= maxColors) break;
        }

        return uniqueColors.ToArray();
    }

    private Vector3 RGBToLab(Color color)
    {
        float r = color.r, g = color.g, b = color.b;
        float x = 0.4124564f * r + 0.3575761f * g + 0.1804375f * b;
        float y = 0.2126729f * r + 0.7151522f * g + 0.0721750f * b;
        float z = 0.0193339f * r + 0.1191920f * g + 0.9503041f * b;

        x = x / 0.95047f;
        y = y / 1.00000f;
        z = z / 1.08883f;

        float fx = x > 0.008856f ? Mathf.Pow(x, 1f / 3f) : (7.787f * x) + (16f / 116f);
        float fy = y > 0.008856f ? Mathf.Pow(y, 1f / 3f) : (7.787f * y) + (16f / 116f);
        float fz = z > 0.008856f ? Mathf.Pow(z, 1f / 3f) : (7.787f * z) + (16f / 116f);

        return new Vector3((116f * fy) - 16f, 500f * (fx - fy), 200f * (fy - fz)); // L*, a*, b*
    }
}
