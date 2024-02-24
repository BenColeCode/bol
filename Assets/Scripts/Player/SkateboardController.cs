using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using PathCreation;
public class SkateboardController : MonoBehaviour {
   
    //player position
    public Transform m_skateboard;
    public Collider wall_hit;
    public Collider front_detector;
    public Collider back_detector;
    public Collider rail_detector;
    private PathCreator rail;
    public EndOfPathInstruction rule;
    public Transform m_mesh;
    public Transform positioner;
    
    public Transform back_Ray;
    public Transform front_Ray;
    public Transform left_Ray;
    public Transform right_Ray;
    Quaternion defaultRotation;
    
    public float m_rayDistance = 2f;
    public float m_surfDistance = 2f;
    public bool m_onSurface;
    public Animator m_animator;
    
    private Collision m_surfaceCollisionInfo;
    private Rigidbody m_rigidbody;
    
   
    //movement
    public float acceleration = 20;
    public float topSpeed = 70;
    private float m_Forward;
    private float current_Direction = 1f;
    private Quaternion settingLookAngle;
    

    //grinding
    private bool starting_grind = false;
    private bool starting_to_turn = false;
    private bool grinding = false;
    private float distanceTravelled;
    private float grind_Speed;
    private float grindDir;
    private Vector3 starting_point;
    


    //turning
    public float m_TurnForce = 3;
    private int turn_controller = 2;
    private float m_Turn;
    private Vector3 face_Turn;
    private bool halfpipe = false;
    private bool halfpipe_in_progress = false;
    private Quaternion rotation;


    //wall running
    private bool wall_Running;
    private Vector3 wall_Run_Vector;
    private Vector3 face_New_Direction;
    private int turningFrames; 
    private Vector3 side1;

    //jumping
    public float m_JumpForce = 10;
    public int m_jumps = 2;
    public int m_current_jumps;

    private bool m_Jump;
    private bool space_Hold;
    private bool grabbing_board;


    //attacking
    private bool m_Attack = false;
    private bool m_SpinAttack = false;
    private bool m_spinning = false;
    
    private bool lock_attack = false;

    // Start is called before the first frame update
    void Start() {
        m_rigidbody = GetComponent<Rigidbody>();
        wall_hit = GetComponent<Collider>();
        m_animator = GetComponentInChildren<Animator>();
        defaultRotation = m_mesh.rotation;

    }
          
    // Update is called once per frame
    void Update() { 
        ProcessInputs();
        if (!m_SpinAttack){
            m_mesh.rotation = Quaternion.RotateTowards(m_mesh.rotation, settingLookAngle, .5f); 
        }
    }

    private void FixedUpdate() {
        ProcessForce();
        if (m_Attack && !lock_attack && !m_SpinAttack ) { 
            m_animator.SetTrigger("TrAttack");
            lock_attack = true;
            m_Attack = false;
        }
        if (m_SpinAttack && !lock_attack) {
            if (!m_spinning){
                m_animator.SetTrigger("TrGoToSpinAttack");
            }
            
            m_mesh.rotation = m_mesh.rotation * Quaternion.AngleAxis(25, m_mesh.up);
            m_spinning = true;

        }
        rotateMesh();




    }

    private void ProcessInputs() {
        if(Input.GetMouseButtonDown(0)){
            m_Attack = true;
        }
        
        if(Input.GetMouseButton(1)){
            m_SpinAttack = true;
        } else {
            m_SpinAttack = false;
            if (m_spinning) {
                m_animator.SetTrigger("TrStopSpinning");
                m_animator.SetTrigger("TrIdle");
                m_mesh.rotation = settingLookAngle;
            }
            m_spinning = false;
            lock_attack = false;

        }
        
        if (Input.GetButtonDown("Jump")){
            space_Hold = true;
            if (m_current_jumps > 0) {
                m_Jump = true;
            }
        }
        if (Input.GetButtonUp("Jump")){
            space_Hold = false;
        }

        m_Forward = Input.GetAxis("Vertical");
        m_Turn = Input.GetAxis("Horizontal");


    }

    private void ProcessForce() {

        if (halfpipe_in_progress){
            m_rigidbody.AddForce(new Vector3(0, -10f, 0), ForceMode.Acceleration);
        } 
        if (halfpipe) {
            halfpipe = false;
            if ( m_rigidbody.velocity.y > 5){                
                doHalfPipe();
            }
        } else if (wall_Running) {
            wallRunningMovement();
        } else if (grinding) {
            if (rail != null){

                if (starting_grind) {
                    snapToRail();
                } else {
                    grindingMovement();
                }
            }
        } else {
            //automatic turning
            if (turningFrames > 0){
                autoTurning();
            } else {
                //turn when you hold left and right
                turningLogic();
                //move when you press forwards and backwards
                normalMovement();
                grind_Speed = m_rigidbody.velocity.magnitude;
                //gravity? i dunno it feels better when its not floaty.
                m_rigidbody.AddForce(new Vector3(0, -45f, 0), ForceMode.Acceleration);

                jumpLogic();

            }
            
        }
        
        

    }

