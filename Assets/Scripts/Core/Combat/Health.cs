using System;
using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    [field: SerializeField] public int MaxHealth { get; private set; } = 100;

    // only the server can modify this variable.
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>();

    private bool isDead = false;

    public event Action<Health> OnDie;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        CurrentHealth.Value = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        ModifyHealth(-damage);
    }

    public void RestoreHealth(int Heal)
    {
        ModifyHealth(Heal);
    }

    public void ModifyHealth(int value)
    {
        if (!isDead)
        {
            int newHealth = Mathf.Clamp
                (
                    CurrentHealth.Value + value,
                    0,
                    MaxHealth
                );
            
            CurrentHealth.Value = newHealth;
            
            if (newHealth == 0) 
            {
                OnDie?.Invoke(this);
                isDead = true;
            }
        }
    }
}
