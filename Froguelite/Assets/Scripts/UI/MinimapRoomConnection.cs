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
    [SerializeField] private Sprite connectionTypeActiveSprite;
    [SerializeField] private Sprite connectionTypeInactiveSprite;
    [SerializeField] private Image connectionTypeImgActive;
    [SerializeField] private Image connectionTypeImgInactive;

    [Header("Animation Settings")]
    [SerializeField] private float transferAnimationDuration = 2f;

    private bool isExplored = false;


    #endregion


    #region SETUP


    // Sets up the connection appearance based on orientation
    public void SetupConnection(ConnectionOrientation orientation)
    {
        if (orientation == ConnectionOrientation.Horizontal)
        {
            parentRectTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            parentRectTransform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        connectionTypeImgInactive.sprite = connectionTypeInactiveSprite;
        connectionTypeImgActive.sprite = connectionTypeActiveSprite;
        connectionImg.color = connectionUnexploredColor;
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
