using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrcController : Player
{
    //player position
    private bool canSecondary = true;
    private bool canMovementSkill = true;
    private bool charging = false;
    private bool spinning = false;
    private bool prematureStop = false;
    private bool endSpinning = false;


    private float Secondary_Cooldown;
    private float Movement_Cooldown;

    //UI ELEMENTS
    private TextMeshProUGUI Health_UI;
    private TextMeshProUGUI Secondary_UI;
    private TextMeshProUGUI Movement_Skill_UI;
    private Slider HealthBar;

    public GameObject primaryHitbox;
    public GameObject secondaryHitbox;
    public GameObject tertiaryHitbox;


    // Start is called before the first frame update
    public override void onStart()
    {
        StartingMaxHP = 200;
        damage = 25f;

        var movementPanel = Instantiate(Resources.Load("Prefabs/MovementPanel"), Canvas.transform);
        var secondaryPanel = Instantiate(Resources.Load("Prefabs/SecondaryPanel"), Canvas.transform);
        HealthBar = (Instantiate(Resources.Load("Prefabs/HealthBar"), Canvas.transform) as GameObject).GetComponent<Slider>();
        Secondary_UI = GameObject.Find("Canvas/MovementPanel(Clone)/MovementSkill").GetComponent<TextMeshProUGUI>();
        Movement_Skill_UI = GameObject.Find("Canvas/SecondaryPanel(Clone)/SecondarySkill").GetComponent<TextMeshProUGUI>();


        MaxHP = StartingMaxHP + Items.GetValueOrDefault("MaxHPUp", 0);
        CurrentHP = MaxHP;
        HealthBar.maxValue = MaxHP;
        HealthBar.value = CurrentHP;

    }
    public IEnumerator ChargeCD(float x, bool flip, Action<bool> flipThisBool)
    {
        yield return new WaitForSeconds(x);
        flipThisBool(flip);
        m_animator.SetTrigger("TrEndCharge");
        charging = false;

    }
    public IEnumerator ResetTrigger(float x)
    {
        yield return new WaitForSeconds(x);
        m_animator.ResetTrigger("TrAttack");
    }
    //fixedupdate is called once per frame based on 
    public override void onFixedUpdate()
    {
        HealthBar.maxValue = MaxHP;
        if (Secondary_Cooldown <= 0) canSecondary = true;
        if (Movement_Cooldown <= 0) canMovementSkill = true;

        if (CurrentHP != HealthBar.value)
        {
            HealthBar.value = Mathf.MoveTowards(HealthBar.value, CurrentHP, Mathf.Abs(5));
        }

        if (canSecondary)
        {
            Secondary_UI.text = "RMB";
        }
        else
        {
            Secondary_Cooldown = Secondary_Cooldown - Time.deltaTime;
            Secondary_UI.text = Secondary_Cooldown.ToString("0.00");
        }
        if (canMovementSkill)
        {
            Movement_Skill_UI.text = "Shift";
        }
        else
        {
            Movement_Cooldown = Movement_Cooldown - Time.deltaTime;
            Movement_Skill_UI.text = Movement_Cooldown.ToString("0.00");
        }


        if (m_PrimaryAttack == true)
        {
            m_PrimaryAttack = false;

            if (lock_attack == false && spinning == false)
            {
                lock_attack = true;
                m_animator.SetFloat("attack_speed", attack_speed * (1 + (.05f * Items.GetValueOrDefault("AttackSpeed", 0))));
                m_animator.SetTrigger("TrAttack");
            }
        }

        if (m_SecondaryAttack == true)
        {
            m_SecondaryAttack = false;
            if (canSecondary)
            {
                SecondaryAttack();
            }
        }
        if (spinning)
        {
            spin_radians = (spin_radians + 25) % 360;
            m_mesh.rotation = projectedRotation.rotation;
            m_mesh.RotateAround(projectedRotation.position, projectedRotation.up, spin_radians);
        }

        if (m_MovementSkill == true)
        {
            m_MovementSkill = false;
            if (canMovementSkill)
            {
                if (spinning)
                {
                    prematureStop = true;
                }
                zeroOut();
                MovementSkill();
            }
        }

        if (endSpinning || prematureStop)
        {
            if (endSpinning)
            {
                m_animator.SetTrigger("TrStopSpinning");
            }
            prematureStop = false;
            spinning = false;
            endSpinning = false;
            spin_radians = 0;
        }

    }

    private void PrimaryAttack()
    {
        primaryHitbox = Instantiate(Resources.Load("Prefabs/Characters/Sonny/PrimaryHitbox"), transform) as GameObject;
        primaryHitbox.GetComponent<PlayerHitbox>().damage = damage + (5 * Items.GetValueOrDefault("AttackDamage", 0));

    }
    private void SecondaryAttack()
    {
        StartCoroutine(ResetTrigger(.2f));
        canSecondary = false;
        m_animator.ResetTrigger("TrStopSpinning");
        m_animator.SetTrigger("TrSpinAttack");
        spinning = true;
        endSpinning = false;
        lock_attack = true;
        StartCoroutine(CooldownTimer(5, lock_attack, result => lock_attack = !result));
        StartCoroutine(CooldownTimer(5, endSpinning, result => endSpinning = !result));
        Secondary_Cooldown = 10f;
        //StartCoroutine(CooldownTimer(10, canSecondary, result => canSecondary = !result));

    }
    private void MovementSkill()
    {
        StartCoroutine(ResetTrigger(.2f));
        lock_attack = true;
        charging = true;
        zeroOut();
        m_animator.SetTrigger("TrCharge");
        Movement_Cooldown = 8f;
        canMovementSkill = false;
        m_rigidbody.AddForce(m_skateboard.forward * 4000);
        if (grinding)
        {
            stopGrinding();
        }
        StartCoroutine(ChargeCD(1, lock_attack, result => lock_attack = !result));
    }
    private void ConsecutiveHit()
    {
        if (!charging && !spinning)
        {
            if (attacking)
            {
                m_animator.SetTrigger("TrAttack");
            }
            else
            {
                m_animator.ResetTrigger("TrAttack");
            }
        }
    }

    private void TakeDamage(int damage)
    {
        CurrentHP = CurrentHP - damage;
    }

}