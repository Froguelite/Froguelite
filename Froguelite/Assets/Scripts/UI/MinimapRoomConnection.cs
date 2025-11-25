using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class MinimapRoomConnection : MonoBehaviour
{

    // MinimapRoomConnection represents a connection between rooms on the minimap.


    #region VARIABLES


    public enum ConnectionOrientation
    {
        Horizontal,
        Vertical,
    }

    [Header("References")]
    [SerializeField] private RectTransform parentRectTransform;
    [SerializeField] private Image connectionImg;
    [SerializeField] private RectTransform connectionTypeTransform;

    [Header("Sprites and Colors")]
    [SerializeField] private Color connectionUnexploredColor;
    [SerializeField] private Color connectionExploredColor;
    [SerializeField] private Color connectionFinalColor;
    [SerializeField] private Sprite standardConnectionSprite;
    [SerializeField] private Sprite finalConnectionSprite;
    [SerializeField] private Sprite connectionTypeActiveSprite;
    [SerializeField] private Sprite connectionTypeInactiveSprite;
    [SerializeField] private Sprite connectionTypeLockedSprite;
    [SerializeField] private Sprite connectionTypeFinalSprite;
    [SerializeField] private Sprite forestConnectionTypeActiveSprite;
    [SerializeField] private Sprite forestConnectionTypeInactiveSprite;
    [SerializeField] private Sprite forestConnectionTypeLockedSprite;
    [SerializeField] private Sprite forestConnectionTypeFinalSprite;
    [SerializeField] private Image connectionTypeImgActive;
    [SerializeField] private Image connectionTypeImgInactive;

    [Header("Animation Settings")]
    [SerializeField] private float transferAnimationDuration = 2f;

    private bool isExplored = false;
    private bool isLocked = false;
    private int zone = 0;


    #endregion


    #region SETUP


    // Sets up the connection appearance based on orientation
    public void SetupConnection(ConnectionOrientation orientation, bool isLocked, int zone, bool isFinalConnection = false)
    {
        this.isLocked = isLocked;
        this.zone = zone;

        if (orientation == ConnectionOrientation.Horizontal)
        {
            connectionImg.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            connectionImg.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        // Handle final connection setup
        if (isFinalConnection)
        {
            // Set final connection sprites and color
            connectionImg.sprite = finalConnectionSprite;
            connectionImg.color = connectionFinalColor;
            
            Sprite finalSprite = zone == 1 ? forestConnectionTypeFinalSprite : connectionTypeFinalSprite;
            connectionTypeImgInactive.sprite = finalSprite;
            connectionTypeImgActive.sprite = finalSprite;
            
            // Final connections are always explored
            isExplored = true;
            connectionTypeImgInactive.color = new Color(1f, 1f, 1f, 0f);
        }
        else
        {
            // Set standard connection sprite
            connectionImg.sprite = standardConnectionSprite;
            connectionImg.color = connectionUnexploredColor;
            
            // Select sprites based on zone
            Sprite activeSprite = zone == 1 ? forestConnectionTypeActiveSprite : connectionTypeActiveSprite;
            Sprite inactiveSprite = zone == 1 ? forestConnectionTypeInactiveSprite : connectionTypeInactiveSprite;
            Sprite lockedSprite = zone == 1 ? forestConnectionTypeLockedSprite : connectionTypeLockedSprite;

            if (isLocked)
            {
                connectionTypeImgInactive.sprite = lockedSprite;
            }
            else
            {
                connectionTypeImgInactive.sprite = inactiveSprite;
            }

            connectionTypeImgActive.sprite = activeSprite;
        }
    }


    public void UnlockConnection()
    {
        isLocked = false;
        Sprite inactiveSprite = zone == 1 ? forestConnectionTypeInactiveSprite : connectionTypeInactiveSprite;
        connectionTypeImgInactive.sprite = inactiveSprite;
    }


    #endregion


    #region CONTROL AND ANIMATIONS


    // Plays the transfer animation along the connection
    public void PlayTransferAnimation(bool doNotExplore = false)
    {
        LeanTween.cancel(connectionTypeTransform.gameObject);

        // If we are going from unexplored to explored, change color and sprite
        if (doNotExplore)
            return;

        if (!isExplored)
        {
            isExplored = true;
            LeanTween.value(connectionTypeImgInactive.gameObject, 1f, 0f, transferAnimationDuration).setOnUpdate((float val) =>
            {
                connectionTypeImgInactive.color = new Color(1f, 1f, 1f, val);
            });

            LeanTween.value(connectionImg.gameObject, connectionUnexploredColor, connectionExploredColor, transferAnimationDuration)
                .setEaseInOutQuad()
                .setOnUpdate((Color val) =>
                {
                    connectionImg.color = val;
                });
        }
    }


    #endregion


}
