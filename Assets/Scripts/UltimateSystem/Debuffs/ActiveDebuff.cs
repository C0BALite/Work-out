public class ActiveDebuff
{
    public DebuffData Data;
    public ulong TargetId;
    public ulong CasterId;
    public float RemainingTime;
    public bool IsActive;
    public IDebuffEffect Effect; // новое
}