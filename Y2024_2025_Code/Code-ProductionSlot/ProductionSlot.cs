using Core;
using Core.UI.Widget;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using Data;
using Sirenix.Utilities;
using UI.Messages;
using UIStyle;
using UnityEngine;
using UnityEngine.UI;
using Users.Messages;

public partial class ProductionSlot : UIElement
{
#region Widgets
    public partial class ButtonWidgets
    {
        public CommonButton ChargePoint;
        public CommonButton Icon;
    }

    public partial class TextWidgets
    {
        public CommonText Production;
        public CommonText RefreshTime;
    }

    public partial class ImageWidgets
    {
        public Image Production;
    }

#region WidgetsProperty
    [SerializeField] private ButtonWidgets _buttons;
    private ButtonWidgets Buttons => _buttons;

    [Title(nameof(Buttons)), HideLabel, Serializable]
    public partial class ButtonWidgets { }

    [SerializeField] private ImageWidgets _images;
    private ImageWidgets Images => _images;
    [Serializable] public partial class ImageWidgets : BaseWidgets.Images { }
    [SerializeField] private TextWidgets _texts;
    private TextWidgets Texts => _texts;

    [Title(nameof(Texts)), HideLabel, Serializable]
    public partial class TextWidgets { }
#endregion
#endregion

    [Title(DefaultTitleName)]
    [SerializeField] private UIStyles _uiStyle;

    private ProductionType _type = ProductionType.Ap;
    private readonly Dictionary<ProductionType, PointRefresher> _pointRefresherDict = new();

    private BaseProduction _production;
    private long _dungeonTicketChargeTime;

    protected override void AddWidgetListener()
    {
        base.AddWidgetListener();
        Buttons.ChargePoint.AddListener(OnChargePointButtonClicked);
        Buttons.Icon.AddListener(OnTooltipClicked);
    }

    protected override void AwakeProcess()
    {
        base.AwakeProcess();

        _pointRefresherDict.Add(ProductionType.Ap, new(
            refreshTimeText: Texts.RefreshTime,
            state: new()
            {
                PointType = PointType.Ap,
                ReflashCallback = Refresh,
            }
        ));
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        ProductChangeTypeEvent.Subscribe(TypeChangeEvent);
        CountableThingEntityUpdatedEvent.Subscribe(OnCountableThingEntityUpdated);
        AccountSessionCheckEvent.Subscribe(OnAccountSessionCheckEvent);

        Refresh();
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        ProductChangeTypeEvent.Unsubscribe(TypeChangeEvent);
        CountableThingEntityUpdatedEvent.Unsubscribe(OnCountableThingEntityUpdated);
        AccountSessionCheckEvent.Unsubscribe(OnAccountSessionCheckEvent);
    }

    public void Setting(string settingProduction)
    {
        // 1. ProductionType과 1:1 매칭
        _type = settingProduction switch
        {
            "Dia" => ProductionType.TaleStone,
            "DungeonKey" => ProductionType.DungeonTicket,
            "TotalwarTicket" => ProductionType.TotalWarTicket,
            "ArenaTicket" => ProductionType.ArenaTicket,
            "PracticeBookTicket" => ProductionType.PracticeBookTicket,
            _ => ProductionType.None
        };

        // Enum.TryParse(typeProduction, out ProductionType type);
        if (_type == ProductionType.None)
        {
            Enum.TryParse(settingProduction, out _type);
            if (_type == ProductionType.None)
            {
                DebugHelper.LogError($"Production : {settingProduction} Type is None. Please Implement ProductionType and Production class");
                gameObject.SetActive(false);
            }
        }

        Type = _type;

        // 2. ThingId와 1:1 매칭
        var thingProduction = _type.ToThingId();
        if (thingProduction.Equals("None"))
        {
            thingProduction = settingProduction;
        }

        var thingData = DataTable.ThingDataTable.GetById(thingProduction);
        if (thingData == null)
        {
            DebugHelper.Log($"Thing data is null \nSettingProduction [{settingProduction}] ThingProduction [{thingProduction}]", Color.yellow);
            return;
        }

        Images.Production.sprite = AtlasManager.GetItemIcon(thingData.IconPath);

        _uiStyle.SetStyle(_production is IChargeProduction ? "Purchase" : "Default");
    }

