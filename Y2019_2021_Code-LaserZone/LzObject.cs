using System;
using System.Collections;
using UnityEngine;
using DG.Tweening;
using Spine.Unity;
using LitJson;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Spine;
using UniRx;

public class LzObject : BaseEntity, ICrowdControlable, ISwitchable, ISfxSoundPlayable, IInteractable, IRigidbodyable, IPauseable
{
    [SerializeField] private Rigidbody2D rigidbody;
    public Rigidbody2D Rigidbody => rigidbody;

    private MeshRenderer[] _arrRenderer;

    private MaterialPropertyBlock _defaultBlock;
    private MaterialPropertyBlock _secondBlock;

    public LzObjectType lzObjectType = LzObjectType.None;
    public bool isSwitchable = false;
    public bool isPush = false;
    public bool isLaserAttack = false;
    public bool isCharacterAttack = false;
    public bool isDead = false;
    private HealthPoint _healthPoint;
    public HealthPoint HealthPoint => _healthPoint;
    public float weight;
    
    public SkeletonAnimation skeletonAnimation;
    public SkeletonAnimation shadowSkeletonAnimation;
    public Collider2D trigger;
    public Collider2D bulletTrigger;
    public Transform pivotColliderObject;

    private bool isInitilized = false;

    [field: SerializeField] public bool IsOn { get; set; } = true;

    [field: SerializeField] public List<SwitchEntity> OwnerSwitchEntityList { get; set; }

    [SerializeField] private Pauseable pauseable = new Pauseable();

    public Pauseable Pauseable => pauseable;

    [field: SerializeField] public CrowdControlType CrowdControlState { get; set; } = CrowdControlType.None;

    public UiSliderCanvas hpSliderCanvas;

    public bool isTwinkle;

    private static readonly int Black = Shader.PropertyToID("_Black");

    private Spine.TrackEntry _track = null;
    private Tween _backTween = null;
    private Coroutine _routineSwitching;
    private MeshRenderer _meshRenderer;
    private MeshRenderer _shadowMeshRenderer;

    public GameObject interactKeyObject;
    public TextMeshPro interactKeyText;
    private IDisposable nearCheckObservable;

    [SerializeField]
    private Interactable interactable;
    public Interactable Interactable => interactable;
        
    public override JsonSceneEntity ParseSceneToJson()
    {
        JsonSceneEntity entity = base.ParseSceneToJson();
        JsonSceneLZObject lzObject = new JsonSceneLZObject(entity);
        lzObject.IsOn = this.IsOn;
        return lzObject as JsonSceneEntity;
    }

    public override void ParseJsonToScene(JsonData data)
    {
        base.ParseJsonToScene(data);
        IDictionary dictionary = data as IDictionary;
        if (dictionary.Contains("IsOn"))
        {
            this.IsOn = LZUtils.ParseDataStringToBool(data["IsOn"]);
        }

        this.transform.parent = this.ownerRoom.lzObjects;
    }

    protected override void Awake()
    {
        this.entityType = EntityType.LzObject;
        
        SetUpSoundSystem();

        rigidbody = GetComponent<Rigidbody2D>();
        _arrRenderer = GetComponentsInChildren<MeshRenderer>();
        _healthPoint = GetComponent<HealthPoint>();
        _meshRenderer = skeletonAnimation.GetComponent<MeshRenderer>();
        _shadowMeshRenderer = shadowSkeletonAnimation.GetComponent<MeshRenderer>();
        hpSliderCanvas = GetComponentInChildren<UiSliderCanvas>();

        if (trigger == null)
        {
            trigger = GetComponent<Collider2D>();
        }

        if (bulletTrigger == null)
        {
            bulletTrigger = transform.Find("BulletTrigger").GetComponent<Collider2D>();
        }

        pivotColliderObject = transform.Find("PivotCollider");

        if (rigidbody != null)
        {
            rigidbody.isKinematic = isPush != true;

            rigidbody.mass = weight;
        }

        _defaultBlock = new MaterialPropertyBlock();
        _secondBlock = new MaterialPropertyBlock();

        foreach (var item in _arrRenderer)
        {
            item.SetPropertyBlock(_defaultBlock);
        }

        _secondBlock.SetColor(Black, Color.white);

        if (_healthPoint != null)
        {
            if (!ReferenceEquals(hpSliderCanvas, null))
            {
                _healthPoint.EventChangedHp += OnChangedHp;
            }

            _healthPoint.EventTakeDamage += OnTakeDamage;
            _healthPoint.EventDead += OnDead;
        }
        
        Interactable.Init(gameObject);
        Interactable.RegisterInteractCallback(InteractCallback);
        
        pauseable.Init(this, this);
    }
    
    #region Sound

