using System;
using System.Collections.Generic;
using Core.UI.Widget;

namespace Core
{
    public abstract class BaseProduction
    {
        public abstract ProductionType ProductionType { get; }
        public abstract string Text { get; }
        public abstract long Count { get; }
        protected virtual Action<List<Shortcut>> ServerAnswerShortcutDataChanged => null;

        public void Refresh(OzText text)
        {
            text.SetText(Text);
        }

        public abstract void OnTooltipClicked();
    }
}