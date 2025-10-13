using System.Collections.Generic;
using UnityEngine;

public class ShopAlternate : MonoBehaviour
{

    // ShopAlternate is an alternative to the shop system using the floating purchasing functionality


    #region VARIABLES


    private ShopPedestalAlternate[] pedestals;

    [SerializeField] private ItemDefinition woodpeckerItemDef;
    [SerializeField] private ItemDefinition heartItemDef;
    [SerializeField] private bool setupOnStart = false;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Start()
    {
        if (setupOnStart)
        {
            SetupShop();
        }
    }


    public void SetupShop()
    {
        // Find all pedestals in children and set them up
        pedestals = GetComponentsInChildren<ShopPedestalAlternate>();

        // Set up pedestals based on predefined items and prices for now
        PowerFlyData powerFlyData = PowerFlyFactory.Instance.RollCommonFly();
        pedestals[0].Setup(heartItemDef, 2, true);
        pedestals[1].Setup(woodpeckerItemDef, 2);
        pedestals[2].Setup(powerFlyData, 3);
    }


    #endregion


}
