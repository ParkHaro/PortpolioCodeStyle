using BrunoMikoski.AnimationSequencer;
using Cysharp.Threading.Tasks;
using Network;
using Network.Packets.Game.Mission;
using Utils;
using Core.UI.Widget;
using System;
using System.Linq;
using Content.Mission;
using UI;
using UI.Messages;
using UI.Overlay;
using UnityEngine;
using UnityEngine.UI;
using Users;

namespace Content.EventCommon
{
    public class UIEventCommonMissionContent : UIContent, IValueHolder<string>
    {
#region Widgets
        public partial class TextWidgets
        {
            public Text Timer;
            public Text AttainMission;
        }

        public partial class ButtonWidgets
        {
            public Button ReceiveAll;
        }

#region WidgetsProperty
        [SerializeField] private TextWidgets _texts;
        private TextWidgets Texts => _texts;
        [Serializable] public partial class TextWidgets : BaseWidgets.Texts { }

        [SerializeField] private ButtonWidgets _buttons;
        private ButtonWidgets Buttons => _buttons;
        [Serializable] public partial class ButtonWidgets : BaseWidgets.Buttons { }
#endregion
#endregion

        public struct State
        {
            public long EndTime { get; init; }
            public EventGroupItem EventGroupItem { get; init; }
            public string MissionNPC { get; init; }
        }

        [SerializeField] private AnimationSequencerController _close;
        [SerializeField] private Image _backgroundImage;

        [SerializeField] private UIMissionScroll _uiMissionScroll;

        [SerializeField] private CharacterIllustTalker _characterIllustTalker;

        private State _state;

        private delegate void ResetTimeDelegate();
        private event ResetTimeDelegate OnResetTimeUpdate;

        private long _endTime;
        private long _updateLastTime;
        private MissionGroup _missionGroup;

        public string Value => _state.EventGroupItem.GroupId;

        private EventGroupItem EventGroupItem => _state.EventGroupItem;

        private string EventGroupId => _state.EventGroupItem.GroupId;

        public bool IsAllComplete
        {
            get
            {
                bool isAllComplete = false;
                var MissionGroup = User.My.MissionInfo.MissionGroups.FirstOrDefault(m => m.Id == _state.EventGroupItem.GroupId);
                if (MissionGroup != null)
                {
                    isAllComplete = MissionGroup.IsComplete;
                }

                return isAllComplete;
            }
        }

        protected override void AddWidgetListener()
        {
            base.AddWidgetListener();

            Buttons.ReceiveAll.AddListener(OnButtonReceiveAllClicked);
        }

        private void Update()
        {
            var nowTime = ServerTime.Now.ToTimestamp();
            if (_updateLastTime == nowTime)
                return;

            _updateLastTime = nowTime;

            OnResetTimeUpdate?.Invoke();
        }

        private void UpdateResetTime()
        {
            if (_missionGroup == null)
                return;

            if (_endTime < 0)
                return;

            var resetAt = _endTime - ServerTime.Now.ToTimestamp();
            if (resetAt < 0)
            {
                Texts.Timer.SetText("-");

                OnResetTimeUpdate = null;

                return;
            }

            Texts.Timer.SetText(GetTimerText());
        }

        public async UniTask OnEnter(State state)
        {
            OnResetTimeUpdate = null;

            MissionDataUpdateEvent.Subscribe(OnMissionDataUpdateEvent);

            _state = state;

            _endTime = _state.EndTime;

            _characterIllustTalker.OnEnter(new()
            {
                Owner = this,
                GetInputTalkVoiceType = GetInputTalkVoiceType,
                EventGroupId = EventGroupId
            });
        }

        public void OnResume()
        {
            Init().Forget();
        }

        public void OnExit()
        {
            MissionDataUpdateEvent.Unsubscribe(OnMissionDataUpdateEvent);

            _characterIllustTalker.OnExit();

            _close.SetTime(0);
            _close.Play();

            _state = default;
        }

        private async UniTask Init()
        {
            var missionCharacterId = _state.MissionNPC;

            await _characterIllustTalker.LoadCharacter(missionCharacterId);

            _missionGroup = User.My.MissionInfo.EventDashboard[EventGroupId];

            _uiMissionScroll.OnEnter(new() { OnButtonDataUpdateClicked = HandleButtonDataUpdateClicked, OnErrorCallBack = HandleError });
            _uiMissionScroll.UpdateList(_missionGroup.Sort.ToList());

            UpdateListAndRefresh();

            _characterIllustTalker.PlayTalk();
        }

        private bool OnMissionDataUpdateEvent(MissionDataUpdateEvent msg)
        {
            UpdateListAndRefresh();
            return true;
        }

        private void UpdateListAndRefresh()
        {
            UpdateList();
            Refresh();
        }

        private void UpdateList()
        {
            _missionGroup = User.My.MissionInfo.EventDashboard[EventGroupId];

            _uiMissionScroll.UpdateList(_missionGroup.Sort.ToList(), true);
        }

        private void Refresh()
        {
            OnResetTimeUpdate = UpdateResetTime;
            Buttons.ReceiveAll.Interactable = 0 < _missionGroup.RewardCount;

            Texts.AttainMission.SetText($"{_missionGroup.ClearCount}/{_missionGroup.Missions.Count}");
        }

        private VoiceType GetInputTalkVoiceType()
        {
            return EventGroupItem.Scheduler.IsEndedTime
                ? VoiceType.MissionEventOff
                : IsAllComplete
                    ? VoiceType.MissionEventComplete
                    : VoiceType.MissionEventOn;
        }

        private string GetTimerText()
        {
            var resetAt = _endTime - ServerTime.Now.ToTimestamp();
            var timeSpan = TimeSpan.FromSeconds(resetAt);
            var day = string.Format(LocalString.Get("Str_UI_Mission_RemainTime_Date"), $"{timeSpan.Days:D2}");

            var timerText = $"{day} {timeSpan.Hours:D2}:{timeSpan.Minutes:D2}";

            if (timeSpan.Days < 1)
            {
                var isLeftOneMinutes = (timeSpan.Hours == 0 && timeSpan.Minutes == 0 && timeSpan.Seconds > 0);
                timerText = isLeftOneMinutes
                    ? $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}"
                    : $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}";
            }
            else if (resetAt <= 0)
            {
                timerText = "-";
            }

            return timerText;
        }

#region Handle
        private void HandleButtonDataUpdateClicked(Mission mission, bool isListRefresh)
        {
            _characterIllustTalker.PlayTalk(VoiceType.MissionEventClear);
            UpdateListAndRefresh();
        }

        private void HandleError(int errorCode)
        {
            UISystemMessagePopup.Show(LocalString.Get("Str_UI_EventCoin_Unobtainable_Dsec"));
        }
#endregion

#region Events
        private void OnButtonReceiveAllClicked()
        {
            MissionRewardPacket.Send(new()
                {
                    MissionGroupId = _missionGroup.Id,
                    MissionId = ""
                })
                .OnCompleted(response =>
                    {
                        var rewardAny = response.ResCommon.Rewards.Any();
                        if (rewardAny)
                        {
                            UIRewardOverlay.Show(new()
                            {
                                RewardList = response.ResCommon.Rewards,
                            });
                        }
                        else
                        {
                            HandleError(1);
                        }

                        UpdateListAndRefresh();
                    }
                )
                .OnFailed(_ => { });
        }
#endregion
    }
}