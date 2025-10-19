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

    public enum ConnectionPoint
    {
        PointA,
        PointB,
    }

    [Header("References")]
    [SerializeField] private RectTransform parentRectTransform;
    [SerializeField] private Image connectionImg;
    [SerializeField] private Image connectionTypeImg;
    [SerializeField] private Transform connectionPointA; // Left or Top
    [SerializeField] private Transform connectionPointB; // Right or Bottom

    [Header("Sprites and Colors")]
    [SerializeField] private Color connectionUnexploredColor;
    [SerializeField] private Color connectionExploredColor;
    [SerializeField] private Sprite connectionTypeActiveSprite;
    [SerializeField] private Sprite connectionTypeInactiveSprite;

    [Header("Animation Settings")]
    [SerializeField] private float transferAnimationDuration = 2f;

    private ConnectionPoint activeConnectionPoint;
    private bool isExplored = false;


    #endregion


    #region SETUP


    // Sets up the connection appearance based on orientation and active connection point
    public void SetupConnection(ConnectionOrientation orientation, ConnectionPoint activeConnectionPoint)
    {
        if (orientation == ConnectionOrientation.Horizontal)
        {
            parentRectTransform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else
        {
            parentRectTransform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        connectionTypeImg.sprite = connectionTypeInactiveSprite;
        connectionImg.color = connectionUnexploredColor;

        this.activeConnectionPoint = activeConnectionPoint;
        if (activeConnectionPoint == ConnectionPoint.PointA)
        {
            connectionTypeImg.transform.localPosition = connectionPointA.localPosition;
        }
        else
        {
            connectionTypeImg.transform.localPosition = connectionPointB.localPosition;
        }
    }


    #endregion


    #region CONTROL AND ANIMATIONS


    // Plays the transfer animation along the connection
    public void PlayTransferAnimation()
    {
        LeanTween.cancel(connectionTypeImg.gameObject);

        // Get the target connection point based on current connection point
        Vector3 targetPosition = activeConnectionPoint == ConnectionPoint.PointA ?
            connectionPointB.localPosition : connectionPointA.localPosition;

        activeConnectionPoint = activeConnectionPoint == ConnectionPoint.PointA ?
            ConnectionPoint.PointB : ConnectionPoint.PointA;

        // Animate connection type image to the other end
        connectionTypeImg.transform.LeanMoveLocal(targetPosition, transferAnimationDuration).setEaseInOutQuad();

        // If we are going from unexplored to explored, change color and sprite
        if (!isExplored)
        {
            isExplored = true;
            connectionTypeImg.sprite = connectionTypeActiveSprite;

            LeanTween.value(connectionImg.gameObject, connectionUnexploredColor, connectionExploredColor, transferAnimationDuration)
                .setEaseInOutQuad()
                .setOnUpdate((Color val) => {
                    connectionImg.color = val;
                });
        }
    }


    #endregion


}
