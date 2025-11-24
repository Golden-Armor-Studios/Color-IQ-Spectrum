using System;
using UnityEngine;
using FB;
#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

public class InAppPurchaseManager : MonoBehaviour
#if UNITY_PURCHASING
    , IStoreListener
#endif
{
    public static InAppPurchaseManager Instance { get; private set; }

    private const string RemoveAdsPrefKey = "ColorIQ_RemoveAds";
    [SerializeField] string removeAdsProductId = "color_iq_remove_ads";
    [SerializeField] bool simulatePurchaseInEditor = false;
#if UNITY_IOS || UNITY_EDITOR_OSX
    [SerializeField] string vipAchievementId = "ColorIQSpectrumVIP";
#endif

    public bool AdsRemoved => PlayerPrefs.GetInt(RemoveAdsPrefKey, 0) == 1;

    public event Action OnAdsRemoved;

#if UNITY_PURCHASING
    IStoreController storeController;
    IExtensionProvider extensionProvider;
#endif

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
#if UNITY_PURCHASING
        if (!simulatePurchaseInEditor)
        {
            InitializePurchasing();
        }
#endif
    }

#if UNITY_PURCHASING
    void InitializePurchasing()
    {
        if (storeController != null)
        {
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance(AppStore.AppleAppStore));
        builder.AddProduct(removeAdsProductId, ProductType.NonConsumable);
        UnityPurchasing.Initialize(this, builder);
    }
#endif

    public void BuyRemoveAds()
    {
        if (AdsRemoved)
        {
            return;
        }

#if UNITY_PURCHASING
        if (Application.isEditor && simulatePurchaseInEditor)
        {
            CompleteRemoveAdsPurchase();
            return;
        }

        if (storeController == null)
        {
            Debug.LogWarning("[IAP] Store not initialized yet.");
            return;
        }

        storeController.InitiatePurchase(removeAdsProductId);
#else
        CompleteRemoveAdsPurchase();
#endif
    }

    async void CompleteRemoveAdsPurchase()
    {
        PlayerPrefs.SetInt(RemoveAdsPrefKey, 1);
        PlayerPrefs.Save();
        OnAdsRemoved?.Invoke();
        Debug.Log("[IAP] Remove ads purchase completed.");

#if UNITY_IOS || UNITY_EDITOR_OSX
#endif
        AnalyticsService.LogIapConversion(removeAdsProductId);
#if UNITY_IOS || UNITY_EDITOR_OSX
        if (!string.IsNullOrEmpty(vipAchievementId))
        {
            await GameCenterService.ReportAchievementAsync(vipAchievementId, 100.0);
        }
#endif
    }

#if UNITY_PURCHASING
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        extensionProvider = extensions;
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogWarning($"[IAP] Initialization failed: {error}");
    }

#if UNITY_2017_1_OR_NEWER
    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogWarning($"[IAP] Initialization failed: {error} - {message}");
    }
#endif

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        if (string.Equals(args.purchasedProduct.definition.id, removeAdsProductId, StringComparison.Ordinal))
        {
            CompleteRemoveAdsPurchase();
            return PurchaseProcessingResult.Complete;
        }

        return PurchaseProcessingResult.Pending;
    }

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAP] Purchase failed: {product.definition.id} - {failureReason}");
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureInstanceExistsOnLoad()
    {
        EnsureInstanceExists();
    }

    public static void EnsureInstanceExists()
    {
        if (Instance != null)
        {
            return;
        }

        var existing = UnityEngine.Object.FindObjectOfType<InAppPurchaseManager>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        var go = new GameObject("InAppPurchaseManager");
        go.AddComponent<InAppPurchaseManager>();
    }
}
