using Unity.Netcode;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour // NetworkBehaviour is used to acess network properties like : IsOwner
{
    [Header("References")]
    [SerializeField] private InputReader inputReader;
    
    [SerializeField] private Transform bodyTransform;
    
    [SerializeField] private Rigidbody2D rb;

    [SerializeField] private NitroBooster booster;

    [SerializeField] private ParticleSystem dustCloud;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 4f; 

    [SerializeField] private float turningRate = 270f;

    [SerializeField] private float particalEmmisionValue = 10;

    private const float ParticalPositionChangeThreshold = 0.005f;

    private Vector2 previousMovementInput;

    private Vector3 previousPos;

    private ParticleSystem.EmissionModule emissionModule;

    private void Awake()
    {
        emissionModule = dustCloud.emission;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        inputReader.MoveEvent += HandleMove;
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
        if ((transform.position - previousPos).sqrMagnitude > ParticalPositionChangeThreshold)
        {
            // turn on when the position changes.
            emissionModule.rateOverTime = particalEmmisionValue * booster.boostValue.Value;
        }
        else
        {
            // turn off when the position stays the same.
            emissionModule.rateOverTime = 0;
        }
        previousPos = transform.position;

        if (!IsOwner) return;
        rb.linearVelocity = (Vector2)bodyTransform.up * previousMovementInput.y * (moveSpeed * booster.boostValue.Value);
    }
}
