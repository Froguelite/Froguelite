using System.Collections;
using UnityEngine;

public class SubZoneFinalDoor : MonoBehaviour
{

    // SubZoneFinalDoor represents a door that allows travel between sub-zones in the game.


    #region VARIABLES

    public Door.DoorDirection doorDirection;

    private bool isOpen = false;

    [SerializeField] private SpriteRenderer frogRenderer;
    [SerializeField] private SpriteMask frogMask;
    [SerializeField] private ParticleSystem rippleParticles;

    [SerializeField] private Sprite spriteUp;
    [SerializeField] private Sprite spriteDown;
    [SerializeField] private Sprite spriteLeft;
    [SerializeField] private Sprite spriteRight;

    // Floating animation variables
    private Vector3 frogRendShownPos;
    private Vector3 frogRendHiddenPos;
    private float floatSpeed = 1f;
    private float floatRange = 0.3f;
    private float timeOffset;
    private bool inShownPosition = true;


    #endregion


    #region MONOBEHAVIOUR

    private void Update()
    {
        if (frogMask != null)
        {
            FloatFrog();
        }
    }


    #endregion


    #region DATA MANAGEMENT


    // Initializes door with given data
    public void InitializeDoor(Door.DoorDirection direction)
    {
        doorDirection = direction;

        frogRendShownPos = frogRenderer.transform.localPosition;
        frogRendHiddenPos = frogRendShownPos - new Vector3(0, 1, 0);
        timeOffset = Random.Range(0f, 2f * Mathf.PI); // Random starting phase for variety

        UpdateDoorVisuals(false);
    }


    #endregion


    #region SPAWNING AND VISUALS


    // Updates the door's visual representation based on its current state (open, closed, locked, etc.)
    public void UpdateDoorVisuals(bool animate = true)
    {
        // Set sprites to correct direction and locked / unlocked
        switch (doorDirection)
        {
            case Door.DoorDirection.Up:
                frogRenderer.sprite = spriteUp;
                break;
            case Door.DoorDirection.Down:
                frogRenderer.sprite = spriteDown;
                break;
            case Door.DoorDirection.Left:
                frogRenderer.sprite = spriteLeft;
                break;
            case Door.DoorDirection.Right:
                frogRenderer.sprite = spriteRight;
                break;
        }

        // Show or hide the frog accordingly
        if (isOpen)
        {
            if (inShownPosition) return;

            inShownPosition = true;
            LeanTween.cancel(frogRenderer.gameObject);

            frogRenderer.enabled = true;
            rippleParticles.Play();

            if (animate)
            {
                frogRenderer.transform.LeanMoveLocal(frogRendShownPos, 0.5f).setEaseOutSine();
            }
            else
            {
                frogRenderer.transform.localPosition = frogRendShownPos;
            }
        }
        else
        {
            if (!inShownPosition) return;

            inShownPosition = false;
            LeanTween.cancel(frogRenderer.gameObject);
            rippleParticles.Stop();

            if (animate)
            {
                frogRenderer.transform.LeanMoveLocal(frogRendHiddenPos, 0.5f).setEaseInSine().setOnComplete(() =>
                {
                    frogRenderer.enabled = false;
                });
            }
            else
            {
                frogRenderer.transform.localPosition = frogRendHiddenPos;
                frogRenderer.enabled = false;
            }
        }
    }

    // Makes the frog renderer float around its original position in the water
    private void FloatFrog()
    {
        // Create smooth floating motion using sine waves
        float x = Mathf.Sin((Time.time + timeOffset) * floatSpeed) * floatRange;
        float y = Mathf.Sin((Time.time + timeOffset) * floatSpeed * 1.3f) * floatRange * 0.7f;

        // Apply the floating offset as local position
        frogMask.transform.localPosition = new Vector3(x, y, 0);
    }


    #endregion


    #region OPENING AND LOCKING


    // Opens the door if it's not impassable
    public void OpenDoor()
    {
        isOpen = true;
        UpdateDoorVisuals();
    }

    // Closes the door if it's not impassable
    public void CloseDoor()
    {
        isOpen = false;
        UpdateDoorVisuals();
    }


    #endregion


    #region TRAVEL


    // Initiates the travel process through the door if it's open and not locked
    public void OnInteract()
    {
        if (GameManager.Instance.currentPlayerState == GameManager.PlayerState.Exploring && isOpen)
        {
            GameManager.Instance.SetPlayerState(GameManager.PlayerState.InBubble);
            DoorManager.Instance.OnTravelStarted();

            // Make it so the player cannot move or attack, and suck the player into the bubble
            PlayerAttack.Instance.StopTongueExtension(true);
            PlayerAttack.Instance.SetCanAttack(false);
            PlayerMovement.Instance.SetCanMove(false);
            PlayerMovement.Instance.DisableCollision();

            PlayerMovement.Instance.onReachManualMoveTarget.AddListener(() =>
            {
                // Make sure the player is exactly inside the frog, and parent them to it so they move with it
                PlayerMovement.Instance.transform.position = frogRenderer.transform.position + new Vector3(0, 0.75f, 0);
                PlayerMovement.Instance.transform.SetParent(frogRenderer.transform);

                // Start the sinking and spinning sequence
                StartCoroutine(SinkAndSpinSequence());
            });
        }
    }

    // Coroutine that handles the whirlpool-like sinking and spinning animation
    private IEnumerator SinkAndSpinSequence()
    {
        PlayerMovement.Instance.playerSpriteRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // Calculate the final sink position (twice as far down as normal hidden position)
        Vector3 startPos = frogRenderer.transform.localPosition;
        Vector3 sinkPos = frogRendShownPos - new Vector3(0, 1, 0);
        
        //float initialSpinDuration = 1f; // Time to spin before sinking starts
        float sinkDuration = 4f; // Total time to sink
        float spinInterval = 0.2f; // Time between sprite direction changes
        
        // Array of directions to cycle through (creates the spinning effect)
        Door.DoorDirection[] directions = new Door.DoorDirection[]
        {
            Door.DoorDirection.Up,
            Door.DoorDirection.Right,
            Door.DoorDirection.Down,
            Door.DoorDirection.Left
        };
        
        // Find the starting direction index
        int currentDirectionIndex = System.Array.IndexOf(directions, doorDirection);
        if (currentDirectionIndex == -1) currentDirectionIndex = 0;
        
        // Phase 1: Initial spinning without sinking
        float elapsedTime = 0f;
        float nextSpinTime = spinInterval;
        
        // Phase 2: Sinking while spinning
        elapsedTime = 0f;
        
        while (elapsedTime < sinkDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Smoothly interpolate the position downward with easing
            float t = elapsedTime / sinkDuration;
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            frogRenderer.transform.localPosition = Vector3.Lerp(startPos, sinkPos, easedT);
            
            yield return null;
        }
        
        // Ensure we end at the exact position
        frogRenderer.transform.localPosition = sinkPos;
        PlayerMovement.Instance.transform.SetParent(null);
        DontDestroyOnLoad(PlayerMovement.Instance.gameObject);

        //Suppress await warning _=
        _= LevelManager.Instance.LoadScene(LevelManager.Scenes.MainScene, LevelManager.LoadEffect.Bubble);
        SaveManager.WriteToFile(); //Save while bubble transition and after completing a sub-zone
    }


    #endregion


}
