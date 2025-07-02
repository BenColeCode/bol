using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using PathCreation;
using TMPro;

public class Harpy : Enemy
{
    private bool harmful = false;
    private bool stopChasing = false;
    private bool canAttack = true;
    private float givenAcceleration;
    private float effectiveTopSpeed;
    // Start is called before the first frame update
    public override void onStart()
    {
        givenAcceleration = acceleration;
        effectiveTopSpeed = topSpeed;
    }
    // Update is called once per frame
    public override void onFixedUpdate()
    {
        if (Vector3.Distance(transform.position, TargetPlayer.position) < 40 && canAttack)
        {
            normalAttack();
        }
    }
    public IEnumerator StartAttack(float x, bool flip, Action<bool> flipThisBool)
    {
        yield return new WaitForSeconds(x);
        flipThisBool(flip);
        m_rigidbody.AddForce(transform.forward * givenAcceleration * 400);

    }
    public override void normalMovement()
    {

        var yVel = m_rigidbody.velocity.y;

        var newVel = new Vector3(transform.forward.x, 0, transform.forward.z).normalized * (float)Math.Sqrt((m_rigidbody.velocity.x * m_rigidbody.velocity.x) + (m_rigidbody.velocity.z * m_rigidbody.velocity.z));
        newVel.y = yVel;

        //point momentum forwards
        m_rigidbody.velocity = current_Direction * newVel;

        //add momentum
        var idealDrag = givenAcceleration / effectiveTopSpeed;
        givenAcceleration = acceleration;
        if (stopChasing)
        {
            if (m_rigidbody.velocity.magnitude > 1)
            {
                m_rigidbody.velocity = m_rigidbody.velocity * 0;
            }
        }


        m_rigidbody.AddForce(transform.forward * givenAcceleration);

        if (new Vector3(m_rigidbody.velocity.x, 0, m_rigidbody.velocity.z).magnitude > effectiveTopSpeed)
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
        newVel.y = 0;
    }

    public override void turningLogic()
    {
        var lookPos = TargetPlayer.position - transform.position;
        lookPos.y = 0;
        var rotation = Quaternion.LookRotation(lookPos);
        if (!stopChasing)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 3);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * 6);
        }

    }

    public override void normalAttack()
    {
        stopChasing = true;
        harmful = true;

        canAttack = false;
        StartCoroutine(StartAttack(1f, stopChasing, result => stopChasing = !result));
        StartCoroutine(CooldownTimer(3f, harmful, result => harmful = false));
        StartCoroutine(CooldownTimer(6f, canAttack, result => canAttack = !result));
    }
    void OnTriggerEnter(Collider triggerData)
    {

        var player = triggerData.transform.gameObject;

        if (triggerData.GetComponent<Collider>().gameObject.layer == 9)
        {

            if (!harmful)
            {
                return;
            }
            else
            {
                player.SendMessage("TakeDamage", 25, SendMessageOptions.RequireReceiver);
            }
            harmful = false;
        }
    }
    private void TakeDamage(int damage)
    {
        DamageText = Instantiate(Resources.Load("Prefabs/DamageText"), RenderOverhead.transform) as GameObject;
        DamageText.GetComponent<TextMeshProUGUI>().text = damage.ToString();
        var currentPos = DamageText.GetComponent<TextMeshProUGUI>().rectTransform.localPosition;
        DamageText.GetComponent<TextMeshProUGUI>().rectTransform.localPosition = new Vector3(currentPos.x + UnityEngine.Random.Range(-1.0f, 1.0f), currentPos.y + UnityEngine.Random.Range(-1.0f, 1.0f), currentPos.z + UnityEngine.Random.Range(-1.0f, 1.0f));
        CurrentHP = CurrentHP - damage;

    }
}
