using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_IOS || UNITY_EDITOR_OSX
using Apple.GameKit;
using Apple.GameKit.Leaderboards;
#endif

public class ShareIQController : MonoBehaviour
{
    private const string ScorePrefKey = "ShareIQScore";

#if UNITY_IOS || UNITY_EDITOR_OSX
    [Header("Game Center")]
    [SerializeField] private string leaderboardId = "com.goldenarmorstudio.coloriq.leaderboard";
    [SerializeField] private string achievementId = "com.goldenarmorstudio.coloriq.achievement";
    [SerializeField] private string firstPlayAchievementId = "ColorIQSpectrumFirstIQ";
    private const string FirstPlayPrefKey = "ColorIQSpectrumFirstPlayAwarded";
#endif

    private Text userScoreText;
    private Text messageText;
    private readonly List<Text> leaderboardEntryTexts = new List<Text>(10);
    private bool uiBuilt;
    [Header("Monetization")]
    [SerializeField] private List<GameObject> additionalMonetizationObjects = new List<GameObject>();
    private readonly List<GameObject> monetizationTargets = new List<GameObject>();
    [SerializeField] private GameObject removeAdsObject;
    [SerializeField] private string removeAdsObjectName = "RemoveAdsButton";
    private Button removeAdsButton;
    private RectTransform leaderboardPanel;

    private async void Start()
    {
        BuildScoreboardUI();
        InitializeMonetizationUI();
        await SetupDataAsync();
        UpdateMonetizationVisibility();
    }

    private void OnEnable()
    {
        SubscribeToAdsRemovedEvents(true);
    }

    private void OnDisable()
    {
        SubscribeToAdsRemovedEvents(false);
    }

    private async Task SetupDataAsync()
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        await UpdateGameCenterDataAsync();
#else
        UpdateUserScoreUI(GetStoredScore(), null);
        ShowMessage("Game Center leaderboard available on iOS builds.");
#endif
    }

    private void BuildScoreboardUI()
    {
        if (uiBuilt)
        {
            return;
        }

        var canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[ShareIQ] Canvas not found in scene.");
            return;
        }

        foreach (Transform child in canvas.transform)
        {
            if (child.gameObject.name == removeAdsObjectName)
            {
                continue;
            }
            child.gameObject.SetActive(false);
        }

        var panelGO = new GameObject("LeaderboardPanel", typeof(RectTransform), typeof(RoundedPanel));
        var panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.SetParent(canvas.transform, false);
        panelRect.anchorMin = new Vector2(0.1f, 0.1f);
        panelRect.anchorMax = new Vector2(0.9f, 0.9f);
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        leaderboardPanel = panelRect;
        var roundedPanel = panelGO.GetComponent<RoundedPanel>();
        roundedPanel.color = new Color(0.05f, 0.07f, 0.11f, 0.95f);
        roundedPanel.NormalizedCornerRadius = 0.08f;

        var layout = panelGO.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 12f;
        layout.padding = new RectOffset(24, 24, 24, 24);

        userScoreText = CreateText("UserScore", panelRect, 84, FontStyle.Bold, TextAnchor.MiddleCenter);
        messageText = CreateText("Message", panelRect, 37, FontStyle.Italic, TextAnchor.MiddleCenter);
        messageText.color = new Color(0.85f, 0.85f, 0.85f, 1f);

        var header = CreateText("Header", panelRect, 74, FontStyle.Bold, TextAnchor.MiddleCenter);
        header.text = "Top 10 Global Scores";

        for (int i = 0; i < 10; i++)
        {
            var entry = CreateText($"Entry{i + 1}", panelRect, 73, FontStyle.Normal, TextAnchor.MiddleLeft);
            entry.text = $"{i + 1}. —";
            leaderboardEntryTexts.Add(entry);
        }

        var playAgainButton = CreateButton("PlayAgainButton", panelRect, "Play Again", 78, 150f);
        playAgainButton.onClick.AddListener(RestartGameScene);

#if UNITY_IOS || UNITY_EDITOR_OSX
        var challengeButton = CreateButton("ChallengeButton", panelRect, "Challenge Friends", 68);
        challengeButton.onClick.AddListener(OnChallengeFriendsClicked);
        challengeButton.gameObject.SetActive(!Application.isEditor);
