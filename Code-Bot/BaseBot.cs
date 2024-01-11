using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Haro.InGame
{
    public abstract class BaseBot : MonoBehaviour
    {
        private CommonPlayer _commonPlayer;
        private BasePlayer _basePlayer;

        protected BasePlayer BasePlayer
        {
            get
            {
                if (_basePlayer == null)
                {
                    _basePlayer = GetComponent<BasePlayer>();
                }

                return _basePlayer;
            }
        }

        protected CommonPlayer CommonPlayer
        {
            get
            {
                if (_commonPlayer == null)
                {
                    _commonPlayer = GetComponent<CommonPlayer>();
                }

                return _commonPlayer;
            }
        }

        public bool IsBotActive
        {
            get => _isBotActive;
            protected set => _isBotActive = value;
        }

        private bool _isBotActive;

        private Vector3 _myPos;
        protected Vector3 MyPos => _myPos;
        private Vector3 _destPos;
        private Vector3 _correctDir;

        private readonly Dictionary<int, Vector3> _directionDict = new();

        private void OnEnable()
        {
            if (!ManagerTable.GameLogicManager.IsMaster)
            {
                return;
            }

            BeginBotProcess();
        }

        private void OnDisable()
        {
            EndBotProcess();
        }

        private void FixedUpdate()
        {
            if (!ManagerTable.GameLogicManager.IsMaster)
            {
                return;
            }

            if (!_isBotActive)
            {
                return;
            }

            _myPos = transform.position;
            var destDir = Vector3.zero;

            if (!float.IsInfinity(_destPos.x))
            {
                var destVector = _destPos - MyPos;
                if (destVector.sqrMagnitude < 1f)
                {
                    SetDestination(Vector3.positiveInfinity);
                }
                else
                {
                    destDir = destVector.normalized;
                }
            }

            var isInputPossible = !CommonPlayer.IsLocalPlayer || ManagerTable.InputManager.IsInputPossible;
            Vector3 targetVelocity = isInputPossible && BasePlayer.IsUpdateVelocity
                ? GetTargetVelocity(destDir)
                : Vector3.zero;
            UpdateDirection(nameof(GetTargetVelocity).GetHashCode(), targetVelocity);
            BasePlayer.InputVector = targetVelocity.normalized;
            BasePlayer.SetVelocity(GetDirection(nameof(GetTargetVelocity).GetHashCode()));
        }

        public void BeginBotProcess()
        {
            Helper.CreateCancellationTokenSourceSafe(ref _botProcessCancelTokenSource);
            ControlEnableBotAsync().Forget();
            BotProcessAsync().Forget();
            ChangeDecisionProcessAsync().Forget();
            SelectDecisionProcessAsync().Forget();
        }

        private CancellationTokenSource _botProcessCancelTokenSource;

        public void EndBotProcess()
        {
            Helper.CancelAsyncSafe(ref _botProcessCancelTokenSource);
        }

        protected abstract UniTaskVoid ControlEnableBotAsync();
        protected abstract UniTaskVoid BotProcessAsync();
        protected abstract UniTaskVoid ChangeDecisionProcessAsync();
        protected abstract UniTaskVoid SelectDecisionProcessAsync();

        #region Utility

        [Button]
        protected void SetDestination(Vector2 pos)
        {
            _destPos = pos;
        }

        protected void UpdateDirection(int nameHash, Vector3 direction)
        {
            if (!_directionDict.ContainsKey(nameHash))
            {
                _directionDict.Add(nameHash, direction);
                return;
            }

            _directionDict[nameHash] = direction;
        }

        protected Vector3 GetDirection(int nameHash)
        {
            return _directionDict.TryGetValue(nameHash, out var value)
                ? value
                : Vector3.zero;
        }

        #endregion

        protected abstract Vector2 GetTargetVelocity(Vector3 destDir);
    }
}