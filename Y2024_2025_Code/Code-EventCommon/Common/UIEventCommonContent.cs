using System;
using Cysharp.Threading.Tasks;
using Messages;
using Network;
using Notifications;
using Utils;
using Core.UI.Widget;
using UIStyle;
using UnityEngine;
using Users;

namespace Content.EventCommon.Content
{
    public abstract class UIEventCommonContent<TState> : UIContent
        where TState : struct, IHasEventGroupItemState
    {
#region Widgets
        public partial class EventCommonStyleWidgets
        {
            public UIStyles MissionButton;
        }

        public partial class EventCommonButtonWidgets
        {
            public CommonButton Shop;
            public CommonButton Mission;
        }

#region WidgetsProperty
        [SerializeField] private EventCommonStyleWidgets _commonStyles;
        private EventCommonStyleWidgets CommonStyles => _commonStyles;
        [Serializable] public partial class EventCommonStyleWidgets : BaseWidgets.Styles { }
        [SerializeField] private EventCommonButtonWidgets _commonButtons;
        private EventCommonButtonWidgets CommonButtons => _commonButtons;
        [Serializable] public partial class EventCommonButtonWidgets : BaseWidgets.Buttons { }
#endregion
#endregion

        private TState _state;
        protected TState State => _state;

        private EventGroupItem _eventGroupItem;
        protected EventGroupItem EventGroupItem
        {
            get
            {
                if (!_eventGroupItem.IsValid)
                {
                    DebugHelper.LogError("EventGroupItem is not valid. Please Set EventGroupItem on EventContent/OnEnter.");
                    return default;
                }

                return _eventGroupItem;
            }
            private set => _eventGroupItem = value;
        }

        private bool IsEventAvailable
        {
            get
            {
                var startTime = EventGroupItem.StartTime;
                var endTime = EventGroupItem.EndTime;
                var currentTime = ServerTime.Now.ToTimestamp();

                return currentTime > startTime && currentTime < endTime;
            }
        }

        protected bool MissionAvailable
        {
            set
            {
                CommonButtons.Mission.Interactable = value;
                CommonStyles.MissionButton.SetStyle(value ? "Default" : "Dim");
                var targets = CommonButtons.Mission.GetComponentsInChildren<UINotificationList>(true);
                foreach (var target in targets)
                {
                    target.gameObject.SetActive(value);
                }
            }
        }

        protected abstract string ShopNPC { get; }
        protected abstract string MissionNPC { get; }

        private bool _lastEventAvailable = true;

        protected override void AddWidgetListener()
        {
            base.AddWidgetListener();
            CommonButtons.Shop?.AddListener(OnButtonShopClicked);
            CommonButtons.Mission?.AddListener(OnButtonMissionClicked);
        }

        public async UniTask OnEnter(TState state)
        {
            _state = state;
            EventGroupItem = _state.EventGroupItem;

            SchedulerUpdateEvent.Subscribe(OnSchedulerUpdateEvent);
            await OnEnterProcess();
        }

        protected abstract UniTask OnEnterProcess();

        public void OnExit()
        {
            SchedulerUpdateEvent.Unsubscribe(OnSchedulerUpdateEvent);
            OnExitProcess();
            _state = default;
        }

        protected abstract void OnExitProcess();

        public void OnResume()
        {
            _lastEventAvailable = true;
            
            MissionAvailable = false;
            MissionAvailable = true;

            OnResumeProcess();
            UpdateEventAvailable();
        }

        protected abstract void OnResumeProcess();

        private void UpdateEventAvailable()
        {
            if (IsEventAvailable)
            {
                _lastEventAvailable = IsEventAvailable;
                return;
            }

            if (_lastEventAvailable)
            {
                MissionAvailable = false;
                _lastEventAvailable = false;
                EventFromStartToEnd();
            }
        }

        private void EventFromStartToEnd()
        {
            EventFromStartToEndProcess();
        }

        protected abstract void EventFromStartToEndProcess();

#region Events
        protected virtual void OnButtonShopClicked()
        {
            UIEventCommonShopPanel.Open(new()
            {
                EventGroupItem = EventGroupItem,
                ShopNPC = ShopNPC,
            });
        }

        protected virtual void OnButtonMissionClicked()
        {
            UIEventCommonMissionPanel.Open(new()
            {
                EventGroupItem = EventGroupItem,
                MissionNPC = MissionNPC,
            });
        }
#endregion

#region Messages
        private bool OnSchedulerUpdateEvent(SchedulerUpdateEvent message)
        {
            var data = message.Data;
            if (data.SchedulerId != EventGroupItem.SchedulerId)
            {
                return false;
            }

            switch (data.Type)
            {
                case 0: // Start - End
                    break;
                case 1: // End - Reward
                    UpdateEventAvailable();
                    break;
                case 2: // Close
                    GlobalMessageEvent.Publish(new() { ErrorCode = (int)ErrCode.Types.T.CloseScheduler });
                    break;
            }

            return true;
        }
#endregion
    }
}