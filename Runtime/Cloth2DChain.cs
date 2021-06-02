using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cloth2D
{
    public class Cloth2DChain : MonoBehaviour
    {
        public Sprite sprite;
        public Color color = Color.white;
        [Tooltip("The last anchor index. If point's index <= lastAnchor, the position is fixed.")]
        [Min(0)] public int lastAnchor = 0;
        [Tooltip("Use FixedUpdate instead of Update")]
        public bool useFixedUpdate = false;
        [Tooltip("How far the first anchor is from transform position. 0.5 means the center.")]
        [Range(0f, 1f)] public float anchorOffset = 0.5f;
        [Tooltip("Total number of chain points simulated.")]
        [Range(2, 16)] public int chainPoints = 5;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float mass = 1f;
        [Tooltip("How much velocity is added when snapping back to the correct length.")]
        [Range(0f, 6f)] public float elasticResponse = 0f;

        private Transform _transform;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Material _material;
        
        private float _seed;
        private float _width;
        private float _height;
        private float _segmentHeight;
        private Vertex[] _vertices;
        private Vector3[] _positions;
        private int _preSpriteId = -1;
        private int _preChainPoints;


        private void Awake()
        {
            Initialize();
            _transform.localScale = Vector3.one;
        }

        private void Start()
        {
            _meshFilter.sharedMesh = _mesh;
        }

        private void Update()
        {
            if (!useFixedUpdate)
            {
                StepChainCloth(Time.deltaTime);
            }
        }
        private void FixedUpdate()
        {
            if (useFixedUpdate)
            {
                StepChainCloth(Time.fixedDeltaTime);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (sprite == null)
            {
                if (_preSpriteId != -1)
                {
                    _preSpriteId = -1;
                    _vertices = null;
                    UnityEditor.EditorApplication.delayCall += () => _meshFilter.sharedMesh = new Mesh();
                }
                return;
            }
            
            if (_preChainPoints != chainPoints || _preSpriteId != sprite.GetInstanceID())
            {
                Initialize(true);
                UnityEditor.EditorApplication.delayCall += () => { if (_meshFilter != null) _meshFilter.sharedMesh = _mesh; };
            }

            if (_material != null)
                _material.color = color;
        }
#endif

        private void Initialize(bool isOnValidate = false)
        {
            if (sprite == null)
                return;

            _transform = transform;
            _width = sprite.texture.width * _transform.localScale.x / sprite.pixelsPerUnit;
            _height = sprite.texture.height * _transform.localScale.y / sprite.pixelsPerUnit;
#if UNITY_EDITOR
            if (isOnValidate)
            {
                _width = sprite.texture.width / sprite.pixelsPerUnit;
                _height = sprite.texture.height / sprite.pixelsPerUnit;
            }
#endif
            _segmentHeight = _height / chainPoints;
            _seed = Random.value * 999f;

            GenerateMesh(isOnValidate);
        }

        private void GenerateMesh(bool isOnValidate = false)
        {
            if (sprite == null)
                return;

            // Set values
            _preChainPoints = chainPoints;
            _preSpriteId = sprite.GetInstanceID();

            _meshFilter = GetComponent<MeshFilter>();
            _material = GetComponent<MeshRenderer>().sharedMaterial;
#if UNITY_EDITOR
            if (isOnValidate && _material != null)
            {
                _material = new Material(_material);
                GetComponent<MeshRenderer>().material = _material;
            }
#endif

            if (_material == null)
                return;

            _material.mainTexture = sprite.texture;
            _material.color = color;

            _mesh = new Mesh();
            _mesh.name = "ChainClothMesh";

            int length = chainPoints * 2;

            // Set Vertices
            _vertices = new Vertex[chainPoints];
            _positions = new Vector3[length];
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 newPos = i == 0 ? Vector3.right * (_width * anchorOffset) : _vertices[i - 1].pos + Vector3.down * _segmentHeight;
                _vertices[i].pos = newPos;
                _vertices[i].vel = Vector3.zero;
                _vertices[i].f = Vector3.zero;
            }
            SetVertexPositions(_positions);
            _mesh.SetVertices(_positions);

            // Set Triangles
            List<int> tris = new List<int>(chainPoints * 6);
            for (int i = 0; i < length - 2; i++)
            {
                if (i % 2 == 0)
                {
                    int[] currentAreaTris = new int[6]
                    {
                        i, i + 1, i + 2,                        // upper left triangle
                        i + 2, i + 1, i + 3                     // lower right triangle
                    };
                    tris.AddRange(currentAreaTris);
                }
            }
            _mesh.SetTriangles(tris, 0);

            // Set Normals
            Vector3[] normals = new Vector3[length];
            for (int i = 0; i < length; i++)
            {
                normals[i] = Vector3.forward;
            }
            _mesh.normals = normals;

            // Set UVs
            List<Vector2> uvs = new List<Vector2>(length);
            for (int i = chainPoints - 1; i >= 0; i--)
            {
                uvs.Add(new Vector2(0f, (float)i / (chainPoints - 1)));
                uvs.Add(new Vector2(1f, (float)i / (chainPoints - 1)));
            }
            _mesh.SetUVs(0, uvs);
        }

        private void StepChainCloth(float dt)
        {
            if (sprite == null)
                return;

            ComputeForces(dt);
            IntegrateEuler(dt);
            for (int i = 0; i < _vertices.Length; i++)
                AdjustSegmentLength(i);

            // Update mesh
            SetVertexPositions(_positions);
            _mesh.SetVertices(_positions);
        }

        private void ApplyGravity(int i)
        {
            _vertices[i].f.y -= 98.1f * gravity / sprite.pixelsPerUnit;
        }

        private void ApplyWinds(int i, float dt)
        {
            foreach(var wind2d in Wind2DReceiver.Instance.Winds.Values)
            {
                float wind =  wind2d.GetWind(_transform.position);
                float turbulence =  wind2d.GetTurbulence(_transform.position);
                
                Vector3 windForce = Mathf.Pow(wind / mass, 1.5f) * wind2d.windDriection * 10f;
                _vertices[i].f.x += windForce.x * _segmentHeight;
                _vertices[i].f.y += windForce.y * _segmentHeight;
                _vertices[i].f.x += (Mathf.PerlinNoise(Time.time + i * _segmentHeight * 0.3f, _seed) - 0.5f) * turbulence * _segmentHeight * 10f;
                _vertices[i].f.y += (Mathf.PerlinNoise(_seed, Time.time + i * _segmentHeight * 0.3f) - 0.5f) * turbulence * _segmentHeight * 10f;
            }
        }

        private void ComputeForces(float dt)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].f = Vector3.zero;
                if (i > lastAnchor)
                {
                    ApplyGravity(i);
                    ApplyWinds(i, dt);
                }

                // Dampling
                _vertices[i].f += -1.25f * _vertices[i].vel;
            }
        }

        private void IntegrateEuler(float dt)
        {
            float dtMass = dt / mass;
            int i = 0;
            float maxLimit = 100f / sprite.pixelsPerUnit * _segmentHeight * 5f;

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += _vertices[i].f * dtMass / 2f;
                Cloth2DUtils.ClampVelocity(ref _vertices[i].vel, maxLimit);
                _vertices[i].pos += dt * _vertices[i].vel;
            }
        }

        private void AdjustSegmentLength(int i)
        {
            if (i > lastAnchor)
            {
                Vector3 offset = _vertices[i].pos - _vertices[i - 1].pos;
                if (offset.magnitude > _segmentHeight)
                {
                    Vector3 newpos = _vertices[i - 1].pos + offset.normalized * _segmentHeight;
                    _vertices[i].vel += elasticResponse * (newpos - _vertices[i].pos);
                    _vertices[i].pos = newpos;
                }
            }
        }

        private void SetVertexPositions(Vector3[] _positions)
        {
            Vector3 p0, p1;
            float sign = 1f;
            float rad = 0f;

            // Calculate mesh vertex points by chain vertices.
            for (int i = 0; i < _vertices.Length; i++)
            {
                p0 = p1 = Vector3.zero;
                p0.x -= _width * anchorOffset;
                p1.x += _width * (1f - anchorOffset);

                if (i > lastAnchor)
                {
                    if (i < _vertices.Length - 1)
                    {
                        Vector3 diff = _vertices[i + 1].pos - _vertices[i].pos;
                        rad = Mathf.Acos(Vector3.Dot(Vector3.down, diff.normalized));
                        sign = Mathf.Sign(diff.x);
                    }
                    Cloth2DUtils.RotateVector(ref p0, rad * sign);
                    Cloth2DUtils.RotateVector(ref p1, rad * sign);
                }

                _positions[i * 2] = _vertices[i].pos + p0;
                _positions[i * 2 + 1] = _vertices[i].pos + p1;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_vertices == null || _vertices.Length < 1)
                return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 curPos = _transform.position;
            float rad = _transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 curVPos = curPos + Cloth2DUtils.TransformVector(_vertices[i].pos, _transform.lossyScale, rad);
                Gizmos.DrawWireCube(curVPos, Vector3.one * 0.05f);
                if (i > 0)
                {
                    Gizmos.DrawLine(curPos + Cloth2DUtils.TransformVector(_vertices[i - 1].pos, _transform.lossyScale, rad), curVPos);
                }
            }
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }
#endif

    }
}