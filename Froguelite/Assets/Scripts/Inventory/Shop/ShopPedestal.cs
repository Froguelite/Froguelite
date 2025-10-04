using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ShopPedestal : MonoBehaviour
{
    [Header("Pedestal Item")]
    public ItemDefinition itemDef;
    public int priceInLotuses = 1;
    public Sprite icon;
    public string displayName;

    [Header("Who can open")]
    public string playerTag = "Player";
    public string tongueTag = "Tongue"; 
    public bool openOnTouch = true;

    private bool openedOnce;

    public string DisplayName =>
        !string.IsNullOrEmpty(displayName) ? displayName :
        (itemDef ? itemDef.displayName : "(Unnamed)");

    public Sprite Icon => icon ? icon : (itemDef ? itemDef.icon : null);

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true; // ensure triggers work
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!openOnTouch || openedOnce) return;

        if (other.CompareTag(playerTag) || other.CompareTag(tongueTag))
        {
            openedOnce = true;
            ShopManagerScript.Instance.OpenPedestal(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) || other.CompareTag(tongueTag)) 
            openedOnce = false; 
    }

    public void Consume() => Destroy(gameObject);
}
