using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MinibossRushHandler : MonoBehaviour
{
    
    // MinibossRushHandler handles the spawning and management of miniboss rush events in the game.


    #region VARIABLES


    [SerializeField] private List<EnemyBase> minibosses = new List<EnemyBase>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool spawnOnStart = false;

    private List<EnemyBase> activeMinibosses = new List<EnemyBase>();
    private int minibossCount = 3;


    #endregion


    #region SPAWN


    void Start()
    {
        if (spawnOnStart)
        {
            StartMinibossRush();
        }
    }


    // Spawns 3 random minibosses at 3 random spawn points
    public void StartMinibossRush()
    {
        for (int i = 0; i < minibossCount; i++)
        {
            // Pick a random miniboss and spawn point
            EnemyBase minibossToSpawn = minibosses[Random.Range(0, minibosses.Count)];
            minibosses.Remove(minibossToSpawn); // Ensure unique minibosses
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Count)];
            spawnPoints.Remove(spawnPoint); // Ensure unique spawn points
            EnemyBase newMiniboss = Instantiate(minibossToSpawn, spawnPoint.position, Quaternion.identity);

            // Subscribe to miniboss death event to check when all are defeated
            newMiniboss.onDeathEvent.AddListener(OnMinibossDefeated);
            activeMinibosses.Add(newMiniboss);
        }

        StartCoroutine(WaitThenAlertCo());
    }


    private IEnumerator WaitThenAlertCo()
    {
        yield return new WaitForSeconds(1f);

        foreach (EnemyBase miniboss in activeMinibosses)
        {
            miniboss.BeginPlayerChase();
        }
    }


    #endregion


    #region RUSH COMPLETION


    // Called when a miniboss is defeated, check if criteria are met for rush completion
    public void OnMinibossDefeated()
    {
        minibossCount--;

        // Check if all minibosses are defeated
        if (minibossCount == 0)
        {
            // All minibosses defeated, complete the rush
            StartCoroutine(RushCompletionCo());
        }
    }


    private IEnumerator RushCompletionCo()
    {
        yield return new WaitForSeconds(5f);

        LevelManager.Instance.LoadScene(LevelManager.Scenes.MainScene, LevelManager.LoadEffect.Portal);
    }


    #endregion

}
