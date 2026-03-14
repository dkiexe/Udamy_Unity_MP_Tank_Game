using Unity.Netcode;
using UnityEngine;

public class PlayerColorDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private TeamColorLookup teamColorLookup;
    [SerializeField] private TankPlayer tankPlayer;
    [SerializeField] private SpriteRenderer[] playerSpriteComponents;

    private void Start()
    {
        if (IsServer)
        {
            updateColor(0, tankPlayer.TeamID.Value);
            tankPlayer.TeamID.OnValueChanged += updateColor;
        }
        else
        {
            updatColor(default, tankPlayer.PlayerColor.Value);
            tankPlayer.TeamID.OnValueChanged += updateColor;
        }
    }

    private void updatColor(Color _, Color newColor)
    {
        foreach (SpriteRenderer spriteRenderer in playerSpriteComponents)
        {
            spriteRenderer.color = newColor;
        }
    }

    private void updateColor(int teamValueOld, int teamValueNew) 
    {
        Color teamColor = teamColorLookup.GetTeamColor(teamValueNew);
        foreach (SpriteRenderer spriteRenderer in playerSpriteComponents)
        {
            spriteRenderer.color = teamColor;
        }
        tankPlayer.PlayerColor.Value = teamColor;
    }

    public override void OnDestroy()
    {
        if (IsServer)
        {
            tankPlayer.TeamID.OnValueChanged -= updateColor;
        }
        else
        {
            tankPlayer.TeamID.OnValueChanged -= updateColor;
        }
    }
}
