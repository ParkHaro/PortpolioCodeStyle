using DG.Tweening;
using Gomble.MergeLion.Data;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Haro.View
{
    public class RewardItemElement : MonoBehaviour
    {
        private bool isDestroy = false;
        public bool IsDestroy => isDestroy;
        
        private ItemData itemData;
        private Material targetMaterial;

        [SerializeField] private ParticleSystem mainIconParticle;
        
        public void Init(ItemData newItemData, Vector2 newInitPos)
        {
            itemData = newItemData;
            
            Transform tf = transform;
            tf.position = newInitPos;
            tf.localScale = Vector3.zero;

            if (mainIconParticle != null)
            {
                mainIconParticle.GetComponent<Renderer>().material =
                    ResourceDataObject.Instance.RewardItemMaterialList[Helper.EnumToInt(itemData.itemType)];
            }
        }

        private void OnDestroy()
        {
            isDestroy = true;
        }

        public void Appear(Vector2 newBeginPos, Vector2 moveOffset, float duration, float randomBeginMoveRange = 0f)
        {
            gameObject.SetActive(true);
            
            Vector2 movePos = Vector2.zero;
            if (randomBeginMoveRange != 0f)
            {
                movePos = new (Random.Range(-randomBeginMoveRange, randomBeginMoveRange),
                    Random.Range(-randomBeginMoveRange, randomBeginMoveRange));
            }
            movePos += moveOffset;
            
            transform.DOKill();
            transform.DOScale(1f, duration).SetEase(Ease.OutBack);
            Vector2 targetPos = newBeginPos + movePos;
            
            Camera mainCam = Camera.main;
            float orthoSize = mainCam.orthographicSize;
            float halfOrthoSize = orthoSize * 0.5f;
            float cameraCenterYPos = mainCam.transform.position.y;
            
            targetPos.x = Mathf.Clamp(targetPos.x, -halfOrthoSize * 0.95f, halfOrthoSize * 0.95f);
            targetPos.y = Mathf.Clamp(targetPos.y, -orthoSize * 0.9f + cameraCenterYPos, 
                orthoSize * 0.9f + cameraCenterYPos);
            
            Vector3 targetPos3 = targetPos;
            targetPos3.z = Random.Range(-0.1f, 0f);
            transform.DOMove(targetPos3, duration).SetEase(Ease.OutBack);
        }

        public void MoveToTarget(Vector2 newEndPos, float duration)
        {
            float delayNoise = Random.Range(0f, 0.3f);
            transform.DOKill();
            transform.DOScale(0, duration).SetDelay(delayNoise);
            transform.DOMove(newEndPos, duration)
                .SetDelay(delayNoise)
                .OnComplete(() => Desappear(0.3f));
        }

        public void Desappear(float duration)
        {
            GlobalManagerTable.SoundManager.PlaySFX(Key.Sound.SfxCollectItem, 0.3f);
            transform.gameObject.SetActive(false);
        }
    }
}