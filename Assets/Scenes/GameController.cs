using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

public class GameController : MonoBehaviour
{
    private const string ScorePrefKey = "ShareIQScore";

    GameBoard gameBoard;
    GameObject ScoreIQ;
    GameObject PlayAgain;
    GameObject Share;
    GameObject ShareIQCanvas;
    Canvas ShareIQCanvasSR;
    Text ScoreIQText;
    float TimeLeft = 60.0f;
    bool bonusTileSpawned = false;
    bool isGameOver = false;
    bool isLoadingShareScene = false;
    bool uiBindingWarningLogged = false;

    GameObject timer;
    Text timerText;

    GameObject score;

    Text scoreText;
    Button shareButton;
    Button playAgainButton;
    Button removeAdsButton;

    [Header("LevelPlay (ironSource)")]
    [SerializeField] string ironSourceAppKey = "245718645";
    [SerializeField] string rewardedPlacementName = "98n6yjsog4opnciv";
    [SerializeField] bool simulateRewardedAdsInEditor = true;

    bool rewardedAvailable;
    bool adShowing;
    Action pendingRewardCallback;

    void Start()
    {
        InAppPurchaseManager.EnsureInstanceExists();
        InitializeLevelPlayAds();

        ScoreIQ = GameObject.Find("IQScore");
        PlayAgain = GameObject.Find("PlayAgain");
        Share = GameObject.Find("Share");
        GameObject removeAdsObject = GameObject.Find("RemoveAdsButton");
        ShareIQCanvas = GameObject.Find("ShareIQCanvas");
        ScoreIQText = ScoreIQ.GetComponent<Text>();
        playAgainButton = PlayAgain.GetComponent<Button>();
        shareButton = Share.GetComponent<Button>();
        if (removeAdsObject != null) {
            if (removeAdsObject.GetComponent<RemoveAdsPurchaseTrigger>() == null) {
                removeAdsObject.AddComponent<RemoveAdsPurchaseTrigger>();
            }
            removeAdsButton = removeAdsObject.GetComponent<Button>();
        }
        if (shareButton != null)
        {
            shareButton.enabled = false;
            shareButton.interactable = false;
            shareButton.gameObject.SetActive(false);
        }
        ShareIQCanvasSR = ShareIQCanvas.GetComponent<Canvas>();
        if (ShareIQCanvasSR != null)
        {
            ShareIQCanvasSR.enabled = false;
        }
        playAgainButton.onClick.AddListener(delegate { playAgain(); });
        if (shareButton != null)
        {
            shareButton.onClick.AddListener(LoadShareScene);
        }
        if (removeAdsButton != null) {
            removeAdsButton.onClick.AddListener(HandleRemoveAdsButton);
            EnsureRemoveAdsButtonLayer();
        }
        UpdateRemoveAdsButton();

        if (InAppPurchaseManager.Instance != null) {
            InAppPurchaseManager.Instance.OnAdsRemoved += UpdateRemoveAdsButton;
        }
        gameBoard = new GameBoard(this);
        gameBoard.buildGameBoard();
        bonusTileSpawned = false;
        timer = GameObject.Find("Timer");
        score = GameObject.Find("Score");
        timerText = timer.GetComponent<Text>();
        scoreText = score.GetComponent<Text>();
    }

