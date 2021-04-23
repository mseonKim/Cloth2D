using System;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2D : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        private float _wind = 0.3f; // Vanila wind
        [Tooltip("Apply attenuation by distance.")]
        public bool attenuation;
        [Tooltip("How far the wind could reach.")]
        [Range(0f, 10000f)] public float maxDistance = 1000f;
        [SerializeField]
        [Range(0f, 1f)] private float _turbulence = 0.1f;

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
            Wind2DReceiver.GetInstance().RegisterWind(this);
        }

        public float GetWind(Vector3 pos)
        {
            float dist = (transform.position - pos).magnitude;
            
            if (dist > maxDistance)
                return 0f;

            if (!attenuation)
                return _wind;
            
            return Mathf.Max(0f, _wind * (1 - dist / maxDistance));
        }

        public float GetTurbulence(Vector3 pos)
        {
            float dist = (transform.position - pos).magnitude;
            
            if (dist > maxDistance)
                return 0f;

            if (!attenuation)
                return _turbulence;
            
            return Mathf.Max(0f, _turbulence * (1 - dist / maxDistance));
        }

        void OnDisable()
        {
            Wind2DReceiver.GetInstance().UnRegisterWind(this.gameObject.GetInstanceID());
        }

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
    }
}

