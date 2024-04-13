using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Dictionary<string, int> Items = new Dictionary<string, int>{};
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    void GetItem (string itemName) {
        var newValue = Items.GetValueOrDefault(itemName, 0);
        Items.Remove(itemName);
        Items.Add(itemName, newValue + 1);

    }
}
