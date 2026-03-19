using System;

[Serializable]
public class DungeonEnterReq
{
    public int partyId;
    public int partyLeaderUserId;
    public int[] memberUserIds;
}

[Serializable]
public class DungeonEnterRes
{
    public string status;      // "ok" | "full"
    public string sessionName; // status == "ok" 일 때
    public string message;     // status == "full" 일 때
}
