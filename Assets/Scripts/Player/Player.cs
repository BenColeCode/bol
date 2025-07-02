using System.Collections;
using System.Collections.Generic;
using System;
using PathCreation;
using UnityEngine;
using TMPro;

public class Player : MonoBehaviour
{
    //Objects
    public Transform m_skateboard;
    public Collider wall_hit;
    public Collider front_detector;
    public Collider back_detector;
    public Collider rail_detector;
    private PathCreator rail;
    private EndOfPathInstruction rule;
    public Transform m_mesh;
    public Transform projectedRotation;

    public Transform positioner;

    //Text
    public TextMeshProUGUI TextPrefab;
    public TextMeshProUGUI SubtextPrefab;
    public TextMeshProUGUI ItemText;
    public TextMeshProUGUI ItemDesc;
    public GameObject Canvas;

    //Stats
    public int StartingMaxHP;
    public int MaxHP;
    public int CurrentHP;

    //All four of these are used to define the angle of the visible rotateable mesh
    public Transform back_Ray;
    public Transform front_Ray;
    public Transform left_Ray;
    public Transform right_Ray;


    // at this distance to the surface, jumps reset
    public float m_surfDistance = 1f;
    public bool m_onSurface;
    public Animator m_animator;
    //rigidbody of player
    public Rigidbody m_rigidbody;


    //movement
    public float acceleration = 20;
    public float topSpeed = 70;
    public float m_Forward;
    public float current_Direction = 1f;
    private Quaternion settingLookAngle;
    private bool canPedal;


    //grinding
    public bool starting_grind = false;
    public bool starting_to_turn = false;
    public bool grinding = false;
    private float distanceTravelled;
    private float grind_Speed;
    private float grindDir;
    private Vector3 starting_point;

    //turning
    public float m_TurnForce = 3;
    private float m_Turn;
    private Vector3 face_Turn;
    public bool halfpipe = false;
    public bool halfpipe_in_progress = false;
    private bool returningToHalfpipe = false;

    //wall running
    public bool wall_Running;
    private Vector3 wall_Run_Vector;
    private Vector3 face_New_Direction;
    private int turningFrames;
    private Vector3 side1;
    public Collider wallCollider;
    public Quaternion Wallrunning_Look_Target;
    public Transform Camera_Focus;

    //jumping
    public float m_JumpForce = 20;
    public int m_jumps = 2;
    public int m_current_jumps;
    public bool m_Jump;
    public bool space_Hold;
    public bool lock_jump = false;
    public bool canJump = true;



    //attacking
    public float damage;
    public float attack_speed = 1;
    public bool m_PrimaryAttack = false; 
    public bool attacking = false;

    public bool m_SecondaryAttack = false;
    public bool m_MovementSkill = false;
    public bool lock_attack = false;
    public float spin_radians;


    //in x seconds, bool = !bool
    public IEnumerator CooldownTimer(float x, bool flip, Action<bool> flipThisBool)
    {
        yield return new WaitForSeconds(x);
        flipThisBool(flip);
    }

    //over 'x' seconds, turn camera focus from 'from' to 'to'
    public IEnumerator CameraRotator(int Camera_Y_Angle_Target)
    {
        while (MathF.Abs(Camera_Focus.localRotation.y) - MathF.Abs(Camera_Y_Angle_Target) > MathF.Abs(4))
        {
                float y = Camera_Focus.localRotation.eulerAngles.y;
                float delta = (Camera_Y_Angle_Target - y) * Time.deltaTime * .5f;
                Camera_Focus.localRotation = Quaternion.Euler(Camera_Focus.localRotation.eulerAngles.x, y + (((Camera_Y_Angle_Target - y) / Mathf.Abs(Camera_Y_Angle_Target - y)) * Mathf.Abs(delta)), Camera_Focus.localRotation.eulerAngles.z);
                yield return null;
        }
        Camera_Focus.localRotation = Quaternion.Euler(Camera_Focus.localRotation.eulerAngles.x, Camera_Y_Angle_Target, Camera_Focus.localRotation.eulerAngles.z);
        yield return 0;
    }

