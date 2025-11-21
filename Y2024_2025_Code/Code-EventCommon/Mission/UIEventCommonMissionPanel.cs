using Cysharp.Threading.Tasks;
using Messages;
using Core.UI.Widget;
using Script.Content.EventCommon;
using Sirenix.OdinInspector;
using UI.Panel;
using UnityEngine;
using Users;

namespace Content.EventCommon
{
    public class UIEventCommonMissionPanel : UIPanel<UIEventCommonMissionPanel, UIEventCommonMissionPanel.State>
    {
        public struct State
        {
            public EventGroupItem EventGroupItem { get; init; }
            public string MissionNPC { get; init; }
        }

        private State _state;

        [Title(DefaultTitleName)]
        [ReadOnly]
        [SerializeField] private UIEventCommonMissionContent _eventCommonMissionContent;

        private readonly ContentLoader<UIEventCommonMissionContent> _contentLoader = new();

        private string EventId => _state.EventGroupItem.EventId;
        private string ResourceCode => _state.EventGroupItem.Resource;
        private string ContentName => _state.EventGroupItem.ContentName;

        protected override async UniTask OnEnter(State state)
        {
            _state = state;

            SchedulerUpdateEvent.Subscribe(OnSchedulerUpdateEvent);

            if (string.IsNullOrEmpty(EventId) ||
                string.IsNullOrEmpty(_state.MissionNPC) ||
                string.IsNullOrEmpty(ResourceCode))
            {
                DebugHelper.LogError("UIEventCommonMissionPanel: ContentTypeValue or MissionNPC is null or empty.");
                return;
            }

            _eventCommonMissionContent = await _contentLoader.LoadContent(this,
                contentName: ContentName,
                resourceCode: ResourceCode,
                prefabName: "EventCommonMission");


            await _eventCommonMissionContent.OnEnter(new()
            {
                EndTime = _state.EventGroupItem.EndTime,
                EventGroupItem = _state.EventGroupItem,
                MissionNPC = _state.MissionNPC,
            });
        }

        public override void OnResume()
        {
            base.OnResume();
            _eventCommonMissionContent?.OnResume();
            EventCommonUtil.UpdatePanelSetting("EventCommonMissionPanel", EventId);
        }

        public override UniTask OnExit()
        {
            SchedulerUpdateEvent.Unsubscribe(OnSchedulerUpdateEvent);

            _eventCommonMissionContent?.OnExit();
            _eventCommonMissionContent = null;
            _contentLoader.Release();
            _state = default;
            return base.OnExit();
        }

#region Messages
        private bool OnSchedulerUpdateEvent(SchedulerUpdateEvent message)
        {
            var data = message.Data;
            if (data.SchedulerId != _state.EventGroupItem.SchedulerId)
            {
                return false;
            }

            if (data.Type != 1) // End
            {
                return false;
            }

            GlobalMessageEvent.Publish(new() { ErrorCode = (int)ErrCode.Types.T.CloseScheduler });

            return true;
        }
#endregion
    }
}