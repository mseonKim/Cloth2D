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

    public class Cloth2DUtils
    {
        public static void RotateVector(ref Vector3 pos, float rad)
        {
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            float x = cos * pos.x + -sin * pos.y;
            float y = sin * pos.x + cos * pos.y;
            pos.x = x;
            pos.y = y;
        }

        public static void ClampVelocity(ref Vector3 targetV, float max)
        {
            if (targetV.sqrMagnitude > max * max)
            {
                targetV = targetV.normalized * max;
            }
        }

#if UNITY_EDITOR
        public static Vector3 TransformVector(in Vector3 v, in Vector3 scale, float rad)
        {
            Vector3 vector = new Vector3(v.x * scale.x, v.y * scale.y, v.z);
            RotateVector(ref vector, rad);
            return vector;
        }
#endif
    }
}