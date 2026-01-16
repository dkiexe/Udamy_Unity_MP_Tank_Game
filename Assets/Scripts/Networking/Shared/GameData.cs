using System;

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


[Serializable]
public class UserData
{
    /// <summary>
    /// Stores user data that a user sends over the network when connecting to a server.
    /// </summary>
    public string userName;
    public string userAuthId;
    public GameInfo userGamePreferences;
}

[Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQueue gameQueue;

    public string ToMultiplayQueue()
    {
        return "";
    }
}