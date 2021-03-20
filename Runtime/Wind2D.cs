using System;
using UnityEngine;

namespace Cloth2D
{
    public class Wind2D : MonoBehaviour
    {
        [Range(0f, 1f)] public float wind = 0.3f; // Vanila wind
        [Tooltip("Apply attenuation by distance.")]
        public bool attenuation;
        [Tooltip("How far the wind could reach.")]
        [Range(0f, 1000f)] public float maxDistance = 100f;
        [Range(0f, 1f)] public float turbulence = 0.01f;

        public Vector3 windDriection { get {
            float rad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
        }}


        public float GetAttenuatedWind(Vector3 pos)
        {
            float dist = (transform.position - pos).magnitude;

            if (dist > maxDistance)
                return 0f;
                
            if (!attenuation)
                return wind;
            
            return Mathf.Max(0f, wind * (1 - dist / maxDistance));
        }

        private void OnDrawGizmosSelected()
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

