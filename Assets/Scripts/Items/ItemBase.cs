using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;


public class ItemBase : MonoBehaviour
{
    public string itemName;

    protected Player player;

    private Transform m_Object;
    private int spin_speed = 3;
    private bool settled;
    protected enum ItemState
    {
        InAttractMode,
        IsCollected,
        IsExpiring
    }

    protected ItemState itemState;
    private TextMeshProUGUI textmeshpro;
    protected virtual void Awake ()
    {
        m_Object = GetComponent<Transform> ();
        textmeshpro = GameObject.Find("Canvas/Text").GetComponent<TextMeshProUGUI>();
    }

    void FixedUpdate() {
        m_Object.RotateAround(m_Object.position, m_Object.up, spin_speed);

        if (!settled) {
            var hit = new RaycastHit();

            int layerMask = 1 << 3;            
            layerMask |= (1 << 0);  
            var onSurface = Physics.Raycast(m_Object.position, Vector3.down, out hit, 4f, layerMask);
            if (!onSurface) {
                m_Object.position = Vector3.MoveTowards(m_Object.position, new Vector3(m_Object.position.x,m_Object.position.y - .1f,m_Object.position.z), .2f);
            } else {
                settled = true;
            }
        }
    }

    /// <summary>
    /// 2D support
    /// </summary>
    protected virtual void OnTriggerEnter2D (Collider2D other)
    {
        ItemCollected (other.gameObject);
    }

    /// <summary>
    /// 3D support
    /// </summary>
    protected virtual void OnTriggerEnter (Collider other)
    {
        ItemCollected (other.gameObject);
    }

    protected virtual void ItemCollected (GameObject gameObjectCollectingItem)
    {
        if (gameObjectCollectingItem.tag != "Player" || itemState == ItemState.IsCollected || itemState == ItemState.IsExpiring)
        {
            return;
        }

        itemState = ItemState.IsCollected;
        Debug.Log(itemName);
        textmeshpro.text = itemName;
 
        // We must have been collected by a player, store handle to player for later use      
        player = gameObjectCollectingItem.GetComponent<Player> ();

        // We move the power up game object to be under the player that collect it, this isn't essential for functionality 
        // presented so far, but it is neater in the gameObject hierarchy
        gameObject.transform.parent = player.gameObject.transform;
        gameObject.transform.position = player.gameObject.transform.position;
        player.SendMessage("GetItem", itemName, SendMessageOptions.RequireReceiver);

        // Collection effects
        ItemEffects ();           

        // Payload      
        ItemPayload ();
    }

    protected virtual void ItemEffects ()
    {
    }

    protected virtual void ItemPayload ()
    {
        Debug.Log ("Power Up collected, issuing payload for: " + gameObject.name);

        // If we're instant use we also expire self immediately
            ItemHasExpired ();
    }

    protected virtual void ItemHasExpired ()
    {
        if (itemState == ItemState.IsExpiring)
        {
            return;
        }
        itemState = ItemState.IsExpiring;

        Debug.Log ("Power Up has expired, removing after a delay for: " + gameObject.name);
        DestroySelfAfterDelay ();
        
    }

    protected virtual void DestroySelfAfterDelay ()
    {
        spin_speed = 25;
        Destroy (gameObject, 1f);
    }
}

