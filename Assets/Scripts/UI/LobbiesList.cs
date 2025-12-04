using System;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField] private Transform lobbyItemParent;

    [SerializeField] private LobbyItem lobbyitemPrefab;

    private bool isJoining = false;
    private bool isRefreshing = false;

    private void OnEnable()
    {
        refreshLobbyList();
    }

    private void OnDisable()
    {
        
    }

    public async void JoinAsync(Lobby lobby)
    {
        if (isJoining) return; // prevent multiple join attempts

        isJoining = true;

        try
        {
            Lobby joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

            string joinCode = joinedLobby.Data["JoinCode"].Value;

            await ClientSingelton.Instance.GameManager.StartClientAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        isJoining = false;
    }
    public async void refreshLobbyList()
    {
        if (isRefreshing) return; // prevent multiple refresh attempts

        isRefreshing = true;

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions()
            {
                Count = 25,
                Filters = new List<QueryFilter>()
                {
                    new QueryFilter // filter empty out lobbies 
                    (
                        field: QueryFilter.FieldOptions.AvailableSlots,
                        op: QueryFilter.OpOptions.GT,
                        value: "0"
                    ),
                    new QueryFilter // filter out locked lobbies
                    (
                        field: QueryFilter.FieldOptions.IsLocked,
                        op: QueryFilter.OpOptions.EQ,
                        value: "0"
                    )
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Transform child in lobbyItemParent) // removing all previous lobby items
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in queryResponse.Results)
            {
                LobbyItem lobbyitem = Instantiate(lobbyitemPrefab, lobbyItemParent);
                lobbyitem.Initialize(this, lobby);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        isRefreshing = false;
    }
}
