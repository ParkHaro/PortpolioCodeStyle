using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Serialization;

public class HealthPoint : MonoBehaviour
{
    private BaseEntity _owner;
    public BaseEntity Owner => _owner;
    public Action<IAttackable, float, Vector3> EventTakeDamage = (o,f,p) => { };
    public Action<float> EventTakeHealth = (f) => { };
    public Action<float> EventChangedMaxHp = (f) => { };
    public Action<float> EventChangedHp = (f) => { };
    public Action EventDead = ()=> { };

    [SerializeField] private float maxHp = 100f;
    public float MaxHp {
        get => maxHp;
        set
        {
            maxHp = value;
            EventChangedMaxHp(maxHp);
        }
    }

    [SerializeField] private float hp = 100f;
    public float Hp {
        get => hp;
        set {
            if (value <= 0f)
            {
                hp = 0f;
            }
            else if (value > maxHp)
            {
                hp = maxHp;
            }
            else
            {
                hp = value;
            }
            EventChangedHp(hp);
        }
    }

    public float CurrentHpRatio => hp / maxHp;

    private void Awake()
    {
        _owner = GetComponentInParent<BaseEntity>();
    }

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        Hp = MaxHp;
    }

    public void TakeDamage(IAttackable attacker, float damage, Vector2 hitPos)
    {
        if(Hp <= 0)
        {
            return;
        }

        Hp -= damage;
        EventTakeDamage(attacker, damage, hitPos);

        if (Hp <= 0)
        {
            EventDead();
        }
    }

    public void TakeHealth(float health)
    {
        Hp += health;
        EventTakeHealth(health);
    }

    private void OnDestroy()
    {
        EventTakeDamage = null;
        EventTakeHealth = null;
        EventDead = null;
    }
}
