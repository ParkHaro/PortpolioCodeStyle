using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using System;
using System.Linq;

namespace Haro.InGame.MiniGame
{
    public class Bot : CommonBot<DecisionType, WeightDirectionType, BotLevelData, BotLevelPlayData>
    {
        private Player Player => CommonPlayer as Player;

        private IEnumerator Start()
        {
            while (BasePlayer.PlayerData.botData == null)
            {
                yield return null;
            }

            Init(BasePlayer.PlayerData.botData);
        }

        private void Init(RawBotData rawBotData)
        {
            var botPlayData = PlayData.Bot.GetBotPlayData((BotLevel)rawBotData.level);

            base.Init(botPlayData);

            AddWeightDirectionTypeList(new Dictionary<int, GetCalculateDirection>
                {
                    { Helper.EnumToInt(WeightDirectionType.ToPlatformCenter), GetToPlatformCenterDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToPlatformMove), GetToPlatformMoveDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToNewItem), GetToNewItemDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToDropItem), GetToDropItemDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToStorage), GetToStorageDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToAvoidTrigger), GetToAvoidTriggerDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToAvoidNearTrigger), GetToAvoidTriggerDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToEnemyRight), GetToEnemyRightDirection },
                    { Helper.EnumToInt(WeightDirectionType.ToPlayer), GetToPlayerDirection },
                }
            );
        }

        private void Update()
        {
            if (BotPlayData == null)
            {
                return;
            }

            BasePlayer.SetMoveSpeed(IsBotActive ? PlayData.Player.MoveSpeed + 0.1f : PlayData.Player.MoveSpeed);

            if (!BotPlayData.IsDebug)
            {
                return;
            }

            var playerInfoPanel = Player.CommonPlayerInfoPanel;
            playerInfoPanel.DebugText.gameObject.SetActive(BotPlayData.IsDebug);
            playerInfoPanel.DebugText.text = $"{(DecisionType)SelectedBotDecisionType}";
        }

        protected override async UniTaskVoid SelectDecisionProcessAsync()
        {
            try
            {
                while (!ManagerTable.GameFlowManager.IsPossiblePlayerMove || !ManagerTable.GameFlowManager.IsGameStarted
                                                                          || Player.IsLocalPlayerControl)
                {
                    if (Player.IsLocalPlayerControl)
                    {
                        EndBotProcess();
                    }

                    await UniTask.Yield(BotProcessCancelTokenSource.Token);
                }

                while (true)
                {
                    if (!ManagerTable.GameLogicManager.IsMaster)
                    {
                        await UniTask.Yield(BotProcessCancelTokenSource.Token);
                    }

                    if (BotPlayData == null || BotPlayData.IsTest || Player.IsLocalPlayerControl)
                    {
                        await UniTask.Yield(BotProcessCancelTokenSource.Token);
                        continue;
                    }

                    if (Player.ItemModule.HasItem)
                    {
                        await StartDecisionProcessAsync(DecisionType.ReturnStorage);
                    }
                    else
                    {
                        await StartDecisionProcessAsync(DecisionType.GetItem);
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(SelectDecisionDelay),
                        cancellationToken: BotProcessCancelTokenSource.Token);
                }
            }
            catch (Exception e)
            {
                Utils.MakeAsyncCancelLog(nameof(SelectDecisionProcessAsync), e);
            }
        }

        [Button]
        public void SelectDecision(DecisionType botDecisionType)
        {
            NewBotDecisionType = (int)botDecisionType;
        }

        // Arrive
        private Vector3 GetToPlatformCenterDirection(WeightDirectionData weightDirectionData)
        {
            var currentPlatform = Player.CurrentPlatform;
            if (currentPlatform == null)
            {
                return Vector3.zero;
            }

            var targetPos = currentPlatform.transform.position;
            var playerPos = Player.transform.position;
            return ApplyWeightDirection(playerPos, targetPos, weightDirectionData);
        }

        private Vector3 GetToPlatformMoveDirection(WeightDirectionData weightDirectionData)
        {
            var currentPlatform = Player.CurrentPlatform;
            if (currentPlatform == null)
            {
                return Vector3.zero;
            }

            var dir = currentPlatform.Dir;
            return weightDirectionData.isOpposite ? -dir : dir;
        }

        // Get Item
        private Vector3 GetToNewItemDirection(WeightDirectionData weightDirectionData)
        {
            var itemSpawnerList = ManagerTable.ItemSpawnManager.ItemSpawnerList;
            var spawnTargetList = itemSpawnerList.Select(m => m.transform).ToList();
            var targetWithDirection =
                CalculateToDirectionByList(weightDirectionData, in spawnTargetList);
            return targetWithDirection.Direction;
        }

        private Vector3 GetToDropItemDirection(WeightDirectionData weightDirectionData)
        {
            var dropItemStr = LayerManager.ConvertToLayerName(Layer.DropItem);
            var result = CalculateToLayerDirectionByRay(weightDirectionData, dropItemStr);
            return result.Direction;
        }

        // Return Base
        private Vector3 GetToStorageDirection(WeightDirectionData weightDirectionData)
        {
            var storage = ManagerTable.GameFlowManager.StorageDict[Player.PlayerData.seatIndex];
            var storagePos = storage.transform.position;
            var playerPos = Player.transform.position;
            return ApplyWeightDirection(playerPos, storagePos, weightDirectionData);
        }

        private Vector3 GetToAvoidTriggerDirection(WeightDirectionData weightDirectionData)
        {
            var dropItemStr = LayerManager.ConvertToLayerName(Layer.AvoidTrigger);
            var result = CalculateToLayerDirectionByRay(weightDirectionData, dropItemStr);
            return result.Direction;
        }

        private Vector3 GetToEnemyRightDirection(WeightDirectionData weightDirectionData)
        {
            InitEnemyBasePlayerList();
            TargetWithDirection<BasePlayer> result =
                CalculateToDirectionByList(weightDirectionData, EnemyBasePlayerList);
            Vector3 direction = Quaternion.Euler(0, 0, 90) * result.Direction;

            return direction;
        }

        private List<Player> _localPlayerList;

        private Vector3 GetToPlayerDirection(WeightDirectionData weightDirectionData)
        {
            if (_localPlayerList == null)
            {
                _localPlayerList = new List<Player>();
            }

            _localPlayerList.Clear();
            foreach (var player in ManagerTable.GameFlowManager.PlayerDict
                         .Where(player => player.Value.IsLocalPlayerControl))
            {
                _localPlayerList.Add(player.Value);
            }

            var result = CalculateToDirectionByList(weightDirectionData, _localPlayerList);
            return result.Direction;
        }
    }
}