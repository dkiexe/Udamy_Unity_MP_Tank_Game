using Unity.Netcode;
using UnityEngine;

public class ContactDamageDealer : MonoBehaviour
{
    [Header("Referances")]
    [SerializeField] private Projectile projectile;

    [Header("Settings")]
    [SerializeField] private int damage = 20;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody == null) return;
        
        if (projectile.TeamID != -1)
        {
            if (collision.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer tankPlayer))
            {
                if (projectile.TeamID == tankPlayer.TeamID.Value) return;
            }
        }

        if (collision.attachedRigidbody.TryGetComponent<Health>(out Health RigidBodyHealth))
        {
            RigidBodyHealth.TakeDamage(damage);
        }
    }
}
