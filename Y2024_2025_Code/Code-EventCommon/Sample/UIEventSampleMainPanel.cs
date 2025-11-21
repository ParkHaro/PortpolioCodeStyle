using Cysharp.Threading.Tasks;
using Core.UI.Widget;
using Sirenix.OdinInspector;
using UI.Panel;
using UnityEngine;
using Users;

namespace Content.EventCommon
{
    public class UIEventSampleMainPanel : UIPanel<UIEventSampleMainPanel, ScheduleEventState>
    {
        [Title(DefaultTitleName)]
        [SerializeField] private UIEventSampleMainContent _eventContent;

        private ScheduleEventState _state;

        private readonly EventContentLoader<UIEventSampleMainContent, EventSampleData> _contentLoader = new();

        protected override async UniTask OnEnter(ScheduleEventState state)
        {
            _state = state;

            _contentLoader.Initialize(this, EventType.EventAdventure, /*DataTable.*/EventSampleData.GetById);
            _eventContent = await _contentLoader.LoadEventContent(_state.UserSchedulerData, "EventSample");

            await _eventContent.OnEnter(new()
            {
                EventGroupItem = _contentLoader.BaseEventGroupItem,
            });
            
            User.My.ContentInfo.SetEnter(ContentInfo.EnterInfo.Type.EventAdeventure, EventAdventureData.Id);
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