#endif

        uiBuilt = true;
    }

    private static Text CreateText(string name, RectTransform parent, int fontSize, FontStyle style, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }

    private Button CreateButton(string name, RectTransform parent, string label, int fontSize = 22, float fixedHeight = -1f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        float height = fixedHeight > 0f ? fixedHeight : 120f;
        rect.sizeDelta = new Vector2(0f, height);

        var layoutElement = go.AddComponent<LayoutElement>();
        layoutElement.minHeight = height;
        layoutElement.preferredHeight = height;
        layoutElement.flexibleHeight = 0;
        layoutElement.minWidth = 400f;
        layoutElement.flexibleWidth = 1f;

        var image = go.GetComponent<Image>();
        image.color = new Color(0.15f, 0.35f, 0.55f, 1f);

        var button = go.AddComponent<Button>();
        var colors = button.colors;
        colors.highlightedColor = new Color(0.2f, 0.45f, 0.65f, 1f);
        colors.pressedColor = new Color(0.1f, 0.25f, 0.4f, 1f);
        button.colors = colors;

        var text = CreateText($"{name}_Label", rect, fontSize, FontStyle.Bold, TextAnchor.MiddleCenter);
        text.text = label;

        return button;
    }

    private void InitializeMonetizationUI()
    {
        EnsureRemoveAdsReference();
        foreach (var extraObject in additionalMonetizationObjects)
        {
            RegisterMonetizationObject(extraObject);
        }
    }

    private void RegisterMonetizationObject(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        if (!monetizationTargets.Contains(target))
        {
            monetizationTargets.Add(target);
        }
    }

    private void EnsureRemoveAdsReference()
    {
        if (removeAdsObject == null)
        {
            removeAdsObject = GameObject.Find(removeAdsObjectName);
        }

        if (removeAdsObject == null)
        {
            Debug.LogWarning("[ShareIQ] RemoveAdsButton object not found in scene.");
            return;
        }

        if (removeAdsObject.GetComponent<RemoveAdsPurchaseTrigger>() == null)
        {
            removeAdsObject.AddComponent<RemoveAdsPurchaseTrigger>();
        }

        removeAdsButton = removeAdsObject.GetComponent<Button>();
        if (removeAdsButton == null)
        {
            Debug.LogWarning("[ShareIQ] RemoveAdsButton is missing a Button component.");
        }
        else
        {
            removeAdsButton.onClick.RemoveListener(HandleRemoveAdsButton);
            removeAdsButton.onClick.AddListener(HandleRemoveAdsButton);
        }

        RegisterMonetizationObject(removeAdsObject);
    }

    private void RestartGameScene()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("GameScene");
    }

    private int GetStoredScore() => PlayerPrefs.GetInt(ScorePrefKey, 0);

    private void UpdateUserScoreUI(int score, long? rank)
    {
        if (userScoreText == null)
        {
            return;
        }

        userScoreText.text = rank.HasValue
            ? $"Your Color IQ: {score} (Rank #{rank.Value})"
            : $"Your Color IQ: {score}";
    }

    private void ShowMessage(string text)
    {
        if (messageText != null)
        {
            messageText.text = text;
        }
    }

    private void UpdateLeaderboardUI(List<(long rank, string alias, long score)> entries)
    {
        for (int i = 0; i < leaderboardEntryTexts.Count; i++)
        {
            var label = leaderboardEntryTexts[i];
            if (label == null)
            {
                continue;
            }

            if (i < entries.Count)
            {
                var entry = entries[i];
                label.text = $"{entry.rank,2}. {entry.alias} — {entry.score}";
            }
            else
            {
                label.text = $"{i + 1}. —";
            }
        }
    }

    private void HandleRemoveAdsButton()
    {
        var manager = InAppPurchaseManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[IAP] InAppPurchaseManager not present in scene.");
            return;
        }

        manager.BuyRemoveAds();
    }

    private void UpdateMonetizationVisibility()
    {
        bool adsRemoved = InAppPurchaseManager.Instance != null && InAppPurchaseManager.Instance.AdsRemoved;
        bool shouldShow = !adsRemoved;

        foreach (var target in monetizationTargets)
        {
            if (target == null)
            {
                continue;
            }

            if (target.activeSelf != shouldShow)
            {
                target.SetActive(shouldShow);
            }

            var button = target.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = shouldShow;
            }
        }
    }

    private void SubscribeToAdsRemovedEvents(bool subscribe)
    {
        var manager = InAppPurchaseManager.Instance;
        if (manager == null)
        {
            return;
        }

        if (subscribe)
        {
            manager.OnAdsRemoved += UpdateMonetizationVisibility;
        }
        else
        {
            manager.OnAdsRemoved -= UpdateMonetizationVisibility;
        }
    }

