using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    public List<GameObject> items;
    private bool spawn;
    static System.Random rnd = new System.Random();
    // Update is called once per frame
    void Update()
    {
        if (spawn && (int)Time.time%5 == 0) {
            spawn = false;
            var rand = rnd.Next(items.Count);
            var position = new Vector3(
                Random.Range(-100, 100),
                Random.Range(10, 100),
                Random.Range(-100, 100)
            );


            Instantiate(items[rand], position, items[rand].transform.rotation);
        }
        if ((int)Time.time%5 == 1) {
            spawn = true;
        }
    }
}
