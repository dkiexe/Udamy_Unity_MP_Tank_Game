using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class LeaderBoardScript : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private Transform teamLeaderboardEntityHolder;
    [SerializeField] private GameObject teamLeaderboardBackground;
    [SerializeField] private LeaderBoardEntityDisplay leaderboardEntityPrefab;
    [SerializeField] private TeamColorLookup teamColorLookup;

    [Header("Leader Board Settings")]
    [SerializeField] private int maxEntitiesDisplay = 8;
    [SerializeField] private Color ownerColor;
    [SerializeField] private string[] teamNames;

    private NetworkList<LeaderBoardEntityState> leaderBoardEntities;
    private List<LeaderBoardEntityDisplay> entityDisplays = new List<LeaderBoardEntityDisplay>();
    private List<LeaderBoardEntityDisplay> teamEntityDisplays = new List<LeaderBoardEntityDisplay>();

    private void Awake()
    {
        leaderBoardEntities = new NetworkList<LeaderBoardEntityState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Check if we are on a team game to enable team scoreboard.
            if (ClientSingelton.Instance.GameManager.UserDataObj.userGamePreferences.gameQueue 
                == GameQueue.Team)
            {
                teamLeaderboardBackground.SetActive(true);

                for (int i = 0; i < teamNames.Length; i++)
                {
                    LeaderBoardEntityDisplay teamLeaderBoardEntityDisplay = Instantiate(
                        leaderboardEntityPrefab, 
                        teamLeaderboardEntityHolder
                        );

                    teamLeaderBoardEntityDisplay.Initialise(i, teamNames[i], 0);

                    Color teamColor = teamColorLookup.GetTeamColor(i);

                    teamLeaderBoardEntityDisplay.SetColor(teamColor);

                    teamEntityDisplays.Add(teamLeaderBoardEntityDisplay);
                }
            }

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
            TeamID = player.TeamID.Value,
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

                    if (NetworkManager.Singleton.LocalClientId == changeEvent.Value.ClientID)
                    {
                        leaderBoardEntityDisplay.SetColor(ownerColor);
                    }

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
        if (!teamLeaderboardBackground.activeSelf) return;

        // getting the teamDisplay of the team releated to the player that caused a change event
        LeaderBoardEntityDisplay teamDisplay =
            teamEntityDisplays.FirstOrDefault(x => x.TeamID == changeEvent.Value.TeamID);

        if (teamDisplay != null)
        {
            // if a player leaves remove his coins from the team.
            if (changeEvent.Type == NetworkListEvent<LeaderBoardEntityState>.EventType.Remove)
            {
                teamDisplay.updateCoins(teamDisplay.Coins - changeEvent.Value.Coins);
            }
            else
            {
                // update team coins amount based current coins + diff between current coins value
                // and previous coins value.
                teamDisplay.updateCoins(
                    teamDisplay.Coins + 
                    (changeEvent.Value.Coins - changeEvent.PreviousValue.Coins)
                    );
            }
        }

        teamEntityDisplays.Sort((x, y) => y.Coins.CompareTo(x.Coins));
        
        for (int i = 0; i < teamEntityDisplays.Count; i++)
        {
            teamEntityDisplays[i].transform.SetSiblingIndex(i);
            teamEntityDisplays[i].updateText();
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
                TeamID = leaderBoardEntities[i].TeamID,
                Coins = newCoins
            };
            return;
        }
    }
}
