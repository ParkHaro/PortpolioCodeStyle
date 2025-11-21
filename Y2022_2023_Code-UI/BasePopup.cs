using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

namespace Haro
{
    public class BasePopup : MonoBehaviour
    {
        protected bool isNowOpen;

        [SerializeField] private Button closeFadePanelButton;
        [SerializeField] protected Button closeButton;
        [SerializeField] private CanvasGroup uiCanvasGroup;
        [SerializeField] private Transform popUpTrans;

        private void Start()
        {
            gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            if (popUpTrans != null)
            {
                popUpTrans.localScale = Vector3.one * 0.7f;
            }
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.DOKill();
                uiCanvasGroup.DOFade(0f, 0f);
            }
        }

        public virtual void Init(object newData)
        {
        }

        public virtual void Open(object data = null, UnityAction done = null)
        {
            if (gameObject.activeSelf)
            {
                Utils.MakeLog("Already Panel Opened");
            }

            if (popUpTrans != null)
            {
                popUpTrans.DOKill();
                popUpTrans.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
            }
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.DOKill();
                uiCanvasGroup.DOFade(1f, 0.1f);
            }

            Init(data);
            isNowOpen = true;
            gameObject.SetActive(true);

            if (closeFadePanelButton)
            {
                SafeOneAddListener(closeFadePanelButton, OnBtnCloseClicked);
            }

            if (closeButton)
            {
                SafeOneAddListener(closeButton, OnBtnCloseClicked);
            }

            Localization();
        }

        protected virtual void Localization()
        {
        }

        public virtual void Close()
        {
            if (!gameObject.activeSelf)
            {
                Utils.MakeLog("Already Panel Closed");
            }

            StopAllCoroutines();

            isNowOpen = false;

            if (popUpTrans != null)
            {
                popUpTrans.DOKill();
                popUpTrans.DOScale(0.7f, 0.2f).SetEase(Ease.InBack);
            }
            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.DOKill();
                uiCanvasGroup.DOFade(0f, 0.1f);
            }
            DOVirtual.Float(0, 0, 0.2f, null).OnComplete(()=>gameObject.SetActive(false));
        }

        public void SafeOneAddListener(Button button, UnityAction action, UnityAction soundAction = null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(soundAction ?? (() => GlobalManagerTable.SoundManager.PlaySFX("Button_29")));
            button.onClick.AddListener(action);
        }

        protected virtual void OnBtnCloseClicked()
        {
            Close();
        }
    }
}