using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CollectionOverlayHandler : MonoBehaviour
{

    // CollectionOverlayHandler manages the overlay UI that appears when collecting power flies


    #region VARIABLES


    public static CollectionOverlayHandler Instance;

    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private Transform contentParent;
    private float contentStartY;
    [SerializeField] private float contentMoveDistance = 50f;
    [SerializeField] private TMP_Text flyNameText;
    [SerializeField] private TMP_Text flyDescriptionText;

    [Header("Lock visuals")]
    [SerializeField] private Image lockImage;
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;
    [SerializeField] private CanvasGroup lockCanvGroup;
    private Vector3 lockStartPos;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        contentStartY = contentParent.localPosition.y;
        lockStartPos = lockImage.transform.localPosition;
    }


    #endregion


    #region TRIGGER OVERLAY


    // Triggers the overlay to show that a power fly has been collected
    public void ShowPowerFlyCollected(PowerFlyData powerFlyData, bool showUnlockVisual = false)
    {
        LeanTween.cancel(overlayCanvasGroup.gameObject);
        LeanTween.cancel(contentParent.gameObject);
        LeanTween.cancel(lockImage.gameObject);
        LeanTween.cancel(lockCanvGroup.gameObject);
        StopCoroutine(MoveLockCo());
        lockImage.transform.localPosition = lockStartPos;
        lockImage.transform.localRotation = Quaternion.Euler(Vector3.zero);

        overlayCanvasGroup.alpha = 0f;
        overlayCanvasGroup.LeanAlpha(1f, fadeDuration).setOnComplete(() =>
        {
            overlayCanvasGroup.LeanAlpha(0f, fadeDuration).setDelay(displayDuration);
        });

        contentParent.localPosition = new Vector3(contentParent.localPosition.x, contentStartY - contentMoveDistance, contentParent.localPosition.z);
        contentParent.LeanMoveLocalY(contentStartY, fadeDuration).setEaseOutQuad().setOnComplete(() =>
        {
            contentParent.LeanMoveLocalY(contentStartY + contentMoveDistance, fadeDuration).setDelay(displayDuration).setEaseInQuad();
        });

        flyNameText.text = powerFlyData.powerFlyName.ToUpper();
        flyDescriptionText.text = powerFlyData.description;

        if (showUnlockVisual)
        {
            StartCoroutine(MoveLockCo());
        }
    }


    private IEnumerator MoveLockCo()
    {
        lockCanvGroup.alpha = 1f;
        lockImage.sprite = lockedSprite;

        yield return new WaitForSeconds(1f);

        lockImage.sprite = unlockedSprite;
        lockImage.transform.LeanMoveLocalX(-100f + lockStartPos.x, 0.6f).setEaseOutQuad();
        lockImage.transform.LeanMoveLocalY(100f + lockStartPos.y, 0.6f).setEaseOutQuad();
        lockImage.transform.LeanRotateZ(60f, 0.6f).setEaseOutQuad();
        lockCanvGroup.LeanAlpha(0f, 0.6f).setDelay(0.6f);
    }


    #endregion
    

}
