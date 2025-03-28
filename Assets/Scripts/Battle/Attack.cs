using UnityEngine;

public class Attack : MonoBehaviour
{
    public int damage = 10; // Dégâts de l'attaque
    public float range = 5f; // Portée de l'attaque
    public float speed = 10f; // Vitesse de l'attaque
    public LayerMask targetLayer; // Layer des cibles (joueur, ennemis, etc.)

    private Vector3 direction;

    public void SetDirection(Vector3 dir)
    {
        direction = dir.normalized;
    }

    void Update()
    {
        // Déplacer l'attaque
        transform.Translate(direction * speed * Time.deltaTime);

        // Détecter les collisions
        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit, speed * Time.deltaTime, targetLayer))
        {
            OnHit(hit.collider);
        }
    }

    void OnHit(Collider other)
    {
        // Appliquer des dégâts ou des effets à la cible
        EntityStats targetStats = other.GetComponent<EntityStats>();
        if (targetStats != null)
        {
            targetStats.TakeDamage(damage);
        }

        // Détruire l'attaque après collision
        Destroy(gameObject);
    }
}