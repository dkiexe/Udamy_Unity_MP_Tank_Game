using Unity.Netcode;
using UnityEngine;

public class ContactDamageDealer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int damage = 20;

    // ulong is just a big intager that can only be positive.
    // used to store client ids on networking systems
    private ulong ownerClientId;

    public void SetOwner(ulong ownerClientID)
    {
        this.ownerClientId = ownerClientID;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.attachedRigidbody == null) return;
        if (collision.attachedRigidbody.TryGetComponent<NetworkObject>(out NetworkObject netobj))
        {
            if (ownerClientId == netobj.OwnerClientId) return;
        }
        if (collision.attachedRigidbody.TryGetComponent<Health>(out Health RigidBodyHealth))
        {
            RigidBodyHealth.TakeDamage(damage);
        }
    }
}
