using UnityEngine;

public class PlayerColorDisplay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TeamColorLookup teamColorLookup;
    [SerializeField] private TankPlayer tankPlayer;
    [SerializeField] private SpriteRenderer[] playerSpriteComponents;

    private void Start()
    {
        updateColor(0, tankPlayer.TeamID.Value);

        tankPlayer.TeamID.OnValueChanged += updateColor;
    }

    private void updateColor(int _, int newVal) 
    {
        Color teamColor = teamColorLookup.GetTeamColor(newVal);
        foreach (SpriteRenderer spriteRenderer in playerSpriteComponents)
        {
            spriteRenderer.color = teamColor;
        }
    }

    private void OnDestroy()
    {
        tankPlayer.TeamID.OnValueChanged -= updateColor;
    }
}
