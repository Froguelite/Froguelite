using UnityEngine;

public class PlayerAttack : MonoBehaviour
{

    // PlayerAttack handles the player's tongue attack mechanics


    #region VARIABLES


    public static PlayerAttack Instance { get; private set; }

    [Header("Tongue Settings")]
    [SerializeField] Transform tongue;
    [SerializeField] float tongueDistance = 3f;
    [SerializeField] float tongueSpeed = 10f;

    private Vector3 targetPosition;
    private bool isExtending = false;
    private bool isRetracting = false;


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
    }


    void Update()
    {
        HandleTongueAttack();
    }


    #endregion


    #region TONGUE ATTACK


    // If possible, initiate attack
    public void OnInitiateAttack()
    {
        if (!isExtending && !isRetracting)
        {
            Debug.Log("Tongue attack started!");
            StartTongueAttack();
        }
    }


    // Starts the tonuge attack
    void StartTongueAttack()
    {
        // Set the tongue's initial position relative to the player
        Vector3 initialPosition = tongue.position;

        // Get mouse position in world space
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;

        // Calculate the target position
        Vector3 direction = (mousePosition - initialPosition).normalized;
        targetPosition = initialPosition + direction * tongueDistance;

        isExtending = true;
    }


    // Handles the tongue attack, extending or retracting as required
    public void HandleTongueAttack()
    {
        if (isExtending)
        {
            tongue.position = Vector3.MoveTowards(tongue.position, targetPosition, tongueSpeed * Time.deltaTime);
            if (Vector3.Distance(tongue.position, targetPosition) < 0.01f)
            {
                Debug.Log("Tongue reached target position!");
                isExtending = false;
                isRetracting = true;
            }
        }
        else if (isRetracting)
        {
            // Dynamically calculate the player's current position as the retract target
            Vector3 playerPosition = transform.position;
            Vector3 retractDirection = (playerPosition - tongue.position).normalized; // Normalize direction
            tongue.position += retractDirection * tongueSpeed * Time.deltaTime; // Move at consistent speed

            if (Vector3.Distance(tongue.position, playerPosition) < 0.01f)
            {
                Debug.Log("Tongue retracted to player's position!");
                isRetracting = false;
            }
        }
    }


    #endregion


}