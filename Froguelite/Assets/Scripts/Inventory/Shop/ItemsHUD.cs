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
        // Use Start instead of OnEnable to ensure InventoryManager has initialized in Awake
        inv = InventoryManager.Instance;
        Debug.Log($"[ItemHUD] Start called, inv = { (inv == null ? "null" : inv.name) }");
        Debug.Log($"[ItemHUD] LotusSlot = {(lotusSlot ? "assigned" : "NULL")}");
        Debug.Log($"[ItemHUD] WoodpeckerSlot = {(woodpeckerSlot ? "assigned" : "NULL")}");

        if (inv == null) 
        {
            Debug.LogError("[ItemHUD] InventoryManager.Instance is NULL!");
            return;
        }

        inv.OnItemChanged += HandleItemChanged;
        inv.OnPowerFlyCountChanged += HandlePowerFlyCountChanged;
        Debug.Log($"[ItemHUD] Subscribed to events. Current items count: {inv.Items.Count}");

        foreach (var kv in inv.Items)
        {
            Debug.Log($"[ItemHUD] Initializing item: {kv.Key} x{kv.Value.count}");
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
        Debug.Log($"[ItemHUD] HandleItemChanged called: {e.id} x{e.count}, icon={(e.icon ? "yes" : "NO")}");
        
        // Skip PowerFly - handled separately
        if (e.id == "Powerfly") 
        {
            Debug.Log($"[ItemHUD] Skipping Powerfly (handled separately)");
            return;
        }
        
        // Find the appropriate slot for this item type
        ItemRowUI targetSlot = GetSlotForItem(e.id);
        Debug.Log($"[ItemHUD] Target slot for {e.id}: {(targetSlot ? targetSlot.name : "NULL")}");
        
        if (targetSlot != null)
        {
            Debug.Log($"[ItemHUD] Setting slot {targetSlot.name}: display={e.display}, count={e.count}");
            targetSlot.Set(e.display, e.icon, e.count);
            // Show slot if count > 0, hide if count is 0
            bool shouldShow = e.count > 0;
            Debug.Log($"[ItemHUD] Setting {targetSlot.name} active={shouldShow}");
            targetSlot.gameObject.SetActive(shouldShow);
        }
        else
        {
            Debug.LogError($"[ItemHUD] No slot found for item: {e.id}");
        }
    }
    
    void HandlePowerFlyCountChanged(int count)
    {
        UpdatePowerFlyDisplay();
    }
    
    void UpdatePowerFlyDisplay()
    {
        if (inv == null || powerFlyContainer == null || powerFlyRow == null) return;
        
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
            foreach (var powerFly in inv.collectedPowerFlies)
            {
                GameObject iconObj = Instantiate(powerFlyIconPrefab, powerFlyContainer);
                
                // Set proper size for the icon
                RectTransform iconRect = iconObj.GetComponent<RectTransform>();
                if (iconRect)
                {
                    iconRect.sizeDelta = new Vector2(40, 40); // Fixed size 40x40
                    iconRect.anchorMin = new Vector2(0, 0.5f);
                    iconRect.anchorMax = new Vector2(0, 0.5f);
                    iconRect.pivot = new Vector2(0.5f, 0.5f);
                }
                
                Image iconImage = iconObj.GetComponent<Image>();
                if (iconImage && powerFly.displayImg)
                {
                    iconImage.sprite = powerFly.displayImg;
                    iconImage.preserveAspect = true;
                }
            }
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
