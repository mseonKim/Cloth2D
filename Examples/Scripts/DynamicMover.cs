using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMover : MonoBehaviour
{
    public float moveOffset = 5f;
    private Transform _transform;
    private Rigidbody2D _rigidbody;
    private Camera _cam;
    private Vector3 _preMousePos;
    private bool _isSelected;

    void Start()
    {
        _transform = transform;
        _rigidbody = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit2D hitInfo;
            Vector3 worldPos = _cam.ScreenToWorldPoint(Input.mousePosition);
            hitInfo = Physics2D.Raycast(worldPos, worldPos - _cam.transform.position);
            if (hitInfo.rigidbody != null && hitInfo.rigidbody == _rigidbody)
            {
                _isSelected = true;
                _preMousePos = Input.mousePosition;
                _rigidbody.isKinematic = true;
            }
        }

        if (!_isSelected)
            return;

        Vector3 delta = Vector3.zero;
        if (Input.GetMouseButton(0))
        {
            delta = Input.mousePosition - _preMousePos;
            _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 newPos = _cam.ScreenToWorldPoint(Input.mousePosition);
            newPos.z = -1f;
            _transform.position = newPos;
            _preMousePos = Input.mousePosition;
        }
        else
        {
            delta = Input.mousePosition - _preMousePos;
            _isSelected = false;
            _rigidbody.isKinematic = false;
            _rigidbody.AddForce(delta.normalized * moveOffset);
        }
    }
}
