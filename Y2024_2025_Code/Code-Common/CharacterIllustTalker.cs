using Cysharp.Threading.Tasks;
using Data;
using Core.UI.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace UI
{
    [Serializable]
    public class CharacterIllustTalker
    {
        public struct State
        {
            public Widget Owner { get; init; }
            public Func<VoiceType> GetInputTalkVoiceType { get; init; }
            public string EventGroupId { get; init; }
        }

        [SerializeField] private UICharacterIllust _uiCharacterIllust;
        [SerializeField] private UITalkBox _uiTalkBox;

        private CharacterData _characterData;
        private List<CharacterTalkData> _characterTalkDataList = new();

        private State _state;
        public string CharacterId => _characterData?.Id;

        public void OnEnter(State state)
        {
            _state = state;
            if (_uiCharacterIllust == null)
            {
                Debug.LogError("CharacterIllustTalker: UICharacterIllust is not assigned. " +
                               $"[{GlobalUtil.Object.GetFullHierarchyPath(_state.Owner.gameObject)}]");
                return;
            }

            if (_uiTalkBox == null)
            {
                Debug.LogError($"CharacterIllustTalker: UITalkBox is not assigned. " +
                               $"[{GlobalUtil.Object.GetFullHierarchyPath(_state.Owner.gameObject)}]");
                return;
            }

            _uiTalkBox.gameObject.SetActive(false);
        }

        public void OnExit()
        {
            _uiCharacterIllust.OnPointerDownEvent = null;
            _uiCharacterIllust.Release();
            _uiTalkBox.Release();
            _state = default;
        }

        public async UniTask LoadCharacter(string characterId)
        {
            _characterData = DataTable.CharacterDataTable.GetById(characterId);
            await _uiCharacterIllust.LoadAndApplyCharacterOffset(_characterData);
            _uiCharacterIllust.OnPointerDownEvent = OnCharacterClick;
        }

        private void OnCharacterClick(PointerEventData eventData)
        {
            if (_uiTalkBox.gameObject.activeSelf)
            {
                return;
            }

            InputPlayTalk();
        }

        private void InputPlayTalk()
        {
            PlayTalk(_state.GetInputTalkVoiceType?.Invoke() ?? VoiceType.None);
        }

        public void PlayTalk(VoiceType voiceType = VoiceType.None)
        {
            var targetVoiceType = voiceType;
            if (targetVoiceType == VoiceType.None && _state.GetInputTalkVoiceType != null)
            {
                targetVoiceType = _state.GetInputTalkVoiceType();
            }

            DebugHelper.Log($" == TalkPlay / VoiceType : [{voiceType}] / TargetVoiceType : [{targetVoiceType}] / EventGroupId : [{_state.EventGroupId}]", Color.yellow);

            if (targetVoiceType == VoiceType.None)
            {
                return;
            }

            var hasEventGroupId = !string.IsNullOrEmpty(_state.EventGroupId);
            _characterTalkDataList = hasEventGroupId
                ? DataTable.CharacterTalkDataTable.GetGroupByCharacterIdAndVoiceTypeAndEventGroupId(
                    _characterData.Id, targetVoiceType, _state.EventGroupId)
                : DataTable.CharacterTalkDataTable.GetGroupByCharacterIdAndVoiceType(_characterData.Id, targetVoiceType);

            if (_characterTalkDataList == null || !_characterTalkDataList.Any())
            {
                DebugHelper.Log($"-- No TalkData / CharacterId : [{_characterData.Id}] / VoiceType : [{targetVoiceType}] / EventGroupId : [{_state.EventGroupId}]", Color.yellow);
                return;
            }

            var talkData = _characterTalkDataList[Random.Range(0, _characterTalkDataList.Count)];

            _uiTalkBox.Init(talkData, _uiCharacterIllust.CharacterIllust);
            _uiTalkBox.Play();
        }

        public void SetCharacterOffset(CharacterIllust.OffsetType offsetType)
        {
            _uiCharacterIllust.ApplyOffset(offsetType);
        }
    }
}