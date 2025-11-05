using Unity.Netcode;
using UnityEngine;

// this means that objcts of this class cannot exists, only 
// child class objects that inherit from this can exist.
public abstract class Coin : NetworkBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;


    protected int coinValue;
    protected bool alreadyCollected;

    public abstract int Collect();

    public void SetValue(int value)
    {
        coinValue = value;
    }

    protected void show(bool show)
    {
        spriteRenderer.enabled = show;
    }
}
