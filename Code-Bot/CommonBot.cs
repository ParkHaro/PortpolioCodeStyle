using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;
using Cysharp.Threading.Tasks;

namespace Haro.InGame
{
    [Serializable]
    public class DirectionTaskContainer
    {
        public Vector3 direction;
        public UniTask task;
    }

    public abstract partial class
        CommonBot<TDecisionType, TWeightDirectionType, TBotLevelData, TBotLevelPlayData> : BaseBot
        where TDecisionType : Enum
        where TWeightDirectionType : Enum
        where TBotLevelData : CommonBotLevelData
        where TBotLevelPlayData : CommonBotLevelPlayData<TBotLevelData>, new()
    {
        protected delegate Vector3 GetCalculateDirection(WeightDirectionData weightDirectionData);

        protected RawBotData RawBotData => BasePlayer.PlayerData.botData;

        protected BotPlayData<TDecisionType, TWeightDirectionType, TBotLevelData, TBotLevelPlayData>
            BotPlayData => _botPlayData;

        private int _selectedBotDecisionType;

        protected int SelectedBotDecisionType
        {
            get => _selectedBotDecisionType;
            private set => _selectedBotDecisionType = value;
        }

        protected Vector3 CorrectDir
        {
            get => _correctDir;
            set => _correctDir = value;
        }

        private int _newBotDecisionType;

        protected int NewBotDecisionType
        {
            get => _newBotDecisionType;
            set => _newBotDecisionType = value;
        }

        protected readonly RaycastHit2D[] Hits = new RaycastHit2D[30];
        protected DecisionDataWithType<TDecisionType, TWeightDirectionType> CurrentDecisionDataWithType;

        private BotPlayData<TDecisionType, TWeightDirectionType, TBotLevelData, TBotLevelPlayData> _botPlayData;
        protected float SelectDecisionDelay;
        private Coroutine _botProcessCor, _changeDecisionProcessCor, _selectDecisionProcessCor, _controlEnableBotCor;

        private readonly Dictionary<TWeightDirectionType, DirectionTaskContainer> _directionTaskContainerDict = new();
        private readonly List<Vector2> _sensorDirectionList = new();
        private Dictionary<int, GetCalculateDirection> _weightDirectionTypeList = new();
        private List<BasePlayer> _allyBasePlayerList;
        private List<BasePlayer> _enemyBasePlayerList;
        protected List<BasePlayer> EnemyBasePlayerList => _enemyBasePlayerList;

        protected void Init(
            BotPlayData<TDecisionType, TWeightDirectionType, TBotLevelData, TBotLevelPlayData> botPlayData)
        {
            _weightDirectionTypeList.Clear();

            destPos.x = Single.PositiveInfinity;
            SelectedBotDecisionType = 0;

            AddWeightDirectionTypeList(new Dictionary<int, GetCalculateDirection>
                {
                    { Helper.EnumToInt(WeightDirectionType.ToWall), GetToWallDirection },
                    { Helper.EnumToInt(WeightDirectionType.Wander), GetToWanderDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToCenter), GetToCenterDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToAlly), GetToAllyDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToEnemy), GetToEnemyDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToAllAllyCenter), GetToAllAllyCenterDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToAllEnemyCenter), GetToAllEnemyCenterDirection }
                }
            );

            _botPlayData = botPlayData;
            SelectDecisionDelay = botPlayData.SelectDecisionDelay;

            UpdateSensorDirection();
        }

        private void UpdateSensorDirection()
        {
            var rayCnt = 16;

            var degree = 360f / rayCnt;
            var makeDir = Vector3.up;
            for (var i = rayCnt - 1; i >= 0; i--)
            {
                _sensorDirectionList.Add(makeDir);
                makeDir = Quaternion.Euler(0f, 0f, -degree) * makeDir;
            }
        }

        [Button]
        public float SelectDecision(int botDecisionType)
        {
            NewBotDecisionType = botDecisionType;
            var decision = GetDecision(NewBotDecisionType);
            if (decision == null)
            {
                Utils.MakeLog(Utils.LogCategory.ERROR,
                    $"Decision Not Exist {_newBotDecisionType}\nReturn Default : 5 sec");
                return 5f;
            }

            return decision.decisionData.duration;
        }

        protected UniTask StartDecisionProcessAsync(TDecisionType botDecisionType)
        {
            return DecisionProcessAsync(Helper.EnumToInt(botDecisionType));
        }

        private async UniTask DecisionProcessAsync(int botDecisionType)
        {
            try
            {
                var duration = SelectDecision(botDecisionType);
                await UniTask.Delay(TimeSpan.FromSeconds(duration), true, PlayerLoopTiming.Update,
                    botProcessCancelTokenSource.Token);
            }
            catch (Exception e)
            {
                Utils.MakeAsyncCancelLog(nameof(DecisionProcessAsync), e);
            }
        }

        protected DecisionDataWithType<TDecisionType, TWeightDirectionType> GetDecision(int decisionType)
        {
            return BotPlayData.GetDecision(decisionType);
        }

        #region UniTask

        protected override async UniTaskVoid ControlEnableBotAsync()
        {
            while (BasePlayer.PlayerData.IsEmpty())
            {
                await UniTask.Yield(botProcessCancelTokenSource.Token);
            }

            var playerId = BasePlayer.PlayerData.playerId;

            while (true)
            {
                if (!ManagerTable.GameLogicManager.IsMaster)
                {
                    await UniTask.Yield(botProcessCancelTokenSource.Token);
                }

                // 플레이어가 미연결 상태이거나, 네트워크 불안정 상태일때 봇로직을 계산하지않음.
                var isBotActiveThisFrame =
                    !ManagerTable.GameLogicManager.IsPlayerConnected(playerId) &&
                    !ManagerTable.GameFlowManager.IsRumbyMoveNetworkInstability;

                // 봇이 활성화 상태에서 비활성화 상태로 넘어가는 경우 (재접속) input 방향 초기화. 정지.
                if (IsBotActive && !isBotActiveThisFrame)
                {
                    if (LocalPlayManager.Instance.IsLocalPlay)
                    {
                    }
                    else
                    {
                        if (CommonPlayer.IsLocalPlayer)
                        {
                            InGame.ManagerTable.InputManager.InitPressedKey();
                        }
                    }
                }

                IsBotActive = isBotActiveThisFrame;
                if (LocalPlayManager.Instance.IsLocalPlay && CommonPlayer.PlayerData.isLocalPlayerControl)
                {
                    IsBotActive = false;
                }
                else if (LocalPlayManager.Instance.IsLocalPlay)
                {
                    IsBotActive = true;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(0.1f), true, PlayerLoopTiming.Update,
                    botProcessCancelTokenSource.Token);
            }
        }

        protected override async UniTaskVoid BotProcessAsync()
        {
            while (true)
            {
                if (!ManagerTable.GameLogicManager.IsMaster)
                {
                    await UniTask.Yield(botProcessCancelTokenSource.Token);
                }

                if (BotPlayData == null)
                {
                    await UniTask.Yield(botProcessCancelTokenSource.Token);
                }

                UpdateDecisionDataByType();
                await DecisionProcessAsync(CurrentDecisionDataWithType);
            }
        }

        private void UpdateDecisionDataByType()
        {
            if (BotPlayData != null)
            {
                CurrentDecisionDataWithType = BotPlayData.GetDecision(SelectedBotDecisionType);
            }
        }

        protected override async UniTaskVoid ChangeDecisionProcessAsync()
        {
            NewBotDecisionType = 0;
            while (true)
            {
                if (!ManagerTable.GameLogicManager.IsMaster)
                {
                    await UniTask.Yield(botProcessCancelTokenSource.Token);
                }

                if (NewBotDecisionType != SelectedBotDecisionType)
                {
                    SelectedBotDecisionType = NewBotDecisionType;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(SelectDecisionDelay), true, PlayerLoopTiming.Update,
                    botProcessCancelTokenSource.Token);
            }
        }

        protected bool IsSelect(int select, int minRate, int maxRate)
        {
            return select >= minRate && select < maxRate;
        }

        protected async UniTask DirectionProcessAsync(
            WeightDirectionDataWithType<TWeightDirectionType> weightDirectionDataWithType,
            GetCalculateDirection getDirection)
        {
            await UniTask.Yield(botProcessCancelTokenSource.Token);

            var weightDirectionType = weightDirectionDataWithType.weightDirectionType;
            while (_directionTaskContainerDict.ContainsKey(weightDirectionType))
            {
                DirectionTaskContainer container = _directionTaskContainerDict[weightDirectionType];
                container.direction = Vector3.zero;
                UpdateDirection(weightDirectionType.GetHashCode(),
                    getDirection.Invoke(weightDirectionDataWithType.weightDirection));
                container.direction += GetDirection(weightDirectionType.GetHashCode());
                await UniTask.Delay(TimeSpan.FromSeconds(weightDirectionDataWithType.weightDirection.delay), false,
                    PlayerLoopTiming.Update, botProcessCancelTokenSource.Token);
            }
        }

        private async UniTask DecisionProcessAsync(
            DecisionDataWithType<TDecisionType, TWeightDirectionType> decisionDataWithType)
        {
            if (decisionDataWithType == null)
            {
                Utils.MakeLog(Utils.LogCategory.WARNING, $"DecisionDataWithType is NULL");
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                return;
            }

            var decisionData = decisionDataWithType?.decisionData;
            if (decisionData == null)
            {
                Utils.MakeLog(Utils.LogCategory.WARNING,
                    $"{decisionDataWithType.GetDecisionType()} DecisionData is NULL");
                await UniTask.Delay(TimeSpan.FromSeconds(0.1f));
                return;
            }

            BeginAllDirectionProcess(decisionData);

            while (_selectedBotDecisionType == decisionDataWithType.GetDecisionType())
            {
                await UniTask.Yield(botProcessCancelTokenSource.Token);
            }
        }

        private void BeginAllDirectionProcess(DecisionData<TWeightDirectionType> decisionData)
        {
            EndAllDirectionProcess();
            foreach (var weightWithType in decisionData.weightWithTypeList)
            {
                WeightDirectionProcess(weightWithType);
            }
        }

        private void EndAllDirectionProcess()
        {
            _directionTaskContainerDict.Clear();
        }

        #endregion

        #region Utility

        private void WeightDirectionProcess(
            WeightDirectionDataWithType<TWeightDirectionType> weightDirectionDataWithType)
        {
            if (!weightDirectionDataWithType.isUse)
            {
                return;
            }

            var weightDirectionType = weightDirectionDataWithType.weightDirectionType;
            var container = DirectionProcessByType(weightDirectionDataWithType);

            _directionTaskContainerDict.Add(weightDirectionType, container);
        }

        protected void AddWeightDirectionTypeList(Dictionary<int, GetCalculateDirection> addList)
        {
            _weightDirectionTypeList = _weightDirectionTypeList.Concat(addList).ToDictionary(x => x.Key,
                x => x.Value);
        }

        private DirectionTaskContainer DirectionProcessByType(
            WeightDirectionDataWithType<TWeightDirectionType> weightDirectionDataWithType)
        {
            var container = new DirectionTaskContainer();
            int weightDirectionType = Helper.EnumToInt(weightDirectionDataWithType.weightDirectionType);
            StartDirectionProcess(ref container, weightDirectionDataWithType,
                _weightDirectionTypeList[weightDirectionType]);
            return container;
        }

        protected void StartDirectionProcess(ref DirectionTaskContainer container,
            WeightDirectionDataWithType<TWeightDirectionType> weightDirectionDataWithType,
            GetCalculateDirection getCalculateDirection)
        {
#if UNITY_WEBGL
            container.task =
 UniTask.Create(async () => await DirectionProcessAsync(weightDirectionDataWithType, getCalculateDirection));
#else
            container.task =
                UniTask.RunOnThreadPool(() => DirectionProcessAsync(weightDirectionDataWithType, getCalculateDirection),
                    true, botProcessCancelTokenSource.Token);
#endif
        }

        #endregion

        #region CalcDirection Utility

        protected override Vector2 GetTargetVelocity(Vector3 destDir)
        {
            var targetDirection = destDir;
            foreach (KeyValuePair<TWeightDirectionType, DirectionTaskContainer> pair in
                     _directionTaskContainerDict)
            {
                targetDirection += pair.Value.direction;
            }

            if (targetDirection == Vector3.zero)
            {
                return targetDirection;
            }

            targetDirection += CorrectDir;
            var lerpTime =
                CurrentDecisionDataWithType != null && CurrentDecisionDataWithType.decisionData != null
                    ? CurrentDecisionDataWithType.decisionData.lerpTime
                    : 1f;
            targetDirection = Vector2.Lerp(
                BasePlayer.Direction * 0.5f,
                targetDirection,
                lerpTime * Time.fixedDeltaTime).normalized;

            return targetDirection;
        }

        protected struct TargetWithDirection<T>
        {
            public T Target;
            public Vector3 Direction;
            public Vector3 TargetPos;
        }

        protected Vector3 ApplyWeightDirection(Vector3 beginPos, Vector3 targetPos,
            WeightDirectionData weightDirectionData, float roundDistance = 0.3f)
        {
            var targetVector = (targetPos - beginPos);
            var magnitude = targetVector.magnitude;
            if (magnitude < Mathf.Epsilon * 2f || magnitude < roundDistance)
            {
                return Vector3.zero;
            }

            var distanceRatio = 1f;

            if (weightDirectionData.range > 0f)
            {
                var distance = magnitude;
                if (distance > weightDirectionData.range)
                {
                    return Vector3.zero;
                }

                distanceRatio = 1 - distance / weightDirectionData.range;
            }

            var targetDir = targetVector.normalized;
            targetDir *= (weightDirectionData.weight * distanceRatio);
            targetDir = weightDirectionData.isOpposite ? -targetDir : targetDir;

            return targetDir;
        }


        protected TargetWithDirection<RaycastHit2D> CalculateToLayerDirectionByRay(
            WeightDirectionData weightDirectionData,
            string layerName)
        {
            Vector3 beginPos = MyPos;

            TargetWithDirection<RaycastHit2D> result;
            result.Direction = result.TargetPos = Vector3.zero;
            result.Target = new RaycastHit2D();

            var isNearest = weightDirectionData.isNearest;

            var selectedDistance = isNearest ? Single.MaxValue : Single.MinValue;
            foreach (Vector2 direction in _sensorDirectionList)
            {
                var cnt = Physics2D.CircleCastNonAlloc(beginPos, 0.3f, direction, Hits,
                    weightDirectionData.range < 0f ? float.PositiveInfinity : weightDirectionData.range,
                    LayerMask.GetMask(layerName));
                for (var i = 0; i < cnt; i++)
                {
                    var hit = Hits[i];
                    if (hit.collider == null) continue;
                    Vector3 tmpTargetPos = hit.point;
                    var tmpDistance = hit.distance;

                    if (isNearest)
                    {
                        if (!(tmpDistance < selectedDistance)) continue;
                        selectedDistance = tmpDistance;
                        result.TargetPos = tmpTargetPos;
                        result.Target = hit;
                    }
                    else
                    {
                        if (!(tmpDistance > selectedDistance)) continue;
                        selectedDistance = tmpDistance;
                        result.TargetPos = tmpTargetPos;
                        result.Target = hit;
                    }
                }
            }

            if (result.TargetPos != Vector3.zero)
            {
                result.Direction = ApplyWeightDirection(beginPos, result.TargetPos, weightDirectionData);
            }

            return result;
        }

        protected TargetWithDirection<RaycastHit2D> CalculateToLayerDirectionByRay(
            CommonBot<TDecisionType, TWeightDirectionType, TBotLevelData, TBotLevelPlayData> sender,
            WeightDirectionData weightDirectionData,
            string[] layerName)
        {
            var beginPos = MyPos;

            TargetWithDirection<RaycastHit2D> result;
            result.Direction = result.TargetPos = Vector3.zero;
            result.Target = new RaycastHit2D();

            var isNearest = weightDirectionData.isNearest;

            var selectedDistance = isNearest ? float.MaxValue : float.MinValue;
            foreach (var cnt in _sensorDirectionList.Select(direction => Physics2D.CircleCastNonAlloc(beginPos, 0.3f, direction, Hits,
                         weightDirectionData.range < 0f ? float.PositiveInfinity : weightDirectionData.range,
                         LayerMask.GetMask(layerName))))
            {
                for (var i = 0; i < cnt; i++)
                {
                    var hit = Hits[i];
                    if (hit.collider == null) continue;
                    if (hit.collider.GetComponentInParent<CommonPlayer>() == sender.CommonPlayer)
                    {
                        continue;
                    }

                    Vector3 tmpTargetPos = hit.point;
                    var tmpDistance = hit.distance;

                    if (isNearest)
                    {
                        if (!(tmpDistance < selectedDistance)) continue;
                        selectedDistance = tmpDistance;
                        result.TargetPos = tmpTargetPos;
                        result.Target = hit;
                    }
                    else
                    {
                        if (!(tmpDistance > selectedDistance)) continue;
                        selectedDistance = tmpDistance;
                        result.TargetPos = tmpTargetPos;
                        result.Target = hit;
                    }
                }
            }

            if (result.TargetPos != Vector3.zero)
            {
                result.Direction = ApplyWeightDirection(beginPos, result.TargetPos, weightDirectionData);
            }

            return result;
        }

        protected TargetWithDirection<T> CalculateToDirectionByList<T>(WeightDirectionData weightDirectionData,
            in List<T> targetList) where T : Component
        {
            var beginPos = MyPos;

            TargetWithDirection<T> result;
            result.Direction = result.TargetPos = Vector3.zero;
            result.Target = null;

            var isNearest = weightDirectionData.isNearest;

            result = GetNearestTarget(targetList, isNearest, beginPos);

            if (result.TargetPos != Vector3.zero)
            {
                result.Direction = ApplyWeightDirection(beginPos, result.TargetPos, weightDirectionData);
            }

            return result;
        }

        protected TargetWithDirection<T> CalculateToDirectionByList<T>(WeightDirectionData weightDirectionData,
            in List<T> targetList, float roundDistance = 0.3f) where T : Component
        {
            var beginPos = MyPos;

            TargetWithDirection<T> result;
            result.Direction = result.TargetPos = Vector3.zero;
            result.Target = null;

            var isNearest = weightDirectionData.isNearest;

            result = GetNearestTarget(targetList, isNearest, beginPos);

            if (result.TargetPos != Vector3.zero)
            {
                result.Direction = ApplyWeightDirection(beginPos, result.TargetPos, weightDirectionData, roundDistance);
            }

            return result;
        }

        protected TargetWithDirection<T> CalculateToDirection<T>(WeightDirectionData weightDirectionData,
            T target) where T : Component
        {
            var beginPos = MyPos;

            TargetWithDirection<T> result;
            result.Direction = result.TargetPos = Vector3.zero;
            result.Target = null;
            
            result = GetTarget(target);

            if (result.TargetPos != Vector3.zero)
            {
                result.Direction = ApplyWeightDirection(beginPos, result.TargetPos, weightDirectionData);
            }

            return result;
        }

        protected TargetWithDirection<T> GetNearestTarget<T>(List<T> targetList, bool isNearest, Vector3 beginPos)
            where T : Component
        {
            var result = new TargetWithDirection<T>();
            var selectedDistance = isNearest ? Single.MaxValue : Single.MinValue;
            foreach (T target in targetList)
            {
                var tmpTargetPos = target.transform.position;
                var vec = tmpTargetPos - beginPos;
                var tmpDistance = vec.magnitude;

                if (isNearest)
                {
                    if (!(tmpDistance < selectedDistance)) continue;
                    selectedDistance = tmpDistance;
                    result.TargetPos = tmpTargetPos;
                    result.Target = target;
                }
                else
                {
                    if (!(tmpDistance > selectedDistance)) continue;
                    selectedDistance = tmpDistance;
                    result.TargetPos = tmpTargetPos;
                    result.Target = target;
                }
            }

            return result;
        }

        protected TargetWithDirection<T> GetTarget<T>(T target)
            where T : Component
        {
            var result = new TargetWithDirection<T>();
            var tmpTargetPos = target.transform.position;
            result.TargetPos = tmpTargetPos;
            result.Target = target;

            return result;
        }

        private Vector3 CalculateToCenterDirection(WeightDirectionData weightDirectionData, List<Transform> list)
        {
            var count = list.Count;
            var sumPos = Vector3.zero;
            foreach (var tf in list)
            {
                var pos = tf.position;
                sumPos += pos;
            }

            var resultPos = sumPos / count;
            var resultDirection = ApplyWeightDirection(MyPos, resultPos, weightDirectionData);
            return resultDirection;
        }

        private Vector3 CutDirection(Vector3 beforeNormalizedDirection, float distance)
        {
            var result = beforeNormalizedDirection;
            if (beforeNormalizedDirection.sqrMagnitude < distance)
            {
                result = Vector3.zero;
            }

            return result;
        }

        #endregion

        #region CalcDirection WeightType

        private Vector3 GetToWallDirection(WeightDirectionData weightDirectionData)
        {
            // TODO TMP Layer "1" is WALL
            var result = CalculateToLayerDirectionByRay(weightDirectionData, "1");
            return result.Direction;
        }

        private Vector3 GetToWanderDirection(WeightDirectionData weightDirectionData)
        {
            var targetDir = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f).normalized *
                            weightDirectionData.weight;
            return weightDirectionData.isOpposite ? -targetDir : targetDir;
        }

        private Vector3 GetToCenterDirection(WeightDirectionData weightDirectionData)
        {
            Vector3 result;
            if (CutDirection(MyPos, 0.5f) == Vector3.zero)
            {
                result = Vector3.zero;
            }
            else
            {
                result = (weightDirectionData.isOpposite ? MyPos.normalized : -MyPos.normalized) *
                         weightDirectionData.weight;
            }

            return result;
        }
        
        private void InitAllyBasePlayerList()
        {
            if (_allyBasePlayerList != null) return;
            var resultList = ManagerTable.GameFlowManager.BasePlayerList.Select(m => m.Value).ToList();
            resultList.Remove(BasePlayer);
            _allyBasePlayerList = resultList;
        }

        private Vector3 GetToAllyDirection(WeightDirectionData weightDirectionData)
        {
            InitAllyBasePlayerList();
            _allyBasePlayerList = _allyBasePlayerList.FindAll(m => !m.IsDead);
            var result =
                CalculateToDirectionByList(weightDirectionData, _allyBasePlayerList);
            return result.Direction;
        }
        
        protected void InitEnemyBasePlayerList()
        {
            if (_enemyBasePlayerList != null) return;
            var resultList = ManagerTable.GameFlowManager.BasePlayerList.Select(m => m.Value).ToList();
            resultList.Remove(BasePlayer);
            _enemyBasePlayerList = resultList;
        }

        private Vector3 GetToEnemyDirection(WeightDirectionData weightDirectionData)
        {
            InitEnemyBasePlayerList();
            _enemyBasePlayerList = _enemyBasePlayerList.FindAll(m => !m.IsDead);
            TargetWithDirection<BasePlayer> result =
                CalculateToDirectionByList(weightDirectionData, _enemyBasePlayerList);
            return result.Direction;
        }

        private Vector3 GetToAllAllyCenterDirection(WeightDirectionData weightDirectionData)
        {
            InitAllyBasePlayerList();
            var tfList = _allyBasePlayerList.Select(m => m.transform).ToList();
            var resultDirection = CalculateToCenterDirection(weightDirectionData, tfList);
            return resultDirection;
        }

        private Vector3 GetToAllEnemyCenterDirection(WeightDirectionData weightDirectionData)
        {
            InitEnemyBasePlayerList();
            var tfList = _enemyBasePlayerList.Select(m => m.transform).ToList();
            var resultDirection = CalculateToCenterDirection(weightDirectionData, tfList);
            return resultDirection;
        }

        #endregion

        #region DEBUG

