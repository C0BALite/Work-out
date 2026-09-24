using Unity.Netcode;
using UnityEngine;

// Сетевой мост между раскраской дизайнера и экраном босса.
// Сервер выбирает картинку и эталонные цвета, дизайнер присылает заливки, сервер считает итог.
public class ColoringBossSync : NetworkBehaviour
{
    public static ColoringBossSync Instance { get; private set; }

    [SerializeField] private Texture2D[] pictures;
    [SerializeField] private Color32[] palette = new Color32[10];
    [SerializeField, Range(0f, 1f)] private float correctActionAccuracy = 0.6f;

    public NetworkVariable<int> PictureIndex = new NetworkVariable<int>(-1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> Submitted = new NetworkVariable<bool>(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> Accuracy = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkList<byte> TargetColors;
    public NetworkList<byte> Fills;

    public Color32[] Palette => palette;

    private ColoringPicture picture;
    private int builtPictureIndex = -1;

    private void Awake()
    {
        Instance = this;
        TargetColors = new NetworkList<byte>();
        Fills = new NetworkList<byte>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer || pictures == null || pictures.Length == 0) return;

        PictureIndex.Value = Random.Range(0, pictures.Length);
        ColoringPicture built = BuildPicture();
        if (built == null) return;

        TargetColors.Clear();
        Fills.Clear();
        foreach (byte color in built.GenerateTargetColors(palette.Length))
        {
            TargetColors.Add(color);
            Fills.Add(ColoringPicture.Empty);
        }
    }

    // Готово, когда реплицированы и номер картинки, и эталон для всех её областей.
    public bool TryGetPicture(out ColoringPicture result)
    {
        result = BuildPicture();
        return result != null && TargetColors.Count == result.RegionCount && Fills.Count == result.RegionCount;
    }

    // Разбиение строится лениво на каждой машине из одной и той же текстуры.
    private ColoringPicture BuildPicture()
    {
        int index = PictureIndex.Value;
        if (index != builtPictureIndex)
        {
            picture = index >= 0 && pictures != null && index < pictures.Length && pictures[index] != null
                ? new ColoringPicture(pictures[index])
                : null;
            builtPictureIndex = index;
        }
        return picture;
    }

    public void CopyTargets(byte[] destination) => Copy(TargetColors, destination);
    public void CopyFills(byte[] destination) => Copy(Fills, destination);

    [Rpc(SendTo.Server)]
    public void FillRegionRpc(int region, byte color, RpcParams rpcParams = default)
    {
        if (!IsDesigner(rpcParams.Receive.SenderClientId) || Submitted.Value) return;
        if (region < 0 || region >= Fills.Count || color >= palette.Length) return;

        if (Fills[region] != color)
            Fills[region] = color;
    }

    [Rpc(SendTo.Server)]
    public void SubmitRpc(RpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        if (!IsDesigner(senderId) || Submitted.Value) return;

        var targets = new byte[TargetColors.Count];
        var fills = new byte[Fills.Count];
        CopyTargets(targets);
        CopyFills(fills);

        Accuracy.Value = ColoringPicture.Accuracy(targets, fills, palette);
        Submitted.Value = true;

        if (Accuracy.Value >= correctActionAccuracy && MiniGameEventSystem.Instance != null)
            MiniGameEventSystem.Instance.ReportCorrectAction(senderId);
    }

    private static bool IsDesigner(ulong clientId)
    {
        return RoleAssignmentManager.Instance != null &&
               RoleAssignmentManager.Instance.GetRoleFor(clientId) == GameRole.Artist;
    }

    private static void Copy(NetworkList<byte> source, byte[] destination)
    {
        int count = Mathf.Min(source.Count, destination.Length);
        for (int i = 0; i < count; i++) destination[i] = source[i];
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
        base.OnDestroy();
    }
}
