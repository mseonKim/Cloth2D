using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cloth2D
{
    [ExecuteInEditMode]
    public class ClothSprite : MonoBehaviour
    {
        public struct Vertex
        {
            public Vector3 pos;
            public Vector3 vel;
        }


        public Sprite sprite;
        public bool reverseTexture;
        [Range(2, 16)] public int resolution = 2;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float weight = 1f;
        [Range(0f, 1f)] public float stiffness = 0f;
        [Range(1f, 5f)] public float flexibleScale = 1.25f;
        public List<int> anchors = new List<int>();


        private Wind2D wind2d;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Material _material;

        private Vertex[] _vertices;
        private float _width;
        private float _height;
        private List<Vector3> _anchorPositions;
        private float seed;
        private float _segmentWidth;
        private float _segmentHeight;
        private float _maxSegmentWidthLength;
        private float _maxSegmentHeightLength;

        private int _preResolution;


        void Awake()
        {
            Initialize();
            GenerateMesh();
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
            UpdateCloth(Time.deltaTime);
        }


        private void Initialize()
        {
            if (sprite == null)
                return;

            _width = sprite.texture.width / sprite.pixelsPerUnit * transform.lossyScale.x;
            _height = sprite.texture.height / sprite.pixelsPerUnit * transform.lossyScale.y;
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

            _preResolution = resolution;
            
            // Set values
            _meshFilter = GetComponent<MeshFilter>();
            _material = GetComponent<MeshRenderer>().sharedMaterial;
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
            }
            _mesh.SetVertices(GetVertexPositions());

            // Set Triangles
            List<int> tris = new List<int>();
            tris.Capacity = offset * offset * 6;
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
            List<Vector2> uvs = new List<Vector2>();
            uvs.Capacity = length;
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

        private void UpdateCloth(float dt)
        {
            if (_vertices == null || wind2d == null)
                return;

            float lengthOffset = stiffness + flexibleScale * (1f - stiffness);
            _segmentWidth = _width / resolution;
            _segmentHeight = _height / resolution;
            _maxSegmentWidthLength = _segmentWidth * lengthOffset;
            _maxSegmentHeightLength = _segmentHeight * lengthOffset;
        
            for (int i = 0; i < _vertices.Length; i++)
            {
                if (!isAnchorVertex(i))
                {
                    ApplyGravity(i, dt);
                    ApplyWind(i);
                    // ApplyCollision(i, dt);

                    ApplyVelocity(i, dt);

                    // Snap to maxdist, add elastic response velocity
                    AdjustSegementLength(i, dt);
                }
            }

            // Update mesh
            _mesh.SetVertices(GetVertexPositions());
        }

        private List<Vector3> GetVertexPositions()
        {
            List<Vector3> positions = new List<Vector3>();
            positions.Capacity = resolution * resolution;
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

        private void ApplyGravity(int i, float dt)
        {
            _vertices[i].vel.y -= 9.81f * gravity * dt;
        }

        private void ApplyWind(int i)
        {
            float wind =  wind2d.GetAttenuatedWind(transform.position);
            _vertices[i].vel.y += (Mathf.PerlinNoise(seed, Time.time + i * _segmentHeight * 0.3f) * 2f - 1f) * wind2d.turbulence;
            _vertices[i].vel.y += Mathf.Pow(wind / weight, 2f) * wind2d.windDriection.y;
            _vertices[i].vel.x += (Mathf.PerlinNoise(Time.time + i * _segmentWidth * 0.3f, seed) * 2f - 1f) * wind2d.turbulence;
            _vertices[i].vel.x += Mathf.Pow(wind / weight, 2f) * wind2d.windDriection.x;
        }

        private void ApplyVelocity(int i, float dt)
        {
            _vertices[i].pos += _vertices[i].vel * dt;
        }

        private void AdjustSegementLength(int i, float dt)
        {
            Vector3 diff;
            Vector3 preVertexPos = _vertices[0].pos;
            float elasticResponse = 3f * Mathf.Pow(1f - stiffness, 2f) + 1f;

            if (i < resolution)
            {
                if (i > 0)
                    preVertexPos = _vertices[i - 1].pos;
                diff = _vertices[i].pos - preVertexPos;

                if (diff.magnitude > _maxSegmentWidthLength)
                {
                    Vector3 newPos = preVertexPos + diff.normalized * _maxSegmentWidthLength;
                    _vertices[i].vel += elasticResponse * (newPos - _vertices[i].pos);
                    _vertices[i].pos = newPos;
                }
            }
            else
            {
                preVertexPos = _vertices[i - resolution].pos;
                diff = _vertices[i].pos - preVertexPos;

                if (diff.magnitude > _maxSegmentHeightLength)
                {
                    Vector3 newPos = preVertexPos + diff.normalized * _maxSegmentHeightLength;
                    _vertices[i].vel += elasticResponse * (newPos - _vertices[i].pos);
                    _vertices[i].pos = newPos;
                }
            }

        }


        private void OnDrawGizmosSelected()
        {
            if (_vertices == null || _vertices.Length < 1)
                return;

            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            for (int i = 0; i < _vertices.Length; i++)
            {
                Vector3 curPos = transform.position;
                Gizmos.DrawWireCube(curPos + _vertices[i].pos, Vector3.one * 0.05f);
                if (i % resolution < resolution - 1)
                {
                    Gizmos.DrawLine(curPos + _vertices[i].pos, curPos + _vertices[i + 1].pos);
                    if (i < _vertices.Length - resolution)
                    {
                        Gizmos.DrawLine(curPos + _vertices[i].pos, curPos + _vertices[i + resolution].pos);
                        Gizmos.DrawLine(curPos + _vertices[i + 1].pos, curPos + _vertices[i + resolution].pos);
                        Gizmos.DrawLine(curPos + _vertices[i + 1].pos, curPos + _vertices[i + resolution + 1].pos);
                    }
                }
            }
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }

    }
}