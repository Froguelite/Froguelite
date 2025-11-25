using UnityEngine;

public class StumpUnlocksOverlapHandler : MonoBehaviour
{

    // StumpUnlocksOverlapHandler handles overlaps for stump unlocks shop


    #region VARIABLES


    [SerializeField] private StumpUnlocksShop stumpUnlocksShop;


    #endregion


    #region OVERLAP


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Tongue") && PlayerAttack.Instance.IsAttacking())
        {
            stumpUnlocksShop.TryBuyFly();
            PlayerAttack.Instance.StopTongueExtension();
        }
    }


    #endregion

}
