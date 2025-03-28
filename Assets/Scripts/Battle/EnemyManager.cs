using UnityEngine;
using TMPro;

public enum AttackType
{
    Normal, // Attaque normale
    Fire,   // Attaque de feu (dégâts par seconde)
    Knockback, // Attaque de knockback (recul)
    Heal    // Attaque de soin
}

[System.Serializable]
public class Enemy
{
    public string name;
    public int health;
    public int defense;
    public int attack;
    public float attackDuration;
    public Vector3 attackSize = Vector3.one;
}

public class EnemyManager : MonoBehaviour
{
    public Enemy[] enemies; // Liste des ennemis disponibles
    public TMP_Dropdown enemyDropdown; // Référence au menu déroulant TMP
    public TextMeshProUGUI enemyStatsText; // Référence au texte des stats TMP du monstre
    public TextMeshProUGUI playerStatsText; // Référence au texte des stats TMP du joueur
    public EntityStats playerStats; // Référence aux stats du joueur
    public GameObject attackHitboxPrefab; // Préfab de la hitbox d'attaque
    private Enemy selectedEnemy; // Ennemi actuellement sélectionné

    public GameObject monsterObject; // Référence à l'objet du monstre

    void Start()
    {
        // Remplir le dropdown avec les noms des ennemis
        enemyDropdown.ClearOptions();
        foreach (Enemy enemy in enemies)
        {
            enemyDropdown.options.Add(new TMP_Dropdown.OptionData(enemy.name));
        }

        // Sélectionner le premier ennemi par défaut
        SelectEnemy(0);
        enemyDropdown.onValueChanged.AddListener(SelectEnemy);

        // Afficher les stats initiales du joueur
        UpdatePlayerStatsDisplay();
    }

    void SelectEnemy(int index)
    {
        selectedEnemy = enemies[index];
        UpdateStatsDisplay();
    }

    void UpdateStatsDisplay()
    {
        enemyStatsText.text = $"Nom: {selectedEnemy.name}\n" +
                            $"Vie: {selectedEnemy.health}\n" +
                            $"Défense: {selectedEnemy.defense}\n" +
                            $"Attaque: {selectedEnemy.attack}\n" +
                            $"Durée de l'attaque: {selectedEnemy.attackDuration}";
    }

    public void UpdatePlayerStatsDisplay()
    {
        if (playerStats != null && playerStatsText != null)
        {
            playerStatsText.text = $"Joueur:\n" +
                                $"Vie: {playerStats.currentHealth} / {playerStats.maxHealth}\n" +
                                $"Défense: {playerStats.defense}\n" +
                                $"Attaque: {playerStats.attack}";
        }
        else
        {
            Debug.LogWarning("PlayerStats ou PlayerStatsText non défini !");
        }
    }

    public void SetPlayerStats(EntityStats newPlayerStats)
    {
        playerStats = newPlayerStats;
        UpdatePlayerStatsDisplay();
    }

    public void LaunchNormalAttack()
    {
        LaunchAttack(AttackType.Normal);
    }

    public void LaunchFireAttack()
    {
        LaunchAttack(AttackType.Fire);
    }

    public void LaunchKnockbackAttack()
    {
        LaunchAttack(AttackType.Knockback);
    }

    public void LaunchHealAttack()
    {
        LaunchAttack(AttackType.Heal);
    }

    private void LaunchAttack(AttackType attackType)
    {
        if (selectedEnemy == null || attackHitboxPrefab == null || monsterObject == null)
        {
            Debug.LogWarning("Ennemi, préfab d'attaque ou objet monstre non défini !");
            return;
        }

        // Instancier l'attaque à la position du monstre
        GameObject attack = Instantiate(attackHitboxPrefab, monsterObject.transform.position, Quaternion.identity);

        // Configurer les dégâts, la taille, la durée et le type de l'attaque
        AttackHitbox hitbox = attack.GetComponent<AttackHitbox>();
        if (hitbox != null)
        {
            hitbox.damage = selectedEnemy.attack;
            hitbox.size = selectedEnemy.attackSize; // Appliquer la taille de l'attaque
            hitbox.lifetime = selectedEnemy.attackDuration; // Appliquer la durée de vie de l'attaque
            hitbox.attackType = attackType; // Définir le type d'attaque
        }
    }
}