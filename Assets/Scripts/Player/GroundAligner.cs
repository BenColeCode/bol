/* 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundAligner : MonoBehaviour
{
    public Transform m_skateboard;
    public float m_alignSpeed = 7.5f;
    public float m_rayDistance = 2f;
    [Range(-1, 1)]


    private Vector3 m_surfaceNormal = new Vector3();
    private Vector3 m_collisionPoint = new Vector3();
    public bool m_useRaycast = true;
    private bool m_onSurface;
    private Collision m_surfaceCollisionInfo;
    private Rigidbody m_rigidbody;
    // Start is called before the first frame update
    void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void FixedUpdate()
    {
        AlignVisualToSurface();
    }

    private void OnCollisionStay(Collision other)
    {
        m_onSurface = true;
        m_surfaceCollisionInfo = other;
        m_surfaceNormal = other.GetContact(0).normal;
        m_collisionPoint = other.GetContact(0).point;
    }

    private void OnCollisionExit(Collision other)
    {
        m_surfaceCollisionInfo = null;
        m_onSurface = false;
    }

    void AlignVisualToSurface()
    {
        if (m_useRaycast)
        {
            var hit = new RaycastHit();
            var onSurface = Physics.Raycast(transform.position, Vector3.down, out hit, m_rayDistance);
            if (onSurface)
            {
                var localRot = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
                var euler = localRot.eulerAngles;
                euler.y = 0;
                localRot.eulerAngles = euler;
                m_skateboard.localRotation = Quaternion.LerpUnclamped(m_skateboard.localRotation, localRot, m_alignSpeed * Time.fixedDeltaTime);
            }
        }
        else
        {
            if (m_onSurface)
            {
                var localRot = Quaternion.FromToRotation(transform.up, m_surfaceNormal) * transform.rotation;
                var euler = localRot.eulerAngles;
                euler.y = 0;
                localRot.eulerAngles = euler;
                m_skateboard.localRotation = Quaternion.LerpUnclamped(m_skateboard.localRotation, localRot, m_alignSpeed * Time.fixedDeltaTime );
            }
        }
    }
}
*/