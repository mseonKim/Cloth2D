using System;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2D : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)] private float _wind = 0.3f; // Vanila wind strength
        [Tooltip("Apply wind regardless of distance.")]
        public bool infiniteDistance;
        [Tooltip("Apply linear attenuation by distance if InfiniteDistance is disabled.")]
        public bool attenuation;
        [Tooltip("How far the wind could reach.")]
        [Range(0f, 1000f)] public float maxDistance = 100f;
        [SerializeField]
        [Range(0f, 1f)] private float _turbulence = 0.5f;

        public float windStrength { get { return _wind; } }

        public Vector3 windDriection
        {
            get
            {
                float rad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
            }
        }

        void OnEnable()
        {
            Wind2DReceiver.Instance.RegisterWind(this);
        }

        public float GetWind(Vector3 pos)
        {
            if (infiniteDistance)
                return _wind;

            pos.z = transform.position.z;
            float dist = (transform.position - pos).magnitude;
            if (dist > maxDistance)
                return 0f;

            if (!attenuation)
                return _wind;
            
            return Mathf.Max(0f, _wind * (1 - dist / maxDistance));
        }

        public void SetWindStrength(float value)
        {
            _wind = value;
        }

        public float GetTurbulence(Vector3 pos)
        {
            if (infiniteDistance)
                return _wind;

            pos.z = transform.position.z;
            float dist = (transform.position - pos).magnitude;
            if (dist > maxDistance)
                return 0f;

            if (!attenuation)
                return _turbulence;
            
            return Mathf.Max(0f, _turbulence * (1 - dist / maxDistance));
        }

        void OnDisable()
        {
            Wind2DReceiver.Instance.UnregisterWind(this.gameObject.GetInstanceID());
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            int number = 5;
            Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.25f);

            for (int i = 0; i < number; i++)
            {
                Vector3 start = transform.localPosition;
                start.y += (i - number / 2f) / (number * 2f);
                Gizmos.DrawLine(start, start + windDriection * 2.5f);
            }
            
            Gizmos.color = new Color(1f, 1f, 1f, 1f);
        }

        private void OnDrawGizmosSelected()
        {
            if (!infiniteDistance)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawWireSphere(transform.position, maxDistance);
            }
        }
#endif
    }
}

