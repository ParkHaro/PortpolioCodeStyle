using Cysharp.Threading.Tasks;
using Core.UI.Widget;
using Sirenix.OdinInspector;
using UI.Overlay;
using UnityEngine;
using Users;

namespace Content.EventCommon
{
    public class UIEventSampleSubOverlay : UIOverlay<UIEventSampleSubOverlay, UIEventSampleSubOverlay.State>
    {
        public struct State
        {
            public EventGroupItem EventGroupItem { get; init; }
        }

        [Title(DefaultTitleName)]
        [SerializeField] private UIEventSampleSubContent _eventContent;

        private State _state;

        private readonly EventSubContentLoader<UIEventSampleSubContent> _contentLoader = new();

        public override async UniTask OnEnter(State state)
        {
            _state = state;

            _contentLoader.Initialize(this, state.EventGroupItem);
            _eventContent = await _contentLoader.LoadEventSubContent("EventTest");

            await _eventContent.OnEnter(new()
            {
                EventGroupItem = _contentLoader.BaseEventGroupItem,
            });
        }
        
        public override UniTask OnExit()
        {
            _eventContent.OnExit();
            _eventContent = null;
            _contentLoader.Release();

            _state = default;

            return base.OnExit();
        }
    }
}