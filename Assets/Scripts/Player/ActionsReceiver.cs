using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionsReceiver : MonoBehaviour
{
     public GameObject target;

    public string message;

    public void UnlockAttack()
    {
        if (target != null)
        {
            target.SendMessage("UnlockAttack", SendMessageOptions.RequireReceiver);
        }
    }

}
