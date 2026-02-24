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

    [SerializeField] private AudioClip AC_Idle;

    [SerializeField] private AudioClip AC_move;

    [Header("Settings")]
    [SerializeField] private float moveSpeed = 4f; 

    [SerializeField] private float turningRate = 270f;

    [SerializeField] private float particalEmmisionValue = 10;

    private const float ParticalPositionChangeThreshold = 0.005f;

    private AudioSource audioSource;

    private Vector2 previousMovementInput;

    private Vector3 previousPos;

    private ParticleSystem.EmissionModule emissionModule;

    private MovmentPhase currentPhase;

    private enum MovmentPhase
    {
        Idle,
        Move
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        emissionModule = dustCloud.emission;

        SwapPlayerAudio(MovmentPhase.Idle);
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
        MovmentPhase NewMovmentPhase;
        
        previousMovementInput = movmentInput;
        if (movmentInput.magnitude > 0)
        {
            NewMovmentPhase = MovmentPhase.Move;
        }
        else
        {
            NewMovmentPhase = MovmentPhase.Idle;
        }

        if (currentPhase != NewMovmentPhase)
        {
            currentPhase = NewMovmentPhase;
            SwapPlayerAudio(currentPhase);
        }
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

    private void SwapPlayerAudio(MovmentPhase phase)
    {
        audioSource.Stop();
        audioSource.clip = phase == MovmentPhase.Idle ? AC_Idle: AC_move ;
        audioSource.Play();
    }
}
