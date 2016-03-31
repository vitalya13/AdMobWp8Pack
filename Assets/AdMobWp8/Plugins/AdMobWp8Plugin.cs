#if POST_BUILD

using GoogleAds;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AdMobWp8
{
    /// <summary>
    /// Add AdMobWp8.Creator.init(DrawingSurfaceBackground, this.Dispatcher); 
    /// in MainPage.xaml.cs constructor
    /// </summary>
    public static class Creator
    {
        static public void init(Grid grid, Dispatcher dispatcher)
        {
            AdMobWp8Plugin.initNative = (AdMobWp8Plugin adMobWp8Plugin) => {
                adMobWp8Plugin.native = new AdMobWp8Plugin_Native(grid, dispatcher, adMobWp8Plugin);
            };
        }        
    }

    public class AdMobWp8Plugin_Native : IAdMobWp8Plugin
    {
        private AdView bannerAd;
        private InterstitialAd interstitialAd;
        private Grid grid;
        private AdMobWp8Plugin adMobWp8Plugin;
        private AdMobInitData adMobInitData;
        private Dispatcher dispatcher;

        public AdMobWp8Plugin_Native(Grid grid_, Dispatcher dispatcher_, AdMobWp8Plugin adMobWp8Plugin_)
        {
            grid = grid_;
            adMobWp8Plugin = adMobWp8Plugin_;
            adMobInitData = adMobWp8Plugin.adMobInitData;
            dispatcher = dispatcher_;

            dispatcher.BeginInvoke(() => {
                try
                {
                    if (adMobInitData.bannerEnable)
                    {
                        bannerAd = new AdView
                        {
                            Format = (GoogleAds.AdFormats)adMobInitData.format,
                            AdUnitID = adMobInitData.adUnitId
                        };
                        bannerAd.HorizontalAlignment = (System.Windows.HorizontalAlignment)adMobInitData.horizontalAlignment;
                        bannerAd.VerticalAlignment = (System.Windows.VerticalAlignment)adMobInitData.verticalAlignment;
                        bannerAd.ReceivedAd += bannerAd_ReceivedAd;
                        bannerAd.FailedToReceiveAd += bannerAd_FailedToReceiveAd;

                        AdRequest request = new AdRequest();
                        request.ForceTesting = adMobInitData.forceTesting;
                        bannerAd.LoadAd(request);

                        grid.Children.Add(bannerAd);
                    }                    
                }
                catch { }                
            });
        }

        private void bannerAd_FailedToReceiveAd(object sender, AdErrorEventArgs e)
        {
            adMobWp8Plugin.setFailedToReceiveAdErrorCode((AdErrorCode)e.ErrorCode);            
        }

        private void bannerAd_ReceivedAd(object sender, AdEventArgs e)
        {
            adMobWp8Plugin.setReceivedAdSignal(true);
        }

        public void bannerRefresh()
        {
            dispatcher.BeginInvoke(() =>
            {
                AdRequest request = new AdRequest();
                request.ForceTesting = adMobInitData.forceTesting;
                bannerAd.LoadAd(request);
            });
        }

        public void bannerDestroy()
		{
            if (bannerAd != null)
            {
                dispatcher.BeginInvoke(() => {
                    bannerAd.IsEnabled = false;
                    grid.Children.Remove(bannerAd);
                    bannerAd = null;
                });
            }                            
        }

        public void interstitialShow()
        {
            interstitialAd = new InterstitialAd(adMobInitData.interstitialUnitId);
            interstitialAd.ReceivedAd += interstitialAd_ReceivedAd;
            AdRequest request = new AdRequest();
            request.ForceTesting = adMobInitData.forceTesting;
            interstitialAd.LoadAd(request);
        }

        private void interstitialAd_ReceivedAd(object sender, AdEventArgs e)
        {
            dispatcher.BeginInvoke(() =>
            {
                interstitialAd.ShowAd();
            });            
        }

        public void interstitialDestroy()
        {
            if (interstitialAd != null)
            {
                interstitialAd = null;
            }
        }
    }
}

#else

using System;

namespace AdMobWp8
{
    public class AdMobWp8Plugin : IAdMobWp8Plugin
    {
        public IAdMobWp8Plugin native;
        public static Action<AdMobWp8Plugin> initNative;
        bool receivedAdSignal = false;
        AdErrorCode failedToReceiveAdErrorCode = AdErrorCode.NoError;
        public AdMobInitData adMobInitData;
        static readonly object _locker = new object();

        public AdMobWp8Plugin(AdMobInitData data_)
        {
            adMobInitData = data_;
            initNative(this);
        }

        public void bannerRefresh()
        {
            native.bannerRefresh();
        }

        public void bannerDestroy()
        {
            native.bannerDestroy();
        }

        public void interstitialShow()
        {
            native.interstitialShow();
        }

        public void interstitialDestroy()
        {
            native.interstitialDestroy();
        }

        public void checkSignal()
        {
            if (receivedAdSignal)
            {
                if (adMobInitData.receivedAd != null)
                {
                    var temp = adMobInitData.receivedAd;
                    temp();
                }

                setReceivedAdSignal(false);
            }

            if (failedToReceiveAdErrorCode != AdErrorCode.NoError)
            {
                if (adMobInitData.failedToReceiveAd != null)
                {
                    var temp = adMobInitData.failedToReceiveAd;
                    temp(failedToReceiveAdErrorCode);
                }

                setFailedToReceiveAdErrorCode(AdErrorCode.NoError);
            }
        }

        public void setReceivedAdSignal(bool receivedAdSignal_)
        {
            lock (_locker)
            {
                receivedAdSignal = receivedAdSignal_;
            }
        }

        public void setFailedToReceiveAdErrorCode(AdErrorCode code)
        {
            lock (_locker)
            {
                failedToReceiveAdErrorCode = code;
            }
        }
    }

    public interface IAdMobWp8Plugin
    {
        void bannerRefresh();
        void bannerDestroy();
        void interstitialShow();
        void interstitialDestroy();
    }

    public class AdMobInitData
    {
        public bool bannerEnable;
        public string adUnitId;
        public AdFormats format;
        public HorizontalAlignment horizontalAlignment;
        public VerticalAlignment verticalAlignment;
        public bool forceTesting;
        public Action receivedAd;
        public Action<AdErrorCode> failedToReceiveAd;
        public bool interstitialEnable;
        public string interstitialUnitId;
    }

    public enum HorizontalAlignment
    {
        Left, Center, Right, Stretch
    }

    public enum VerticalAlignment
    {
        Top, Center, Bottom, Stretch
    }

    public enum AdFormats
    {
        Banner, SmartBanner
    }

    public enum AdErrorCode
    {
        NoError = 0,
        InvalidRequest = 1,
        NoFill = 2,
        NetworkError = 3,
        InternalError = 4,
        StaleInterstitial = 5,
        Cancelled = 6
    }
}

#endif