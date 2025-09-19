using UnityEngine;

public class tongueAttack : MonoBehaviour
{
    [Header("Tongue Settings")]
    [SerializeField] Transform tongue; 
    [SerializeField] float tongueDistance = 3f;
    [SerializeField] float tongueSpeed = 10f;

    private Vector3 targetPosition;
    private bool isExtending = false;
    private bool isRetracting = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isExtending && !isRetracting)
        {
            Debug.Log("Tongue attack started!");
            StartTongueAttack();
        }

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
}