using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OrcController : Player
{
    //player position
    private bool canSecondary = true;
    private bool canMovementSkill = true;
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


    // Start is called before the first frame update
    public override void onStart()
    {



        var movementPanel = Instantiate(Resources.Load("Prefabs/MovementPanel"), Canvas.transform);
        var secondaryPanel = Instantiate(Resources.Load("Prefabs/SecondaryPanel"), Canvas.transform);
        HealthBar = (Instantiate(Resources.Load("Prefabs/HealthBar"), Canvas.transform) as GameObject).GetComponent<Slider>();


        Secondary_UI = GameObject.Find("Canvas/MovementPanel(Clone)/MovementSkill").GetComponent<TextMeshProUGUI>();
        Movement_Skill_UI = GameObject.Find("Canvas/SecondaryPanel(Clone)/SecondarySkill").GetComponent<TextMeshProUGUI>();


        MaxHP = StartingMax + Items.GetValueOrDefault("MaxHPUp", 0);
        CurrentHP = MaxHP;
        HealthBar.maxValue = MaxHP;
        HealthBar.value = CurrentHP;

    }
    override void GetItem(string itemName)
    {
        var newValue = Items.GetValueOrDefault(itemName, 0);
        Items.Remove(itemName);
        Items.Add(itemName, newValue + 1);
        ItemText = Instantiate(TextPrefab, Canvas.transform);
        ItemText.text = itemName;
    }
    //fixedupdate is called once per frame based on 
    public override void onFixedUpdate()
    {
        HealthBar.maxValue = MaxHP;
        
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
                PrimaryAttack();
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
                MovementSkill();
            }
        }

        if (endSpinning || prematureStop)
        {
            prematureStop = false;
            spinning = false;
            endSpinning = false;
            m_animator.SetTrigger("TrStopSpinning");
            spin_radians = 0;
        }

    }

    private void PrimaryAttack()
    {
        m_animator.SetTrigger("TrAttack");
    }
    private void SecondaryAttack()
    {
        canSecondary = false;
        m_animator.ResetTrigger("TrStopSpinning");
        m_animator.SetTrigger("TrSpinAttack");
        spinning = true;
        endSpinning = false;
        StartCoroutine(CooldownTimer(5, endSpinning, result => endSpinning = !result));
        Secondary_Cooldown = 10f;
        StartCoroutine(CooldownTimer(10, canSecondary, result => canSecondary = !result));

    }
    private void MovementSkill()
    {
        CurrentHP = CurrentHP - 25;
        canMovementSkill = false;
        Movement_Cooldown = 8f;
        StartCoroutine(CooldownTimer(8, canMovementSkill, result => canMovementSkill = !result));
        m_animator.SetTrigger("TrCharge");
        m_rigidbody.AddForce(m_skateboard.forward * 4000);
        if (grinding)
        {
            stopGrinding();
        }
    }

    private void TakeDamage(int damage)
    {
        CurrentHP = CurrentHP - damage;
    }

}