using System;
using System.Collections;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

[Serializable]
public class SlashAnimationConfig
{
    public float arcAmount = 0.5f;
    public float length = 1f;
    public float duration = 0.3f;
    public int segments = 20;
    public Color color = Color.white;
}

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SwordSlashAnimator : MonoBehaviour
{
    private static readonly int Color1 = Shader.PropertyToID("_BaseColor");
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private Mesh _mesh;
    private Material _material;

    // Position of the start and end of the slash
    private Vector3 _startPosition, _endPosition;

    // Directions of the start and end of the slash, this represents the direction that the sword is facing
    // The x axis is the sword's edge, the y axis is the sword's flat side and the z axis is the sword's tip
    private Vector3 _startDirection, _endDirection;
    private Vector3 _p0, _p1, _p2, _p3;
    
    private SlashAnimationConfig _config;
    
    private Coroutine _coroutine;

    private bool _isReverse = false;
    
    private void Awake()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        transform.SetParent(null);
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        InitSlash();
    }

    private void InitSlash()
    {
        _mesh = new Mesh { name = "Slash Mesh" };
        _meshFilter.sharedMesh = _mesh;
        _material = new Material(Shader.Find("Effects/SwordSlash"));
        _meshRenderer.sharedMaterial = _material;
    }

    void GenerateCurvePoints()
    {
        _p0 = _startPosition;
        _p3 = _endPosition;
        int dir = _isReverse ? -1 : 1;

        
        Vector3 startRight = Quaternion.LookRotation(_startDirection) * Vector3.right * dir;
        Vector3 endRight = Quaternion.LookRotation(_endDirection) * Vector3.right * dir;
        Vector3 startUp = Quaternion.LookRotation(_startDirection) * Vector3.up * dir;
        Vector3 endUp = Quaternion.LookRotation(_endDirection) * Vector3.up * dir;

        Vector3 chord = _p3 - _p0;
        float chordMagnitude = chord.magnitude;

        // Estimate the 'up' direction of the arc as the average of the transforms' up vectors
        Vector3 upDirection = (startUp - endUp).normalized;

        // Project the right vectors onto the plane perpendicular to the 'up' direction
        Vector3 startRightProjected = Vector3.ProjectOnPlane(startRight, upDirection).normalized;
        Vector3 endRightProjected = Vector3.ProjectOnPlane(endRight, upDirection).normalized;

        // Calculate the angle between the projected right vectors
        float angle =
            Mathf.Acos(Mathf.Clamp(Vector3.Dot(startRightProjected, endRightProjected), -1f, 1f));

        // Heuristic for handle length based on the angle and chord length
        float handleLengthFactor = 0.551915f; // Magic number for quarter circle approximation
        float handleLength =
            handleLengthFactor * chordMagnitude *
            (angle / (Mathf.PI * 0.5f)); // Scale handle length by the proportion of a half circle

        _p1 = _p0 + startRight * handleLength;
        _p2 = _p3 - endRight * handleLength; // Adjust sign based on the expected swing direction

        Debug.DrawRay(_p0, _p1, Color.red, 1);
        Debug.DrawRay(_p3, _p2, Color.green, 1);
    }
    
    Vector3 CubicBezier(float t, Vector3 a, Vector3 b, Vector3 c, Vector3 d) {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        return uuu * a
               + 3f * uu * t * b
               + 3f * u * tt * c
               + ttt * d;
    }

    Vector3 BezierFirstDerivative(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
        return
            3 * Mathf.Pow(1 - t, 2) * (p1 - p0) +
            6 * (1 - t) * t * (p2 - p1) +
            3 * Mathf.Pow(t, 2) * (p3 - p2);
    }
    
    void GenerateSlashMesh(int segments, float swordLength) {
        
        int dir = _isReverse ? -1 : 1;
        Vector3 startUp = Quaternion.LookRotation(_startDirection) * Vector3.up * dir;
        Vector3 endUp = Quaternion.LookRotation(_endDirection) * Vector3.up * dir;
        
        Vector3[] vertices = new Vector3[segments * 2];
        Vector2[] uvs = new Vector2[segments * 2];
        int[] triangles = new int[(segments - 1) * 6];
        
        for (int i = 0; i < segments; i++) {
            float t = i / (segments - 1f);
            
            // float swordLength = Mathf.Lerp(minLength, maxLength, t);
            
            Vector3 pos = CubicBezier(t, _p0, _p1, _p2, _p3);
            Vector3 tangent = BezierFirstDerivative(t, _p0, _p1, _p2, _p3).normalized;

            Vector3 localUp = Vector3.Slerp(startUp, endUp, t).normalized;
            Vector3 tipDir = Vector3.Cross(tangent, localUp).normalized;
            
            // The slash goes from the curve outward (tip direction)
            Vector3 baseVertex = pos;
            Vector3 tipVertex = pos + tipDir * swordLength;

            int idx = i * 2;
            vertices[idx + 0] = baseVertex;
            vertices[idx + 1] = tipVertex;

            uvs[idx + 0] = new Vector2(t, 0);
            uvs[idx + 1] = new Vector2(t, 1);

            if (i < segments - 1) {
                int tri = i * 6;
                triangles[tri + 0] = idx;
                triangles[tri + 1] = idx + 2;
                triangles[tri + 2] = idx + 1;
                triangles[tri + 3] = idx + 1;
                triangles[tri + 4] = idx + 2;
                triangles[tri + 5] = idx + 3;
            }
        }

        _mesh.Clear();
        _mesh.vertices = vertices;
        _mesh.uv = uvs;
        _mesh.triangles = triangles;
        _mesh.RecalculateNormals();
    }
    
    void SetShaderProperties(SlashAnimationConfig animationConfig) {
        _material.SetColor(Color1, animationConfig.color);
    }
    
    public void Configure(SlashAnimationConfig config)
    {
        _config = config;
    }
    
    public void SetupSlash(Vector3 startPos, Vector3 endPos, Vector3 startDir, Vector3 endDir, bool reverse = false)
    {
        SetupSlash(startPos, endPos, startDir, endDir, reverse, _config);
    }

    public void SetupSlash(Vector3 startPos, Vector3 endPos, Vector3 startDir, Vector3 endDir, bool reverse, SlashAnimationConfig config)
    {
        _startPosition = startPos;
        _endPosition = endPos;
        _startDirection = startDir;
        _endDirection = endDir;
        _isReverse = reverse;

        GenerateCurvePoints();
        GenerateSlashMesh(config.segments, config.length);
        SetShaderProperties(config);
    }

    private IEnumerator PlaySlashRoutine(float duration, Action onComplete)
    {
        float t = 0f;
        _material.SetFloat(Progress, 0f);
        _meshRenderer.enabled = true;
        
        while (t < duration)
        {
            _material.SetFloat(Progress, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        
        _material.SetFloat(Progress, 1f);
        _meshRenderer.enabled = false;
        onComplete?.Invoke();
    }
    
    public void PlaySlash(Action onComplete = null)
    {
        if (_coroutine != null)
        {
            StopCoroutine(_coroutine);
        }

        _coroutine = StartCoroutine(PlaySlashRoutine(_config.duration, onComplete));
    }
}