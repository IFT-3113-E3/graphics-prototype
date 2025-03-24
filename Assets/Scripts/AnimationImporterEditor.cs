using UnityEditor;
using UnityEngine;

public class AnimationImporterEditor : EditorWindow
{
    private AnimationClip _clip;

    [MenuItem("Tools/Animation Cleaner")]
    public static void ShowWindow()
    {
        GetWindow<AnimationImporterEditor>("Animation Cleaner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Animation Cleanup Tool", EditorStyles.boldLabel);

        _clip = (AnimationClip)EditorGUILayout.ObjectField("Animation Clip", _clip, typeof(AnimationClip), false);

        if (_clip == null)
        {
            EditorGUILayout.HelpBox("Please assign an Animation Clip.", MessageType.Info);
            return;
        }

        if (GUILayout.Button("Disable Interpolation (Constant Tangents)"))
        {
            DisableInterpolation(_clip);
        }

        if (GUILayout.Button("Cleanup Duplicate Keyframes"))
        {
            CleanupDuplicateKeyframes(_clip);
        }
    }

    private static void DisableInterpolation(AnimationClip clip)
    {
        Undo.RecordObject(clip, "Disable Interpolation");

        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            }
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"Interpolation disabled for {clip.name}");
    }

    private static void CleanupDuplicateKeyframes(AnimationClip clip)
    {
        Undo.RecordObject(clip, "Cleanup Duplicate Keyframes");

        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var cleanedKeys = new System.Collections.Generic.List<Keyframe>();

            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (i > 0 && Mathf.Approximately(curve.keys[i].value, curve.keys[i - 1].value))
                    continue; // Skip duplicate

                cleanedKeys.Add(curve.keys[i]);
            }

            curve.keys = cleanedKeys.ToArray();
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        Debug.Log($"Duplicate keyframes cleaned for {clip.name}");
    }
}
