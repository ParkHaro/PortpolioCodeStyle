using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Haro
{
    public enum PanelBeginAnimationType
    {
        Animator,
        None,
    }

    public abstract class BasePanel : MonoBehaviour
    {
        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }

                return _rectTransform;
            }
        }

        public bool IsPlayingAnimation => _isPlayingAnimation;
        public bool IsNowOpen => isNowOpen;

        [SerializeField] private PanelBeginAnimationType panelBeginAnimationType;
        [SerializeField] private bool isOnEditorChangingParameter;

        protected CanvasScaler CanvasScaler
        {
            get
            {
                if (_canvasScaler == null)
                {
                    _canvasScaler = GetComponentInParent<CanvasScaler>();
                }

                if (_canvasScaler == null)
                {
                    Utils.MakeLog(Utils.LogCategory.WARNING, "Scene에서 Canvas내에 배치한 후에 작업히세요");
                }

                return _canvasScaler;
            }
        }

        protected Animator Animator
        {
            get
            {
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>(true);
                }

                return _animator;
            }
        }

        protected bool IsOnEditorChangingParameter => isOnEditorChangingParameter;
        protected bool isNowOpen;

        private RectTransform _rectTransform;
        private CanvasScaler _canvasScaler;
        private Animator _animator;

        private Coroutine _playAnimationCor;
        private bool _isPlayingAnimation;

        [Button]
        protected virtual void ApplyBeginParameters()
        {
        }

        protected virtual void Init(object newData)
        {
        }

        [PropertyOrder(-5)]
        [ButtonGroup("Button")]
        [Button]
        public virtual void Open(object data = null, UnityAction done = null)
        {
            if (gameObject.activeSelf)
            {
                Utils.MakeLog($"Already Panel Opened - {name}");
            }

            Init(data);
            isNowOpen = true;
            gameObject.SetActive(true);
            if (panelBeginAnimationType == PanelBeginAnimationType.None)
            {
                done?.Invoke();
            }
            else
            {
                PlayAnimation(done);
            }

            Localization();
        }

        protected virtual void Localization()
        {
        }

        [PropertyOrder(-5)]
        [ButtonGroup("Button")]
        [Button]
        public virtual void Close(UnityAction done = null)
        {
            if (!gameObject.activeSelf)
            {
                Utils.MakeLog("Already Panel Closed");
            }

            if (panelBeginAnimationType != PanelBeginAnimationType.None)
            {
                StopAnimation();
            }

            StopAllCoroutines();

            done?.Invoke();
            isNowOpen = false;
            gameObject.SetActive(false);
        }

        private void PlayAnimation(UnityAction done)
        {
            _playAnimationCor = StartCoroutine(BasePlayAnimationCor(done));
        }

        private void StopAnimation()
        {
            if (_playAnimationCor != null)
            {
                StopCoroutine(_playAnimationCor);
                _playAnimationCor = null;
            }
        }

        private IEnumerator BasePlayAnimationCor(UnityAction done)
        {
            _isPlayingAnimation = true;
            yield return StartCoroutine(PlayAnimationCor());
            _isPlayingAnimation = false;
            done?.Invoke();
        }

        protected virtual IEnumerator PlayAnimationCor()
        {
            yield return null;
        }

        protected IEnumerator WaitAnimationEnd(string animName)
        {
            while (!Animator.GetCurrentAnimatorStateInfo(0).IsName(animName))
            {
                yield return null;
            }

            while (Animator.GetCurrentAnimatorStateInfo(0).IsName(animName) &&
                   Animator.GetCurrentAnimatorStateInfo(0).normalizedTime <= 0.9f)
            {
                yield return null;
            }
        }

        public void SafeOneAddListener(Button button, UnityAction action, UnityAction soundAction = null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(soundAction ?? (() => GlobalManagerTable.SoundManager.PlaySFX("Button_29")));
            button.onClick.AddListener(action);
        }
    }
}