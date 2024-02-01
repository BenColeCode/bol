using UnityEngine;
using System.Collections;
public class ThirdPersonMovement : MonoBehaviour {
public Rigidbody controller;


//turning
private float horizontalAim;
public float turnSpeed = 120f;
private float airTurnSpeed;
public Transform cam;
public float turnSmoothTime = 0.3f;
private float turnSmoothVelocity = 1f;

//movement
private bool doKick = true;
Vector3 velocity;
public float acceleration;
public float drag;
private float momentum;
private IEnumerator KickTimer()
{
    doKick = false;
    yield return new WaitForSeconds(1);
    doKick = true;
}
    private IEnumerator GrindTimer()
{
    doGrind = false;
    yield return new WaitForSeconds(.5f);
    doGrind = true;
}


//doubleJump
private float jumpSpeed = 10f;
public float gravity = -9.81f;
public int startjumps = 0;
private int jumps;

//GroundCheck
bool isGrounded;
bool collision;
public Transform collisionCheck;
public Transform groundCheck;
private float groundDistance = 0.4f;
public LayerMask groundMask;

public float maxSpeed = 49f;

//Rails
private Collider[] nearbyRails;
private bool onRail;
private bool grinding;
private bool doGrind = true;
public float railDistance = .3f;
public LayerMask railMask;
private float moveSpeed;
private bool grindComplete; 
private float transition=0f;
private int grindPoint;
private int grindDir;

//RailJump
private float spacePressedTime;
private bool spaceHeld;

private void Grind(GameObject GO, int currentSeg) {
    Rail rail = GO.GetComponentInParent<Rail>();
    int ret = 0;
    if (!rail){
        return;
    }
    float currentRailLength;
    if (currentSeg < rail.nodes.Length-1) {
        currentRailLength = Vector3.Distance(rail.nodes[currentSeg].position, rail.nodes[currentSeg+1].position);
    } else {
        currentRailLength = Vector3.Distance(rail.nodes[currentSeg-1].position, rail.nodes[currentSeg].position);
    }

    transition += Time.deltaTime * moveSpeed/currentRailLength * grindDir;
    if (transition > 1) {
        transition = 0;
        ret = 1;
        grindPoint += 1;
        
        }
    if (transition < 0) {
        transition = 1;
        ret = -1;
        grindPoint -= 1;
        
        Debug.Log(grindPoint);
        
        Debug.Log(transition);
    }

    if ((grindPoint == rail.nodes.Length-1) || (grindPoint == -1)) {
        transform.rotation = rail.Orientation(grindPoint, transition);
        doGrind = false;
        StartCoroutine(GrindTimer());
        grindComplete = true;
        
    } else if (ret == 0) {
        Vector3 railLinPo = rail.LinearPosition(grindPoint, transition);
        transform.position = new Vector3(railLinPo.x, railLinPo.y + (float)3.3, railLinPo.z);      
    } else {
        Vector3 railLinPo = rail.LinearPosition(grindPoint, transition);
        transform.position = new Vector3(railLinPo.x, railLinPo.y + (float)3.3, railLinPo.z);  
    }
}
private float DistanceLineSegmentPoint( Vector3 a, Vector3 b, Vector3 p )
{
    // If a == b line segment is a point and will cause a divide by zero in the line segment test.
    // Instead return distance from a
    if (a == b)
        return Vector3.Distance(a, p);
    
    // Line segment to point distance equation
    Vector3 ba = b - a;
    Vector3 pa = a - p;
    return (pa - ba * (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba))).magnitude;
}
private void GetGrindPoint (GameObject GO) {
    Rail rail = GO.GetComponentInParent<Rail>();
    int retNode = -1;
    float startTransition =0;
    for(int x = 0; x<rail.nodes.Length-1; x++){
        if (DistanceLineSegmentPoint(rail.nodes[x].position, rail.nodes[x+1].position, new Vector3(transform.position.x,transform.position.y -(float)3.3,transform.position.z)) < 4f) {
            float distToNode = Vector3.Distance(rail.nodes[x].position, transform.position);
            retNode = x;
            startTransition = distToNode / Vector3.Distance(rail.nodes[x].position, rail.nodes[x+1].position);
            Vector3 toA = rail.nodes[x].position - transform.position;
            Vector3 toB = rail.nodes[x+1].position - transform.position;
            float angleToA = Vector3.Angle(toA, transform.forward);
            float angleToB = Vector3.Angle(toB, transform.forward);
            if (angleToA >= angleToB) {
                grindDir = 1;
            } else {
                grindDir = -1;
            }
                
            break;
        }
    }
    grindPoint = retNode;
    transition = startTransition;
}
// Start is called before the first frame update
void Start() {
    controller = GetComponent<Rigidbody>();
    controller.freezeRotation = true;
    airTurnSpeed = turnSpeed * 3;
}