    private void turningLogic() {
               
        if (m_Turn > 0) {
             if (turn_controller != 0) {
                m_animator.SetTrigger("TrLeanRight");
            }
            turn_controller = 0;
            
        }
        if (m_Turn < 0) {
            if (turn_controller != 1) {
                m_animator.SetTrigger("TrLeanLeft");
            }
            turn_controller = 1;
           
        }
         if(m_Turn == 0) {
            if (turn_controller != 2){
                m_animator.SetTrigger("TrIdle");
            }
            turn_controller = 2;
            
        }
 
        if (m_onSurface) {
            transform.Rotate(new Vector3(0, m_TurnForce * m_Turn, 0), Space.Self);
        } else {
            transform.Rotate(new Vector3(0, m_TurnForce * .25f * m_Turn, 0), Space.Self);            
        }
    }

    private void normalMovement() {
        var yVel = m_rigidbody.velocity.y;
        if (m_onSurface) {                
            var newVel = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + ( m_rigidbody.velocity.z *  m_rigidbody.velocity.z));
            newVel.y = yVel;

            if (newVel.magnitude < 1f && newVel.magnitude > -1f) {
                if (m_Forward == 1) {
                    current_Direction = 1;
                } 
                if (m_Forward == -1) {
                    current_Direction = -1;
                } 
            }

        //point momentum forwards
            m_rigidbody.velocity = current_Direction * newVel;
                

            //add momentum
            if (new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z).magnitude < topSpeed) {
                
                if (m_rigidbody.velocity.magnitude < 20f) {
                    
                    if (m_onSurface && turningFrames == 0) {
                        if (m_Forward == 1) {
                            m_animator.SetTrigger("TrPushoff");
                        }
                        if (m_Forward == -1) {
                            m_animator.SetTrigger("TrRevPushoff");
                        }
                    }
                    m_rigidbody.AddForce(m_skateboard.forward * 3 * acceleration * m_Forward);
                } else {
                    m_rigidbody.AddForce(m_skateboard.forward * acceleration * m_Forward);
                }
            }
        }      
    }
    private void jumpLogic() {
        if (m_Jump && (m_current_jumps > 0 || m_onSurface)) {
            if (m_rigidbody.velocity.y < 0f){
                m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z);
            }
            if (!m_onSurface) {
                m_rigidbody.AddForce(0, m_JumpForce, 0);
                m_animator.SetTrigger("TrDoubleJump");
                m_current_jumps = m_current_jumps- 1;
            } else {
                m_rigidbody.AddForce(0, m_JumpForce, 0);
                m_animator.SetTrigger("TrJump");
            }
        }
        m_Jump=false;

    }
    private void StartGrind() {    
        if (rail != null) {   
            m_animator.SetTrigger("TrGotoGrind");

            if (rail.path.isClosedLoop) {
                rule = (EndOfPathInstruction) 0;
            } else {
                rule = (EndOfPathInstruction) 2;
            }
            starting_grind = true;
            starting_to_turn = true;
            grinding = true;
            
            m_rigidbody.velocity = Vector3.zero;
            distanceTravelled = rail.path.GetClosestDistanceAlongPath(m_rigidbody.position);
            starting_point = rail.path.GetPointAtDistance(distanceTravelled, rule);
            
            var next_point = rail.path.GetPointAtDistance((distanceTravelled + grind_Speed), rule);
            var last_point= rail.path.GetPointAtDistance((distanceTravelled - grind_Speed), rule); 

            var nextAngle= Mathf.Abs(Vector3.SignedAngle((next_point-starting_point).normalized, m_skateboard.forward, Vector3.up));
            var lastAngle= Mathf.Abs(Vector3.SignedAngle((last_point-starting_point).normalized, m_skateboard.forward, Vector3.up));    

            grindDir = 1;
            if (nextAngle > lastAngle) {
                grindDir = -1;
            } 

        }
    }
    private void snapToRail() {  
        var snapPoint = rail.path.GetPointAtDistance(distanceTravelled, rule);
        snapPoint = new Vector3(snapPoint.x,snapPoint.y + .1f,snapPoint.z);
        if (Vector3.Distance(m_rigidbody.position, snapPoint) < 1) {
            starting_grind = false;
        } else {
            m_rigidbody.MovePosition(Vector3.MoveTowards(m_rigidbody.position, snapPoint, grind_Speed * Time.deltaTime));
            ///Vector3.Distance(m_rigidbody.position, snapPoint)));
        }
    }
    private void faceToRail() {   
        var targetRotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
        if (grindDir == -1) {    
           targetRotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
        }

        if (m_rigidbody.rotation == targetRotation) {
            starting_to_turn = false;
        } else {
          
            m_rigidbody.rotation = Quaternion.RotateTowards(m_rigidbody.rotation, targetRotation, 4);
        }
    }
    private void grindingMovement() {  

        distanceTravelled += (grind_Speed * Time.deltaTime * grindDir);
        var nextPoint = rail.path.GetPointAtDistance(distanceTravelled, rule);

        if ((space_Hold && rail != null) && !(!rail.path.isClosedLoop && ((nextPoint == rail.path.GetPointAtTime(0, rule) && grindDir < 0) || (nextPoint == rail.path.GetPointAtTime(1, rule) && grindDir > 0)))){               
            m_rigidbody.position = nextPoint;
            if (starting_to_turn) {
                faceToRail();
            } else {
            if (grindDir == -1) {    
                m_rigidbody.rotation = rail.path.GetReversedRotationAtDistance(distanceTravelled, rule);
            } else {
                m_rigidbody.rotation = rail.path.GetRotationAtDistance(distanceTravelled, rule);
            }
        }
        } else {
            stopGrinding();
        }
    }

    private void stopGrinding() {   
      
        starting_grind = false;
        starting_to_turn =false;
        grinding = false;
        m_animator.SetTrigger("TrStopGrinding");

        //speed up a little on the dismount
        m_rigidbody.velocity = (m_skateboard.forward).normalized * grind_Speed * 1.2f;
        //dont bump the rail
        m_rigidbody.MovePosition(transform.position + Vector3.up);
        //jump
        m_rigidbody.AddForce(0, m_JumpForce * 1.2f, 0);
        m_animator.SetTrigger("TrDoubleJump");

        rail = null;
    }

    private void wallRunningMovement() {        
        if (!space_Hold) {
            endWallRun();
            wall_Running=false;
        } else {    
            m_rigidbody.velocity = wall_Run_Vector;
            settingLookAngle = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(wall_Run_Vector, Vector3.up), 5);
        }
    }
    private void doHalfPipe() {
            halfpipe_in_progress = true;
            m_animator.SetTrigger("TrBoardGrab");
            m_rigidbody.velocity = new Vector3(0, 1, 0) * m_rigidbody.velocity.magnitude ;
            m_rigidbody.MovePosition(transform.position + -transform.forward * .2f);

    }
    private void autoTurning() {        
        turningFrames--;
        if (turningFrames < 1) {
            halfpipe_in_progress = false;
        }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face_Turn, Vector3.up), 5);

    }
    
    private void endWallRun() {    
        turningFrames = 9;
        m_rigidbody.AddForce(0, m_JumpForce, 0);
        m_animator.SetTrigger("TrIdle");
        m_animator.SetTrigger("TrDoubleJump");

    }
    
    private void OnCollisionStay(Collision other) {
        m_onSurface = true;
        m_surfaceCollisionInfo = other;
    }

    private void OnCollisionExit(Collision other) {
        m_surfaceCollisionInfo = null;
        m_onSurface = false;
        m_animator.SetTrigger("TrStopPushing");

    }

    void rotateMesh(){
        var hit = new RaycastHit();
        var front = new RaycastHit();
        var back = new RaycastHit();
        var left = new RaycastHit();
        var right = new RaycastHit();


        var onSurface = Physics.Raycast(transform.position, Vector3.down, out hit, m_surfDistance);
        if (onSurface) {
            m_current_jumps = m_jumps;
        }
        int layerMask = 1 << 3;
        layerMask |= (1 << 0);
        var frontRaycast = Physics.Raycast(front_Ray.position, Vector3.down, out front, 300f, layerMask);
        var backRaycast = Physics.Raycast(back_Ray.position, Vector3.down, out back, 300f, layerMask);

        var leftRaycast = Physics.Raycast(left_Ray.position, Vector3.down, out left, 300f, layerMask);
        var rightRaycast = Physics.Raycast(right_Ray.position, Vector3.down, out right, 300f, layerMask);
        
        var VerticalLeanAngle = front.point - back.point ;
        var HorizontalLeanAngle = left.point - right.point ;
        

        if (!grinding && !wall_Running){
            var rot = m_mesh.rotation;
            var viewAngle = Mathf.Abs(Vector3.Angle(VerticalLeanAngle.normalized, m_skateboard.forward));
            var sideAngle = Mathf.Abs(Vector3.Angle(HorizontalLeanAngle.normalized, -m_skateboard.right));

            if (front.point.y > back.point.y) {   
                viewAngle = -viewAngle;
            }
            if (left.point.y > right.point.y) {
                sideAngle = -sideAngle;
            }
        
            if (Vector3.Distance(front.point, front_Ray.position) > 7f && Vector3.Distance(back.point, back_Ray.position) > 7f) {
                if (!grabbing_board) {
                    m_animator.SetTrigger("TrBoardGrab");
                }
                grabbing_board = true;
            } else {
                if (grabbing_board) {
                    grabbing_board = false;
                    m_animator.SetTrigger("TrIdle");
                }
            }

            rot.eulerAngles = new Vector3(viewAngle, m_skateboard.rotation.eulerAngles.y, sideAngle);

            settingLookAngle = rot;


        } else {
            var rot = m_mesh.rotation;
            rot.eulerAngles = new Vector3(m_skateboard.rotation.eulerAngles.x, m_skateboard.rotation.eulerAngles.y, m_skateboard.rotation.eulerAngles.z);
            settingLookAngle = rot;
        }
    }
    
    void UnlockAttack(){
        lock_attack = false;
    }

    void startHalfpipe(Collider triggerData){

        var closeTriggerPoint=triggerData.ClosestPoint(positioner.position);
        var triggerDirection = positioner.position - closeTriggerPoint;

        triggerDirection = triggerDirection.normalized;
        triggerDirection.y = 0;


        var perp = (Quaternion.AngleAxis(90, Vector3.up) * triggerDirection).normalized;


        face_Turn = Vector3.Reflect(m_skateboard.forward, triggerDirection);
        face_Turn = face_Turn.normalized;
        var pointAngle = Vector3.Angle(m_skateboard.forward, face_Turn);

        turningFrames = Mathf.Abs((int)((pointAngle)/5));
        halfpipe = true;
    }
    
    void OnTriggerEnter(Collider triggerData) {  
        if (triggerData.GetComponent<Collider>().gameObject.layer == 7) {
            if (front_detector.bounds.Intersects((triggerData.bounds))&& m_rigidbody.velocity.y > 0) {
                startHalfpipe(triggerData);
            }
            if (back_detector.bounds.Intersects((triggerData.bounds)) && m_rigidbody.velocity.y < 0) {
                m_rigidbody.velocity = m_rigidbody.velocity * 1.1f;
            }
        }

        if (triggerData.GetComponent<Collider>().gameObject.layer == 6) {
            if (rail_detector.bounds.Intersects((triggerData.bounds))) {
                if (space_Hold && !starting_grind){
                    if (triggerData.GetComponent<Collider>().gameObject.GetComponentInChildren<PathCreator>() != null) {
                        rail = triggerData.GetComponent<Collider>().gameObject.GetComponentInChildren<PathCreator>();
                    }
                    StartGrind();
                }
            }   
        }   

        if (wall_hit.bounds.Intersects((triggerData.bounds)) && triggerData.GetComponent<Collider>().GetType() == typeof(MeshCollider) && triggerData.GetComponentInChildren<MeshCollider>().convex ) {
            var wallHitInfo = new RaycastHit();
            var forwardRay = new Ray(m_skateboard.position, m_skateboard.forward);
            var goodAngle = triggerData.Raycast(forwardRay, out wallHitInfo,5f);
            if (goodAngle) {

                var p0=m_skateboard.position;
                var p1=triggerData.ClosestPoint(m_skateboard.position);
                var p2=wallHitInfo.point;

                side1 = p1-p0;
                var side2 = p2-p0;
                
                var wall_Run_Angle = Vector3.Angle(side1, side2);
                
                face_Turn = (p0 - p1).normalized;
                face_New_Direction = (p2-p1).normalized;
                if (wall_Run_Angle > 20 && m_rigidbody.velocity.magnitude > 20 && space_Hold) {

                    wall_Running = true;
                    m_current_jumps = m_jumps;

                    wall_Run_Vector = 1.2f * face_New_Direction * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + ( m_rigidbody.velocity.z *  m_rigidbody.velocity.z));
                    if((Vector3.Distance(p1,left_Ray.position) > Vector3.Distance(p1,right_Ray.position))) {
                        m_animator.SetTrigger("WallrideRight");
                    } else {
                        m_animator.SetTrigger("WallrideLeft");
                    }

                }
            }
        }

    }
    void OnTriggerExit(Collider triggerData) {
        wall_Running = false;
    }



}