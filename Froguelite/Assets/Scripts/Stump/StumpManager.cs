using Unity.Cinemachine;
using UnityEngine;

public class StumpManager : MonoBehaviour
{

    // StumpManager manages the stump, its loading, and unloading.


    #region VARIABLES


    [SerializeField] private BoxCollider2D cameraCollisionBounds;


    #endregion


    #region LOADING


    public void LoadStump()
    {
        MinimapManager.Instance.HideMinimap();
        FrogueliteCam.Instance.ConfineCamToBounds(cameraCollisionBounds);
    }


    #endregion


}