    void OnDestroy() {
        if (InAppPurchaseManager.Instance != null) {
            InAppPurchaseManager.Instance.OnAdsRemoved -= UpdateRemoveAdsButton;
        }
#if LEVELPLAY_SDK && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        IronSourceEvents.onRewardedVideoAvailabilityChangedEvent -= OnRewardedVideoAvailabilityChanged;
        IronSourceEvents.onRewardedVideoAdShowFailedEvent -= OnRewardedVideoShowFailed;
        IronSourceEvents.onRewardedVideoAdClosedEvent -= OnRewardedVideoClosed;
        IronSourceEvents.onRewardedVideoAdRewardedEvent -= OnRewardedVideoRewarded;
        IronSourceEvents.onRewardedVideoAdOpenedEvent -= OnRewardedVideoOpened;
        IronSourceEvents.onRewardedVideoAdStartedEvent -= OnRewardedVideoStarted;
        IronSourceEvents.onRewardedVideoAdEndedEvent -= OnRewardedVideoEnded;
#endif
    }

    
    void Update()
    {   
        if (isGameOver)
        {
            return;
        }

        if (adShowing) {
            // Pause time and interaction while a rewarded ad is onscreen.
            return;
        }

        TimeLeft -= Time.deltaTime;
        if (TimeLeft < 0.0f)
        {
            TimeLeft = 0.0f;
        }
        if (timerText != null) {
            timerText.text = Math.Floor(TimeLeft).ToString();
        } else {
            LogMissingUIBinding("Timer");
        }
        if (TimeLeft <= 0.0f) {
            EndGame();
            return;
        }
        if (gameBoard != null) {
            string scoreValue = gameBoard.getScore().ToString();
            if (scoreText != null) {
                scoreText.text = scoreValue;
            }
            else {
                LogMissingUIBinding("Score");
            }
            if (ScoreIQText != null) {
                ScoreIQText.text = scoreValue;
            }
            else {
                LogMissingUIBinding("IQScore");
            }
        }

        /* Temporarily disabled: late-round Level 2 tile spawn.
        if (gameBoard != null) {
            if (!bonusTileSpawned && TimeLeft <= 5.0f && TimeLeft > 0.0f) {
                if (gameBoard.SpawnBonusTile()) {
                    bonusTileSpawned = true;
                }
            } else if (bonusTileSpawned && !gameBoard.HasActiveBonusTile()) {
                bonusTileSpawned = false;
            }
        }
        */

        if (Input.touchCount >= 1) {
            if( Input.touches[0].phase == TouchPhase.Began ){
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.touches[0].position), Vector2.zero);
                if(hit.collider) {
                    gameBoard.addPoints(hit.collider.gameObject.name, hit, setTime);
                }  
            }
        }
    }

    public void setTime(float timeAdded){
        TimeLeft = TimeLeft + timeAdded;
    }

    public float getTime(){
        return TimeLeft;
    }

    void playAgain() {
        if (ShouldUseRewardedAds()) {
            RequestRewardedAd(ResetAndRestart);
            return;
        }

        ResetAndRestart();
    }

    void ResetAndRestart() {
        ShareIQCanvasSR.enabled = false;
        TimeLeft = 60f;
        scoreText.text = "0";
        ScoreIQText.text = "0";
        gameBoard = new GameBoard(this);
        gameBoard.buildGameBoard();
        bonusTileSpawned = false;
        isGameOver = false;
        isLoadingShareScene = false;
        if (shareButton != null)
        {
            shareButton.enabled = false;
            shareButton.interactable = false;
            shareButton.gameObject.SetActive(false);
        }
        UpdateRemoveAdsButton();
    }

    void LoadShareScene() {
        if (isLoadingShareScene) {
            return;
        }

        SaveScore();
        isLoadingShareScene = true;
        StartCoroutine(LoadShareSceneAsync());
    }

    IEnumerator LoadShareSceneAsync() {
        AsyncOperation loadScene = SceneManager.LoadSceneAsync("ShareIQScene");
        while(!loadScene.isDone) {
            yield return null;
        }
    }

    void EndGame() {
        if (isGameOver) {
            return;
        }

        TimeLeft = 0.0f;
        timerText.text = "0";
        isGameOver = true;
        gameBoard.removeGamePieces();
        LoadShareScene();
    }

    void SaveScore() {
        int scoreValue = gameBoard != null ? gameBoard.getScore() : 0;
        PlayerPrefs.SetInt(ScorePrefKey, scoreValue);
        PlayerPrefs.Save();
    }

    void HandleRemoveAdsButton() {
        if (InAppPurchaseManager.Instance == null) {
            Debug.LogWarning("[IAP] InAppPurchaseManager not present in scene.");
            return;
        }
        InAppPurchaseManager.Instance.BuyRemoveAds();
    }

    void UpdateRemoveAdsButton() {
        bool adsRemoved = InAppPurchaseManager.Instance != null && InAppPurchaseManager.Instance.AdsRemoved;
        if (removeAdsButton != null) {
            removeAdsButton.gameObject.SetActive(!adsRemoved);
            removeAdsButton.interactable = !adsRemoved;
            EnsureRemoveAdsButtonLayer();
        }
    }

    void EnsureRemoveAdsButtonLayer() {
        if (removeAdsButton == null) {
            return;
        }

        RectTransform rect = removeAdsButton.GetComponent<RectTransform>();
        if (rect != null) {
            rect.SetAsLastSibling();
            Vector3 anchored = rect.anchoredPosition3D;
            anchored.z = -10f;
            rect.anchoredPosition3D = anchored;
        }
    }

    void LogMissingUIBinding(string name) {
#if UNITY_EDITOR
        if (uiBindingWarningLogged) {
            return;
        }
        Debug.LogWarning($"[UI] Missing reference for {name}. Ensure the object exists in the scene.");
        uiBindingWarningLogged = true;
#endif
    }

    #region Unity Ads

    bool ShouldUseRewardedAds() {
        if (InAppPurchaseManager.Instance != null && InAppPurchaseManager.Instance.AdsRemoved) {
            return false;
        }
#if UNITY_EDITOR
        return simulateRewardedAdsInEditor;
#elif UNITY_IOS || UNITY_ANDROID
#if LEVELPLAY_SDK
        return rewardedAvailable;
#else
        return false;
#endif
#else
        return false;
#endif
    }

    void InitializeLevelPlayAds() {
#if LEVELPLAY_SDK && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (string.IsNullOrEmpty(ironSourceAppKey)) {
            Debug.LogWarning("[Ads] IronSource app key missing. Rewarded ads disabled.");
            return;
        }
        RegisterLevelPlayEvents();
        IronSource.Agent.validateIntegration();
        IronSource.Agent.shouldTrackNetworkState(true);
        IronSource.Agent.init(ironSourceAppKey, IronSourceAdUnits.REWARDED_VIDEO);
