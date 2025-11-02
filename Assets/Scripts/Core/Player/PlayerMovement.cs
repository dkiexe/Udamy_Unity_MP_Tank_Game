using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour // NetworkBehaviour is used to acess network properties like : IsOwner
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    
    [SerializeField] private Transform bodyTransform;
    
    [SerializeField] private Rigidbody2D rb;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 4f; 

    [SerializeField] private float turningRate = 270f;

    private Vector2 previousMovementInput;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent += HandleMove;
        previousMovementInput = (Vector2) transform.position;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent -= HandleMove;
    }

    public void HandleMove(Vector2 movmentInput)
    {
        previousMovementInput = movmentInput;
    }

    void Update()
    {
       if (!IsOwner) return;
        float zRotation = previousMovementInput.x * -turningRate * Time.deltaTime;
        bodyTransform.Rotate(0f, 0f, zRotation);
    }
    private void FixedUpdate() // works best with physics of a rigid body 2D
    {
        if (!IsOwner) return;
        rb.linearVelocity = (Vector2)bodyTransform.up * previousMovementInput.y * moveSpeed;
    }
}
