using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public struct LeaderBoardEntityState : INetworkSerializable, IEquatable<LeaderBoardEntityState>
{
    /// This struct give the option to sync a custom
    /// datatype over the network, this struct would allow the server to sync the following :
    /// -> Client IDs
    /// -> Names
    /// -> Client Coin Counts
    /// NOTE : Every data you sync through a network variable has to be a struct it cant be a class with references.

    public ulong ClientID;
    public FixedString32Bytes PlayerName;
    public int Coins;

    /// This is the data we want to sync, but its not as simple as just leaving it like this and sending it over the network.
    /// We actually need to tell unity NGO how to actually serialize this.
    /// Serializing : Converting your custom struct (data in memory) into a format that can be written to a network stream, sent over the network, and then reconstructed on the other side.
    /// Serializing allows packing struct data into bytes → sending it → unpacking it back into the same struct
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        /// "ref" keywords are needed because We want NGO to write the incoming data 
        /// directly into the actual fields of the struct, Using ref ensures that happens.
        serializer.SerializeValue(ref ClientID);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Coins);
    }

    public bool Equals(LeaderBoardEntityState other)
    {
        /// This method allows unity's NGO to know the difference between LeaderBoardEntityState
        /// objects sent over the network, This is a crucial part if you need to work with networklists
        return ClientID == other.ClientID && PlayerName.Equals(other.PlayerName) && Coins == other.Coins;
    }

}