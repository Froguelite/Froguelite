using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopManagerScript : MonoBehaviour
{
    public static ShopManagerScript Instance { get; private set; }

    [Header("Popup UI")]
    [SerializeField] private GameObject shopBackDrop;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TMP_Text itemNameTxt;
    [SerializeField] private TMP_Text itemPriceTxt;
    [SerializeField] private Image itemIconImg;
    [SerializeField] private TMP_Text LotusTxt;
    [SerializeField] private Button buyButton;                 // <- assign in Inspector
    [SerializeField] private bool pauseGameWhileOpen = false;

    [Header("Refs")]
    [SerializeField] private InventoryManager inventory;

    private ShopPedestal currentPedestal;

    void Awake()
    {
        // Singleton (avoid multiple managers)
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Shop] Duplicate ShopManagerScript destroyed: {name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Auto-grab InventoryManager (including inactive objects)
        if (!inventory)
            inventory = InventoryManager.Instance
                ?? FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);
    }

    void OnEnable()
    {
        if (inventory) inventory.OnLotusesChanged += HandleLotusesChanged;
    }
    void OnDisable()
    {
        if (inventory) inventory.OnLotusesChanged -= HandleLotusesChanged;
    }

    void Start()
    {
        if (!inventory)
            Debug.LogError("[Shop] InventoryManager not found in scene!");

        HandleLotusesChanged(inventory ? inventory.lotuses : 0);

        if (shopPanel)    shopPanel.SetActive(false);
        if (shopBackDrop) shopBackDrop.SetActive(false);
        if (buyButton)    buyButton.interactable = false; // can't buy until a pedestal opens
    }

    private void HandleLotusesChanged(int value)
    {
        if (LotusTxt) LotusTxt.text = "Lotuses: " + value;
    }

    // Called by ShopPedestal
    public void OpenPedestal(ShopPedestal ped)
    {
        currentPedestal = ped;
        Debug.Log($"[Shop] OpenPedestal -> {ped.name}, item={(ped.itemDef ? ped.itemDef.id : "null")}, price={ped.priceInLotuses}");

        if (itemNameTxt)  itemNameTxt.text  = ped.DisplayName;
        if (itemPriceTxt) itemPriceTxt.text = ped.priceInLotuses.ToString();
        if (itemIconImg)  itemIconImg.sprite = ped.Icon;

        if (shopBackDrop) shopBackDrop.SetActive(true);
        if (shopPanel)    shopPanel.SetActive(true);
        if (buyButton)    buyButton.interactable = true;

        if (pauseGameWhileOpen) Time.timeScale = 0f;
    }

    public void CloseShop()
    {
        currentPedestal = null;

        if (shopPanel)    shopPanel.SetActive(false);
        if (shopBackDrop) shopBackDrop.SetActive(false);
        if (buyButton)    buyButton.interactable = false;

        if (pauseGameWhileOpen) Time.timeScale = 1f;
    }

    public void BuyCurrent()
    {
        if (!inventory)
            inventory = InventoryManager.Instance
                ?? FindFirstObjectByType<InventoryManager>(FindObjectsInactive.Include);

        Debug.Log($"[Shop] Buy pressed. ped={(currentPedestal ? currentPedestal.name : "null")}, inv={(inventory ? "ok" : "null")}");

        if (!currentPedestal || !inventory)
        {
            Debug.LogWarning("[Shop] No pedestal open or inventory missing.");
            return;
        }

        var def   = currentPedestal.itemDef;
        int price = currentPedestal.priceInLotuses;

        if (!def || string.IsNullOrEmpty(def.id))
        {
            Debug.LogError("[Shop] Current pedestal has no valid ItemDefinition/id.");
            return;
        }

        if (inventory.TryPurchase(def, price))
        {
            Debug.Log("[Shop] Purchase success.");
            currentPedestal.Consume();
            CloseShop();
        }
        else
        {
            Debug.Log("[Shop] Not Enough Lotuses");
        }
    }
}
