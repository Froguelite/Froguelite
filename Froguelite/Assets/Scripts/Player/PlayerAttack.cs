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
    [SerializeField] PlayerMovement movement;
    [SerializeField] Rigidbody2D rb;
    RigidbodyConstraints2D savedConstraints;  

    private Vector3 targetPosition;
    private bool isExtending = false;
    private bool isRetracting = false;


    #endregion


    #region MONOBEHAVIOUR AND SETUP


    void Awake()
    {   if (rb == null) rb = GetComponent<Rigidbody2D>();            
        if (rb) savedConstraints = rb.constraints; 
        if (movement == null) movement = GetComponent<PlayerMovement>();
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
        if (movement) movement.enabled = false;
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;              
            rb.angularVelocity = 0f;
            rb.constraints = savedConstraints 
                | RigidbodyConstraints2D.FreezePosition;  // freeze position
        }
        // Set the tongue's initial position relative to the player
        Vector3 initialPosition = transform.position;
        tongue.position = initialPosition;  
        float depthFromCam = Camera.main.orthographic
                ? 0f
                : Mathf.Abs(initialPosition.z - Camera.main.transform.position.z);

            Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, depthFromCam)
            );

            mouseWorld.z = initialPosition.z;

            Vector3 dir = (mouseWorld - initialPosition).normalized;
            targetPosition = initialPosition + dir * tongueDistance;
            targetPosition.z = tongue.position.z;   // lock Z

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
            Vector3 playerPosition = transform.position;
            playerPosition.z = tongue.position.z; //lock z

            tongue.position = Vector3.MoveTowards(
                tongue.position, playerPosition,
                tongueSpeed * Time.deltaTime);

            if (Vector3.Distance(tongue.position, playerPosition) < 0.01f)
            {
                Debug.Log("Tongue retracted to player's position!");
                isRetracting = false;
                if (movement) movement.enabled = true;
                if (rb) rb.constraints = savedConstraints;   
            }
        }
    }


    #endregion


}