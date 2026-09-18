using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static WorkOutTheme;

/// <summary>Common session chrome. All live numbers come from the replicated game state.</summary>
public class WorkOutDesktop : MonoBehaviour
{
    [SerializeField] private TMP_Text clockLabel, countLabel, progressLabel, roleLabel, percentLabel;
    [SerializeField] private Image progressFill;
    [SerializeField] private TMP_Text[] playerNames = new TMP_Text[4];
    [SerializeField] private TMP_Text[] playerRoles = new TMP_Text[4];
    [SerializeField] private Image[] cards = new Image[4];
    [SerializeField] private Image[] presence = new Image[4];
    [SerializeField] private WorkOutIcon[] connections = new WorkOutIcon[4];
    private float nextRefresh;
    private bool built;

    private void Awake()
    {
        built = clockLabel != null && countLabel != null && progressFill != null;
    }

#if UNITY_EDITOR
    public void Initialize(TMP_Text existingTimer = null)
    {
        if (transform.Find("WorkOutDesktop") != null) return;
        built = true;
        ScaleCanvas(gameObject);
        var backdrop = Panel("WorkOutDesktop", transform, Surface.Inset, false);
        Place(backdrop, 0, 0, 1, 1);
        backdrop.SetAsFirstSibling();

        var sidebar = Panel("TeamSidebar", backdrop);
        Place(sidebar, 0.02f, 0.05f, 0.218f, 0.97f);
        var sideHeader = Panel("TeamHeader", sidebar, Surface.Header, false);
        Place(sideHeader, 0, 1, 1, 1, 0, -78, 0, 0);
        Fixed(Icon(sideHeader, WorkOutIcon.Symbol.People, Blue).transform, 22, -22, 34, 34);
        Fixed(Label("TeamTitle", sideHeader, "Team", 27).transform, 72, -18, 195, 42);
        for (int i = 0; i < 4; i++)
        {
            var card = Panel("PlayerCard" + i, sidebar, Surface.Raised);
            Place(card, 0, 1f - (i + 1) * 0.145f, 1, 1f - i * 0.145f, 17, -82, 17, 98);
            cards[i] = card.GetComponent<Image>();
            var avatar = Panel("Avatar", card, Surface.Tint, false);
            Fixed(avatar, 16, -19, 64, 64);
            avatar.GetComponent<Image>().color = PlayerColors[i];
            Place(Icon(avatar, WorkOutIcon.Symbol.Person, Color.Lerp(PlayerColors[i], Color.white, 0.6f)).transform, 0.15f, 0.12f, 0.85f, 0.9f);
            var status = Panel("Presence", card, Surface.Green, false);
            presence[i] = status.GetComponent<Image>();
            presence[i].color = Muted;
            Fixed(status, 64, -66, 15, 15);
            playerNames[i] = Label("PlayerName", card, "Open slot", 21);
            Place(playerNames[i].transform, 0, 0.45f, 1, 0.86f, 95, 0, 24, 0);
            playerRoles[i] = Label("PlayerRole", card, "Waiting for player", 16, Muted);
            Place(playerRoles[i].transform, 0, 0.12f, 1, 0.49f, 95, 0, 38, 0);
            connections[i] = Icon(card, WorkOutIcon.Symbol.Bars, Muted);
            Fixed(connections[i].transform, -35, -58, 20, 20, 1, 1);
        }
        var session = Panel("SessionInfo", sidebar, Surface.Inset, false);
        Place(session, 0, 0, 1, 0.16f, 18, 22, 18, 0);
        Fixed(Icon(session, WorkOutIcon.Symbol.Monitor, Muted).transform, 18, -20, 28, 28);
        Place(Label("SessionTitle", session, "Work session", 19).transform, 0, 0.5f, 1, 1, 60, 0, 10, 0);
        Place(Label("SessionHint", session, "Discuss tasks with your\nteam in voice chat", 15, Muted).transform, 0, 0, 1, 0.52f, 18, 10, 12, 0);

        var timer = Panel("TimerPill", backdrop, Surface.Red);
        Place(timer, 0.24f, 0.866f, 0.38f, 0.97f);
        Fixed(Icon(timer, WorkOutIcon.Symbol.Clock).transform, 22, -28, 36, 36);
        clockLabel = existingTimer != null ? existingTimer : Label("SessionTimer", timer, "00:00", 38);
        clockLabel.transform.SetParent(timer, false);
        Text(clockLabel, 38, Ink, TextAlignmentOptions.Center);
        clockLabel.fontStyle = FontStyles.Bold;
        Place(clockLabel.transform, 0, 0, 1, 1, 64, 0, 18, 0);

        var progress = Panel("RoundProgress", backdrop);
        Place(progress, 0.397f, 0.866f, 0.842f, 0.97f);
        progressLabel = Label("ProgressCaption", progress, "Shift ends in", 20, Muted);
        Place(progressLabel.transform, 0, 0.48f, 1, 1, 28, 0, 28, 8);
        percentLabel = Label("ProgressPercent", progress, "", 20, Blue, TextAlignmentOptions.MidlineRight);
        Place(percentLabel.transform, 0.75f, 0.48f, 1, 1, 0, 0, 28, 8);
        var track = Panel("ProgressTrack", progress, Surface.Track, false);
        Place(track, 0, 0, 1, 0.25f, 28, 21, 28, -10);
        var fill = Panel("ProgressFill", track, Surface.Blue, false);
        Place(fill, 0, 0, 1, 1);
        progressFill = fill.GetComponent<Image>();
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillAmount = 0;

        var count = Panel("PlayerCount", backdrop);
        Place(count, 0.859f, 0.866f, 0.98f, 0.97f);
        Fixed(Icon(count, WorkOutIcon.Symbol.People).transform, 24, -28, 36, 36);
        countLabel = Label("PlayerCountLabel", count, "0 / 4", 29);
        Place(countLabel.transform, 0, 0, 1, 1, 80, 0, 16, 0);
        var footer = Label("BrandFooter", backdrop, "WORK OUT INC.  /  TEAM SHIFT", 14, Muted);
        Place(footer.transform, 0.025f, 0.007f, 0.55f, 0.038f);
        roleLabel = Label("RoleFooter", backdrop, "", 14, Muted, TextAlignmentOptions.MidlineRight);
        Place(roleLabel.transform, 0.55f, 0.007f, 0.976f, 0.038f);
    }

#endif

