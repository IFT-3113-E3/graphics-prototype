using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    public int damage = 10; // Dégâts de l'attaque
    public float lifetime = 1f; // Durée de vie de l'attaque
    public Vector3 size = Vector3.one; // Taille de la hitbox
    public float speed = 5f; // Vitesse de déplacement de l'attaque
    public AttackType attackType = AttackType.Normal; // Type d'attaque

    private Vector3 direction; // Direction de l'attaque
    private EnemyManager enemyManager; // Référence à EnemyManager

    void Start()
    {
        // Appliquer la taille de la hitbox
        transform.localScale = size;

        // Détruire l'attaque après un certain temps
        Destroy(gameObject, lifetime);

        // Définir la direction de l'attaque (vers le joueur)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            direction = (player.transform.position - transform.position).normalized;
        }
        
        enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager == null)
        {
            Debug.LogWarning("EnemyManager non trouvé dans la scène !");
        }
    }

    void Update()
    {
        // Déplacer l'attaque
        transform.Translate(direction * speed * Time.deltaTime);
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collision détectée avec : {other.name}");

        // Vérifier si l'attaque touche le joueur
        if (other.CompareTag("Player"))
        {
            Debug.Log("Le joueur a été touché !");

            EntityStats playerStats = other.GetComponent<EntityStats>();
            if (playerStats != null)
            {
                // Appliquer les effets en fonction du type d'attaque
                switch (attackType)
                {
                    case AttackType.Fire:
                        playerStats.ApplyEffect(new StatusEffect(
                            "Fire", 5f, dps: 5 // 5 dégâts par seconde pendant 5 secondes
                        ));
                        break;

                    case AttackType.Knockback:
                        playerStats.ApplyEffect(new StatusEffect(
                            "Knockback", 0.5f, knockback: new Vector3(-5, 0, 0) // Recule le joueur de 5 unités sur l'axe X
                        ));
                        break;

                    case AttackType.Heal:
                        playerStats.Heal(30); // Soigne le joueur de 30 points de vie
                        break;

                    case AttackType.Normal:
                    default:
                        playerStats.TakeDamage(damage); // Attaque normale
                        break;
                }
                if (enemyManager != null)
                {
                    enemyManager.UpdatePlayerStatsDisplay();
                }
            }
            else
            {
                Debug.LogWarning("Le joueur n'a pas de composant EntityStats !");
            }
            Destroy(gameObject);
        }
    }
}