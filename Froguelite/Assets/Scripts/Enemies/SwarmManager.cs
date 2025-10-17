using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public class SwarmManager : MonoBehaviour
{

    // SwarmManager manages the swarm state for any swarm-type enemies


    #region VARIABLES


    public class SwarmCenter
    {
        [System.Serializable]
        public class SwarmInfo
        {
            public string swarmName; // The name of this swarm, for use in identification
            public float swarmTargetDistance; // How close this swarm center should get to the player
            public float centerMoveSpeed = 2f; // How fast the swarm center moves toward its target position

            public bool circleUponReachingPlayer = false; // If true, the swarm will circle around the player when within target distance
            public float circleSpeed = 1f; // Speed of circling movement (radians per second)

            public float minTimeBetweenSwarmActions = 3.5f; // Minimum time between swarm actions (such as attacks)
            public float actionIntervalScalePerEnemy = 0.7f; // Scaling factor for time between swarm actions per enemy in swarm
            public bool actionTriggersOnAll = false; // If true, all enemies in the swarm will trigger actions simultaneously
        }

        public SwarmInfo swarmInfo { get; private set; }

        public Transform swarmTargetTransform = null; // The transform of this swarm center, for enemies to follow
        public NavMeshAgent swarmNavAgent = null; // The NavMeshAgent of this swarm center

        private float currentCircleAngle = 0f; // Current angle in the circle
        private bool isCircling = false; // Whether the swarm is currently circling

        public List<EnemyBehavior_Swarm> swarmEnemies = new List<EnemyBehavior_Swarm>();
        public float timeSinceLastSwarmAction = 0f; // Time since last swarm action (such as attack) occurred

        public void SetSwarmInfo(SwarmInfo info)
        {
            swarmInfo = info;
        }

        public void UpdateSwarmBehavior(float deltaTime)
        {
            UpdateSwarmNavTarget();
            CheckForSwarmAction(deltaTime);
        }

        public void UpdateSwarmNavTarget()
        {
            if (swarmNavAgent != null && PlayerMovement.Instance != null)
            {
                Vector3 playerPosition = PlayerMovement.Instance.transform.position;
                Vector3 swarmPosition = swarmNavAgent.transform.position;
                float distanceToPlayer = Vector3.Distance(swarmPosition, playerPosition);

                // Check if we should start or continue circling
                if (swarmInfo.circleUponReachingPlayer && distanceToPlayer <= swarmInfo.swarmTargetDistance * 1.2f) // Small buffer to prevent flickering
                {
                    if (!isCircling)
                    {
                        // Start circling - calculate initial angle based on current position in XY plane (2D)
                        Vector3 directionFromPlayer = (swarmPosition - playerPosition).normalized;
                        currentCircleAngle = Mathf.Atan2(directionFromPlayer.y, directionFromPlayer.x);

                        // Greatly reduce acceptance tolerance for the navigation
                        swarmNavAgent.stoppingDistance = 0.2f;
                        swarmNavAgent.speed = 20f; // Increase speed for circling
                        isCircling = true;
                    }

                    // Update circle angle
                    currentCircleAngle += swarmInfo.circleSpeed * Time.deltaTime;
                    
                    // Calculate target position on the circle (XY plane for 2D, Z stays same as player)
                    Vector3 circleOffset = new Vector3(
                        Mathf.Cos(currentCircleAngle) * swarmInfo.swarmTargetDistance,
                        Mathf.Sin(currentCircleAngle) * swarmInfo.swarmTargetDistance,
                        0f
                    );
                    
                    Vector3 circleTarget = playerPosition + circleOffset;
                    swarmNavAgent.SetDestination(circleTarget);
                }
                else
                {
                    // Move directly toward player
                    isCircling = false;
                    swarmNavAgent.stoppingDistance = swarmInfo.swarmTargetDistance;
                    swarmNavAgent.speed = swarmInfo.centerMoveSpeed;
                    swarmNavAgent.SetDestination(playerPosition);
                }
            }
        }

        public void AddEnemyToSwarm(EnemyBehavior_Swarm enemy)
        {
            if (!swarmEnemies.Contains(enemy))
            {
                swarmEnemies.Add(enemy);
            }
        }

        public void RemoveEnemyFromSwarm(EnemyBehavior_Swarm enemy)
        {
            if (swarmEnemies.Contains(enemy))
            {
                swarmEnemies.Remove(enemy);
            }
        }

        private void CheckForSwarmAction(float deltaTime)
        {
            timeSinceLastSwarmAction += deltaTime;

            // Calculate adjusted time between actions based on number of enemies in swarm
            float adjustedTimeBetweenActions = swarmInfo.minTimeBetweenSwarmActions;
            adjustedTimeBetweenActions *= Mathf.Pow(swarmInfo.actionIntervalScalePerEnemy, swarmEnemies.Count - 1);

            if (timeSinceLastSwarmAction >= adjustedTimeBetweenActions)
            {
                // Trigger swarm action
                if (swarmInfo.actionTriggersOnAll)
                {
                    foreach (EnemyBehavior_Swarm enemy in swarmEnemies)
                    {
                        if (!enemy.ReadyToTriggerSwarmAction())
                            continue;

                        enemy.TriggerSwarmAction();
                    }

                    timeSinceLastSwarmAction = 0f;
                    return;
                }
                else if (swarmEnemies.Count > 0)
                {
                    // Trigger action on a random enemy in the swarm
                    int randomIndex = Random.Range(0, swarmEnemies.Count);
                    if (swarmEnemies[randomIndex].ReadyToTriggerSwarmAction())
                    {
                        swarmEnemies[randomIndex].TriggerSwarmAction();
                        timeSinceLastSwarmAction = 0f;
                        return;
                    }
                    else
                    {
                        // Find another enemy that is not currently triggering an action
                        for (int i = 0; i < swarmEnemies.Count; i++)
                        {
                            int indexToCheck = (randomIndex + i) % swarmEnemies.Count;
                            if (swarmEnemies[indexToCheck].ReadyToTriggerSwarmAction())
                            {
                                swarmEnemies[indexToCheck].TriggerSwarmAction();
                                timeSinceLastSwarmAction = 0f;
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    public static SwarmManager Instance { get; private set; }

    public List<SwarmCenter> activeSwarms { get; private set; } = new List<SwarmCenter>();

    [SerializeField] private NavMeshAgent swarmCenterNavPrefab;


    #endregion


    #region MONOBEHAVIOUR


    // Awake
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }


    // Update
    void Update()
    {
        UpdateSwarms();
    }


    #endregion


    #region SWARM INIT AND GET


    // Gets the SwarmCenter for a given swarm name, or null if not found
    public SwarmCenter GetSwarmCenter(string swarmName)
    {
        SwarmCenter swarm = activeSwarms.Find(s => s.swarmInfo.swarmName == swarmName);
        return swarm;
    }


    // Adds a new swarm to the active swarms list, with initial center set to be between all enemies of given type
    public SwarmCenter AddSwarmCentered(SwarmCenter.SwarmInfo swarmInfo, string swarmId, Room room)
    {
        List<Vector3> positionsToUse = new List<Vector3>();

        // TEMPORARY TODO: loop through ALL enemies
        IEnemy[] allEnemies = FindObjectsByType<EnemyBase>(FindObjectsSortMode.None);
        foreach (IEnemy enemy in allEnemies)
        {
            if (enemy.GetEnemyId() == swarmId)
            {
                positionsToUse.Add(((EnemyBase)enemy).transform.position);
            }
        }
        /*foreach (IEnemy enemy in room.enemies)
        {
            if (enemy.GetEnemyId() == swarmId)
            {
                positionsToUse.Add(((EnemyBase)enemy).transform.position);
            }
        }*/

        if (positionsToUse.Count > 0)
        {
            // Calculate the average position to use as the swarm center
            Vector3 averagePosition = Vector3.zero;
            foreach (Vector3 pos in positionsToUse)
            {
                averagePosition += pos;
            }
            averagePosition /= positionsToUse.Count;

            return AddSwarm(swarmInfo, averagePosition);
        }
        else
        {
            return AddSwarm(swarmInfo, PlayerMovement.Instance.transform.position);
        }
    }


    // Adds a new swarm to the active swarms list
    public SwarmCenter AddSwarm(SwarmCenter.SwarmInfo swarmInfo, Vector3 initialCenter)
    {
        // Make sure another swarm doesn't exist by this name, if it does, remove it
        SwarmCenter existingSwarm = GetSwarmCenter(swarmInfo.swarmName);
        if (existingSwarm != null)
        {
            activeSwarms.Remove(existingSwarm);
        }

        NavMeshAgent swarmCenterAgent = Instantiate(swarmCenterNavPrefab, initialCenter, Quaternion.identity);
        swarmCenterAgent.transform.parent = this.transform;
        swarmCenterAgent.gameObject.name = "SwarmCenter_" + swarmInfo.swarmName;

        swarmCenterAgent.stoppingDistance = swarmInfo.swarmTargetDistance;
        swarmCenterAgent.speed = swarmInfo.centerMoveSpeed;

        // Add the new swarm
        SwarmCenter newSwarm = new SwarmCenter
        {
            swarmTargetTransform = swarmCenterAgent.transform,
            swarmNavAgent = swarmCenterAgent
        };

        newSwarm.SetSwarmInfo(swarmInfo);

        activeSwarms.Add(newSwarm);
        return newSwarm;
    }


    #endregion


    #region SWARM MOVEMENT


    // Updates the swarms (time since last action and navigation behavior)
    public void UpdateSwarms()
    {
        foreach (SwarmCenter swarm in activeSwarms)
        {
            swarm.UpdateSwarmBehavior(Time.deltaTime);
        }
    }


    #endregion


}
