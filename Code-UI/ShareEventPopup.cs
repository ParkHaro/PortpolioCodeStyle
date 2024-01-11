using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Gomble.MergeLion.Manager;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Haro.View
{
    public class ShareEventPopup : BasePopup
    {
        [SerializeField] private TextMeshProUGUI titleTextLoc;
        [SerializeField] private TextMeshProUGUI descriptionTextLoc;
        [SerializeField] private TextMeshProUGUI shareButtonTextLoc;

        [SerializeField] private TextMeshProUGUI resetTimeText;

        [SerializeField] private GameObject shareRewardObject;

        [SerializeField] private Button shareButton;

        private CancellationTokenSource _resetTimeTextUpdateCts;

        private void OnDestroy()
        {
            Helper.CancelAsyncSafe(ref _resetTimeTextUpdateCts);
        }

        public override void Open(object data = null, UnityAction done = null)
        {
            base.Open(data, done);

            SafeOneAddListener(closeButton, OnBtnCloseClicked);
            SafeOneAddListener(shareButton, OnBtnShareClicked);
            
            Helper.CreateCancellationTokenSourceSafe(ref _resetTimeTextUpdateCts);
            ResetTimeTextUpdateProcessAsync(_resetTimeTextUpdateCts.Token).Forget();
            
            Localization();
            resetTimeText.text = GlobalManagerTable.TimeManager.RemainTimeStringToEndDay;
            Refresh();
            LobbyManagerTable.RedDotManager.HomeRedDotProcess();

            GlobalManagerTable.TimeManager.OnChangeDay += Refresh;
        }

        public override void Close()
        {
            base.Close();
            shareRewardObject.SetActive(false);

            GlobalManagerTable.TimeManager.OnChangeDay -= Refresh;
        }

        private void Refresh()
        {
            int shareRewardRemainCount = GlobalManagerTable.DataManager.UserData.State.ShareRewardRemainCount;
            shareRewardObject.SetActive(shareRewardRemainCount > 0);
        }

        protected override void Localization()
        {
            base.Localization();
            GlobalStringManager stringManager = GlobalManagerTable.StringManager;
            titleTextLoc.text = stringManager.GetStringByKey(Key.String.TextShare1);
            descriptionTextLoc.text = stringManager.GetStringByKey(Key.String.TextShare2);
            descriptionTextLoc.ForceMeshUpdate();

            shareButtonTextLoc.text = stringManager.GetStringByKey(Key.String.TextShare5);
        }

        private void OnBtnShareClicked()
        {
            if (ShareManager.Instance.CanShareEvent())
            {
                if (ShareManager.Instance.ShowShareText(Const.ShareURL, "Share App"))
                {
                    int shareEventCount = GlobalManagerTable.DataManager.UserData.State.ShareRewardRemainCount;
                    shareEventCount--;
                    GlobalManagerTable.DataManager.UserData.State.UpdateShareRewardRemainCount(shareEventCount, null, false);
                    Action serverCallback = () =>
                    {
                        LobbyManagerTable.RedDotManager.HomeRedDotProcess();
                        Close();
                    };
                    ItemManager.Instance.AddAllItem(
                        1,
                        serverCallback,
                        true,
                        true,
                        shareButton.transform.position, 
                        LobbyManagerTable.LobbyManager.StartButtonTransform.position,
                        Vector2.up * 8f);
                }
            }
            else
            {
                ShareManager.Instance.ShowShareText(Const.ShareURL, "Share App");
                Close();
            }
            Refresh();
        }
        
        private async UniTaskVoid ResetTimeTextUpdateProcessAsync(CancellationToken cancellationToken)
        {
            try
            {
                do
                {
                    resetTimeText.text = GlobalManagerTable.TimeManager.RemainTimeStringToEndDay;
                    await UniTask.Delay(TimeSpan.FromSeconds(1f), true, cancellationToken: cancellationToken);
                } while (true);
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Utils.MakeLog($"ERROR : {e}");
                }
            }
        }
    }
}