using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{

    [SerializeField] private Projectile projectile;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // UGS games without team matchmaking have a TeamID of -1.
        if (projectile.TeamID != -1) 
        {
            if (collision.attachedRigidbody != null)
            {
                if (collision.TryGetComponent<TankPlayer>(out TankPlayer player))
                {
                    if (player.TeamID.Value == projectile.TeamID) return;
                }
            }
        }
        Destroy(gameObject);
    }
}
