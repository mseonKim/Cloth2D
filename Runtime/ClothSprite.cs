﻿using System.Collections;
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

    [ExecuteAlways]
    public class ClothSprite : MonoBehaviour
    {
        public Sprite sprite;
        public bool reverseTexture;
        [Range(4, 16)] public int resolution = 12;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float mass = 1f;
        [Range(0f, 1f)] public float stiffness = 0.5f;
        [Range(1f, 2f)] public float flexibleScale = 1.2f;
        [Range(0f, 1f)] public float wetness = 0f;
        [Range(0f, 10f)] public float drySpeed = 1f;
        public List<int> anchors = new List<int>();


        private Wind2D wind2d;
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
        private int _preResolution;
        private int[] _colliderPoints;


        void Awake()
        {
            Initialize();
            GenerateMesh();
            GenerateSprings();
        }

        // Start is called before the first frame update
        void Start()
        {
            wind2d = FindObjectOfType<Wind2D>();
            seed = Random.value * 999f;
        }

        // Update is called once per frame
        void Update()
        {
            GenerateMesh();
            // UpdateCollider();
            UpdateCloth(Time.deltaTime);
        }


        private void Initialize()
        {
            if (sprite == null)
                return;

            _width = sprite.texture.width / sprite.pixelsPerUnit;
            _height = sprite.texture.height / sprite.pixelsPerUnit;
            _collider = GetComponent<PolygonCollider2D>();
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

            if (resolution == _preResolution || sprite == null)
                return;

            // Set values
            _preResolution = resolution;

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
                _vertices[i].pos = new Vector3(_width / offset * (i % resolution), -_height / offset * (i / resolution), 0f);
                _vertices[i].vel = Vector3.zero;
                _vertices[i].f = Vector3.zero;
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
                normals[i] = -Vector3.forward;
            }
            _mesh.normals = normals;

            // Set UVs
            List<Vector2> uvs = new List<Vector2>(length);
            for (int i = length - 1; i >= 0; i--)
            {
                uvs.Add(new Vector2(_vertices[i].pos.x / _width, -_vertices[i].pos.y / _height));
            }
            if (reverseTexture)
            {
                uvs.Reverse();
            }
            _mesh.SetUVs(0, uvs);

            _meshFilter.mesh = _mesh;
        }

        private void GenerateSprings()
        {
            if (_vertices == null)
                GenerateMesh();

            Vector3 delta;
            int r = resolution;
            _springs = new List<Spring>(_vertices.Length);
            float	KsStruct = 0.5f, KdStruct = -0.25f;
            float	KsShear = 0.5f, KdShear = -0.25f;
            float	KsBend = 0.85f, KdBend = -0.25f;

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
            if (_vertices == null || wind2d == null || _springs == null)
                return;

            // float scaleOffset = stiffness + flexibleScale * (1f - stiffness);
            _segmentWidth = _width / resolution;
            _segmentHeight = _height / resolution;

            ComputeForces(dt);
            // IntegrateEuler(dt);
            // IntegrateRK4(dt);
            IntegrateMidPointEuler(dt);
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

        private bool isAnchorVertex(int i)
        {
            return anchors.Contains(i);
        }

        private void ApplyGravity(int i)
        {
            // _vertices[i].vel.y -= 0.0981f * gravity * dt / sprite.pixelsPerUnit;
            _vertices[i].f.y -= 98.1f * gravity / sprite.pixelsPerUnit;
        }

        private void ApplyWinds(int i, float dt)
        {
            float wind =  wind2d.GetWind(transform.position);
            float wet = wetness * 0.25f;
            
            _vertices[i].f.x += Mathf.Pow(wind / (mass + wet), 1.5f) * wind2d.windDriection.x * _segmentWidth * 10f;
            _vertices[i].f.y += Mathf.Pow(wind / (mass + wet), 1.5f) * wind2d.windDriection.y * _segmentHeight * 10f;
            _vertices[i].f.x += (Mathf.PerlinNoise(Time.time + i * _segmentWidth * 0.3f, seed) - 0.5f) / (1f + wet) * wind2d.turbulence * _segmentWidth * 10f;
            _vertices[i].f.y += (Mathf.PerlinNoise(seed, Time.time + i * _segmentHeight * 0.3f) - 0.5f) / (1f + wet) * wind2d.turbulence * _segmentHeight * 10f;
            
            if (wetness > 0f)
            {
                wetness -= wind * dt * drySpeed / 2500f;
                if (wetness < 0f)
                    wetness = 0f;
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

                if (springForce.magnitude > 100f)
                    springForce = springForce.normalized * 100f;

                if (!isAnchorVertex(s.p1))
                    _vertices[s.p1].f += springForce;
                if (!isAnchorVertex(s.p2))
                    _vertices[s.p2].f -= springForce;
            }
        }

        private void IntegrateEuler(float dt)
        {
            float dtMass = dt / mass;

            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 oldV = _vertices[i].vel;
                _vertices[i].vel += _vertices[i].f * dtMass;
                _vertices[i].pos += dt * oldV;
            }
        }

        private void IntegrateMidPointEuler(float dt)
        {
            float halfDeltaTime = dt / 2f;
            float dtMass = halfDeltaTime / mass;
            int i = 0;

            for (i = 0; i < _vertices.Length; i++)
            {
                Vector3 oldV = _vertices[i].vel;
                _vertices[i].vel += _vertices[i].f * dtMass;
                _vertices[i].pos += dt * oldV;
            }

            ComputeForces(dt);

            dtMass = dt / mass;
            for (i = 0; i < _vertices.Length; i++)
            {
                Vector3 oldV = _vertices[i].vel;
                _vertices[i].vel += _vertices[i].f * dtMass;
                _vertices[i].pos += dt * oldV;
            }
        }

        private void IntegrateRK4(float dt)
        {
            float halfDeltaTime  = dt / 2f;
            float thirdDeltaTime = 1 / 3f;
            float sixthDeltaTime = 1 / 6f;
            float halfDTMass = halfDeltaTime/ mass;
            float dtMass = dt/ mass;
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
            Spring s;
            for (int i = 0; i < _springs.Count; i++)
            {
                s = _springs[i];
                Vector3 deltaP = _vertices[s.p1].pos - _vertices[s.p2].pos;
                float dist = deltaP.magnitude;

                if (dist > s.restLength)
                {
                    dist -= s.restLength;
                    dist /= 2f;
                    deltaP.Normalize();
                    deltaP *= dist;

                    if (isAnchorVertex(s.p1))
                    {
                        _vertices[s.p2].vel += deltaP;
                    }
                    else if (isAnchorVertex(s.p2))
                    {
                        _vertices[s.p1].vel -= deltaP;
                    }
                    else
                    {
                        _vertices[s.p1].vel -= deltaP;
                        _vertices[s.p2].vel += deltaP;
                    }
                }
            }
        }

        private List<int> GetAdjacentVertexIndices(int i)
        {
            List<int> indices = new List<int>(4);

            // For Vertical Anchors
            if (anchors.Count == 2 && anchors[0] % resolution == anchors[1] % resolution)
            {
                if (i % resolution > 0)
                    indices.Add(i - 1);

                if (i / resolution > 0)
                    indices.Add(i - resolution);

                if (i / resolution < resolution - 1)
                    indices.Add(i + resolution);

                if (i % resolution < resolution - 1)
                    indices.Add(i + 1);

                // Reverse if the anchor is right
                if (anchors.Count > 0 && anchors[0] == resolution - 1)
                    indices.Reverse();
            }
            // For Horizontal & Etc.
            else
            {
                if (i / resolution > 0)
                    indices.Add(i - resolution);

                if (i % resolution > 0)
                    indices.Add(i - 1);

                if (i % resolution < resolution - 1)
                    indices.Add(i + 1);

                if (i / resolution < resolution - 1)
                    indices.Add(i + resolution);

                // Reverse if the anchor is down
                if (anchors.Count > 0 && anchors[0] / resolution == resolution - 1)
                    indices.Reverse();
            }
            
            return indices;
        }

        public void OnCollisionEnter2D(Collision2D collision)
        {
            // TODO: implement collision
            foreach (var p in collision.contacts)
            {
                int index = FindCloseHitPoint(p.point);
                _vertices[index].vel += new Vector3(p.rigidbody.velocity.x, p.rigidbody.velocity.y, 0f);
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
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 curPos = transform.position;
                Vector3 scale = transform.localScale;
                Gizmos.DrawWireCube(curPos + Vector3.Scale(_vertices[i].pos, scale) , Vector3.one * 0.05f);
                if (i % resolution < resolution - 1)
                {
                    Gizmos.DrawLine(curPos + Vector3.Scale(_vertices[i].pos, scale), curPos + Vector3.Scale(_vertices[i + 1].pos, scale));
                    if (i < _vertices.Length - resolution)
                    {
                        Gizmos.DrawLine(curPos + Vector3.Scale(_vertices[i].pos, scale), curPos + Vector3.Scale(_vertices[i + resolution].pos, scale));
                        Gizmos.DrawLine(curPos + Vector3.Scale(_vertices[i + 1].pos, scale), curPos + Vector3.Scale(_vertices[i + resolution].pos, scale));
                        Gizmos.DrawLine(curPos + Vector3.Scale(_vertices[i + 1].pos, scale), curPos + Vector3.Scale(_vertices[i + resolution + 1].pos, scale));
                    }
                }
            }
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }

    }
}