#else
        rewardedAvailable = simulateRewardedAdsInEditor;
#endif
    }

    void RegisterLevelPlayEvents() {
#if LEVELPLAY_SDK && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        IronSourceEvents.onRewardedVideoAvailabilityChangedEvent += OnRewardedVideoAvailabilityChanged;
        IronSourceEvents.onRewardedVideoAdShowFailedEvent += OnRewardedVideoShowFailed;
        IronSourceEvents.onRewardedVideoAdClosedEvent += OnRewardedVideoClosed;
        IronSourceEvents.onRewardedVideoAdRewardedEvent += OnRewardedVideoRewarded;
        IronSourceEvents.onRewardedVideoAdOpenedEvent += OnRewardedVideoOpened;
        IronSourceEvents.onRewardedVideoAdStartedEvent += OnRewardedVideoStarted;
        IronSourceEvents.onRewardedVideoAdEndedEvent += OnRewardedVideoEnded;
#endif
    }

    public void RequestRewardedAd(Action onComplete) {
        if (adShowing) {
            return;
        }

        if (!ShouldUseRewardedAds()) {
            onComplete?.Invoke();
            return;
        }

        pendingRewardCallback = onComplete;
        ShowRewardedAdInternal();
    }

    void ShowRewardedAdInternal() {
#if LEVELPLAY_SDK
#if UNITY_EDITOR
        if (simulateRewardedAdsInEditor) {
            Debug.Log("[Ads] Simulating rewarded ad in editor.");
            StartCoroutine(SimulateEditorRewardedAd());
            return;
        }
#endif
#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        if (!rewardedAvailable) {
            Debug.LogWarning("[Ads] Rewarded ad not ready; continuing without showing.");
            CompleteRewardSequence();
            return;
        }

        adShowing = true;
        Debug.Log("[Ads] Showing LevelPlay rewarded ad.");
        if (string.IsNullOrEmpty(rewardedPlacementName)) {
            IronSource.Agent.showRewardedVideo();
        } else {
            IronSource.Agent.showRewardedVideo(rewardedPlacementName);
        }
#else
        CompleteRewardSequence();
#endif
#else
        if (simulateRewardedAdsInEditor) {
            Debug.Log("[Ads] Simulating rewarded ad without LevelPlay SDK.");
            StartCoroutine(SimulateEditorRewardedAd());
        } else {
            CompleteRewardSequence();
        }
#endif
    }

    IEnumerator SimulateEditorRewardedAd() {
        adShowing = true;
        yield return new WaitForSeconds(0.5f);
        adShowing = false;
        CompleteRewardSequence();
    }

    void CompleteRewardSequence() {
        var callback = pendingRewardCallback;
        pendingRewardCallback = null;
        callback?.Invoke();
    }

#if LEVELPLAY_SDK && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    void OnRewardedVideoAvailabilityChanged(bool available) {
        rewardedAvailable = available;
        Debug.Log($"[Ads] Rewarded availability changed: {available}");
    }

    void OnRewardedVideoShowFailed(IronSourceError error) {
        Debug.LogWarning($"[Ads] Rewarded show failed: {error.getDescription()}");
        adShowing = false;
        CompleteRewardSequence();
    }

    void OnRewardedVideoClosed() {
        Debug.Log("[Ads] Rewarded ad closed.");
        adShowing = false;
        CompleteRewardSequence();
    }

    void OnRewardedVideoRewarded(IronSourcePlacement placement) {
        Debug.Log($"[Ads] Reward granted: {placement?.getRewardName() ?? "reward"}");
    }

    void OnRewardedVideoOpened() {
        Debug.Log("[Ads] Rewarded ad opened.");
    }

    void OnRewardedVideoStarted() {
        Debug.Log("[Ads] Rewarded ad started.");
    }

    void OnRewardedVideoEnded() {
        Debug.Log("[Ads] Rewarded ad ended.");
    }
#endif

    #endregion
}
