using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Map
{
    Default
}

public enum GameMode
{
    Default
}

public enum GameQueue
{
    Solo,
    Team
}
[Serializable] // a Serializable class: tells Unity (or C#) that your class can be turned into data and stored: in a file, JSON, in memory
public class UserData
{

    // Only storing data in this class -> no need to inherit Monobehaviour

    public string userName;

    // Default: -1 for non team games (solo)
    public int teamIndex = -1; 

    public string userAuthId; // this is the permanent user identity from your auth system (ex: from firebase, steam, etc.) + does not change every game session (it is permanent)

    public GameInfo userGamePreferences = new GameInfo(); // this constructor will make the enums become its default value
}

[Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQueue gameQueue;
    public string ToMultiplayQueue() // For this line in MatchplayMatchMaker.cs: string queueName = data.userGamePreferences.ToMultiplayQueue();
    {
        return gameQueue switch 
        {
            GameQueue.Solo => "solo-queue", // If gameQueue equals GameQueue.Solo → it returns "solo-queue"
            GameQueue.Team => "team-queue", //  If gameQueue equals GameQueue.Team → it returns "team-queue".
            _ => "solo-queue" // If it’s anything else --> defaults to "solo-queue".
        };
    }
}



