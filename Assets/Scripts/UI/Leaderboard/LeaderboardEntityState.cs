using System;
using Unity.Collections;
using Unity.Netcode;

// creating our own custom for the Leaderboard script
// struct = a value type (NOT A CLASS)
// Stored directly in memory + Faster and lighter for small data
// INetworkSerializable Requires you to write how your struct’s data gets serialized (written into data streams) for networking
// Note: Serialize over network = means turning data into a format you can store or send over network
// it makes your struct usable in NetworkVariables or RPCs
// IEquatable<T> → lets you define how to check if two things are equal (in this case: compare iteself to another LeaderboardEntityState)
public struct LeaderboardEntityState : INetworkSerializable, IEquatable<LeaderboardEntityState>
{
    // Vars start with Capital
    public ulong ClientId;
    public int TeamIndex;
    public FixedString32Bytes PlayerName;
    public int Coins;

    // Implement this method from the INetworkSerializable Interface
    // This method serializes the above vars
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Serialize every value here using this format of lines
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Coins);
        serializer.SerializeValue(ref TeamIndex);
    }

    // Compare to the other LeaderboardEntityState -> to check if from one instance to another -> has anything changed??
    // When do we use: for other scripts to use this metnod to check leaderboards
    public bool Equals(LeaderboardEntityState other)
    {
        return ClientId == other.ClientId &&
            TeamIndex == other.TeamIndex &&
            PlayerName.Equals(other.PlayerName) &&
            Coins == other.Coins; // PlayerName must use .Equals due to its special type
    }
}