using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StumpUnlocksShop : MonoBehaviour
{

    // StumpUnlocksShop manages the shop interface for stump unlocks.


    #region VARIABLES


    [SerializeField] private Transform flySpawnPosition;
    [SerializeField] private Transform capsuleMidpointPosition;
    [SerializeField] private Transform flyOutputPosition;

    [SerializeField] private int buyCost = 0;

    private List<PowerFly> fliesInShop = new List<PowerFly>();

    // Lever handling
    [SerializeField] private SpriteRenderer leverRenderer;
    [SerializeField] private Sprite leverUpSprite, leverDownSprite;
    private bool leverInCooldown = false;
    [SerializeField] private const float leverResetTime = 4f;


    #endregion


    #region SETUP


    void Start()
    {
        SetupShop();
    }


    public void SetupShop()
    {
        PowerFlyData[] allFlyDatas = PowerFlyFactory.Instance.GetAllPowerFlyDatas();
        for (int i = 0; i < allFlyDatas.Length; i++)
        {
            PowerFly newFly = PowerFlyFactory.Instance.SpawnPowerFly(allFlyDatas[i], flySpawnPosition, flySpawnPosition.position, true);
            newFly.SetCanCollect(false);
            fliesInShop.Add(newFly);
        }
    }


    #endregion


    #region BUY


    public void TryBuyFly()
    {
        if (leverInCooldown)
            return;

        if (fliesInShop.Count == 0)
            return;

        // TODO: Currency check

        PowerFly flyToBuy = fliesInShop[Random.Range(0, fliesInShop.Count)];
        fliesInShop.Remove(flyToBuy);
        flyToBuy.ManualMoveToPosition(flyOutputPosition.position, capsuleMidpointPosition.position, 2f);
        flyToBuy.SetCanCollect(true);

        // TODO: Add to inventory

        StartCoroutine(LeverCooldownCo());
    }
    

    private IEnumerator LeverCooldownCo()
    {
        leverInCooldown = true;
        leverRenderer.sprite = leverDownSprite;

        yield return new WaitForSeconds(leverResetTime);

        leverRenderer.sprite = leverUpSprite;
        leverInCooldown = false;
    }


    #endregion


}
