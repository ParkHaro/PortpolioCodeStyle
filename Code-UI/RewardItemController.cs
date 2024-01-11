using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Gomble.MergeLion.Data;

namespace Haro.View
{
    public class RewardItemController : Singleton<RewardItemController>
    {
        [SerializeField] private float randomRange = 3f;
        [SerializeField] private int maxCount = 20;

        [SerializeField] private float appearDuration = 0.2f;
        [SerializeField] private float waitForMoveDuration = 0.2f;
        [SerializeField] private float moveDuration = 0.5f;
        
        [SerializeField] private int preSpawnCount = 20;
        [SerializeField] private int spawnCount = 40;
        [SerializeField] private Transform rewardItemContainer;
        
        private List<RewardItemElement> _rewardItemElementList;
        private int _currentIndex;

        private CancellationTokenSource _poolCts;

        protected override void Awake()
        {
            base.Awake();
            if (isDestoryed)
            {
                return;
            }

            PreparePoolAsync().Forget();
        }

        private async UniTaskVoid PreparePoolAsync()
        {
            Helper.CreateCancellationTokenSourceSafe(ref _poolCts);
            CancellationToken token = _poolCts.Token;

            try
            {
                await UniTask.Yield(cancellationToken: token);

                for (int i = 0; i < rewardItemContainer.childCount; i++)
                {
                    Destroy(rewardItemContainer.GetChild(i).gameObject);
                }

                await UniTask.Yield(cancellationToken: token);

                GameObject rewardItemElementPrefab = ResourceDataObject.Instance.RewardItemElementPrefab;
                RewardItemElement rewardItemElement;
                _rewardItemElementList = new List<RewardItemElement>();

                for (int i = 0; i < preSpawnCount; i++)
                {
                    Instantiate(rewardItemElementPrefab, rewardItemContainer).TryGetComponent(out rewardItemElement);
                    _rewardItemElementList.Add(rewardItemElement);
                    rewardItemElement.gameObject.SetActive(false);
                }

                for (int i = 0; i < spawnCount - preSpawnCount; i++)
                {
                    Instantiate(rewardItemElementPrefab, rewardItemContainer).TryGetComponent(out rewardItemElement);
                    _rewardItemElementList.Add(rewardItemElement);
                    rewardItemElement.gameObject.SetActive(false);
                    await UniTask.Yield(cancellationToken: token);
                }
            }
            catch (Exception e)
            {
                if (e is not OperationCanceledException)
                {
                    Utils.MakeLog($"ERROR : {e}");
                }
            }
        }

        private void OnDestroy()
        {
            Helper.CancelAsyncSafe(ref _poolCts);
            if (rewardItemContainer != null)
            {
                for (int i = 0; i < rewardItemContainer.childCount; i++)
                {
                    rewardItemContainer.GetChild(i).TryGetComponent(out RewardItemElement rewardItemElement);
                    if (rewardItemElement != null)
                    {
                        if (!rewardItemElement.IsDestroy)
                        {
                            Destroy(rewardItemElement.gameObject);
                        }
                    }
                }
            }
        }

        public void ShowItem(int itemCount, ItemData itemData, Vector3 initPosition, Vector3 endPosition,
            Vector3 moveOffset)
        {
            Utils.MakeLog($"ShowItem : {itemCount} / itemType : {itemData.itemType}");
            if (itemCount <= 0)
            {
                return;
            }

            StartCoroutine(ShowRewardAnimation(itemCount, itemData, initPosition, endPosition, moveOffset));
        }

        private RewardItemElement GetAvailableRewardItemElement()
        {
            RewardItemElement rewardItemElement = _rewardItemElementList[_currentIndex++];
            _currentIndex %= _rewardItemElementList.Count;
            return rewardItemElement;
        }

        private IEnumerator ShowRewardAnimation(
            int rewardCount,
            ItemData itemData,
            Vector2 initPosition,
            Vector2 endPosition,
            Vector2 moveOffset)
        {
            int amount = 0;

            int objCount = rewardCount > maxCount ? maxCount : rewardCount;
            int count = 0;
            var list = new List<RewardItemElement>(rewardCount);

            for (int i = 0; i < objCount; i++)
            {
                RewardItemElement rewardItemElement = GetAvailableRewardItemElement();
                rewardItemElement.Init(itemData, initPosition);
                rewardItemElement.Appear(initPosition, moveOffset, appearDuration, randomRange);

                list.Add(rewardItemElement);
                yield return null;
            }

            GlobalManagerTable.SoundManager.PlaySFX(Key.Sound.SfxAddItem);

            yield return new WaitForSecondsRealtime(waitForMoveDuration);

            for (int i = 0; i < list.Count; i++)
            {
                list[i].MoveToTarget(endPosition, moveDuration);
                yield return null;
            }
        }
    }
}