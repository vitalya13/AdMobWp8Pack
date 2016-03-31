using UnityEngine;
using AdMobWp8;

public class AdMob : MonoBehaviour
{
    public bool bannerEnable;
    public string unitId = string.Empty;
    public AdFormats format;
    public HorizontalAlignment horizontalAlignment;
    public VerticalAlignment verticalAlignment;
    public bool interstitialEnable;
    public string interstitialUnitId = string.Empty;
    public bool forceTesting;

    AdMobWp8Plugin adMobWp8Plugin = null;

    void Awake()
    {
        AdMobInitData data = new AdMobInitData();
        data.bannerEnable = bannerEnable;
        data.adUnitId = unitId;
        data.format = format;
        data.horizontalAlignment = horizontalAlignment;
        data.verticalAlignment = verticalAlignment;
        data.forceTesting = forceTesting;
        data.receivedAd = receivedAd;
        data.failedToReceiveAd = failedToReceiveAd;
        data.interstitialEnable = interstitialEnable;
        data.interstitialUnitId = interstitialUnitId;

#if !UNITY_EDITOR && UNITY_WP8
        adMobWp8Plugin = new AdMobWp8Plugin(data);
#endif
    }

    void Start()
    {
        if (adMobWp8Plugin != null && interstitialEnable)
        {
            interstitialShow();
        }
    }

    void OnGUI()
    {
        if (adMobWp8Plugin != null)
        {
            adMobWp8Plugin.checkSignal();
        }
    }

    void OnDestroy()
    {
        if (adMobWp8Plugin != null)
        {
            if (bannerEnable)
            {
                adMobWp8Plugin.bannerDestroy();
            }

            if (interstitialEnable)
            {
                adMobWp8Plugin.interstitialDestroy();
            }

            adMobWp8Plugin = null;
        }
    }

    /// <summary>
    /// Handler received ad
    /// </summary>
    void receivedAd()
    {
    }

    /// <summary>
    /// Handler error ad
    /// </summary>
    /// <param name="code"></param>
    void failedToReceiveAd(AdErrorCode code)
    {
    }

    public void interstitialShow()
    {
        if (adMobWp8Plugin != null)
        {
            adMobWp8Plugin.interstitialShow();
        }
    }
}
