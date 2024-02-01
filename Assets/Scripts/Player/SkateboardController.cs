using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class SkateboardController : MonoBehaviour {
   
    //player position
    public Transform m_skateboard;
    public Transform front_ray;
    public Transform back_ray;
    public Collider wall_hit;
    
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
   
   //turning
    public float m_TurnForce = 3;
    private int turn_controller = 2;
    private float m_Turn;
    private Vector3 face_Turn;
    private bool halfpipe = false;
    private Quaternion rotation;


    //wall running
    private bool wall_Running;
    private Vector3 wall_Run_Vector;
    private Vector3 face_New_Direction;
    private int turningFrames = 0; 

    //jumping
    public float m_JumpForce = 10;
    public int m_jumps = 2;
    public int m_current_jumps;

    private bool m_Jump;
    private bool space_Hold;


    //attacking
    private bool m_Attack = false;
    private bool lock_attack = false;

    // Start is called before the first frame update
    void Start() {
        m_animator = GetComponent<Animator>();
        m_rigidbody = GetComponent<Rigidbody>();
        wall_hit = GetComponent<Collider>();

    }
    
    void OnGUI() {
        
         GUILayout.BeginArea(new Rect(Screen.width - 400, 0, 400, Screen.height));

         GUI.Label(new Rect(0,0,400,400),m_Turn.ToString());
        GUILayout.EndArea();
       
    }
        
    // Update is called once per frame
    void Update() { 

        ProcessInputs();

    }

    private void FixedUpdate() {
        AlignToSurface();
        ProcessForce();
        m_rigidbody.AddForce(new Vector3(0, -15f, 0), ForceMode.Acceleration);
    }

    private void ProcessInputs() {
        if(Input.GetMouseButtonDown(0)){
            m_Attack = true;
            
        }
        if (Input.GetButtonDown("Jump")){
            space_Hold = true;
            m_current_jumps = m_current_jumps- 1;
            if (m_current_jumps > 0) {
                m_Jump = true;
            }
        }
        if (Input.GetButtonUp("Jump")){
            space_Hold = false;
        }

        if (Input.GetButtonDown("Halfpipe")){
            halfpipe = true;
        }

        m_Forward = Input.GetAxis("Vertical");
        m_Turn = Input.GetAxis("Horizontal");


    }

    private void ProcessForce() {


        //turn 180 degrees and convert all momentum to upwards
        if (halfpipe) {
            turningFrames = 36;
            face_Turn = -transform.forward;
            m_rigidbody.velocity = new Vector3(0, 1, 0) * m_rigidbody.velocity.magnitude;
            m_rigidbody.AddForce(face_Turn * 50);
            halfpipe = false;
        }

        if (m_Attack && lock_attack == false) { 
            m_animator.SetTrigger("TrAttack");
            lock_attack = true;
            m_Attack = false;
        }


        if (wall_Running) {
            if (!space_Hold) {
                endWallRun();
                wall_Running=false;
            } else {
                wallRunningMovement();
            }
        } else {

            //automatic turning
            if (turningFrames > 0){
                turningFrames--;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(face_Turn, Vector3.up), 5);
            } else {
                //turn when you hold left and right
                turningLogic();
                //move when you press forwards and backwards
                normalMovement();
            }
            
        }
        
        
        if (m_Jump && m_current_jumps > 0) {
            if (!m_onSurface) {
                if (m_rigidbody.velocity.y < 0f){
                    m_rigidbody.velocity = new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z);
                }
                m_rigidbody.AddForce(0, m_JumpForce, 0);
                m_animator.SetTrigger("TrDoubleJump");
            } else {
                m_rigidbody.AddForce(0, m_JumpForce, 0);
                m_animator.SetTrigger("TrJump");
            }
            m_Jump=false;
            
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
                
                if (new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z).magnitude < 20f) {
                    
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

    private void wallRunningMovement() {        
        m_rigidbody.velocity = wall_Run_Vector;
    }
    private void endWallRun() {    
        turningFrames = 9;
        m_rigidbody.AddForce(0, m_JumpForce, 0);
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

    void AlignToSurface() {
        var bHit = new RaycastHit();
        var fHit = new RaycastHit();
        var hit = new RaycastHit();


        var onSurface = Physics.Raycast(transform.position, Vector3.down, out hit, m_surfDistance);
        var frontHit = Physics.Raycast(front_ray.position, Vector3.down, out fHit, m_rayDistance);
        var backHit = Physics.Raycast(back_ray.position, Vector3.down, out bHit, m_rayDistance);
        if (onSurface) {
            m_current_jumps = m_jumps;
        }        
    }
    
    void UnlockAttack(){
        lock_attack = false;
    }
    void OnTriggerEnter(Collider triggerData) {  
        Debug.Log(m_rigidbody.velocity.magnitude);
    
        var wallHitInfo = new RaycastHit();
        var forwardRay = new Ray(m_skateboard.position, m_skateboard.forward);
        var goodAngle = triggerData.Raycast(forwardRay, out wallHitInfo,5f);
        if (goodAngle) {

            var p0=m_skateboard.position;
            var p1=triggerData.ClosestPoint(m_skateboard.position);
            var p2=wallHitInfo.point;

            var side1 = p1-p0;
            var side2 = p2-p0;
            
            var wall_Run_Angle = Vector3.Angle(side1, side2);
            
            face_Turn = (p0 - p1).normalized;
            face_New_Direction = (p2-p1).normalized;
            if (wall_Run_Angle > 20 && m_rigidbody.velocity.magnitude > 20 && space_Hold) {

                wall_Running = true;
                m_current_jumps = m_jumps;

                wall_Run_Vector = 1.2f * face_New_Direction * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + ( m_rigidbody.velocity.z *  m_rigidbody.velocity.z));
            }
        }
            
    }
    void OnTriggerExit(Collider triggerData) {
        wall_Running = false;
    }



}