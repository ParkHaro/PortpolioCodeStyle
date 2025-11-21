using System;
using System.Collections.Generic;
using Messages;
using Script.Data.Extension;
using Users;

namespace Core
{
    public abstract class ItemBaseProduction : BaseProduction
    {
        protected abstract ItemType ItemType { get; }
        public override string Text => $"{Count.ToFormattedString()}";
        public override long Count => User.My.InventoryInfo.GetCount(ProductionType.ToString()); // 
        protected virtual string CustomId => string.Empty;

        public override void OnTooltipClicked()
        {
            var groupId = !string.IsNullOrEmpty(CustomId)
                ? CustomId
                : ItemType == ItemType.None
                    ? ProductionType.ToString()
                    : ItemType.GetId();

            if (string.IsNullOrEmpty(groupId) == false)
            {
                ShortCutOverlayEvent.Publish(new()
                {
                    ShortCutType = ShortCutType.Item,
                    GroupId = groupId,
                    ServerAnswerShortcutDataChanged = ServerAnswerShortcutDataChanged,
                });
            }
        }
    }
}