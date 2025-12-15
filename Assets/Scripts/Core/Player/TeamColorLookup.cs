
using UnityEngine;

[CreateAssetMenu(fileName = "NewTeamColorLookup", menuName = "Team Color")]
public class TeamColorLookup : ScriptableObject // independent of gameobject -> is an ASSET for pur gameobjs to reference
{
    [SerializeField] private Color[] teamColors; // array: fixed length and one type only

    public Color GetTeamColor(int teamIndex)
    {
        // Just in case teamIndex is less than zero (-1: non team games) or the teamindex is more than the number of avail team colors in the array
        if (teamIndex < 0 || teamIndex >= teamColors.Length)
        {
            return Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f); // a good set of values for a random color -> u can reuse this lol
        }
        else
        {
            return teamColors[teamIndex];
        }

    }

}