    private SoundSystem _soundSystem;
    public SoundSystem SoundSystem => _soundSystem;

    private const int SoundGroundedEntityForcedMove = 1000;
    private const int SoundBoxDestruction = 1273;
    
    public virtual void SetUpSoundSystem()
    {
        _soundSystem = new SoundSystem(GetComponents<AudioSource>(), 1);
        SoundSystem.AddSoundData(SoundGroundedEntityForcedMove);
        SoundSystem.AddSoundData(SoundBoxDestruction);
    }

    #endregion

    protected virtual void Start()
    {
        if (hpSliderCanvas != null)
        {
            hpSliderCanvas.SetMaxValue(_healthPoint.MaxHp);
        }

        if (isSwitchable)
        {
            if (IsOn)
            {
                TurnOn();
            }
            else
            {
                TurnOff();
            }
        }

        _beforePos = transform.position;
        isInitilized = true;
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
    }
    
    private Vector2 _beforePos;
    private bool _isPushing = false;

    private void Update()
    {
        if (lzObjectType != LzObjectType.Push)
        {
            return;
        }
        
        if (Vector2.Distance(_beforePos, transform.position) > 0.1f)
        {
            if (_isPushing == false)
            {
                _isPushing = true;
                SoundSystem.PlaySfx(SoundGroundedEntityForcedMove);
            }
        }
        else
        {
            if (_isPushing)
            {
                _isPushing = false;
                SoundSystem.StopLoopSfx();
            }
        }
        _beforePos = transform.position;
    }

    public virtual void TwinkleSpineMaterial(float duration)
    {
        if (isTwinkle == true)
        {
            StopCoroutine($"CoTwinkleSpineMaterial");
            isTwinkle = false;
        }

        StartCoroutine(CoTwinkleSpineMaterial(duration));
    }

    private IEnumerator CoTwinkleSpineMaterial(float duration)
    {
        isTwinkle = true;

        foreach (var item in _arrRenderer)
        {
            item.SetPropertyBlock(_secondBlock);
        }

        yield return new WaitForSeconds(duration);
        isTwinkle = false;

        foreach (var item in _arrRenderer)
        {
            item.SetPropertyBlock(_defaultBlock);
        }
    }

    private void OnChangedHp(float hp)
    {
        if (hpSliderCanvas != null)
        {
            hpSliderCanvas.SetValue(hp);
        }
    }

    protected virtual void OnTakeDamage(IAttackable attacker, float damage, Vector3 hitPos)
    {
        if (_healthPoint.Hp <= 0)
        {
            return;
        }

        if (hpSliderCanvas != null)
        {
            hpSliderCanvas.ChangeValue(damage, false, GameManager.Instance.isDamageFontOn);
        }

        _track = skeletonAnimation.AnimationState.SetAnimation(0, "hit", false);

        if (!(_healthPoint.Hp > 0)) return;

        _backTween?.Kill();

        _backTween = DOVirtual.DelayedCall(_track.AnimationEnd,
            () =>
            {
                if (isDead == false)
                {
                    skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
                }
            });
    }

    public void TurnOn()
    {
        if (this is ItemBox)
        {
            return;
        }

        this.IsOn = true;

        Arrive();
    }

    public void TurnOff()
    {
        if (this is ItemBox)
        {
            return;
        }

        this.IsOn = false;

        Dead();
    }

    public virtual void InteractCallback(GameObject owner, BaseEntity requestEntity)
    {
        
    }

    public void TurnOnPushObjectKey(Vector3 playerPos)
    {
        if ((lzObjectType == LzObjectType.Normal) && isPush)
        {
            if (interactKeyText != null)
            {
                interactKeyObject.SetActive(true);
                Vector3 dir = playerPos - transform.position;
                nearCheckObservable = Observable.EveryUpdate()
                    .Subscribe(n =>
                    {
                        if (!((dir.x < 1f) && (dir.y < 1f)) || (rigidbody.velocity.magnitude > 0.9f))
                        {
                            DOVirtual.DelayedCall(0.6f, TurnOffPushObjectKey);
                        }
                    }).AddTo(this.gameObject);

                if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
                {
                    if (dir.x > 0)
                    {
                        interactKeyText.text = "←";
                    }
                    else
                    {
                        interactKeyText.text = "→";
                    }
                }
                else if (Mathf.Abs(dir.x) < Mathf.Abs(dir.y))
                {
                    if (dir.y > 0)
                    {
                        interactKeyText.text = "↓";
                    }
                    else
                    {
                        interactKeyText.text = "↑";
                    }
                }
            }
        }
    }

    public void TurnOffPushObjectKey()
    {
        if ((lzObjectType == LzObjectType.Normal) && isPush)
        {
            nearCheckObservable.Dispose();
            if (interactKeyObject != null)
            {
                interactKeyObject.SetActive(false);
            }
        }
    }

