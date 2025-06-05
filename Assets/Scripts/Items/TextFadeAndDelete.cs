using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class TextFadeAndDelete : MonoBehaviour
{
    public TextMeshProUGUI UGUI;
    // Start is called before the first frame update
    void Start()
    {
        UGUI = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void FixedUpdate()
    {
        UGUI.color = new Color(UGUI.color.r, UGUI.color.g, UGUI.color.b, UGUI.color.a - .01f);
        if (UGUI.color.a < 0f)
        {
            Destroy(this.gameObject);
        }
    }
}
