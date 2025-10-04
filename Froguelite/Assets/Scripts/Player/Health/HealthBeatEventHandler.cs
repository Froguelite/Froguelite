using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HealthBeatEventHandler : MonoBehaviour
{

    // HealthBeatEventHandler handles the events related to health beating, sending out the major beat events


    #region VARIABLES


    public static HealthBeatEventHandler Instance;

    public UnityEvent smallBeatEvent1 { get; private set; } = new UnityEvent();
    public UnityEvent smallBeatEvent2 { get; private set; } = new UnityEvent();

    public const float smallBeatSeparation = .75f;
    
    public const float bigBeatScale = 1.5f;
    public const float smallBeatScale = 1.1f;

    public const float bigBeatDuration = 1f;
    public const float smallBeatDuration = .2f;


    #endregion


    #region EVENTS


    // Awake
    //-------------------------------------//
    void Awake()
    //-------------------------------------//
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        StartCoroutine(TriggerSmallBeatEvents());

    } // END Awake


    // Triggers small beat events (1 is standard, 2 is double-time)
    //-------------------------------------//
    private IEnumerator TriggerSmallBeatEvents()
    //-------------------------------------//
    {
        while (true)
        {
            smallBeatEvent1.Invoke();
            yield return new WaitForSeconds(smallBeatSeparation / 2f);
            smallBeatEvent2.Invoke();
            yield return new WaitForSeconds(smallBeatSeparation / 2f);
        }

    } // END TriggerSmallBeatEvents


    #endregion


} // END ResourceBeatEventHandler.cs
