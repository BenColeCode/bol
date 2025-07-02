using System.Collections;
using System.Collections.Generic;
using System;
using PathCreation;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    //Objects
    public Transform transform;
    public Collider wall_hit;
    public Collider front_detector;
    public Collider back_detector;
    public Transform m_mesh;
    public Transform projectedRotation;
    public Transform positioner;
    
    public Transform TargetPlayer;
    private Quaternion settingLookAngle;


    //Text
    public GameObject DamageText;
    public GameObject Canvas;
    public GameObject RenderOverhead;

    //Stats
    public int StartingMaxHP;
    public int MaxHP;
    public int CurrentHP;

    //All four of these are used to define the angle of the visible rotateable mesh
    public Transform front_Ray;
    public Transform back_Ray;
    public Transform left_Ray;
    public Transform right_Ray;


    // at this distance to the surface, jumps reset
    public Animator m_animator;
    //rigidbody of player
    public Rigidbody m_rigidbody;


    //movement
    public float acceleration = 20;
    public float topSpeed = 60;
    public float m_Forward;
    public float current_Direction = 1f;
    private bool canPedal;
   
    //attacking
    public bool lock_attack = false;

    //in x seconds, bool = !bool
    public IEnumerator CooldownTimer(float x, bool flip, Action<bool> flipThisBool)
    {
        yield return new WaitForSeconds(x);
        flipThisBool(flip);
    }

      void Start()
    {
        m_rigidbody = GetComponent<Rigidbody>();
        m_animator = GetComponentInChildren<Animator>();



        GameObject[] gos;
        gos = GameObject.FindGameObjectsWithTag("Player");
        GameObject closest = null;
        float distance = Mathf.Infinity;
        Vector3 position = transform.position;
        foreach (GameObject go in gos)
        {
            Vector3 diff = go.transform.position - position;
            float curDistance = diff.sqrMagnitude;
            if (curDistance < distance)
            {
                closest = go;
                distance = curDistance;
            }
        }
        TargetPlayer = closest.GetComponent<Transform>();

        onStart();
    }

    private void FixedUpdate()
    {
        ProcessForce();
        rotateMesh();
        onFixedUpdate();

    }

    public virtual void onStart() { }
    public virtual void onFixedUpdate() { }
    public virtual void normalMovement() { } 
    public virtual void turningLogic() { } 
    public virtual void normalAttack() { } 

    private void ProcessForce()
    {
        turningLogic();
        //move when you press forwards and backwards
        normalMovement();
        //gravity 
        m_rigidbody.AddForce(new Vector3(0, -45f, 0), ForceMode.Acceleration);
    }
      
       void rotateMesh()
    {
        var hit = new RaycastHit();
        var front = new RaycastHit();
        var back = new RaycastHit();
        var left = new RaycastHit();
        var right = new RaycastHit();


        int layerMask = 1 << 3;
        layerMask |= (1 << 0);
        var frontRaycast = Physics.Raycast(front_Ray.position, Vector3.down, out front, 300f, layerMask);
        var backRaycast = Physics.Raycast(back_Ray.position, Vector3.down, out back, 300f, layerMask);

        var leftRaycast = Physics.Raycast(left_Ray.position, Vector3.down, out left, 300f, layerMask);
        var rightRaycast = Physics.Raycast(right_Ray.position, Vector3.down, out right, 300f, layerMask);

        var VerticalLeanAngle = front.point - back.point;
        var HorizontalLeanAngle = left.point - right.point;

        var rot = projectedRotation.rotation;
     
        var viewAngle = Mathf.Abs(Vector3.Angle(VerticalLeanAngle.normalized, transform.forward));
        var sideAngle = Mathf.Abs(Vector3.Angle(HorizontalLeanAngle.normalized, -transform.right));

        if (front.point.y > back.point.y)
        {
            viewAngle = -viewAngle;
        }
        if (left.point.y > right.point.y)
        {
            sideAngle = -sideAngle;
        }

        rot.eulerAngles = new Vector3(viewAngle, transform.rotation.eulerAngles.y, sideAngle);
    
        settingLookAngle = rot;


        projectedRotation.rotation = Quaternion.RotateTowards(projectedRotation.rotation, settingLookAngle, 5f);
        m_mesh.rotation = projectedRotation.rotation;

    }
   
    void OnTriggerEnter(Collider triggerData)
    {
      
    }

    public void zeroOut()
    {
        m_animator.SetTrigger("ZeroOut");
    }
}