    public Dictionary<string, int> Items = new Dictionary<string, int> { };
    // Start is called before the first frame update
    void Start()
    {
        Canvas = GameObject.Find("Canvas");
        m_rigidbody = GetComponent<Rigidbody>();
        wall_hit = GetComponent<Collider>();
        m_animator = GetComponentInChildren<Animator>();
        onStart();

        
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInputs();
    }

    private void FixedUpdate()
    {
        ProcessForce();

        if (!wall_Running)
        {
            rotateMesh();
        }
        onFixedUpdate();

    }


    void GetItem(Item info)
    {
        var itemCount = Items.GetValueOrDefault(info.itemName, 0);
        Items.Remove(info.itemName);

        Items.Add(info.itemName, itemCount + 1);
        ItemText = Instantiate(TextPrefab, Canvas.transform);
        ItemDesc = Instantiate(SubtextPrefab, Canvas.transform);
        ItemText.text = info.displayName;
        ItemDesc.text = info.description;

        //StatChanges
        MaxHP = StartingMaxHP + Items.GetValueOrDefault("MaxHPUp", 0);
    }

    public virtual void onStart() { }
    public virtual void onFixedUpdate() { }
    public virtual void ConsecutiveHit() { }

    private void ProcessInputs()
    {
        if (Input.GetButton("Pause"))
        {
            Debug.Break();
        }
        if (Input.GetButton("Fire1"))
        {
            m_PrimaryAttack = true;
        }
        if (Input.GetButtonDown("Fire1"))
        {
            attacking = true;
        }
        if (Input.GetButtonUp("Fire1"))
        {
            attacking = false;
        }

        if (Input.GetButton("Fire2"))
            {
                m_SecondaryAttack = true;
            }

        if (Input.GetButtonDown("Fire3"))
        {
            m_MovementSkill = true;
        }

        if (Input.GetButtonDown("Jump"))
        {
            space_Hold = true;
            if (m_current_jumps > 0)
            {
                m_Jump = true;
            }
        }

        if (Input.GetButtonUp("Jump"))
        {
            space_Hold = false;
        }

        m_Forward = Input.GetAxis("Vertical");
        m_Turn = Input.GetAxis("Horizontal");
    }
    private void ProcessForce()
    {

        if (halfpipe_in_progress)
        {
            m_rigidbody.AddForce(new Vector3(0, -10f, 0), ForceMode.Acceleration);
        }
        if (halfpipe)
        {
            halfpipe = false;
            if (m_rigidbody.velocity.y > 5)
            {
                doHalfPipe();
            }
        }
        else if (wall_Running)
        {
            wallRunningMovement();
        }
        else if (grinding)
        {
            if (rail != null)
            {

                if (starting_grind)
                {
                    snapToRail();
                }
                else
                {
                    grindingMovement();
                }
            }
        }
        else
        {
            //automatic turning
            if (turningFrames > 0)
            {
                autoTurning();
            }
            else
            {
                //turn when you hold left and right
                turningLogic();
                //move when you press forwards and backwards
                normalMovement();
                grind_Speed = m_rigidbody.velocity.magnitude;
                //gravity 
                m_rigidbody.AddForce(new Vector3(0, -45f, 0), ForceMode.Acceleration);

                jumpLogic();

            }

        }



    }
    private void turningLogic()
    {
        if (m_Turn > 0)
        {
            m_animator.SetTrigger("TrLeanRight");
        }
        else if (m_Turn < 0)
        {
            m_animator.SetTrigger("TrLeanLeft");
        }
        else if (m_Turn == 0)
        {
            m_animator.SetTrigger("TrIdle");
        }

        if (m_onSurface)
        {
            transform.Rotate(new Vector3(0, m_TurnForce * m_Turn, 0), Space.Self);
        }
        else
        {
            transform.Rotate(new Vector3(0, m_TurnForce * .25f * m_Turn, 0), Space.Self);
        }
    }
    private void normalMovement()
    {
        var givenAcceleration = acceleration + (20 * Items.GetValueOrDefault("Acceleration", 0));
        var effectiveTopSpeed = topSpeed + (10 * Items.GetValueOrDefault("MaxSpeed", 0));
        var yVel = m_rigidbody.velocity.y;

        if (m_onSurface)
        {
            var newVel = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + (m_rigidbody.velocity.z * m_rigidbody.velocity.z));
            newVel.y = yVel;

            if (newVel.magnitude < givenAcceleration)
            {
                if (m_Forward == 1)
                {
                    current_Direction = 1;
                }
                if (m_Forward == -1)
                {
                    current_Direction = -1;
                }
            }

            //point momentum forwards
            m_rigidbody.velocity = current_Direction * newVel;

            //add momentum
            var accelerationInDirection = givenAcceleration * m_Forward;
            var idealDrag = accelerationInDirection / effectiveTopSpeed;

            if (new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z).magnitude < 20f)
            {
                if (m_onSurface && turningFrames == 0)
                {
                    if (m_Forward == 1)
                    {
                        m_animator.SetTrigger("TrPushoff");
                    }
                    if (m_Forward == -1)
                    {
                        m_animator.SetTrigger("TrRevPushoff");
                    }
                }
                m_rigidbody.AddForce(m_skateboard.forward * 3 * accelerationInDirection);
            }
            else
            {
                m_rigidbody.AddForce(m_skateboard.forward * accelerationInDirection);
            }
            if (new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z).magnitude > effectiveTopSpeed && m_Forward == 1)
            {
                newVel = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + (m_rigidbody.velocity.z * m_rigidbody.velocity.z));

                m_rigidbody.velocity -= idealDrag * m_rigidbody.velocity * Time.deltaTime;
                newVel.y = yVel;

            }
            else
            {
                newVel = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + (m_rigidbody.velocity.z * m_rigidbody.velocity.z));
                m_rigidbody.velocity += idealDrag * m_rigidbody.velocity * Time.deltaTime;
                newVel.y = yVel;
            }
        }

    }
    
    private void jumpLogic()
    {
        if (m_Jump)
        {
            if (canJump)
            {
                m_Jump = false;
                m_current_jumps = m_current_jumps - 1;

                if (m_onSurface)
                {
                    if (m_rigidbody.velocity.y < 0f)
                    {
                        m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z);
                    }
                }
                m_rigidbody.AddForce(0, m_JumpForce, 0);
                zeroOut();
                if (!m_onSurface)
                {
                    m_animator.SetTrigger("TrDoubleJump");
                }
                else
                {
                    m_animator.SetTrigger("TrJump");
                }
            }
            canJump = false;
            StartCoroutine(CooldownTimer(.1f, canJump, result => canJump = !result));
        }
    }


    private void StartGrind()
    {
        if (rail == null)
        {
            return;
        }

        if (rail.path.isClosedLoop)
        {
            rule = (EndOfPathInstruction)0;
        }
        else
        {
            rule = (EndOfPathInstruction)2;
        }

        starting_grind = true;
        starting_to_turn = true;
        grinding = true;
        EndCharge();
        m_animator.SetTrigger("TrGotoGrind");

        m_rigidbody.velocity = Vector3.zero;
        distanceTravelled = rail.path.GetClosestDistanceAlongPath(m_rigidbody.position);
        starting_point = rail.path.GetPointAtDistance(distanceTravelled, rule);

        var next_point = rail.path.GetPointAtDistance((distanceTravelled + 2), rule);
        var last_point = rail.path.GetPointAtDistance((distanceTravelled - 2), rule);

        var nextAngle = Mathf.Abs(Vector3.SignedAngle((next_point - starting_point).normalized, m_skateboard.forward, Vector3.up));
        var lastAngle = Mathf.Abs(Vector3.SignedAngle((last_point - starting_point).normalized, m_skateboard.forward, Vector3.up));


        grindDir = 1;
        if (nextAngle > lastAngle)
        {
            grindDir = -1;
        }
    }
    private void snapToRail()
    {
        var snapPoint = rail.path.GetPointAtDistance(distanceTravelled, rule);
        snapPoint = new Vector3(snapPoint.x, snapPoint.y + .1f, snapPoint.z);
        if (Vector3.Distance(m_rigidbody.position, snapPoint) < 1)
        {
            starting_grind = false;
        }
        else
        {
            m_rigidbody.MovePosition(Vector3.MoveTowards(m_rigidbody.position, snapPoint, grind_Speed / Vector3.Distance(m_rigidbody.position, snapPoint)));
        }

        if (starting_to_turn)
        {
            faceToRail();
        }
        else
        {
            if (grindDir == -1)
            {
                m_rigidbody.rotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
            }
            else
            {
                m_rigidbody.rotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
            }
        }


    }
    private void faceToRail()
    {
        var targetRotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
        if (grindDir == -1)
        {
            targetRotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
        }

        if (m_rigidbody.rotation != targetRotation)
        {
            m_rigidbody.rotation = Quaternion.RotateTowards(m_rigidbody.rotation, targetRotation, 5);
        }
        if (m_rigidbody.rotation == targetRotation)
        {
            starting_to_turn = false;
        }

    }
    private void grindingMovement()
    {
        distanceTravelled += (grind_Speed * Time.deltaTime * grindDir);

        if (starting_to_turn)
        {
            faceToRail();
        }
        else
        {
            if (grindDir == -1)
            {
                m_rigidbody.rotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
            }
            else
            {
                m_rigidbody.rotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
            }
        }

        var prevPoint = rail.path.GetPointAtDistance(distanceTravelled - (2 * (grind_Speed * Time.deltaTime * grindDir)), rule);
        var nextPoint = rail.path.GetPointAtDistance(distanceTravelled, rule);

        if (space_Hold && rail != null && !(!rail.path.isClosedLoop && ((nextPoint == rail.path.GetPointAtTime(0, rule) && grindDir < 0) || (nextPoint == rail.path.GetPointAtTime(1, rule) && grindDir > 0))))
        {
            if (starting_to_turn)
            {
                faceToRail();
            }
            else
            {
                if (grindDir == -1)
                {
                    m_rigidbody.rotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
                }
                else
                {
                    m_rigidbody.rotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
                }
            }
            m_rigidbody.position = nextPoint;
        }
        else
        {
            stopGrinding();
        }
    }
    public void stopGrinding()
    {

        starting_grind = false;
        starting_to_turn = false;
        grinding = false;
        m_animator.SetTrigger("TrStopGrinding");

        //speed up a little on the dismount
        m_rigidbody.velocity = (m_skateboard.forward).normalized * grind_Speed * 1.2f;
        //jump
        m_rigidbody.AddForce(0, m_JumpForce * 1.2f, 0);
        m_animator.SetTrigger("TrDoubleJump");

        rail = null;
    }
    private void wallRunningMovement()
    {
        m_skateboard.rotation = Quaternion.RotateTowards(m_skateboard.rotation, Wallrunning_Look_Target, 5);
        if (!space_Hold || Vector3.Distance(wallCollider.ClosestPoint(m_rigidbody.position), m_rigidbody.position) > 1)
        {
            endWallRun();
            wall_Running = false;
        }
        else
        {
            m_rigidbody.velocity = wall_Run_Vector;
        }
    }
    private void doHalfPipe()
    {
        halfpipe_in_progress = true;
        m_animator.SetTrigger("TrBoardGrab");
        m_rigidbody.velocity = new Vector3(0, 1, 0) * m_rigidbody.velocity.magnitude;
        m_rigidbody.MovePosition(transform.position + -transform.forward * .2f);

    }
    private void autoTurning()
    {
        turningFrames--;
        if (turningFrames < 1)
        {
            halfpipe_in_progress = false;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face_Turn, Vector3.up), 5);

    }
    private void endWallRun()
    {
        StartCoroutine(CameraRotator(0));

        turningFrames = 9;
        m_rigidbody.AddForce(0, m_JumpForce, 0);
        zeroOut();
        m_animator.SetTrigger("TrDoubleJump");
    }
    private void OnCollisionStay(Collision other)
    {
        m_onSurface = true;
    }
    private void OnCollisionExit(Collision other)
    {
        m_onSurface = false;
    }

    void rotateMesh()
    {
        var hit = new RaycastHit();
        var front = new RaycastHit();
        var back = new RaycastHit();
        var left = new RaycastHit();
        var right = new RaycastHit();


        var onSurface = Physics.Raycast(transform.position, Vector3.down, out hit, m_surfDistance);
        int layerMask = 1 << 3;
        layerMask |= (1 << 0);
        var fronatRaycast = Physics.Raycast(front_Ray.position, Vector3.down, out front, 300f, layerMask);
        var backRaycast = Physics.Raycast(back_Ray.position, Vector3.down, out back, 300f, layerMask);

        var leftRaycast = Physics.Raycast(left_Ray.position, Vector3.down, out left, 300f, layerMask);
        var rightRaycast = Physics.Raycast(right_Ray.position, Vector3.down, out right, 300f, layerMask);

        var VerticalLeanAngle = front.point - back.point;
        var HorizontalLeanAngle = left.point - right.point;

        if (!wall_Running && ((Vector3.Distance(front.point, front_Ray.position) > 15f && Vector3.Distance(back.point, back_Ray.position) > 15f) || returningToHalfpipe))
        {
            m_animator.SetTrigger("TrBoardGrab");
        }
        else
        {
            m_animator.SetTrigger("TrStopGrabbing");
        }
        if (onSurface)
        {
            m_current_jumps = m_jumps + Items.GetValueOrDefault("MaxJumps", 0);
            returningToHalfpipe = false;
        }
        var rot = projectedRotation.rotation;
        if (!grinding)
        {
            var viewAngle = Mathf.Abs(Vector3.Angle(VerticalLeanAngle.normalized, m_skateboard.forward));
            var sideAngle = Mathf.Abs(Vector3.Angle(HorizontalLeanAngle.normalized, -m_skateboard.right));

            if (front.point.y > back.point.y)
            {
                viewAngle = -viewAngle;
            }
            if (left.point.y > right.point.y)
            {
                sideAngle = -sideAngle;
            }

            rot.eulerAngles = new Vector3(viewAngle, m_skateboard.rotation.eulerAngles.y, sideAngle);
        }
        else
        {
            rot.eulerAngles = new Vector3(m_skateboard.rotation.eulerAngles.x, m_skateboard.rotation.eulerAngles.y, m_skateboard.rotation.eulerAngles.z);
        }
        settingLookAngle = rot;


        projectedRotation.rotation = Quaternion.RotateTowards(projectedRotation.rotation, settingLookAngle, 5f);
        m_mesh.rotation = projectedRotation.rotation;

    }
    void startHalfpipe(Collider triggerData)
    {

        var closeTriggerPoint = triggerData.ClosestPoint(positioner.position);
        var triggerDirection = positioner.position - closeTriggerPoint;

        triggerDirection = triggerDirection.normalized;
        triggerDirection.y = 0;


        var perp = (Quaternion.AngleAxis(90, Vector3.up) * triggerDirection).normalized;


        face_Turn = Vector3.Reflect(m_skateboard.forward, triggerDirection);
        face_Turn = face_Turn.normalized;
        var pointAngle = Vector3.Angle(m_skateboard.forward, face_Turn);

        turningFrames = Mathf.Abs((int)((pointAngle) / 5));
        halfpipe = true;
        returningToHalfpipe = true;
    }
    void OnTriggerEnter(Collider triggerData)
    {
        //start halfpiping --REDO--
        if (triggerData.GetComponent<Collider>().gameObject.layer == 7)
        {
            if (front_detector.bounds.Intersects((triggerData.bounds)) && m_rigidbody.velocity.y > 0)
            {
                startHalfpipe(triggerData);
            }
            if (back_detector.bounds.Intersects((triggerData.bounds)) && m_rigidbody.velocity.y < 0)
            {
                m_rigidbody.velocity = m_rigidbody.velocity * 1.1f;
            }
        }

        //start grinding
        if (triggerData.GetComponent<Collider>().gameObject.layer == 6)
        {
            if (rail_detector.bounds.Intersects((triggerData.bounds)))
            {
                if (space_Hold && !starting_grind && !starting_to_turn && !grinding)
                {
                    if (triggerData.GetComponent<Collider>().gameObject.GetComponentInChildren<PathCreator>() != null)
                    {
                        rail = triggerData.GetComponent<Collider>().gameObject.GetComponentInChildren<PathCreator>();
                    }
                    StartGrind();
                }
            }
        }

        //start wallrunning
        if (wall_hit.bounds.Intersects((triggerData.bounds)) && triggerData.GetComponent<Collider>().GetType() == typeof(MeshCollider) && triggerData.GetComponentInChildren<MeshCollider>().convex)
        {
            wallCollider = triggerData.GetComponent<Collider>();
            var wallHitInfo = new RaycastHit();
            var forwardRay = new Ray(m_skateboard.position, m_skateboard.forward);
            var goodAngle = triggerData.Raycast(forwardRay, out wallHitInfo, 5f);
            if (goodAngle)
            {
                //skater
                var p0 = m_skateboard.position;
                //wall close to skater
                var p1 = triggerData.ClosestPoint(m_skateboard.position);
                //wall in front of skater
                var p2 = wallHitInfo.point;

                side1 = p1 - p0;
                var side2 = p2 - p0;

                var wall_Run_Angle = Vector3.Angle(side1, side2);

                face_Turn = (p0 - p1).normalized;
                face_New_Direction = (p2 - p1).normalized;
                if (wall_Run_Angle > 10 && m_rigidbody.velocity.magnitude > 10 && space_Hold && !wall_Running)
                {
                    m_rigidbody.MovePosition(Vector3.MoveTowards(m_rigidbody.position, p1, m_rigidbody.velocity.magnitude));

                    wall_Running = true;
                    m_current_jumps = m_jumps + Items.GetValueOrDefault("MaxJumps", 0);

                    wall_Run_Vector = 1.1f * face_New_Direction * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + (m_rigidbody.velocity.z * m_rigidbody.velocity.z));

                    Vector3 betweenRays = left_Ray.position + .5f * (right_Ray.position - left_Ray.position);

                    EndCharge();
                    if (Vector3.Distance(wallCollider.ClosestPoint(betweenRays), left_Ray.position) > Vector3.Distance(wallCollider.ClosestPoint(betweenRays), right_Ray.position))
                    {
                        m_animator.SetTrigger("WallrideRight");

                        StartCoroutine(CameraRotator(30));
                    }
                    else
                    {
                        m_animator.SetTrigger("WallrideLeft");
                        StartCoroutine(CameraRotator(-30));
                    }

                    Wallrunning_Look_Target = Quaternion.LookRotation(p2 - p1);
                }
            }
        }

    }

    private void EndCharge()
    {
        m_animator.SetTrigger("TrEndCharge");
    }
    void UnlockAttack()
    {
        lock_attack = false;
    }
    void UnlockJump()
    {
        lock_jump = false;
    }
    
    public void zeroOut()
    {
        foreach (var param in m_animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                m_animator.ResetTrigger(param.name);
            }
        }
        m_animator.SetTrigger("ZeroOut");
    }










}
