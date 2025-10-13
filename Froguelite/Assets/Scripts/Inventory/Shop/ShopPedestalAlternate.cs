using TMPro;
using UnityEngine;

public class ShopPedestalAlternate : GroundCollectable
{

    // ShopPedestalAlternate is an alternative to ShopPedestal for the floating purchasing functionality


    #region VARIABLES


    [SerializeField] private SpriteRenderer itemImage;
    [SerializeField] private TMP_Text costText;

    private ItemDefinition itemDef;
    private PowerFlyData powerFlyData;
    private int priceInLotuses;
    private bool healPlayerOnPurchase = false;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    // Sets up this pedestal with the given item definition and price
    public void Setup(ItemDefinition newItemDef, int newPriceInLotuses, bool healPlayerOnPurchase = false)
    {
        itemDef = newItemDef;
        priceInLotuses = newPriceInLotuses;
        this.healPlayerOnPurchase = healPlayerOnPurchase;
        if (healPlayerOnPurchase)
        {
            doHeartbeat = true;
            ResetBeatAnim();
        }
        
        UpdateVisuals();
    }


    // Sets up this pedestal with the given power fly data and price
    public void Setup(PowerFlyData powerFlyData, int newPriceInLotuses)
    {
        this.powerFlyData = powerFlyData;
        priceInLotuses = newPriceInLotuses;
        UpdateVisuals();
    }


    // Updates the visuals of this pedestal based on the item definition and price
    private void UpdateVisuals()
    {
        if (powerFlyData != null)
            itemImage.sprite = powerFlyData.displayImg;
        else if (itemDef != null)
            itemImage.sprite = itemDef.icon;
        costText.text = priceInLotuses.ToString();
    }


    #endregion


    #region OVERLAP


    // On collect, try to buy (or fail)
    public override void OnCollect()
    {// Retract the tongue
        PlayerAttack.Instance.StopTongueExtension();

        // Try to buy the item
        if (InventoryManager.Instance.lotuses >= priceInLotuses)
        {
            BuyItem();
        }
    }


    // Buys this item and adds it to the inventory
    private void BuyItem()
    {
        if (itemDef != null)
        {
            InventoryManager.Instance.AddItem(itemDef);

            if (healPlayerOnPurchase)
            {
                StatsManager.Instance.playerHealth.HealPlayer(2);
            }
        }
        else if (powerFlyData != null)
        {
            InventoryManager.Instance.AddPowerFly(powerFlyData);
        }
        else
        {
            return;
        }

        InventoryManager.Instance.RemoveLotuses(priceInLotuses);
        Destroy(gameObject);
    }


    #endregion


}