    private void Update()
    {
        if (!built || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.2f;
        var state = GameSessionState.Instance;
        if (state != null)
        {
            int seconds = Mathf.CeilToInt(Mathf.Max(0, state.TimeRemaining.Value));
            clockLabel.text = $"{seconds / 60:00}:{seconds % 60:00}";
            float remaining = state.RoundDuration > 0 ? Mathf.Clamp01(state.TimeRemaining.Value / state.RoundDuration) : 0;
            progressFill.fillAmount = remaining;
            percentLabel.text = $"{Mathf.CeilToInt(remaining * 100)}%";
        }
        var manager = LobbyPlayerManager.Instance;
        int count = manager != null && manager.IsSpawned ? manager.Players.Count : 0;
        countLabel.text = $"{count} / 4";
        for (int i = 0; i < 4; i++)
        {
            bool occupied = i < count;
            presence[i].color = occupied ? Color.white : Muted;
            presence[i].sprite = GetSprite(occupied ? Surface.Green : Surface.Track);
            connections[i].color = occupied ? Green : Muted;
            cards[i].color = occupied ? Color.white : new Color(0.64f, 0.69f, 0.8f);
            if (!occupied) { playerNames[i].text = "Open slot"; playerRoles[i].text = "Waiting for player"; continue; }
            var player = manager.Players[i];
            bool local = NetworkManager.Singleton != null && player.ClientId == NetworkManager.Singleton.LocalClientId;
            cards[i].sprite = GetSprite(local ? Surface.Header : Surface.Raised);
            playerNames[i].text = player.PlayerName.ToString() + (local ? " · you" : "");
            var role = RoleAssignmentManager.Instance != null ? RoleAssignmentManager.Instance.GetRoleFor(player.ClientId) : GameRole.None;
            playerRoles[i].text = RoleName(role);
        }
        if (RoleAssignmentManager.Instance != null) roleLabel.text = RoleName(RoleAssignmentManager.Instance.GetMyRole());
    }

    public static string RoleName(GameRole role)
    {
        switch (role)
        {
            case GameRole.Boss: return "Manager";
            case GameRole.Programmer: return "Programmer";
            case GameRole.Artist: return "Designer";
            case GameRole.Typographer: return "Copywriter";
            default: return "No role selected";
        }
    }
}
