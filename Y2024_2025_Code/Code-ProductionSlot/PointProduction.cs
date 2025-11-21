using Messages;
using Script.Data.Extension;
using Users;

namespace Core
{
    public abstract class PointBaseProduction : BaseProduction
    {
        protected abstract PointType PointType { get; }
        public override long Count => User.My.InventoryInfo.Points[PointType].Count;

        public override void OnTooltipClicked()
        {
            var groupId = PointType.GetId();
            if (string.IsNullOrEmpty(groupId))
            {
                return;
            }

            ShortCutOverlayEvent.Publish(new()
            {
                ShortCutType = ShortCutType.Item,
                GroupId = groupId,
                ServerAnswerShortcutDataChanged = ServerAnswerShortcutDataChanged
            });
        }
    }
}