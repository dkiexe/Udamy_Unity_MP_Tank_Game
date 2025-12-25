using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NitroBooster : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private InputReader inputReader;
    [SerializeField] private CoinWallet wallet;
    [SerializeField] private Image BoostBarImage;

    [Header("Settings")]
    [SerializeField] private int BoostMultiplier = 2;
    [SerializeField] private int costToBoost = 10;
    [SerializeField] private float boostDuration = 2f;
    [SerializeField] private float boostCooldown = 2f;

    public NetworkVariable<int> boostValue = new NetworkVariable<int>(value : 1);
    private bool isBoosting = false;
    private float boostCooldownElapsedTime = 0;

    private void Update()
    {
        if (boostCooldownElapsedTime < boostCooldown && !isBoosting)
        {
            boostCooldownElapsedTime += Time.deltaTime;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            BoostBarImage.enabled = false;  
            return;
        }
        inputReader.NitroPressEvent += HandleBoost;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        inputReader.NitroPressEvent -= HandleBoost;
    }

    private void HandleBoost(bool shiftPress)
    {
        if (!shiftPress) return;

        if (isBoosting) return;

        if (boostCooldownElapsedTime >= boostCooldown)
        {
            RequestNitroBoostServerRPC();
            boostCooldownElapsedTime = 0;
        }
    }

    [ServerRpc]
    private void RequestNitroBoostServerRPC()
    {
        if (wallet.TotalCoins.Value < costToBoost) return;
        wallet.SpendCoins(costToBoost);
        boostValue.Value *= BoostMultiplier;
        ControlBoostClientRPC(true);
        StartCoroutine(awaitBoostEnd());
    }

    [ClientRpc]
    private void ControlBoostClientRPC(bool isBoosting)
    {
        if (!IsOwner) return;
        this.isBoosting = isBoosting;
        StartCoroutine
            (
            ProgressNitroBar
                (
                    isBoosting ? boostDuration : 0,
                    isBoosting ? 0 : boostDuration
                )
            );
    }

    private IEnumerator ProgressNitroBar(float startVal, float goalVal)
    {
        int barDirection = startVal < goalVal ? 1 : -1;
        float currentVal = startVal;
        while (barDirection == 1 ? currentVal < goalVal : currentVal > goalVal)
        {
            currentVal += (Time.deltaTime * barDirection);
            BoostBarImage.fillAmount = barDirection == 1 ? currentVal / goalVal : currentVal / startVal;
            yield return null;
        }
    }

    private IEnumerator awaitBoostEnd()
    {
        yield return new WaitForSeconds(boostDuration);
        boostValue.Value = 1;
        ControlBoostClientRPC(false);
    }
}
