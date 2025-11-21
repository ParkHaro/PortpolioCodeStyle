using Gomble.MergeLion.Manager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Haro.View
{
    public class TopPanel : BasePanel
    {
        [SerializeField] private Button shareButton;
        [SerializeField] private GameObject shareRedDotObject;

        [SerializeField] private Button settingButton;

        bool isClickedShareButton = false;

        public override void Open(object data = null, UnityAction done = null)
        {
            base.Open(data, done);

            SafeOneAddListener(shareButton, OnBtnShareClicked);
            SafeOneAddListener(settingButton, OnBtnSettingClicked);
            
            Refresh();
        }

        private void OnBtnShareClicked()
        {
            LobbyManagerTable.UIManager.OpenPopup<ShareEventPopup>();
            LobbyManagerTable.RedDotManager.HomeRedDotProcess();
            isClickedShareButton = true;

            Refresh();
        }

        private void OnBtnSettingClicked()
        {
            LobbyManagerTable.UIManager.OpenPopup<SettingPopup>();
        }

        public void Refresh()
        {
            int shareRewardRemainCount = GlobalManagerTable.DataManager.UserData.State.ShareRewardRemainCount;
            shareRedDotObject.SetActive(shareRewardRemainCount > 0 && !isClickedShareButton);
        }
    }
}