// Update is called once per frame
void FixedUpdate() {
    isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
    collision = Physics.CheckSphere(collisionCheck.position, groundDistance, groundMask);
    nearbyRails =  Physics.OverlapSphere(groundCheck.position, railDistance, railMask);
    
    if (nearbyRails.Length > 0) {
        onRail = true;
    } else {
        onRail = false;
    }

    if (isGrounded || grinding) 
    {
        jumps = startjumps;
        if (velocity.y < 0)
        {
            velocity.y = 0f;
        }
    }


    float horizontal = Input.GetAxisRaw("Horizontal");
    float vertical = Input.GetAxisRaw("Vertical");
    if (collision) {
        momentum = 0; 
    }


    if (isGrounded && !grinding) {
        horizontalAim += horizontal * turnSpeed * Time.deltaTime;
    } else {
        horizontalAim += horizontal * airTurnSpeed * Time.deltaTime;            
    }
    if (horizontalAim > 360) {
    horizontalAim = 0;
    }
    if (horizontalAim < 0) {
    horizontalAim = 360;
    }


    float vDir = momentum/Mathf.Abs(momentum);
    if (momentum == 0){
        vDir = 0f;
    }
    Vector3 direction = new Vector3(0f, 0f, vDir);

    
    if (grinding == false) {
        if (vertical == 1){
            if (momentum < maxSpeed){
                if (doKick) {
                    doKick = false;
                    momentum += 10;
                    StartCoroutine(KickTimer());
                }
            }
        } else if (vertical == -1) {
            if (Mathf.Abs(momentum) < .5) {
                momentum = 0f;
            } else {
                momentum -= (momentum * Time.deltaTime * 2);
            }
        } else {
            if (Mathf.Abs(momentum * Time.deltaTime * .05f) > .3) {
            } else {
                momentum -= (momentum * Time.deltaTime);
            }
            if (Mathf.Abs(momentum) < .5) {
                momentum = 0f;
            }

        }


        
        //gravity
        velocity.y += (2 * gravity * Time.deltaTime);


        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + horizontalAim;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        float magnitude = Mathf.Clamp01(angle) * momentum;
        moveSpeed = magnitude;
        direction = direction.normalized; 
        
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 moveDir =  Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        if (isGrounded) {
            velocity.x = (moveDir.normalized * magnitude).x;
            velocity.z = (moveDir.normalized * magnitude).z;        
        }
    }
    
    if ((Input.GetButtonDown("Jump") && (jumps > 0 || isGrounded)) && !grinding) {
        jumps -= 1;
        velocity.y = Mathf.Sqrt(jumpSpeed * -2 * gravity);
    }
    
    if (Input.GetButtonDown("Jump") &&  grinding) {
        spacePressedTime = Time.timeSinceLevelLoad;
        spaceHeld = false;
    } else if (Input.GetButtonUp("Jump")) {
        if (!spaceHeld) {
            grindDir = grindDir * -1;
        }
    }

        if (Input.GetButton("Jump") && grinding) {
        if (Time.timeSinceLevelLoad - spacePressedTime > .2) {
            // Player has held the Space key for .2 seconds. Consider it "held"
            onRail = false;
            spaceHeld = true;
            jumps -= 1;
            velocity.y = Mathf.Sqrt(jumpSpeed * -2 * gravity);
        }
    }


    if (onRail && Input.GetButton("Grind") && !grindComplete && doGrind) {
        if (grinding == false && nearbyRails.Length > 0){
            GetGrindPoint(nearbyRails[0].gameObject);
        }
        float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + horizontalAim;
        float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
        transform.rotation = Quaternion.Euler(0f, angle, 0f);
        grinding = true;
    } else {
        grinding = false;
    }

    if (grinding && grindPoint > -1) {
        Grind(nearbyRails[0].gameObject, grindPoint);
    } else {
        controller.velocity = velocity;
        grindComplete = false;
    }

}

}