#if UNITY_IOS || UNITY_EDITOR_OSX
    private async Task UpdateGameCenterDataAsync()
    {
        int score = GetStoredScore();
        UpdateUserScoreUI(score, null);

        if (Application.isEditor)
        {
            ShowMessage("Leaderboard loads on device builds.");
            return;
        }

        var localPlayer = GKLocalPlayer.Local;
        if (localPlayer == null || !localPlayer.IsAuthenticated)
        {
            ShowMessage("Sign into Game Center to view leaderboard.");
            return;
        }

        ShowMessage("Syncing with Game Center…");

        GKLeaderboard leaderboard = await ResolveLeaderboardAsync();
        if (leaderboard == null)
        {
            ShowMessage("Leaderboard not found. Check the ID in ShareIQController.");
            return;
        }

        await SubmitScoreAsync(leaderboard, score, localPlayer);
        await GameCenterService.ReportAchievementAsync(achievementId, 100.0);
        await AwardFirstPlayAchievementAsync();
        await LoadLeaderboardEntriesAsync(leaderboard, score);
    }

    private async Task<GKLeaderboard> ResolveLeaderboardAsync()
    {
        if (string.IsNullOrEmpty(leaderboardId))
        {
            return null;
        }

        try
        {
            var leaderboards = await GKLeaderboard.LoadLeaderboards(leaderboardId);
            if (leaderboards != null && leaderboards.Count > 0)
            {
                return leaderboards[0];
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShareIQ] Failed to load leaderboard list: {ex}");
        }

        return null;
    }

    private async Task SubmitScoreAsync(GKLeaderboard leaderboard, int score, GKPlayer player)
    {
        try
        {
            await leaderboard.SubmitScore(score, 0, player);
            Debug.Log($"[ShareIQ] Submitted score {score} to leaderboard '{leaderboardId}'.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShareIQ] Failed to submit leaderboard score: {ex}");
        }
    }

    private async Task LoadLeaderboardEntriesAsync(GKLeaderboard leaderboard, int localScore)
    {
        try
        {
            GKLeaderboardLoadEntriesResponse response =
                await leaderboard.LoadEntries(GKLeaderboard.PlayerScope.Global, GKLeaderboard.TimeScope.AllTime, 1, 10);

            var entries = new List<(long rank, string alias, long score)>();
            if (response.Entries != null)
            {
                foreach (var entry in response.Entries)
                {
                    string alias = entry.Player?.DisplayName ?? "Player";
                    entries.Add((entry.Rank, alias, entry.Score));
                }
            }

            long? rank = null;
            if (response.LocalPlayerEntry != null)
            {
                rank = response.LocalPlayerEntry.Rank;
            }

            UpdateUserScoreUI(localScore, rank);
            UpdateLeaderboardUI(entries);
            ShowMessage(entries.Count > 0 ? string.Empty : "No leaderboard data yet.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShareIQ] Failed to load leaderboard entries: {ex}");
            ShowMessage("Unable to load leaderboard entries.");
        }
    }

    private async Task AwardFirstPlayAchievementAsync()
    {
        if (string.IsNullOrEmpty(firstPlayAchievementId))
        {
            return;
        }

        if (PlayerPrefs.GetInt(FirstPlayPrefKey, 0) == 1)
        {
            return;
        }

        await GameCenterService.ReportAchievementAsync(firstPlayAchievementId, 100.0);
        PlayerPrefs.SetInt(FirstPlayPrefKey, 1);
        PlayerPrefs.Save();
    }
#endif

#if UNITY_IOS || UNITY_EDITOR_OSX
    private async void OnChallengeFriendsClicked()
    {
        if (Application.isEditor)
        {
            ShowMessage("Challenges run on device builds.");
            return;
        }

        var localPlayer = GKLocalPlayer.Local;
        if (localPlayer == null || !localPlayer.IsAuthenticated)
        {
            ShowMessage("Sign into Game Center to challenge friends.");
            return;
        }

        try
        {
            await GKAccessPoint.Shared.TriggerForChallenges();
            ShowMessage("Game Center challenge launched. Tell your friends: 'youcantbeatme'.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ShareIQ] Challenge failed: {ex}");
            ShowMessage("Failed to send challenge.");
        }
    }
#endif
}