    public void Switching(BaseEntity requestEntity)
    {
        if (pauseable.IsPause)
        {
            return;
        }
        
        IsOn = !IsOn;
        if (IsOn)
        {
            TurnOn();
        }
        else
        {
            TurnOff();
        }
    }

    private void OnDead()
    {
        Dead();
    }

    [Button]
    public void Arrive()
    {
        if (_routineSwitching != null)
        {
            StopCoroutine(_routineSwitching);
        }

        _routineSwitching = StartCoroutine(CoArrive());
    }

    private IEnumerator CoArrive()
    {
        if (isInitilized)
        {
            SoundSystem.PlaySfx(SoundBoxDestruction);
        }
        pivotColliderObject.gameObject.SetActive(true);

        _meshRenderer.sortingOrder = 0;
        _shadowMeshRenderer.sortingOrder = -10;

        IsOn = true;
        isDead = false;

        skeletonAnimation.AnimationState.SetAnimation(0, "up", false);
        shadowSkeletonAnimation.AnimationState.SetAnimation(0, "up", false);
        var trackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
        yield return new WaitWhile(() => trackEntry.TrackTime <= 0.6f);
        transform.gameObject.layer = LayerMask.NameToLayer("Objects");
        bulletTrigger.gameObject.layer = LayerMask.NameToLayer("BulletTrigger");
        yield return new WaitWhile(() => trackEntry.TrackTime <= 0.98f);
        skeletonAnimation.AnimationState.SetAnimation(0, "idle", false);
        shadowSkeletonAnimation.AnimationState.SetAnimation(0, "idle", false);
        _routineSwitching = null;
    }

    [Button]
    public virtual void Dead()
    {
        if (isDead)
        {
            return;
        }

        if (_routineSwitching != null)
        {
            StopCoroutine(_routineSwitching);
        }

        _routineSwitching = StartCoroutine(CoDead());
    }

    protected virtual IEnumerator CoDead()
    {
        if (isDead)
        {
            yield break;
        }

        if (isInitilized)
        {
            SoundSystem.PlaySfx(SoundBoxDestruction);
        }

        isDead = true;
        IsOn = false;

        skeletonAnimation.AnimationState.SetAnimation(0, "down", false);
        shadowSkeletonAnimation.AnimationState.SetAnimation(0, "down", false);
        var trackEntry = skeletonAnimation.AnimationState.GetCurrent(0);
        yield return new WaitWhile(() => trackEntry.TrackTime <= 0.5f);
        LayerMask layer = LayerMask.NameToLayer("Dead");
        transform.gameObject.layer = layer;
        bulletTrigger.gameObject.layer = layer;
        yield return new WaitWhile(() => trackEntry.TrackTime <= 0.98f);
        skeletonAnimation.AnimationState.SetAnimation(0, "close", false);
        shadowSkeletonAnimation.AnimationState.SetAnimation(0, "close", false);

        _meshRenderer.sortingOrder = -11;
        _shadowMeshRenderer.sortingOrder = -12;

        pivotColliderObject.gameObject.SetActive(false);
        _routineSwitching = null;
    }

    public void CrowdControl(CrowdControlType crowdControlType)
    {
        this.CrowdControlState = crowdControlType;
        switch (this.CrowdControlState)
        {
            case CrowdControlType.None:
                break;
            case CrowdControlType.Transfer:
                break;
            case CrowdControlType.Blackhole:
                rigidbody.velocity = Vector2.zero;
                rigidbody.isKinematic = true;
                break;
            default:
                break;
        }
    }

    public void Appear()
    {
        Appear(transform.position);
    }

    public void Appear(Vector2 pos)
    {
        transform.position = pos;
        skeletonAnimation.AnimationState.SetAnimation(0, "appear", false);

        void OnComplete(TrackEntry trackEntry)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
            skeletonAnimation.AnimationState.Complete -= OnComplete;
        }

        skeletonAnimation.AnimationState.Complete += OnComplete;
    }

    public void Disappear()
    {
        pivotColliderObject.gameObject.SetActive(false);
        trigger.enabled = false;
        bulletTrigger.enabled = false;
        skeletonAnimation.AnimationState.SetAnimation(0, "disappear", false);

        void OnComplete(TrackEntry trackEntry)
        {
            Destroy(gameObject);
        }

        skeletonAnimation.AnimationState.Complete += OnComplete;
    }

    public virtual void ReplacePause()
    {
        pauseable.IsPause = true;
        if (_healthPoint != null)
        {
            _healthPoint.Hp = 0f;
        }
        Dead();
    }

    public virtual void ReplaceResume()
    {
        pauseable.IsPause = false;
    }
}