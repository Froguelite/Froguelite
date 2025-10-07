// CollectedItemsHUD.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CollectedItemsHUD : MonoBehaviour
{
    [Header("Fixed Slots")]
    [SerializeField] ItemRowUI lotusSlot;      // assign the Lotus slot
    [SerializeField] ItemRowUI woodpeckerSlot; // assign the Woodpecker slot  
    
    [Header("PowerFly Row")]
    [SerializeField] GameObject powerFlyRow;   // assign the PowerFly row container
    [SerializeField] Transform powerFlyContainer; // assign the container for individual fly icons
    [SerializeField] GameObject powerFlyIconPrefab; // assign a simple Image prefab for individual flies

    private InventoryManager inv;

    void Awake()
    {
        // Ensure all slots start inactive
        if (lotusSlot) lotusSlot.gameObject.SetActive(false);
        if (woodpeckerSlot) woodpeckerSlot.gameObject.SetActive(false);
        if (powerFlyRow) powerFlyRow.SetActive(false);
        
        // Clear any existing power fly icons
        if (powerFlyContainer)
        {
            foreach (Transform child in powerFlyContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    void Start()
    {
        // Use Start instead of OnEnable to ensure InventoryManager has initialized
        inv = InventoryManager.Instance;

        if (inv == null) 
        {
            Debug.LogError("[ItemHUD] InventoryManager.Instance is NULL!");
            return;
        }

        inv.OnItemChanged += HandleItemChanged;
        inv.OnPowerFlyCountChanged += HandlePowerFlyCountChanged;

        foreach (var kv in inv.Items)
        {
            HandleItemChanged(kv.Value);
        }
            
        // Update power fly display
        UpdatePowerFlyDisplay();
    }

    void OnDisable()
    {
        if (inv != null) 
        {
            inv.OnItemChanged -= HandleItemChanged;
            inv.OnPowerFlyCountChanged -= HandlePowerFlyCountChanged;
        }
        inv = null;
    }

    void HandleItemChanged(InventoryManager.Entry e)
    {
        // Skip PowerFly - handled separately
        if (e.id == "Powerfly") return;
        
        // Find the appropriate slot for this item type
        ItemRowUI targetSlot = GetSlotForItem(e.id);
        
        if (targetSlot != null)
        {
            targetSlot.Set(e.display, e.icon, e.count);
            // Show slot if count > 0, hide if count is 0
            targetSlot.gameObject.SetActive(e.count > 0);
        }
    }
    
    void HandlePowerFlyCountChanged(int count)
    {
        UpdatePowerFlyDisplay();
    }
    
    void UpdatePowerFlyDisplay()
    {
        if (inv == null || powerFlyContainer == null || powerFlyRow == null) return;
        
        Debug.Log($"[PowerFly] Updating display. Count: {inv.collectedPowerFlies.Count}");
        
        // Clear existing power fly icons
        foreach (Transform child in powerFlyContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Show/hide the power fly row based on count
        bool hasPowerFlies = inv.collectedPowerFlies.Count > 0;
        powerFlyRow.SetActive(hasPowerFlies);
        
        if (hasPowerFlies && powerFlyIconPrefab != null)
        {
            // Create an icon for each collected power fly
            int index = 0;
            foreach (var powerFly in inv.collectedPowerFlies)
            {
                Debug.Log($"[PowerFly] Creating icon {index}: sprite={(powerFly.displayImg ? powerFly.displayImg.name : "NULL")}");
                
                GameObject iconObj = Instantiate(powerFlyIconPrefab, powerFlyContainer);
                iconObj.name = $"PowerFlyIcon_{index}";
                
                // Set proper size for the icon
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                if (iconRect)
                {
                    iconRect.sizeDelta = new Vector2(40, 40); // Fixed size 40x40
                    iconRect.localPosition = Vector3.zero;
                }
                
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage)
                {
                    if (powerFly.displayImg)
                    {
                        iconImage.sprite = powerFly.displayImg;
                        iconImage.preserveAspect = true;
                        Debug.Log($"[PowerFly] Icon {index} sprite set to: {powerFly.displayImg.name}");
                    }
                    else
                    {
                        Debug.LogError($"[PowerFly] PowerFly {index} has NO displayImg sprite assigned!");
                    }
                }
                
                index++;
            }
        }
        else if (powerFlyIconPrefab == null)
        {
            Debug.LogError("[PowerFly] PowerFlyIcon prefab is NOT assigned to ItemHUD!");
        }
    }
    
    // Get the correct slot for each item type
    ItemRowUI GetSlotForItem(string itemId)
    {
        switch (itemId)
        {
            case "Lotus":
                return lotusSlot;
            case "Woodpecker":
                return woodpeckerSlot;
            default:
                Debug.LogWarning($"No slot defined for item: {itemId}");
                return null;
        }
    }
}
