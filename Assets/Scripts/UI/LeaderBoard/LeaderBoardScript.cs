using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class LeaderBoardScript : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderBoardEntityDisplay leaderboardEntityPrefab;

    [Header("Leader Board Settings")]
    [SerializeField] private int maxEntitiesDisplay = 8;

    private NetworkList<LeaderBoardEntityState> leaderBoardEntities;
    private List<LeaderBoardEntityDisplay> entityDisplays = new List<LeaderBoardEntityDisplay>();

    private void Awake()
    {
        leaderBoardEntities = new NetworkList<LeaderBoardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            leaderBoardEntities.OnListChanged += HandleLeaderBoardEntitiesChanged;
            foreach(LeaderBoardEntityState leaderBoardEntityState in leaderBoardEntities)
            {
                HandleLeaderBoardEntitiesChanged(new NetworkListEvent<LeaderBoardEntityState>
                {
                    Type = NetworkListEvent<LeaderBoardEntityState>.EventType.Add,
                    Value = leaderBoardEntityState
                });
            }
        }

        if (IsServer)
        {
            TankPlayer[] players = FindObjectsByType<TankPlayer>(FindObjectsSortMode.None);

            foreach (TankPlayer player in players)
            {
                HandlePlayerSpawned(player);
            }

            TankPlayer.OnPlayerSpawned += HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned += HandlePlayerDeSpawned;
        }
    }
    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderBoardEntities.OnListChanged -= HandleLeaderBoardEntitiesChanged;
        }

        if (IsServer)
        {
            TankPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
            TankPlayer.OnPlayerDespawned -= HandlePlayerDeSpawned;
        }
    }

    private void HandlePlayerSpawned(TankPlayer player)
    {
        leaderBoardEntities.Add(new LeaderBoardEntityState
        {
            ClientID = player.OwnerClientId,
            PlayerName = player.PlayerName.Value,
            Coins = 0
        });

        player.Wallet.TotalCoins.OnValueChanged += (oldVal, newVal) => 
        HandleCoinsChanged(
            player.OwnerClientId,
            newVal
        );

    }

    private void HandlePlayerDeSpawned(TankPlayer player)
    {
        if (leaderBoardEntities == null) return;

        foreach (LeaderBoardEntityState leaderBoardEntityState in leaderBoardEntities)
        {
            if (leaderBoardEntityState.ClientID == player.OwnerClientId)
            {
                leaderBoardEntities.Remove(leaderBoardEntityState);
                break;
            }
        }

        player.Wallet.TotalCoins.OnValueChanged -= (oldVal, newVal) => 
        HandleCoinsChanged(
            player.OwnerClientId,
            newVal
        );
    }

    private void HandleLeaderBoardEntitiesChanged(NetworkListEvent<LeaderBoardEntityState> changeEvent)
    {
        if (!gameObject.scene.isLoaded) return;

        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderBoardEntityState>.EventType.Add:
                // if there are not any entity displays where it matches this clientID spawn one!
                if (!entityDisplays.Any(x => x.ClientID == changeEvent.Value.ClientID))
                {
                    LeaderBoardEntityDisplay leaderBoardEntityDisplay =  
                        Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);

                    leaderBoardEntityDisplay.Initialise(
                        changeEvent.Value.ClientID,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.Coins
                        );

                    entityDisplays.Add(leaderBoardEntityDisplay);
                }
                break;
            
            case NetworkListEvent<LeaderBoardEntityState>.EventType.Remove:
                // finding the first LeaderBoardEntityDisplay that matches the clientID or a  
                LeaderBoardEntityDisplay displayToRemove =
                    entityDisplays.FirstOrDefault(x => x.ClientID == changeEvent.Value.ClientID);
                if (displayToRemove != null)
                {
                    displayToRemove.transform.SetParent(null);
                    Destroy(displayToRemove.gameObject);
                    entityDisplays.Remove(displayToRemove);
                }
                break;
            
            // The value event means that the vale of the LeaderBoardEntityState has changed
            case NetworkListEvent<LeaderBoardEntityState>.EventType.Value:
                LeaderBoardEntityDisplay displayToUpdate =
                    entityDisplays.FirstOrDefault(x => x.ClientID == changeEvent.Value.ClientID);
                if (displayToUpdate != null)
                {
                    displayToUpdate.updateCoins(changeEvent.Value.Coins);
                }
                break;
        }

        entityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));

        for (int i = 0; i < entityDisplays.Count; i++)
        {
            entityDisplays[i].transform.SetSiblingIndex(i);
            entityDisplays[i].updateText();
            
            bool shouldShow = i <= maxEntitiesDisplay - 1;
            
            entityDisplays[i].gameObject.SetActive(shouldShow);
        }

        LeaderBoardEntityDisplay myDisplay = 
            entityDisplays.FirstOrDefault(x => x.ClientID == NetworkManager.Singleton.LocalClientId);

        if (myDisplay != null)
        {
            if (myDisplay.transform.GetSiblingIndex() >= maxEntitiesDisplay)
            {
                leaderboardEntityHolder.GetChild(maxEntitiesDisplay - 1).gameObject.SetActive(false);
                myDisplay.gameObject.SetActive(true);
            }
        }
    }

    private void HandleCoinsChanged(ulong clientId, int newCoins)
    {
        for (int i = 0; i < leaderBoardEntities.Count; i++)
        {
            LeaderBoardEntityState leaderBoardEntityState = leaderBoardEntities[i];
            if (leaderBoardEntities[i].ClientID != clientId) continue;

            leaderBoardEntities[i] = new LeaderBoardEntityState
            {
                ClientID = leaderBoardEntities[i].ClientID,
                PlayerName = leaderBoardEntities[i].PlayerName,
                Coins = newCoins
            };
            return;
        }
    }
}
