using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// no need to be network behaviour since we will be changing the network var in tankplayer, not here
public class PlayerColorDisplay : MonoBehaviour
{
    [SerializeField] TeamColorLookup teamColorLookup;
    [SerializeField] TankPlayer tankPlayer;
    [SerializeField] SpriteRenderer[] spriteRenderers; // since in each player, there is more than one renderer



    void Start()
    {

        // Call it manually JUST IN CASE it did not subscribe in time before the change
        HandleTeamIndexChanged(-1, tankPlayer.TeamIndex.Value);

        tankPlayer.TeamIndex.OnValueChanged += HandleTeamIndexChanged;
        
    }

    private void HandleTeamIndexChanged(int oldTeamIndex, int newTeamIndex)
    {
        // Loop thru all the sprite renderers to change to the team color
        Color teamColor = teamColorLookup.GetTeamColor(newTeamIndex);
        foreach (SpriteRenderer renderer in spriteRenderers)
        {
            renderer.color = teamColor;
        }
    }

    void OnDestroy()
    {
        tankPlayer.TeamIndex.OnValueChanged -= HandleTeamIndexChanged;
    }
}
