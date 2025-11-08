using System;
using UnityEngine;

public class RespawningCoin : Coin
{
    private Vector3 previousPosition;
    public event Action<RespawningCoin> OnCollected;

    private void OnEnable()
    {
        previousPosition = transform.position;
    }

    private void Update()
    {
        if (IsServer) return;
        if (previousPosition != this.transform.position)
        {
            show(true);
            previousPosition = this.transform.position;
        }
    }

    public override int Collect()
    {
        if (!IsServer)
        {
            show(false);
            return 0;
        }
        if (alreadyCollected)
        {
            return 0;
        }
        alreadyCollected = true;
        OnCollected?.Invoke(this);
        return coinValue;
    }

    public void Reset()
    {
        alreadyCollected = false;
    }
}
