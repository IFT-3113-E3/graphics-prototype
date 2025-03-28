using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EntityStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int defense = 10;
    public int attack = 20;
    public float moveSpeed = 5f;

    private List<StatusEffect> activeEffects = new List<StatusEffect>();
    private float fireDamageCooldown = 0f;

    private PlayerRespawnManager respawnManager;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void SetRespawnManager(PlayerRespawnManager manager)
    {
        respawnManager = manager;
    }

    void Update()
    {
        ApplyEffects();
    }

    public void TakeDamage(int damage)
    {
        int damageTaken = Mathf.Max(damage - defense, 0);
        currentHealth = Mathf.Max(currentHealth - damageTaken, 0);
        Debug.Log($"{gameObject.name} a pris {damageTaken} dégâts. Vie restante : {currentHealth}");
        UpdateHealthDisplay();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Debug.Log($"{gameObject.name} a été soigné de {amount} points de vie. Vie restante : {currentHealth}");
        UpdateHealthDisplay();
    }

    public void ApplyEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        Debug.Log($"Effet appliqué : {effect.effectName}");
    }

    void ApplyEffects()
    {
        fireDamageCooldown -= Time.deltaTime;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            if (effect.damagePerSecond > 0 && fireDamageCooldown <= 0f)
            {
                // Appliquer les dégâts de feu une fois par seconde
                currentHealth = Mathf.Max(currentHealth - effect.damagePerSecond, 0); // Empêcher les HP de descendre en dessous de 0
                Debug.Log($"{gameObject.name} a pris {effect.damagePerSecond} dégâts de feu. Vie restante : {currentHealth}");
                UpdateHealthDisplay();

                fireDamageCooldown = 1f;

                // Vérifier si le joueur est mort après avoir appliqué les dégâts de feu
                if (currentHealth <= 0)
                {
                    Die();
                    break; // Sortir de la boucle pour éviter d'appliquer d'autres effets
                }
            }

            if (effect.knockbackForce != Vector3.zero)
            {
                ApplyKnockback(effect.knockbackForce); // Appliquer le knockback
            }
            if (effect.healAmount > 0)
            {
                Heal(effect.healAmount); // Appliquer le soin
            }

            effect.duration -= Time.deltaTime;
            if (effect.duration <= 0)
            {
                activeEffects.RemoveAt(i); // Supprimer l'effet terminé
                Debug.Log($"Effet terminé : {effect.effectName}");
            }
        }
    }

    void ApplyKnockback(Vector3 force)
    {
        // Appliquer une force de knockback
        transform.position -= force * Time.deltaTime;
    }

    void UpdateHealthDisplay()
    {
        // Mettre à jour l'affichage des points de vie dans l'UI
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            enemyManager.UpdatePlayerStatsDisplay();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} est mort.");
        activeEffects.Clear();
        if (respawnManager != null)
        {
            respawnManager.OnPlayerDeath();
        }
        Destroy(gameObject);
    }
}