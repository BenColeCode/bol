using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitbox : MonoBehaviour
{
    public float damage;
    public List<Enemy> EnemiesHit;
   
    // Start is called before the first frame update
    void Start()
    {
        Destroy(gameObject, .2f);
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerStay(Collider other)
    {
        Debug.Log(other);
        if (other.gameObject.GetComponent<Enemy>())
        {
            if (!EnemiesHit.Contains(other.gameObject.GetComponent<Enemy>()))
            {
                EnemiesHit.Add(other.gameObject.GetComponent<Enemy>());
                other.gameObject.GetComponent<Enemy>().SendMessage("TakeDamage", damage, SendMessageOptions.RequireReceiver);
            }
        }
    }
}
