using UnityEngine;

public class PlayerRespawnManager : MonoBehaviour
{
    public GameObject playerPrefab; // Préfab du joueur
    public Vector3 defaultPosition; // Position par défaut du joueur

    private GameObject currentPlayer; // Référence au joueur actuel
    private EnemyManager enemyManager; // Référence à EnemyManager

    void Start()
    {
        enemyManager = FindObjectOfType<EnemyManager>();
        SpawnPlayer();
    }

    public void OnPlayerDeath()
    {
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
        }
        Invoke("SpawnPlayer", 3f);
    }

    void SpawnPlayer()
    {
        currentPlayer = Instantiate(playerPrefab, defaultPosition, Quaternion.identity, transform);
        EntityStats playerStats = currentPlayer.GetComponent<EntityStats>();
        if (playerStats != null)
        {
            playerStats.SetRespawnManager(this);
            if (enemyManager != null)
            {
                enemyManager.SetPlayerStats(playerStats);
            }
        }

        Debug.Log("Le joueur a réapparu.");
    }
}