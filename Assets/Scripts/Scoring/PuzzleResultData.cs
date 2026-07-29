using Unity.Netcode;

public struct PuzzleResultData : INetworkSerializable, System.IEquatable<PuzzleResultData>
{
    public ulong ClientId;
    public GameRole Role;
    public float Score;
    public float Multiplier;
    public int CurrencyEarned;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref Role);
        serializer.SerializeValue(ref Score);
        serializer.SerializeValue(ref Multiplier);
        serializer.SerializeValue(ref CurrencyEarned);
    }

    public bool Equals(PuzzleResultData other) => ClientId == other.ClientId;
}
