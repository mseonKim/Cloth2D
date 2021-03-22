using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Cloth2D
{
    public struct Vertex
    {
        public Vector3 pos;
        public Vector3 vel;
    }

    public struct ClothNode
    {
        public int index;
        public int preIndex;

        public ClothNode(int i, int pre)
        {
            index = i;
            preIndex = pre;
        }
    }

    [ExecuteAlways]
    public class ClothSprite : MonoBehaviour
    {
        public Sprite sprite;
        public bool reverseTexture;
        [Range(4, 16)] public int resolution = 12;
        [Range(-10f, 10f)] public float gravity = 1f;
        [Range(0.1f, 10f)] public float weight = 1f;
        [Range(0f, 1f)] public float stiffness = 0.5f;
        [Range(1f, 2f)] public float flexibleScale = 1.2f;
        [Range(0f, 1f)] public float wetness = 0f;
        [Range(0f, 10f)] public float drySpeed = 1f;
        public List<int> anchors = new List<int>();


        private Wind2D wind2d;
        private MeshFilter _meshFilter;
        private Mesh _mesh;
        private Material _material;

        private Vertex[] _vertices;
        private List<ClothNode> _orderedNodes;
        private float _width;
        private float _height;
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
            SetOrderedClothNodes();
        }

        private void UpdateCloth(float dt)
        {
            if (_vertices == null || wind2d == null || _orderedNodes == null)
                return;

            // float scaleOffset = stiffness + flexibleScale * (1f - stiffness);
            _segmentWidth = _width / resolution;
            _segmentHeight = _height / resolution;
            _maxSegmentWidthLength = _segmentWidth * flexibleScale;
            _maxSegmentHeightLength = _segmentHeight * flexibleScale;
        
            foreach (var node in _orderedNodes)
            {
                if (!isAnchorVertex(node.index))
                {
                    ApplyGravity(node.index, dt);
                    ApplyWinds(node.index, dt);
                    // ApplyCollision(i, dt);
                    ApplyForces(node.index, dt);

                    AdjustSegmentLength(node, dt);
                }
            }

            foreach (var node in _orderedNodes)
            {
                if (!isAnchorVertex(node.index))
                {
                    // AdjustSegmentLength(node, dt);
                }
            }

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

        private void ApplyGravity(int i, float dt)
        {
            _vertices[i].vel.y -= 981f * gravity * dt / sprite.pixelsPerUnit;
        }

        private void ApplyWinds(int i, float dt)
        {
            float wind =  wind2d.GetWind(transform.position);
            float wet = wetness * 0.25f;
            
            _vertices[i].vel.x += Mathf.Pow(wind / (weight + wet), 1.5f) * wind2d.windDriection.x * _segmentWidth;
            _vertices[i].vel.y += Mathf.Pow(wind / (weight + wet), 1.5f) * wind2d.windDriection.y * _segmentHeight;
            _vertices[i].vel.x += (Mathf.PerlinNoise(Time.time + i * _segmentWidth * 0.3f, seed) - 0.5f) / (1f + wet) * wind2d.turbulence * _segmentWidth * 0.5f;
            _vertices[i].vel.y += (Mathf.PerlinNoise(seed, Time.time + i * _segmentHeight * 0.3f) - 0.5f) / (1f + wet) * wind2d.turbulence * _segmentHeight * 0.5f;
            
            if (wetness > 0f)
            {
                wetness -= wind * dt * drySpeed / 2500f;
                if (wetness < 0f)
                    wetness = 0f;
            }
        }

        private void ApplyForces(int i, float dt)
        {
            _vertices[i].pos += _vertices[i].vel * dt;
        }

        private void AdjustSegmentLength(ClothNode node, float dt)
        {
            int cur = node.index;
            float elasticResponse = 1f + 3f * Mathf.Pow(1f - stiffness, 2f) / (1f + wetness * 0.25f);
            Vector3 preVertexPos = node.preIndex != -1 ? _vertices[node.preIndex].pos : _vertices[cur].pos;
            Vector3 diff = _vertices[cur].pos - preVertexPos;

            // Horizontal
            if (anchors.Count > 1 && anchors[0] / resolution == anchors[1] / resolution
                && cur >= anchors[0] && cur < anchors[1])
            {
                // Set bezier curve params
                float u = (float)(cur - anchors[0]) / Mathf.Abs(anchors[1] - anchors[0]);
                float anchorY = _vertices[anchors[0]].pos.y;
                float bezierY = _segmentHeight * 2f * (1f - stiffness) + anchorY;
                Vector3 p1 = new Vector3(_width / 3f, bezierY, 0f);
                Vector3 p2 = new Vector3(_width * 2f / 3f, bezierY, 0f);
                if (wind2d != null)
                {
                    p1.x += wind2d.windDriection.x * wind2d.GetWind(transform.position) * _width / 2f;
                    p2.x += wind2d.windDriection.x * wind2d.GetWind(transform.position) * _width / 2f;
                }

                // Adjust pos by bezier point
                Vector3 bezierPoint = GetBezierCurvePoint(u, _vertices[anchors[0]].pos, p1, p2, _vertices[anchors[1]].pos);
                float limitLowerY = -bezierPoint.y + 2 * anchorY;
                _vertices[cur].pos.x = bezierPoint.x;

                if (_vertices[cur].pos.y > bezierPoint.y)
                {
                    _vertices[cur].vel.y -= (_vertices[cur].pos.y - bezierPoint.y) * 0.6f;
                    _vertices[cur].pos.y = bezierPoint.y;
                }
                if (_vertices[cur].pos.y < limitLowerY)
                {
                    _vertices[cur].vel.y -= (_vertices[cur].pos.y - limitLowerY) * 2f;
                    _vertices[cur].pos.y = limitLowerY;
                }

                return;
            }


            // Vertical
            if (anchors.Count > 1 && anchors[0] % resolution == anchors[1] % resolution
                && cur % resolution == anchors[0] % resolution && cur < anchors[1])
            {
                // Set bezier curve params
                float u = (float)(cur - anchors[0]) / Mathf.Abs(anchors[1] - anchors[0]);
                float anchorX = _vertices[anchors[0]].pos.x;
                float bezierX = _segmentWidth * 2f * (1f - stiffness) + anchorX;
                Vector3 p1 = new Vector3(bezierX, -_height / 3f, 0f);
                Vector3 p2 = new Vector3(bezierX, -_height * 2f / 3f, 0f);
                if (wind2d != null)
                {
                    p1.y += wind2d.windDriection.y * wind2d.GetWind(transform.position) * _height / 2f;
                    p2.y += wind2d.windDriection.y * wind2d.GetWind(transform.position) * _height / 2f;
                }

                // Adjust pos by bezier point
                Vector3 bezierPoint = GetBezierCurvePoint(u, _vertices[anchors[0]].pos, p1, p2, _vertices[anchors[1]].pos);
                float limitLowerX = -bezierPoint.x + 2 * anchorX;
                _vertices[cur].pos.y = bezierPoint.y;
                
                if (_vertices[cur].pos.x > bezierPoint.x)
                {
                    _vertices[cur].vel.x -= (_vertices[cur].pos.x - bezierPoint.x) * 2f;
                    _vertices[cur].pos.x = bezierPoint.x;
                }
                if (_vertices[cur].pos.x < limitLowerX)
                {
                    _vertices[cur].vel.x -= (_vertices[cur].pos.x - limitLowerX) * 2f;
                    _vertices[cur].pos.x = limitLowerX;
                }

                return;
            }

            
            if (Mathf.Abs(cur - node.preIndex) == 1)
            {
                if (diff.magnitude > _maxSegmentWidthLength)
                {
                    Vector3 newPos = preVertexPos + diff.normalized * _maxSegmentWidthLength;
                    _vertices[cur].vel += elasticResponse * (newPos - _vertices[cur].pos);
                    _vertices[cur].pos = newPos;
                }
            }
            else
            {
                if (diff.magnitude > _maxSegmentHeightLength)
                {
                    Vector3 newPos = preVertexPos + diff.normalized * _maxSegmentHeightLength;
                    _vertices[cur].vel += elasticResponse * (newPos - _vertices[cur].pos);
                    _vertices[cur].pos = newPos;
                }
            }
        }


        private void SetOrderedClothNodes()
        {
            _orderedNodes = new List<ClothNode>(_vertices.Length);
            bool[] visited = new bool[_vertices.Length];
            Queue<int> queue = new Queue<int>();
            
            // Set default start index to 0 if no anchor
            if (anchors.Count == 0)
            {
                queue.Enqueue(0);
                visited[0] = true;
                _orderedNodes.Add(new ClothNode(0, -1));
            }

            foreach (int i in anchors)
            {
                queue.Enqueue(i);
                visited[i] = true;
                _orderedNodes.Add(new ClothNode(i, -1));
            }

            // BFS
            while (queue.Count > 0)
            {
                int i = queue.Dequeue();
                // Add to nodes if not visited
                foreach (int next in GetAdjacentVertexIndices(i))
                {
                    if (!visited[next])
                    {
                        queue.Enqueue(next);
                        visited[next] = true;
                        _orderedNodes.Add(new ClothNode(next, i));
                    }
                }
            }
        }

        private int GetVisitedNodeCount(ref bool[] visited)
        {
            int count = 0;
            for (int i = 0; i < visited.Length; i++)
            {
                if (visited[i])
                    count++;
            }
            return count;
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

        private Vector3 GetBezierCurvePoint(float u, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            Vector3 p = Mathf.Pow(1f - u, 3f) * p0;
            p += 3 * u * (1f - u) * (1f - u) * p1;
            p += 3 * u * u * (1f - u) * p2;
            p += u * u * u * p3;
            return p;
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