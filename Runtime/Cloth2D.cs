using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cloth2D
{
    public class Cloth2D : MonoBehaviour
    {
        public const string clothTag = "Cloth2D";
        public Sprite sprite;
        [Tooltip("Flip texture vertically.")]
        public bool flipTexture;
        [Tooltip("Use FixedUpdate instead of Update")]
        public bool useFixedUpdate = true;
        [Tooltip("Total number of segments simulated. The higher resolution the less performance.")]
        [Range(4, 16)] public int resolution = 8;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float mass = 1f;
        [Range(0f, 1f)] public float stiffness = 0.5f;
        [Tooltip("Wetness will reduce the effect of wind. This will be decreased by wind X drySpeed at runtime.")]
        [Range(0f, 1f)] public float wetness = 0f;
        [Range(0f, 10f)] public float drySpeed = 1f;
        [Tooltip("Anchors will be set by mode.")]
        public Cloth2DMode mode = Cloth2DMode.Horizontal_Two;
        [Tooltip("How much the collision will affect.")]
        [Range(0f, 5f)] public float collisionResponse = 1.2f;

        private List<int> _anchors = new List<int>();
        private Transform _transform;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Material _material;
        private PolygonCollider2D _collider;

        private Vertex[] _vertices;
        private List<Spring> _springs;
        private Vector3[] _positions;
        private float _width;
        private float _height;
        private float _seed;
        private float _segmentWidth;
        private float _segmentHeight;
        private Vector2[] _colliderPoints;
        private int[] _colliderIndexPoints;
        private float _rad;
        private int _preSpriteId = -1;
        private int _preResolution;


        private void Awake()
        {
            Initialize();
            _transform.rotation = Quaternion.identity;
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
                StepCloth(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            UpdateCollider();
            if (useFixedUpdate)
            {
                StepCloth(Time.fixedDeltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D collider)
        {
            ApplyCollision(collider);
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            ApplyCollision(collider);
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
            
            if (_preResolution != resolution || _preSpriteId != sprite.GetInstanceID())
            {
                Initialize(true);
                UnityEditor.EditorApplication.delayCall += () => { if (_meshFilter != null) _meshFilter.sharedMesh = _mesh; };
            }
        }
#endif

        private void Initialize(bool isOnValidate = false)
        {
            if (sprite == null)
                return;

            _transform = transform;
            _rad = _transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            _width = sprite.texture.width * _transform.localScale.x / sprite.pixelsPerUnit;
            _height = sprite.texture.height * _transform.localScale.y / sprite.pixelsPerUnit;
#if UNITY_EDITOR
            if (isOnValidate)
            {
                _rad = 0f;
                _width = sprite.texture.width / sprite.pixelsPerUnit;
                _height = sprite.texture.height / sprite.pixelsPerUnit;
            }
#endif
            _segmentWidth = _width / resolution;
            _segmentHeight = _height / resolution;
            _collider = GetComponent<PolygonCollider2D>();
            _seed = Random.value * 999f;

            SetAnchors();
            GenerateMesh(isOnValidate);
            GenerateSprings();
        }


        private void GenerateMesh(bool isOnValidate = false)
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
#if UNITY_EDITOR
            if (isOnValidate)
            {
                _material = new Material(_material);
                GetComponent<MeshRenderer>().material = _material;
            }
#endif

            if (_material == null)
                return;

            _material.mainTexture = sprite.texture;

            _mesh = new Mesh();
            _mesh.name = "Cloth2DMesh";

            int length = resolution * resolution;
            int offset = resolution - 1;

            // Set Vertices
            _vertices = new Vertex[length];
            _positions = new Vector3[length];
            for (int i = 0; i < length; i++)
            {
                Vector3 newPos = new Vector3(_width / offset * (i % resolution), -_height / offset * (i / resolution), 0f);
                _vertices[i].pos = newPos;
                _vertices[i].vel = Vector3.zero;
                _vertices[i].f = Vector3.zero;
                Cloth2DUtils.RotateVector(ref _vertices[i].pos, _rad);
            }
            SetVertexPositions(_positions);
            _mesh.SetVertices(_positions);

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
            if (flipTexture)
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

            // if (_vertices == null)
            //     return;

            _collider.pathCount = 1;
            if (_colliderPoints == null)
            {
                _colliderPoints = new Vector2[17];
                SetColliderPoints();
            }

            for (int i = 0; i < _colliderPoints.Length; i++)
            {
                _colliderPoints[i] = _vertices[_colliderIndexPoints[i]].pos;
            }

            _collider.points = _colliderPoints;
        }

        private void SetColliderPoints()
        {
            _colliderIndexPoints = new int[17];
            int index;
            int sqr = resolution * resolution;

            for (int i = 0; i < 3; i++)
            {
                index = resolution * i / 3;
                _colliderIndexPoints[i] = index;
            }
            for (int i = 3; i < 7; i++)
            {
                index = (sqr - 1) - (sqr - resolution) * (6 - i) / 3; 
                _colliderIndexPoints[i] = FindCloseColliderPoint(index) - 1;
            }
            for (int i = 7; i < 10; i++)
            {
                index = sqr - resolution + resolution * (9 - i) / 3;
                _colliderIndexPoints[i] = index;
            }
            for (int i = 10; i < 12; i++)
            {
                index = (sqr - resolution) * (12 - i) / 3;
                _colliderIndexPoints[i] = FindCloseColliderPoint(index);
            }

            _colliderIndexPoints[12] = _colliderIndexPoints[11] + resolution / 3;
            _colliderIndexPoints[13] = _colliderIndexPoints[4] - resolution / 3;
            _colliderIndexPoints[14] = _colliderIndexPoints[5] - resolution / 3;
            _colliderIndexPoints[15] = _colliderIndexPoints[10] + resolution / 3;
            _colliderIndexPoints[16] = _colliderIndexPoints[11];
        }


        private void StepCloth(float dt)
        {
            if (sprite == null)
                return;

            // if (_vertices == null || _springs == null)
            //     return;

#if UNITY_EDITOR
            // Prevent large time step while selecting other gameobject in editor.
            if (dt > 0.0167f)
                dt = 0.0167f;
#endif

            ComputeForces(dt);
            IntegrateMidPointEuler(dt);
            // IntegrateRK4(dt);
            ApplyProvotDynamicInverse();

            // Update mesh
            SetVertexPositions(_positions);
            _mesh.SetVertices(_positions);
        }

        private void SetVertexPositions(Vector3[] _positions)
        {
            for (int i = 0; i < _vertices.Length; i++)
            {
                _positions[i] = _vertices[i].pos;
            }
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
            foreach(var wind2d in Wind2DReceiver.Instance.Winds.Values)
            {
                float wind =  wind2d.GetWind(_transform.position);
                float turbulence =  wind2d.GetTurbulence(_transform.position);
                float wet = wetness * 0.25f;
                
                Vector3 windForce = Mathf.Pow(wind / (mass + wet), 1.5f) * wind2d.windDriection * 10f;
                _vertices[i].f.x += windForce.x * _segmentWidth;
                _vertices[i].f.y += windForce.y * _segmentHeight;
                _vertices[i].f.x += (Mathf.PerlinNoise(Time.time + i * _segmentWidth * 0.3f, _seed) - 0.5f) / (1f + wet) * turbulence * _segmentWidth * 10f;
                _vertices[i].f.y += (Mathf.PerlinNoise(_seed, Time.time + i * _segmentHeight * 0.3f) - 0.5f) / (1f + wet) * turbulence * _segmentHeight * 10f;
                
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

                float maxForce = Mathf.Max(_segmentWidth, _segmentHeight);
                if (springForce.magnitude > maxForce)
                    springForce = springForce.normalized * maxForce;

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
            float maxLimit = 100f / sprite.pixelsPerUnit * Mathf.Min(_segmentWidth, _segmentHeight) * 5f;

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += _vertices[i].f * dtMass / 2f;
                _vertices[i].pos += dt * _vertices[i].vel;
                Cloth2DUtils.ClampVelocity(ref _vertices[i].vel, maxLimit);
            }

            ComputeForces(dt);

            for (i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].vel += _vertices[i].f * dtMass;
                _vertices[i].pos += dt * _vertices[i].vel;
                Cloth2DUtils.ClampVelocity(ref _vertices[i].vel, maxLimit);
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

        private void ApplyCollision(Collider2D collider)
        {
            Vector3 targetV;
            Vector3 worldPos;
            float unit = 100f / sprite.pixelsPerUnit;
            if (collider.CompareTag(Cloth2D.clothTag))
            {
                for (int i = 0; i < _vertices.Length; i++)
                {
                    worldPos = _transform.position + _vertices[i].pos;
                    if (!isAnchorVertex(i) && collider.OverlapPoint(worldPos))
                    {
                        targetV = _collider.bounds.center - worldPos;
                        
                        Cloth2DUtils.ClampVelocity(ref targetV, unit * collisionResponse);
                        _vertices[i].vel = targetV;
                    }
                }
                return;
            }

            /**
             *  <Step>
             *  1.  Check rigidbody's velocity.
             *  2.  targetV <- vertex.vel + rigidbody.vel
             *        if kinematic, use estimated deltaV together.
             *  3.  Interpolate targetV after checking forthV dot targetV.
             *        if dot < 0, targetV <- targetV + forthV + (clothOriginV)
             *  4.  Clamp targetV & vertex.vel <- targetV
             */
            Vector3 otherV = Vector3.zero;
            Vector3 kinematicForthV = Vector3.zero;
            if (collider.attachedRigidbody != null)
            {
                otherV = collider.attachedRigidbody.velocity;
                if (collider.attachedRigidbody.bodyType == RigidbodyType2D.Kinematic)
                {
                    kinematicForthV = _collider.bounds.center - collider.bounds.center;
                    otherV += collider.GetComponent<Cloth2DKinematicReceiver>()?.DeltaPosition / Time.fixedDeltaTime ?? Vector3.zero;
                }
            }

            for (int i = 0; i < _vertices.Length; i++)
            {
                worldPos = _transform.position + _vertices[i].pos;
                if (!isAnchorVertex(i) && collider.OverlapPoint(worldPos))
                {
                    targetV = _vertices[i].vel + otherV + kinematicForthV;
                    Vector3 direction = targetV.normalized;
                    Vector3 forthV = (worldPos - collider.bounds.center).normalized;
                    if (Vector3.Dot(direction, forthV) < 0f)
                    {
                        Vector3 targetDirection = (direction + forthV + (_collider.bounds.center - worldPos).normalized);
                        targetV.x = Mathf.Abs(targetV.x) * targetDirection.x;
                        targetV.y = Mathf.Abs(targetV.y) * targetDirection.y;
                    }
                    else
                    {
                        if (Mathf.Sign(targetV.x) != Mathf.Sign(forthV.x))
                            targetV.x *= -1f;
                        if (Mathf.Sign(targetV.y) != Mathf.Sign(forthV.y))
                            targetV.y *= -1f;
                    }

                    if (otherV.sqrMagnitude < 0.01f)
                        Cloth2DUtils.ClampVelocity(ref targetV, unit * 0.3f);
                    else
                        Cloth2DUtils.ClampVelocity(ref targetV, unit * collisionResponse);
                    _vertices[i].vel = targetV;
                }
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
                if (i % resolution < resolution - 1)
                {
                    Vector3 rightVPos = curPos + Cloth2DUtils.TransformVector(_vertices[i + 1].pos, _transform.lossyScale, rad);
                    Gizmos.DrawLine(curVPos, rightVPos);
                    if (i < _vertices.Length - resolution)
                    {
                        Vector3 downVPos = curPos + Cloth2DUtils.TransformVector(_vertices[i + resolution].pos, _transform.lossyScale, rad);
                        Vector3 diagonalVPos = curPos + Cloth2DUtils.TransformVector(_vertices[i + resolution + 1].pos, _transform.lossyScale, rad);
                        Gizmos.DrawLine(curVPos, downVPos);
                        Gizmos.DrawLine(rightVPos, downVPos);
                        Gizmos.DrawLine(rightVPos, diagonalVPos);
                    }
                }
            }
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }
#endif

    }
}