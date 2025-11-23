using UnityEngine;
using UnityEngine.EventSystems;

// Enables any GameObject (button, sprite, etc.) to trigger the Remove Ads IAP when tapped.
public class RemoveAdsPurchaseTrigger : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TryBuyRemoveAds();
    }

    private void OnMouseUpAsButton()
    {
        TryBuyRemoveAds();
    }

    private void TryBuyRemoveAds()
    {
        Debug.Log("[IAP] Remove Ads trigger tapped.");
        var manager = InAppPurchaseManager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[IAP] InAppPurchaseManager not present in scene.");
            return;
        }

        manager.BuyRemoveAds();
    }
}
