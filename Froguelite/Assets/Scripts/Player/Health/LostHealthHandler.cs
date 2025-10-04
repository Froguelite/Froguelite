using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LostHealthHandler : MonoBehaviour
{

    // LostHealthHandler handles the animation of a lost health segment (left or right)


    #region VARIABLES


    public enum LostResourceType
    {
        Full,
        LeftHalf,
        RightHalf
    }

    [SerializeField] private Image resourceImg;


    #endregion


    #region FLING


    // Flings the lost resource and destroys when done
    //-------------------------------------//
    public void FlingAndDestroy(LostResourceType resourceType, Sprite displaySprite, Color color)
    //-------------------------------------//
    {
        resourceImg.color = color;
        resourceImg.sprite = displaySprite;

        float flingDuration = 1f;
        float xFlingAmount = 20f + Random.Range(0f, 10f);
        float yFlingAmount = -40f + Random.Range(-10f, 0f);
        float zRotateAmount = -45f;

        switch (resourceType)
        {
            case LostResourceType.Full:
                if (Random.Range(0f, 1f) > .5f)
                {
                    xFlingAmount *= -1;
                    zRotateAmount *= -1;
                }

                resourceImg.transform.LeanMoveLocalX(-xFlingAmount, flingDuration).setEaseOutQuad();
                resourceImg.transform.LeanMoveLocalY(yFlingAmount, flingDuration).setEaseInQuad();
                resourceImg.transform.LeanRotateZ(-zRotateAmount, flingDuration).setEaseInQuad();
                LeanTween.value(resourceImg.gameObject, 1f, 0f, flingDuration).setOnUpdate((float val) =>
                {
                    resourceImg.color = resourceImg.color.WithAlpha(val);
                }).setEaseInQuad();

                break;
            case LostResourceType.LeftHalf:
                resourceImg.transform.LeanMoveLocalX(-xFlingAmount, flingDuration).setEaseOutQuad();
                resourceImg.transform.LeanMoveLocalY(yFlingAmount, flingDuration).setEaseInQuad();
                resourceImg.transform.LeanRotateZ(-zRotateAmount, flingDuration).setEaseInQuad();
                LeanTween.value(resourceImg.gameObject, 1f, 0f, flingDuration).setOnUpdate((float val) =>
                {
                    resourceImg.color = resourceImg.color.WithAlpha(val);
                }).setEaseInQuad();
                break;
            case LostResourceType.RightHalf:
                resourceImg.transform.LeanMoveLocalX(xFlingAmount, flingDuration).setEaseOutQuad();
                resourceImg.transform.LeanMoveLocalY(yFlingAmount, flingDuration).setEaseInQuad();
                resourceImg.transform.LeanRotateZ(zRotateAmount, flingDuration).setEaseInQuad();
                LeanTween.value(resourceImg.gameObject, 1f, 0f, flingDuration).setOnUpdate((float val) =>
                {
                    resourceImg.color = resourceImg.color.WithAlpha(val);
                }).setEaseInQuad();
                break;
        }

        Destroy(gameObject, flingDuration + 0.1f);

    } // END FlingAndDestroy


    #endregion
    

} // END LostResourceHandler.cs
