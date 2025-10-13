using TMPro;
using UnityEngine;

public class FloatingText : MonoBehaviour
{

    // FloatingText displays floating text at a specified position and then fades out


    #region VARIABLES


    [SerializeField] private TMP_Text textComponent;
    [SerializeField] private CanvasGroup canvGroup;


    #endregion


    #region DISPLAY


    // Sets the text of this floating text instance
    public void SetText(string text)
    {
        textComponent.text = text;
    }


    // Starts the animation to show the text
    public void StartAnimation()
    {
        canvGroup.LeanAlpha(1f, 0.3f);
        transform.LeanMoveY(transform.position.y + 1f, 1.5f).setEaseOutCubic().setOnComplete(() =>
        {
            canvGroup.LeanAlpha(0f, 0.5f).setDelay(0.5f).setOnComplete(() =>
            {
                Destroy(gameObject);
            });
        });
    }


    #endregion


}
