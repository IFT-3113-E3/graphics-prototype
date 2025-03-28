using System.Collections.Generic;
using UnityEngine;

public class StatusEffect
{
    public string effectName; // Nom de l'effet
    public float duration; // Durée de l'effet
    public int damagePerSecond; // Dégâts par seconde (pour le feu)
    public Vector3 knockbackForce; // Force de knockback
    public int healAmount; // Montant de soin instantané
    public bool isStunned; // Si l'effet étourdit

    public StatusEffect(string name, float dur, int dps = 0, Vector3 knockback = default, int heal = 0, bool stunned = false)
    {
        effectName = name;
        duration = dur;
        damagePerSecond = dps;
        knockbackForce = knockback;
        healAmount = heal;
        isStunned = stunned;
    }
}

public class EntityEffects : MonoBehaviour
{
    private List<StatusEffect> activeEffects = new List<StatusEffect>();

    void Update()
    {
        ApplyEffects();
    }

    public void AddEffect(StatusEffect effect)
    {
        activeEffects.Add(effect);
        Debug.Log("Effet appliqué : " + effect.effectName);
    }

    void ApplyEffects()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeEffects[i];

            // Appliquer les effets
            if (effect.damagePerSecond > 0)
            {
                GetComponent<EntityStats>().TakeDamage(effect.damagePerSecond); // Dégâts par seconde (feu)
            }
            if (effect.knockbackForce != Vector3.zero)
            {
                ApplyKnockback(effect.knockbackForce); // Appliquer le knockback
            }
            if (effect.healAmount > 0)
            {
                GetComponent<EntityStats>().Heal(effect.healAmount); // Soin instantané
            }
            // if (effect.isStunned)
            // {
            // }
            effect.duration -= Time.deltaTime;
            if (effect.duration <= 0)
            {
                activeEffects.RemoveAt(i);
                Debug.Log("Effet terminé : " + effect.effectName);
            }
        }
    }

    void ApplyKnockback(Vector3 force)
    {
        // Appliquer une force de knockback
        transform.position += force * Time.deltaTime;
    }
}