using System.Collections;
using UnityEngine;

public class FlipbookAnimatorTester : MonoBehaviour
{

    // FlipbookAnimatorTester is a simple tester script for the FlipbookAnimator component.
    // Swaps between different flipbook animations for testing purposes.


    #region VARIABLES


    [SerializeField] private FlipbookAnimator flipbookAnimator;
    [SerializeField] private float switchInterval = 2f; // Time in seconds between switching animations

    [SerializeField] private Sprite[] flipbookFrames1;
    [SerializeField] private float frameDuration1 = 0.1f;
    [SerializeField] private FlipbookLoopMethod loopMethod1;
    [SerializeField] private Sprite[] flipbookFrames2;
    [SerializeField] private float frameDuration2 = 0.1f;
    [SerializeField] private FlipbookLoopMethod loopMethod2;
    [SerializeField] private Sprite[] flipbookFrames3;
    [SerializeField] private float frameDuration3 = 0.1f;
    [SerializeField] private FlipbookLoopMethod loopMethod3;


    #endregion


    #region MONOBEHAVIOUR


    private void Start()
    {
        // Start the animation switching coroutine
        StartCoroutine(SwitchAnimationsRoutine());
    }


    #endregion


    #region ANIMATION SWITCHING


    private IEnumerator SwitchAnimationsRoutine()
    {
        while (true)
        {
            // Play first animation
            flipbookAnimator.SetSprites(flipbookFrames1, frameDuration1, loopMethod1);
            yield return new WaitForSeconds(switchInterval);

            // Play second animation
            flipbookAnimator.SetSprites(flipbookFrames2, frameDuration2, loopMethod2);
            flipbookAnimator.SetFlipX(true);
            yield return new WaitForSeconds(switchInterval);

            // Play third animation
            flipbookAnimator.SetSprites(flipbookFrames3, frameDuration3, loopMethod3);
            flipbookAnimator.SetFlipX(false);
            yield return new WaitForSeconds(switchInterval);
        }
    }


    #endregion


}
