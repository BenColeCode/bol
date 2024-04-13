using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TopSpeed : ItemBase
{
    // Update is called once per frame
    protected void Start () {
        itemState = ItemState.InAttractMode;
        itemName = "TopSpeed";
    }
}
