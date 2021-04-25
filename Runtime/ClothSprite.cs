using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cloth2D
{
    public struct Vertex
    {
        public Vector3 pos;
        public Vector3 vel;
        public Vector3 f;
    }

    public struct Spring
    {
        public int p1;
        public int p2;
        public float ks;
        public float kd;
        public float restLength;

        public Spring(int p1, int p2, float ks, float kd, float restLength)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.ks = ks;
            this.kd = kd;
            this.restLength = restLength;
        }
    }

    public enum Cloth2DMode
    {
        None,
        Top_Left,
        Top_Right,
        Horizontal_Two,
        Horizontal_Three,
        Horizontal_All,
        Vertical_Two,
        Vertical_All,
        Vertical_Right_Two,
        Vertical_Right_All,
        Horizontal_Vertical_Two,
        Horizontal_Vertical_All,
        All_Corners
    }

    public class ClothSprite : MonoBehaviour
    {
        public Sprite sprite;
        [Tooltip("Turn texture upside down.")]
        public bool reverseTexture;
        [Tooltip("Use FixedUpdate instead of Update")]
        public bool useFixedUpdate;
        [Tooltip("How many segments will be. The higher resolution the less performance.")]
        [Range(4, 16)] public int resolution = 12;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float mass = 1f;
        [Range(0f, 1f)] public float stiffness = 0.5f;
        [Tooltip("Wetness will reduce the effect of wind. This will be decreased by wind X drySpeed at runtime.")]
        [Range(0f, 1f)] public float wetness = 0f;
        [Range(0f, 10f)] public float drySpeed = 1f;
        public Cloth2DMode mode = Cloth2DMode.Horizontal_Two;


        private List<int> _anchors = new List<int>();
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Material _material;
        private PolygonCollider2D _collider;

        private Vertex[] _vertices;
        private List<Spring> _springs;
        private float _width;
        private float _height;
        private float seed;
        private float _segmentWidth;
        private float _segmentHeight;
        private int[] _colliderPoints;
        private float _rad;

        private int _preSpriteId = -1;
        private int _preResolution;


        void Awake()
        {
            _rad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            transform.rotation = Quaternion.identity;
            Initialize();
            transform.localScale = Vector3.one;
        }

        // Start is called before the first frame update
        void Start()
        {
            _meshFilter.mesh = _mesh;
            seed = Random.value * 999f;
        }

        // Update is called once per frame
        void Update()
        {
            if (!useFixedUpdate)
            {
                UpdateCollider();
                UpdateCloth(Time.deltaTime);
            }
        }

        void FixedUpdate()
        {
            if (useFixedUpdate)
            {
                UpdateCloth(Time.fixedDeltaTime);
            }
        }

        public void OnTriggerStay2D(Collider2D collider)
        {
            // TODO: implement collision
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (collider.bounds.Contains(_vertices[i].pos))
                {
                    _vertices[i].pos = collider.ClosestPoint(_vertices[i].pos);
                    Debug.Log(i);
                }
            }
            _mesh.SetVertices(GetVertexPositions());
        }

        void OnValidate()
        {
            if (sprite == null)
            {
                if (_preSpriteId != -1)
                {
                    _preSpriteId = -1;
                    _meshFilter.mesh = new Mesh();
                    _vertices = null;
                }
                return;
            }
                
            if (_preResolution != resolution || _preSpriteId != sprite.GetInstanceID())
            {
                Initialize();
            }
        }

        private void Initialize()
        {
            if (sprite == null)
                return;

            _width = sprite.texture.width * transform.localScale.x / sprite.pixelsPerUnit;
            _height = sprite.texture.height * transform.localScale.y / sprite.pixelsPerUnit;
            _segmentWidth = _width / resolution;
            _segmentHeight = _height / resolution;
            _collider = GetComponent<PolygonCollider2D>();

            SetAnchors();
            GenerateMesh();
            GenerateSprings();
        }


        private void GenerateMesh()
        {
            /**
            * @example - 4x4
            *  P0  P1  P2  P3
            *  P4  P5  P6  P7
            *  P8  P9  P10 P11
            *  P12 P13 P14 P15
            */

            if (sprite == null)
                return;

            // Set values
            _preResolution = resolution;
            _preSpriteId = sprite.GetInstanceID();

            _meshFilter = GetComponent<MeshFilter>();
            _material = GetComponent<MeshRenderer>().sharedMaterial;

            if (_material == null)
                return;

            _material.mainTexture = sprite.texture;

            _mesh = new Mesh();
            _mesh.name = "ClothSpriteMesh";

            int length = resolution * resolution;
            int offset = resolution - 1;

            // Set Vertices
            _vertices = new Vertex[length];
            for (int i = 0; i < length; i++)
            {
                Vector3 newPos = new Vector3(_width / offset * (i % resolution), -_height / offset * (i / resolution), 0f);
                _vertices[i].pos = newPos;
                _vertices[i].vel = Vector3.zero;
                _vertices[i].f = Vector3.zero;
                RotateVector(ref _vertices[i].pos, _rad);
            }
            _mesh.SetVertices(GetVertexPositions());

            // Set Triangles
            List<int> tris = new List<int>(offset * offset * 6);
            for (int i = 0; i < length - resolution; i++)
            {
                if (i % resolution < resolution - 1)
                {
                    int[] currentAreaTris = new int[6]
                    {
                        i, i + 1, i + resolution,                       // upper left triangle
                        i + resolution, i + 1, i + resolution + 1       // lower right triangle
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
            if (reverseTexture)
            {
                for (int i = 0; i < length; i++)
                    uvs.Add(new Vector2((i % resolution) / offset, (i / resolution) / offset));
            }
            else
            {
                for (int i = resolution - 1; i >= 0; i--)
                {
                    for (int i2 = 0; i2 < resolution; i2++)
                        uvs.Add(new Vector2((float)i2 / offset, (float)i / offset)); // _vertices[i * resolution + i2].pos / (width, -height)
                }
            }
            _mesh.SetUVs(0, uvs);
        }

        private void RotateVector(ref Vector3 pos, float rad)
        {
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            float x = cos * pos.x + -sin * pos.y;
            float y = sin * pos.x + cos * pos.y;
            pos.x = x;
            pos.y = y;
        }

        private void GenerateSprings()
        {
            if (_vertices == null)
                GenerateMesh();

            float KsStruct = 0.5f, KdStruct = -0.25f;
            float KsShear = 0.5f, KdShear = -0.25f;
            float KsBend = 0.85f, KdBend = -0.25f;

            Vector3 delta;
            int r = resolution;
            _springs = new List<Spring>(_vertices.Length);

            // Horizontal Springs
            for (int v = 0; v < r; v++)
            {
                for (int u = 0; u < r - 1; u++)
                {
                    delta = _vertices[v * r + u].pos - _vertices[v * r + u + 1].pos;
                    _springs.Add(new Spring(v * r + u, v * r + u + 1, KsStruct, KdStruct, delta.magnitude));
                }
            }

            // Vertical Springs
            for (int u = 0; u < r; u++)
            {
                for (int v = 0; v < r - 1; v++)
                {
                    delta = _vertices[v * r + u].pos - _vertices[(v + 1) * r + u].pos;
                    _springs.Add(new Spring(v * r + u, (v + 1) * r + u, KsStruct, KdStruct, delta.magnitude));
                }
            }

            // Shear Springs
            for (int v = 0; v < r - 1; v++)
            {
                for (int u = 0; u < r - 1; u++)
                {
                    delta = _vertices[v * r + u].pos - _vertices[(v + 1) * r + u + 1].pos;
                    _springs.Add(new Spring(v * r + u, (v + 1) * r + u + 1, KsShear, KdShear, delta.magnitude));
                    delta = _vertices[(v + 1) * r + u].pos - _vertices[v * r + u + 1].pos;
                    _springs.Add(new Spring((v + 1) * r + u, v * r + u + 1, KsShear, KdShear, delta.magnitude));
                }
            }

            // Bend Springs
            for (int v = 0; v < r; v++)
            {
                for (int u = 0; u < r - 2; u++)
                {
                    delta = _vertices[v * r + u].pos - _vertices[v * r + u + 2].pos;
                    _springs.Add(new Spring(v * r + u, v * r + u + 2, KsBend, KdBend, delta.magnitude));
                }
            }
            for (int u = 0; u < r; u++)
            {
                for (int v = 0; v < r - 2; v++)
                {
                    delta = _vertices[v * r + u].pos - _vertices[(v + 2) * r + u].pos;
                    _springs.Add(new Spring(v * r + u, (v + 2) * r + u, KsBend, KdBend, delta.magnitude));
                }
            }
        }


        private void UpdateCollider()
        {
            if (_collider == null || !_collider.enabled)
                return;

            if (_vertices == null)
                return;

            _collider.pathCount = 1;
            Vector2[] points = new Vector2[12];

            if (_colliderPoints == null)
                SetColliderPoints();

            for (int i = 0; i < _colliderPoints.Length; i++)
            {
                points[i] = _vertices[_colliderPoints[i]].pos;
            }

            _collider.points = points;
        }

        private void SetColliderPoints()
        {
            _colliderPoints = new int[12];
            int index;
            int sqr = resolution * resolution;

            for (int i = 0; i < 3; i++)
            {
                index = resolution * i / 3;
                _colliderPoints[i] = index;
            }
            for (int i = 3; i < 7; i++)
            {
                index = (sqr - 1) - (sqr - resolution) * (6 - i) / 3; 
                _colliderPoints[i] = FindCloseColliderPoint(index) - 1;
            }
            for (int i = 7; i < 10; i++)
            {
                index = sqr - resolution + resolution * (9 - i) / 3;
                _colliderPoints[i] = index;
            }
            for (int i = 10; i < 12; i++)
            {
                index = (sqr - resolution) * (12 - i) / 3;
                _colliderPoints[i] = FindCloseColliderPoint(index);
            }
        }


        private void UpdateCloth(float dt)
        {
            if (sprite == null || _vertices == null || _springs == null)
                return;

            // Prevent large time step while selecting other gameobject in editor.
#if UNITY_EDITOR
            if (dt > 0.011f)
                dt = 0.011f;
#endif

            ComputeForces(dt);
            IntegrateMidPointEuler(dt);
            // IntegrateRK4(dt);
            ApplyProvotDynamicInverse();

            // Update mesh
            _mesh.SetVertices(GetVertexPositions());
        }

        private List<Vector3> GetVertexPositions()
        {
            List<Vector3> positions = new List<Vector3>(resolution * resolution);
            foreach (var vertex in _vertices)
            {
                positions.Add(vertex.pos);
            }
            return positions;
        }

        private void SetAnchors()
        {
            _anchors.Clear();
            switch (mode)
            {
                case Cloth2DMode.Top_Left:
                    _anchors.Add(0);
                    break;
                case Cloth2DMode.Top_Right:
                    _anchors.Add(resolution - 1);
                    break;
                case Cloth2DMode.Horizontal_Two:
                    _anchors.Add(0);
                    _anchors.Add(resolution - 1);
                    break;
                case Cloth2DMode.Horizontal_Three:
                    _anchors.Add(0);
                    _anchors.Add((resolution - 1) / 2);
                    _anchors.Add(resolution - 1);
                    break;
                case Cloth2DMode.Horizontal_All:
                    for (int i = 0; i < resolution; i++)
                        _anchors.Add(i);
                    break;
                case Cloth2DMode.Vertical_Two:
                    _anchors.Add(0);
                    _anchors.Add(resolution * (resolution - 1));
                    break;
                case Cloth2DMode.Vertical_All:
                    for (int i = 0; i < resolution; i++)
                        _anchors.Add(i * resolution);
                    break;
                case Cloth2DMode.Vertical_Right_Two:
                    _anchors.Add(resolution - 1);
                    _anchors.Add(resolution * resolution - 1);
                    break;
                case Cloth2DMode.Vertical_Right_All:
                    for (int i = 0; i < resolution; i++)
                        _anchors.Add((i + 1) * resolution - 1);
                    break;
                case Cloth2DMode.Horizontal_Vertical_Two:
                    _anchors.Add(0);
                    _anchors.Add(resolution - 1);
                    _anchors.Add(resolution * (resolution - 1));
                    break;
                case Cloth2DMode.Horizontal_Vertical_All:
                    for (int i = 0; i < resolution; i++)
                        _anchors.Add(i);
                    for (int i = 1; i < resolution; i++)
                        _anchors.Add(i * resolution);
                    break;
                case Cloth2DMode.All_Corners:
                    _anchors.Add(0);
                    _anchors.Add(resolution - 1);
                    _anchors.Add(resolution * (resolution - 1));
                    _anchors.Add(resolution * resolution - 1);
                    break;
                default:
                    break;
            }
        }

        private bool isAnchorVertex(int i)
        {
            return _anchors.Contains(i);
        }

        private void ApplyGravity(int i)
        {
            _vertices[i].f.y -= 98.1f * gravity / sprite.pixelsPerUnit;
        }

        private void ApplyWinds(int i, float dt)
        {
            foreach(var wind2d in Wind2DReceiver.GetInstance().Winds.Values)
            {
                float wind =  wind2d.GetWind(transform.position);
                float turbulence =  wind2d.GetTurbulence(transform.position);
                float wet = wetness * 0.25f;
                
                Vector3 windForce = Mathf.Pow(wind / (mass + wet), 1.5f) * wind2d.windDriection * 10f;
                _vertices[i].f.x += windForce.x * _segmentWidth;
                _vertices[i].f.y += windForce.y * _segmentHeight;
                _vertices[i].f.x += (Mathf.PerlinNoise(Time.time + i * _segmentWidth * 0.3f, seed) - 0.5f) / (1f + wet) * turbulence * _segmentWidth * 10f;
                _vertices[i].f.y += (Mathf.PerlinNoise(seed, Time.time + i * _segmentHeight * 0.3f) - 0.5f) / (1f + wet) * turbulence * _segmentHeight * 10f;
                
                if (wetness > 0f)
                {
                    wetness -= wind * dt * drySpeed / 2500f;
                    if (wetness < 0f)
                        wetness = 0f;
                }
            }
        }

        private void ComputeForces(float dt)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].f = Vector3.zero;
                if (!isAnchorVertex(i))
                {
                    ApplyGravity(i);
                    ApplyWinds(i, dt);
                }

                // Dampling
                _vertices[i].f += -1.25f * _vertices[i].vel;
            }

            // Add spring forces
            for (int i = 0; i < _springs.Count; i++)
            {
                Spring s = _springs[i];
                Vector3 deltaP = _vertices[s.p1].pos - _vertices[s.p2].pos;
                Vector3 deltaV = _vertices[s.p1].vel - _vertices[s.p2].vel;
                float dist = deltaP.magnitude;

                float leftTerm = -s.ks * (dist - s.restLength);
                float rightTerm = -s.kd * (Vector3.Dot(deltaV, deltaV) / dist);
                Vector3 springForce = (leftTerm + rightTerm) * deltaP.normalized;

                // float maxForce = Mathf.Max(_segmentWidth, _segmentHeight);
                // if (springForce.magnitude > maxForce)
                //     springForce = springForce.normalized * maxForce;

                if (!isAnchorVertex(s.p1))
                    _vertices[s.p1].f += springForce;
                if (!isAnchorVertex(s.p2))
                    _vertices[s.p2].f -= springForce;
            }
        }

        private void IntegrateMidPointEuler(float dt)
        {
            float dtMass = dt / mass;
            int i = 0;

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += _vertices[i].f * dtMass / 2f;
                _vertices[i].pos += dt * _vertices[i].vel;
            }

            ComputeForces(dt);

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += _vertices[i].f * dtMass;
                _vertices[i].pos += dt * _vertices[i].vel;
            }
        }

        private void IntegrateRK4(float dt)
        {
            float halfDeltaTime  = dt / 2f;
            float thirdDeltaTime = 1 / 3f;
            float sixthDeltaTime = 1 / 6f;
            float halfDTMass = halfDeltaTime / mass;
            float dtMass = dt / mass;
            int i = 0;

            Vector3[] sumF = new Vector3[_vertices.Length];
            Vector3[] sumV = new Vector3[_vertices.Length];

            for (i = 0; i < _vertices.Length; i++)
            {
                sumF[i] = (_vertices[i].f * halfDTMass) * sixthDeltaTime;
		        sumV[i] = halfDeltaTime * _vertices[i].vel  * sixthDeltaTime;
            }

            ComputeForces(dt);

            for (i = 0; i < _vertices.Length; i++)
            {
                sumF[i] = (_vertices[i].f * halfDTMass) * thirdDeltaTime;
		        sumV[i] = halfDeltaTime * _vertices[i].vel  * thirdDeltaTime;
            }

            ComputeForces(dt);

            for (i = 0; i < _vertices.Length; i++)
            {
                sumF[i] = (_vertices[i].f * dtMass) * thirdDeltaTime;
		        sumV[i] = dt * _vertices[i].vel  * thirdDeltaTime;
            }

            ComputeForces(dt);

            for (i = 0; i < _vertices.Length; i++)
            {
                sumF[i] = (_vertices[i].f * dtMass) * sixthDeltaTime;
		        sumV[i] = dt * _vertices[i].vel  * sixthDeltaTime;
            }

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += sumF[i];
                _vertices[i].pos += sumV[i];
            }
        }

        private void ApplyProvotDynamicInverse()
        {
            for (int i = 0; i < _springs.Count; i++)
            {
                int p1 = _springs[i].p1;
                int p2 = _springs[i].p2;
                Vector3 deltaP = _vertices[p1].pos - _vertices[p2].pos;
                float dist = deltaP.magnitude;

                if (dist > _springs[i].restLength)
                {
                    // Min: 0.1, Max: 4
                    float rate = Mathf.Pow((2f * stiffness - 2f), 2f);
                    if (rate < 0.1f)
                        rate = 0.1f;

                    dist = (dist - _springs[i].restLength) / rate;
                    deltaP = deltaP.normalized * dist;

                    if (isAnchorVertex(p1))
                    {
                        _vertices[p2].vel += deltaP;
                    }
                    else if (isAnchorVertex(p2))
                    {
                        _vertices[p1].vel -= deltaP;
                    }
                    else
                    {
                        _vertices[p1].vel -= deltaP;
                        _vertices[p2].vel += deltaP;
                    }
                }
            }
        }

        private int FindCloseColliderPoint(int i)
        {
            int mod = i % resolution;
            return (mod < resolution / 2) ? i - mod : i + (resolution - mod);
        }

        private int FindCloseHitPoint(Vector2 contactPos)
        {
            int point = 0;
            float min = (_collider.points[0] - contactPos).magnitude;

            // Find close point from collider
            for (int i = 1; i < _collider.points.Length; i++)
            {
                float current = (_collider.points[i] * transform.localScale - contactPos).magnitude;
                if (current < min)
                {
                    min = current;
                    point = i;
                }
            }

            // Find mesh index from point.
            return _colliderPoints[point];
        }


        private void OnDrawGizmosSelected()
        {
            if (_vertices == null || _vertices.Length < 1)
                return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            Vector3 curPos = transform.position;
            for (int i = 0; i < _vertices.Length; i++)
            {
                Gizmos.DrawWireCube(curPos + _vertices[i].pos , Vector3.one * 0.05f);
                if (i % resolution < resolution - 1)
                {
                    Gizmos.DrawLine(curPos + _vertices[i].pos, curPos + _vertices[i + 1].pos);
                    if (i < _vertices.Length - resolution)
                    {
                        Gizmos.DrawLine(curPos + _vertices[i].pos, curPos +_vertices[i + resolution].pos);
                        Gizmos.DrawLine(curPos + _vertices[i + 1].pos, curPos + _vertices[i + resolution].pos);
                        Gizmos.DrawLine(curPos + _vertices[i + 1].pos, curPos + _vertices[i + resolution + 1].pos);
                    }
                }
            }
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }

    }
}