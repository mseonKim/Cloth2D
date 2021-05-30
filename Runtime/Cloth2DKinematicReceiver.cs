using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cloth2DKinematicReceiver : MonoBehaviour
{
    public Vector3 DeltaPosition { get; private set; }
    private Transform _transform;
    private Vector3 _lastPosition;

    void Start()
    {
        _transform = transform;
    }

    void FixedUpdate()
    {
        DeltaPosition = _transform.position - _lastPosition;
        _lastPosition = _transform.position;
    }
}
