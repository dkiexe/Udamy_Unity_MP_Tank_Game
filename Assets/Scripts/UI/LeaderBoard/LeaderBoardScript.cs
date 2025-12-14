using Unity.Netcode;
using UnityEngine;

public class LeaderBoardScript : NetworkBehaviour
{
    [Header("Refrences")]
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderBoardEntityDisplay leaderboardEntityPrefab;

    private NetworkList<LeaderBoardEntityState> leaderBoardEntities;

    private void Awake()
    {
        leaderBoardEntities = new NetworkList<LeaderBoardEntityState>();
    }
}
