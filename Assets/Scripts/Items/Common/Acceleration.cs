using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Acceleration : ItemBase
{
    // Update is called once per frame
    protected void Start () {
        itemState = ItemState.InAttractMode;
        itemName = "Acceleration";
    }
}
