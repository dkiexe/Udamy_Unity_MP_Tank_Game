using UnityEngine;

[CreateAssetMenu(fileName = "NewTeamColorLookup", menuName = "Team Color Lookup")]
public class TeamColorLookup : ScriptableObject
{
    [SerializeField] private Color[] teamColors;

    public Color GetTeamColor(int teamId)
    {
        if (teamId < 0 || teamId >= teamColors.Length)
        {
            return Random.ColorHSV(0, 1f, 1f, 1f, 0.5f, 1f);
        }
        else
        {
            return teamColors[teamId];
        }
    }
}
