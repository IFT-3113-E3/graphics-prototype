using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class PaletteLUTGenerator : EditorWindow
{
    Texture2D paletteTexture;
    int lutResolution = 16;
    string outputPath = "Assets/GeneratedLUT.asset";

    [MenuItem("Tools/Palette LUT Generator")]
    public static void ShowWindow()
    {
        GetWindow<PaletteLUTGenerator>("Palette LUT Generator");
    }

    void OnGUI()
    {
        GUILayout.Label("Generate 3D LUT from Palette Texture", EditorStyles.boldLabel);

        paletteTexture = (Texture2D)EditorGUILayout.ObjectField("Palette Texture", paletteTexture, typeof(Texture2D), false);
        lutResolution = EditorGUILayout.IntSlider("LUT Resolution", lutResolution, 4, 64);
        outputPath = EditorGUILayout.TextField("Output Path", outputPath);

        if (GUILayout.Button("Generate LUT"))
        {
            if (!paletteTexture)
            {
                EditorUtility.DisplayDialog("Missing Texture", "Please assign a palette texture.", "OK");
                return;
            }

            GenerateAndSaveLUT();
        }
    }

    void GenerateAndSaveLUT()
    {
        var colors = ExtractPaletteColors(paletteTexture);
        var tex3D = GenerateLut(colors, lutResolution);

        AssetDatabase.CreateAsset(tex3D, outputPath);
        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Success", "LUT generated and saved to:\n" + outputPath, "OK");
    }

    List<Color> ExtractPaletteColors(Texture2D texture)
    {
        var pixels = texture.GetPixels();
        var unique = new HashSet<Color>();

        foreach (var color in pixels)
        {
            // Skip transparent or low-alpha pixels
            // if (color.a > 0.5f)
            unique.Add(color);
        }

        Debug.Log($"Found {unique.Count} unique colors in palette.");
        return unique.ToList();
    }

    static Vector3 RgbToXyz(Color c)
    {
        float R = PivotRgb(c.r);
        float G = PivotRgb(c.g);
        float B = PivotRgb(c.b);

        return new Vector3(
            R * 0.4124f + G * 0.3576f + B * 0.1805f,
            R * 0.2126f + G * 0.7152f + B * 0.0722f,
            R * 0.0193f + G * 0.1192f + B * 0.9505f
        );
    }

    static float PivotRgb(float n)
    {
        return (n > 0.04045f) ? Mathf.Pow((n + 0.055f) / 1.055f, 2.4f) : n / 12.92f;
    }

    static Vector3 XyzToLab(Vector3 xyz)
    {
        float x = PivotXyz(xyz.x / 0.95047f); // D65 reference white
        float y = PivotXyz(xyz.y / 1.00000f);
        float z = PivotXyz(xyz.z / 1.08883f);

        return new Vector3(
            116f * y - 16f,
            500f * (x - y),
            200f * (y - z)
        );
    }

    static float PivotXyz(float n)
    {
        return (n > 0.008856f) ? Mathf.Pow(n, 1f / 3f) : (7.787f * n) + (16f / 116f);
    }
    
    static float DeltaE94(Vector3 lab1, Vector3 lab2)
    {
        float deltaL = lab1.x - lab2.x;
        float deltaA = lab1.y - lab2.y;
        float deltaB = lab1.z - lab2.z;

        float c1 = Mathf.Sqrt(lab1.y * lab1.y + lab1.z * lab1.z);
        float c2 = Mathf.Sqrt(lab2.y * lab2.y + lab2.z * lab2.z);
        float deltaC = c1 - c2;

        float deltaH = deltaA * deltaA + deltaB * deltaB - deltaC * deltaC;
        deltaH = deltaH < 0 ? 0 : Mathf.Sqrt(deltaH);

        float sc = 1f + 0.045f * c1;
        float sh = 1f + 0.015f * c1;

        float deltaLKlsl = deltaL / 1f;
        float deltaCkcsc = deltaC / sc;
        float deltaHkhsh = deltaH / sh;

        float i = deltaLKlsl * deltaLKlsl + deltaCkcsc * deltaCkcsc + deltaHkhsh * deltaHkhsh;
        return Mathf.Sqrt(Mathf.Max(0f, i));
    }

    private static Texture3D GenerateLut(List<Color> palette, int resolution)
    {
        Color[] lut = new Color[resolution * resolution * resolution];

        HashSet<Color> uniqueColorsUsed = new HashSet<Color>();
        
        for (int r = 0; r < resolution; r++)
        for (int g = 0; g < resolution; g++)
        for (int b = 0; b < resolution; b++)
        {
            Color inputColor = new Color(
                (float)r / (resolution - 1),
                (float)g / (resolution - 1),
                (float)b / (resolution - 1)
            );

            var inputLab = XyzToLab(RgbToXyz(inputColor));
            float minDist = float.MaxValue;
            Color closest = inputColor;

            foreach (var palColor in palette)
            {
                var palLab = XyzToLab(RgbToXyz(palColor));
                float dist = DeltaE94(inputLab, palLab);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = palColor;
                }
            }

            int index = r + g * resolution + b * resolution * resolution;
            lut[index] = closest;
            uniqueColorsUsed.Add(closest);
        }
        
        // compare the two hashsets
        
        if (uniqueColorsUsed.Count != palette.Count)
            Debug.LogError($"Unique colors used in LUT: {uniqueColorsUsed.Count}");
        else
            Debug.Log("All palette colors used in LUT.");

        Texture3D tex3D = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, false);
        tex3D.SetPixels(lut);
        tex3D.Apply();
        tex3D.name = "PaletteLUT_" + resolution;

        return tex3D;
    }
}