#if UNITY_EDITOR

        private readonly Dictionary<int, Vector3> _debugNoiseList = new Dictionary<int, Vector3>();

        private void OnDrawGizmos()
        {
            if (!IsBotActive)
            {
                return;
            }

            if (CurrentDecisionDataWithType?.decisionData == null) return;
            var decisionData = CurrentDecisionDataWithType.decisionData;
            DrawLine(Color.gray, transform.position, GetDirection(nameof(GetTargetVelocity).GetHashCode()), 2f);

            foreach (var weightWithType in decisionData
                         .weightWithTypeList)
            {
                if (!weightWithType.isUse)
                {
                    continue;
                }

                if (!weightWithType.IsDebug())
                {
                    continue;
                }

                var weightDirection = weightWithType.weightDirection;
                var range = weightDirection.range;
                if (range <= 1f)
                {
                    range = 0f;
                }

                var direction = GetDirection(weightWithType.weightDirectionType.GetHashCode());
                if (direction == Vector3.zero) continue;
                var beginPos = transform.position;
                if (!_debugNoiseList.ContainsKey(weightWithType.GetHashCode()))
                {
                    _debugNoiseList.Add(weightWithType.GetHashCode(),
                        new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(-0.05f, 0.05f), 0f));
                }

                beginPos += _debugNoiseList[weightWithType.GetHashCode()];

                if (range > 0f)
                {
                    var rangeDebugColor = weightWithType.DebugGetColor() - new Color(0.3f, 0.3f, 0.3f, 0.3f);
                    DrawRange(rangeDebugColor, beginPos, range);
                }

                DrawLine(weightWithType.DebugGetColor(), beginPos, direction);
            }
        }

        private void DrawRange(Color color, Vector3 beginPos, float range)
        {
            Gizmos.color = color;
            Gizmos.DrawWireSphere(beginPos, range);
        }

        private void DrawLine(Color color, Vector3 beginPos, Vector3 dir, float multiplier = 1f)
        {
            if (dir == Vector3.zero)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawLine(beginPos, beginPos + dir * multiplier);
        }
#endif

        #endregion
    }
}