    private BaseProduction CreateProduction(ProductionType type)
    {
        BaseProduction production = type switch
        {
            ProductionType.Ap => new ApPointProduction(),
            ProductionType.Gold => new GoldPointProduction(),
            ProductionType.TaleStone => new TaleStonePointProduction(),
            ProductionType.DungeonTicket => new DungeonTicketPointProduction(),
            ProductionType.TotalWarTicket => new TotalWarTicketPointProduction(),
            ProductionType.ArenaTicket => new ArenaTicketPointProduction(),
            ProductionType.ArcanaCoin => new ArcanaCoinPointProduction(),
            ProductionType.MemoryCoin => new MemoryCoinItemProduction(),
            ProductionType.ExpeditionCoin => new ExpeditionCoinItemProduction(),
            ProductionType.TotalWar_01 => new TotalWarCoinItemProduction(),
            ProductionType.ArenaCoin => new ArenaCoinItemProduction(),
            ProductionType.PracticeBookTicket => new PracticeBookTicketPointProduction(),
            ProductionType.Item_Ticket_GachaTicket => new GachaTicketItemProduction(),
            ProductionType.Item_Ticket_TenRoll_GachaTicket => new TenRollGachaTicketItemProduction(),
            ProductionType.Item_GachaTicket_3GradeFix_TenRoll => new ThreeGradeFixTenRollItemProduction(),
            ProductionType.Item_GachaTicket_RedFix_TenRoll => new RedFixTenRollItemProduction(),
            ProductionType.Item_GachaTicket_BlueFix_TenRoll => new BlueFixTenRollItemProduction(),
            ProductionType.Item_GachaTicket_GreenFix_TenRoll => new GreenFixTenRollItemProduction(),
            ProductionType.Item_GachaTicket_GuardianKnightFix_TenRoll => new GachaTicketGuardianKnightFixTenRollItemProduction(),
            ProductionType.Item_GachaTicket_MageShooterFix_TenRoll => new GachaTicketMageShooterFixTenRollItemProduction(),
            _ => null,
        };

        if (production == null)
        {
            production = type switch
            {
                ProductionType.Item_Coin_EventAdventure_Flask => new EventRefundItemProduction(ProductionType.Item_Coin_EventAdventure_Flask),
                ProductionType.Item_Coin_EventAdventure_FlagPen => new(ProductionType.Item_Coin_EventAdventure_FlagPen),
                ProductionType.Item_EventAdventure_BossTicket => new(ProductionType.Item_EventAdventure_BossTicket),
                ProductionType.Item_Coin_Event_ApBoost_HeroGrowthBoost => new(ProductionType.Item_Coin_Event_ApBoost_HeroGrowthBoost),
                ProductionType.Item_Coin_Event_TileBreak_Halloween2025 => new(ProductionType.Item_Coin_Event_TileBreak_Halloween2025),
                ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Soap => new(ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Soap),
                ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Egg => new(ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Egg),
                ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Towel => new(ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Towel),
                ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Basket => new(ProductionType.Item_Coin_EventAdventure_N002OceanSpa_Basket),
                ProductionType.Item_Coin_GuideMissionStackClear_Hotspring => new(ProductionType.Item_Coin_GuideMissionStackClear_Hotspring),
                ProductionType.Item_EventAdventure_N002OceanSpa_BossTicket => new(ProductionType.Item_EventAdventure_N002OceanSpa_BossTicket),
                ProductionType.Item_Coin_ChallengeMission_XMas2025 => new(ProductionType.Item_Coin_ChallengeMission_XMas2025),
                _ => null
            };
        }

        return production;
    }

    private void Refresh()
    {
        if (this == null)
            return;

        if (Texts.RefreshTime == null)
        {
            DebugHelper.Log($"[{name}] [{nameof(TextWidgets.RefreshTime)}] is NULL");
            return;
        }

        Texts.RefreshTime.gameObject.SetActive(false);
        if (_pointRefresherDict.TryGetValue(_type, out var pointRefresher))
        {
            pointRefresher.Refresh();
        }

        if (_production != null)
        {
            _production.Refresh(Texts.Production);
        }
    }

    private bool OnCountableThingEntityUpdated(CountableThingEntityUpdatedEvent msg)
    {
        Refresh();
        return true;
    }

    private bool OnAccountSessionCheckEvent(AccountSessionCheckEvent msg)
    {
        if (msg.Pause == false)
        {
            Refresh();
        }

        return true;
    }

    private ProductionType Type
    {
        get => _type;
        set
        {
            _type = value;
            _production = CreateProduction(_type);
            Refresh();
        }
    }

    private bool TypeChangeEvent(ProductChangeTypeEvent msg)
    {
        if (_type != msg.ChangeType)
        {
            return true;
        }

        Type = msg.ChangeType;

        return true;
    }

    private void LateUpdate()
    {
        _pointRefresherDict.ForEach(m => m.Value.OnLateUpdate());
    }

#region Events
    private void OnChargePointButtonClicked()
    {
        var chargeProduction = _production as IChargeProduction;
        chargeProduction?.OnChargePointButtonClicked();
    }

    private void OnTooltipClicked()
    {
        if (_production != null)
        {
            _production.OnTooltipClicked();
        }
    }
#endregion
}

public static class Extenstion
{
    public static string ToFormattedString(this int value)
    {
        return value.ToString("N0");
    }

    public static string ToFormattedString(this long value)
    {
        return value.ToString("N0");
    }
}