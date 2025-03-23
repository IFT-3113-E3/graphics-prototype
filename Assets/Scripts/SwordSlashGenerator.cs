using System.Collections;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SwordSlashGenerator : MonoBehaviour {
    private static readonly int P0 = Shader.PropertyToID("_P0");
    private static readonly int P1 = Shader.PropertyToID("_P1");
    private static readonly int P2 = Shader.PropertyToID("_P2");
    private static readonly int P3 = Shader.PropertyToID("_P3");
    private static readonly int Width = Shader.PropertyToID("_Width");
    private static readonly int Color1 = Shader.PropertyToID("_BaseColor");
    private static readonly int Progress = Shader.PropertyToID("_Progress");

    [Header("Slash Control")]
    public Transform startPoint;
    public Transform endPoint;
    
    public bool playAnimation = true;
    
    public float arcAmount = 0.5f;
    public int segments = 20;
    public float width = 0.2f;
    public float minLength = 1f; // Length of the virtual slash
    public float maxLength = 1f; // Length of the virtual slash
    public float finalLength = 1f; // Length of the virtual slash
    public Color color;

    [Header("Sword Follower")]
    public Transform sword;
    [Range(0, 1)] public float swordT;

    private Mesh _mesh;
    private Material _material;

    private Vector3 _p0, _p1, _p2, _p3;
    private MeshRenderer _meshRenderer;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        InitMesh();
    }
    
    public int FPS = 60;
    private float _time;
    private float _timer;
    
    void Update()
    {
        if (playAnimation)
        {
            _time += Time.deltaTime;
            float timeStep = 1f / FPS;

            while (_time > timeStep)
            {
                _time -= timeStep;
                swordT = Mathf.Clamp01(swordT + timeStep * 2);
            }

            if (swordT >= 1f && _timer >= swordT + 1 * Time.deltaTime)
            {
                _time = 0;
                swordT = 0;
                _timer = 0;
            }

            _timer += Time.deltaTime;
        }
        
        
        if (!startPoint || !endPoint) return;

        GenerateCurvePoints();
        GenerateSlashMesh();
        UpdateSwordTransform();
    }


    void InitMesh() {
        if (_mesh == null) {
            _mesh = new Mesh { name = "Slash Mesh" };
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }
        if (_material == null) {
            _material = new Material(Shader.Find("Effects/SwordSlash"));
            _material.SetColor(Color1, color);
        }
        _meshRenderer.sharedMaterial = _material;
    }
    
    void GenerateCurvePoints()
    {
        _p0 = startPoint.position;
        _p3 = endPoint.position;

        Vector3 startRight = startPoint.right.normalized;
        Vector3 endRight = endPoint.right.normalized;

        Vector3 chord = _p3 - _p0;
        float chordMagnitude = chord.magnitude;

        // Estimate the 'up' direction of the arc as the average of the transforms' up vectors
        Vector3 upDirection = (startPoint.up + endPoint.up).normalized;

        // Project the right vectors onto the plane perpendicular to the 'up' direction
        Vector3 startRightProjected = Vector3.ProjectOnPlane(startRight, upDirection).normalized;
        Vector3 endRightProjected = Vector3.ProjectOnPlane(endRight, upDirection).normalized;

        // Calculate the angle between the projected right vectors
        float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(startRightProjected, endRightProjected), -1f, 1f));

        // Heuristic for handle length based on the angle and chord length
        float handleLengthFactor = 0.551915f; // Magic number for quarter circle approximation
        float handleLength = handleLengthFactor * chordMagnitude * (angle / (Mathf.PI * 0.5f)); // Scale handle length by the proportion of a half circle

        _p1 = _p0 + startRight * handleLength;
        _p2 = _p3 - endRight * handleLength; // Adjust sign based on the expected swing direction
        
        // Send to shader
        var mat = _meshRenderer.sharedMaterial;
        if (mat) {
            mat.SetColor(Color1, color);
            mat.SetVector(P0, _p0);
            mat.SetVector(P1, _p1);
            mat.SetVector(P2, _p2);
            mat.SetVector(P3, _p3);
            mat.SetFloat(Width, width);
            mat.SetFloat(Progress, swordT);
        }
    }
    
    Vector3 BezierFirstDerivative(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) {
        return
            3 * Mathf.Pow(1 - t, 2) * (p1 - p0) +
            6 * (1 - t) * t * (p2 - p1) +
            3 * Mathf.Pow(t, 2) * (p3 - p2);
    }
    

    void GenerateSlashMesh() {
        Vector3[] vertices = new Vector3[segments * 2];
        Vector2[] uvs = new Vector2[segments * 2];
        int[] triangles = new int[(segments - 1) * 6];
        
        for (int i = 0; i < segments; i++) {
            float t = i / (segments - 1f);
            
            float swordLength = Mathf.Lerp(minLength, maxLength, t);
            
            Vector3 pos = CubicBezier(t, _p0, _p1, _p2, _p3);
            Vector3 tangent = BezierFirstDerivative(t, _p0, _p1, _p2, _p3).normalized;


            Vector3 startUp = startPoint.up;
            Vector3 endUp = endPoint.up;
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
    
    void UpdateSwordTransform() {
        if (!sword) return;

        float t = swordT;
        Vector3 pos = CubicBezier(t, _p0, _p1, _p2, _p3);
        Vector3 tangent = BezierFirstDerivative(t, _p0, _p1, _p2, _p3).normalized;
        
        Vector3 startUp = startPoint.up;
        Vector3 endUp = endPoint.up;
        Vector3 localUp = Vector3.Slerp(startUp, endUp, t).normalized;
        
        Vector3 tipDir = Vector3.Cross(tangent, localUp).normalized;
        
        sword.position = pos;
        sword.rotation = Quaternion.LookRotation(tipDir, localUp);
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

    void DrawGizmoMeshQuads()
    {
        Gizmos.color = Color.yellow;
        // draw all squares (quads) of the slash mesh without the internal triangles
        Vector3[] vertices = _mesh.vertices;
        int[] triangles = _mesh.triangles;
        
        for (int i = 0; i < triangles.Length; i += 6)
        {
            Vector3 a = vertices[triangles[i + 0]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 5]];
            Vector3 d = vertices[triangles[i + 3]];

            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }
    }
    
    void OnDrawGizmos() {
        if (!startPoint || !endPoint) return;
    
        GenerateCurvePoints();
    
        DrawGizmoMeshQuads();

        // Draw curve
        Gizmos.color = Color.white;
        for (int i = 0; i < segments; i++) {
            float t0 = i / (segments - 1f);
            float t1 = (i + 1) / (segments - 1f);
            Vector3 p0 = CubicBezier(t0, _p0, _p1, _p2, _p3);
            Vector3 p1 = CubicBezier(t1, _p0, _p1, _p2, _p3);
            Gizmos.DrawLine(p0, p1);
        }
        
        // Draw control points
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_p0, 0.02f);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_p0, _p1);
        Gizmos.DrawSphere(_p1, 0.02f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(_p2, _p3);
        Gizmos.DrawSphere(_p2, 0.02f);
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(_p3, 0.02f);
    }

}
