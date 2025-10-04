using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthSingleDisplay : MonoBehaviour
{

    // HealthSingleDisplay handles the display of a single health segment (halves and wholes)


    #region VARIABLES


    public enum ResourceFullness { Empty, Half, Full }

    [SerializeField] private Image disabledImg;
    [SerializeField] private Image enabledImg;

    [SerializeField] private Sprite enabledFullSprite;
    [SerializeField] private Sprite enabledLeftHalfSprite;
    [SerializeField] private Sprite enabledRightHalfSprite;

    [SerializeField] private LostHealthHandler lostHealthPrefab;

    private PlayerHealthDisplay parentHealthDisplay;

    private Color standardColor;
    private Color glowingColor;

    private bool shouldAnimateBeating = false;
    private IEnumerator beatSubscribeCoroutine;

    private ResourceFullness currentFullness = ResourceFullness.Full;


    #endregion


    #region SETUP


    public void SetupHealthDisplay(PlayerHealthDisplay parentDisplay)
    {
        parentHealthDisplay = parentDisplay;

        standardColor = enabledImg.color;
        glowingColor = Color.white;

        SetToFullImmediate();
    }


    // OnEnable, subscribe to beat events
    //-------------------------------------//
    void OnEnable()
    //-------------------------------------//
    {
        if (HealthBeatEventHandler.Instance == null && beatSubscribeCoroutine == null)
        {
            beatSubscribeCoroutine = WaitForBeatEventHandler();
            StartCoroutine(beatSubscribeCoroutine);
            return;
        }

        HealthBeatEventHandler.Instance.smallBeatEvent1.AddListener(SmallBeatCallback1);
        HealthBeatEventHandler.Instance.smallBeatEvent2.AddListener(SmallBeatCallback2);

    } // END OnEnable


    // OnDisable, unsubscribe from beat events
    //-------------------------------------//
    void OnDisable()
    //-------------------------------------//
    {
        if (beatSubscribeCoroutine != null)
        {
            StopCoroutine(beatSubscribeCoroutine);
            beatSubscribeCoroutine = null;
        }

        HealthBeatEventHandler.Instance.smallBeatEvent1.RemoveListener(SmallBeatCallback1);
        HealthBeatEventHandler.Instance.smallBeatEvent2.RemoveListener(SmallBeatCallback2);

    } // END OnDisable


    // Waits for the beat event handler to be initialized, then subscribes to events
    //-------------------------------------//
    private IEnumerator WaitForBeatEventHandler()
    //-------------------------------------//
    {
        while (HealthBeatEventHandler.Instance == null)
        {
            yield return null;
        }

        HealthBeatEventHandler.Instance.smallBeatEvent1.AddListener(SmallBeatCallback1);
        HealthBeatEventHandler.Instance.smallBeatEvent2.AddListener(SmallBeatCallback2);

        beatSubscribeCoroutine = null;

    } // END WaitForBeatEventHandler


    #endregion


    #region FILL / EMPTY


    // Immediately sets this resource to full
    //-------------------------------------//
    public void SetToFullImmediate()
    //-------------------------------------//
    {
        // Nothing to do
        if (currentFullness == ResourceFullness.Full)
            return;

        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.color = standardColor;
        enabledImg.transform.localScale = Vector3.one;

        enabledImg.sprite = enabledFullSprite;
        enabledImg.enabled = true;

        currentFullness = ResourceFullness.Full;

    } // END SetToFullImmediate


    // Sets this resource to full, with an animation
    //-------------------------------------//
    public void SetToFull()
    //-------------------------------------//
    {
        // Nothing to do
        if (currentFullness == ResourceFullness.Full)
            return;

        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.sprite = enabledFullSprite;
        enabledImg.enabled = true;

        PerformBigBeat();

        currentFullness = ResourceFullness.Full;

    } // END SetToFull


    // Immediately sets this resource to empty
    //-------------------------------------//
    public void SetToEmptyImmediate()
    //-------------------------------------//
    {
        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.sprite = null;
        enabledImg.enabled = false;

        currentFullness = ResourceFullness.Empty;

    } // END SetToEmptyImmediate


    // Sets this resource to empty, with an animation
    //-------------------------------------//
    public void SetToEmpty()
    //-------------------------------------//
    {
        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.color = standardColor;
        enabledImg.transform.localScale = Vector3.one;

        enabledImg.sprite = null;
        enabledImg.enabled = false;

        if (currentFullness == ResourceFullness.Full)
        {
            SpawnLostRightHalf();
            SpawnLostLeftHalf();
        }
        else if (currentFullness == ResourceFullness.Half)
        {
            SpawnLostLeftHalf();
        }

        currentFullness = ResourceFullness.Empty;

    } // END SetToEmpty


    // Immediately set this resource to half
    //-------------------------------------//
    public void SetToHalfImmediate()
    //-------------------------------------//
    {
        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.sprite = enabledLeftHalfSprite;
        enabledImg.enabled = true;

        currentFullness = ResourceFullness.Half;

    } // END SetToHalfImmediate



    // Sets this resource to half, with an animation
    //-------------------------------------//
    public void SetToHalf()
    //-------------------------------------//
    {
        LeanTween.cancel(enabledImg.gameObject);
        enabledImg.sprite = enabledLeftHalfSprite;
        enabledImg.enabled = true;

        if (currentFullness == ResourceFullness.Empty)
        {
            PerformBigBeat();
        }
        else if (currentFullness == ResourceFullness.Full)
        {
            SpawnLostRightHalf();
        }

        currentFullness = ResourceFullness.Half;

    } // END SetToHalf


    #endregion


    #region BEATING


    // Performs a single big beat
    //-------------------------------------//
    private void PerformBigBeat()
    //-------------------------------------//
    {
        LeanTween.cancel(enabledImg.gameObject);

        enabledImg.color = glowingColor;
        enabledImg.transform.localScale = Vector3.one * HealthBeatEventHandler.bigBeatScale;

        LeanTween.value(enabledImg.gameObject, glowingColor, standardColor, HealthBeatEventHandler.bigBeatDuration).setEaseInQuad().setOnUpdate((Color val) =>
        {
            enabledImg.color = val;
        });
        enabledImg.transform.LeanScale(Vector3.one, HealthBeatEventHandler.bigBeatDuration).setEaseInQuad().setOnComplete(() =>
        {
            if (shouldAnimateBeating)
            {
                StartSmallBeatsAnim();
            }
        });

    } // END PerformBigBeat


    // Small beats a single time, then calls again if beating has not cancelled
    //-------------------------------------//
    public void StartSmallBeatsAnim()
    //-------------------------------------//
    {
        shouldAnimateBeating = true;

    } // END StartSmallBeatsAnim


    // Stops the small beating animation
    //-------------------------------------//
    public void StopSmallBeatsAnim()
    //-------------------------------------//
    {
        shouldAnimateBeating = false;

    } // END StopSmallBeatsAnim


    // Small beat callback for the first beat (always if we are beating)
    //-------------------------------------//
    public void SmallBeatCallback1()
    //-------------------------------------//
    {
        if (shouldAnimateBeating)
        {
            PulseSmallBeat();
        }

    } // END SmallBeatCallback1


    // Small beat callback for the second beat (only if we are beating and greatly injured)
    //-------------------------------------//
    public void SmallBeatCallback2()
    //-------------------------------------//
    {
        if (parentHealthDisplay == null)
            return;

        if (shouldAnimateBeating && parentHealthDisplay.remainingHealth <= 2)
        {
            PulseSmallBeat();
        }

    } // END SmallBeatCallback2


    // Pulses a single small beat
    // Should be called automatically via event callbacks
    //-------------------------------------//
    public void PulseSmallBeat()
    //-------------------------------------//
    {
        // Size
        LeanTween.value(enabledImg.gameObject, Vector3.one * HealthBeatEventHandler.smallBeatScale, Vector3.one, HealthBeatEventHandler.smallBeatDuration)
        .setEaseInQuad()
        .setOnUpdate((Vector3 val) =>
        {
            enabledImg.transform.localScale = val;
        });

        // Color
        Color halfwayColor = Color.Lerp(glowingColor, standardColor, 0.8f);
        LeanTween.value(enabledImg.gameObject, halfwayColor, standardColor, HealthBeatEventHandler.smallBeatDuration)
        .setEaseInQuad()
        .setOnUpdate((Color val) =>
        {
            enabledImg.color = val;
        });

    } // END PulseSmallBeat


    #endregion


    #region LOST HALF


    // Spawns the lost right half resource prefab (visual animation when right half is lost)
    //-------------------------------------//
    private void SpawnLostRightHalf()
    //-------------------------------------//
    {
        LostHealthHandler lostRightHalf = Instantiate(lostHealthPrefab, transform);
        lostRightHalf.FlingAndDestroy(LostHealthHandler.LostResourceType.RightHalf, enabledRightHalfSprite, standardColor);

    } // END SpawnLostRightHalf


    // Spawns the lost left half resource prefab (visual animation when left half is lost)
    //-------------------------------------//
    private void SpawnLostLeftHalf()
    //-------------------------------------//
    {
        LostHealthHandler lostLeftHalf = Instantiate(lostHealthPrefab, transform);
        lostLeftHalf.FlingAndDestroy(LostHealthHandler.LostResourceType.LeftHalf, enabledLeftHalfSprite, standardColor);

    } // END SpawnLostLeftHalf


    // Spawns the lost full resource prefab (visual animation when full resource is lost)
    //-------------------------------------//
    private void SpawnLostResourceFull()
    //-------------------------------------//
    {
        LostHealthHandler lostFull = Instantiate(lostHealthPrefab, transform);
        lostFull.FlingAndDestroy(LostHealthHandler.LostResourceType.Full, enabledFullSprite, standardColor);

    } // END SpawnLostResourceFull


    #endregion


} // END ResourceSingleDisplay
