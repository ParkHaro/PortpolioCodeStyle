using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Haro.Manager
{
    public class UIManager : MonoBehaviour
    {
        public List<BasePanel> UIPanelList
        {
            get
            {
                if (uiPanelList == null || uiPanelList.Count == 0)
                {
                    uiPanelList = FindObjectsOfType<BasePanel>(true).ToList();
                }
                return uiPanelList;
            }
        }
        public List<BasePanel> OpenedPanelList => openedPanelList;
        public List<BasePopup> UIPopupList
        {
            get
            {
                if (uiPopupList == null || uiPopupList.Count == 0)
                {
                    uiPopupList = FindObjectsOfType<BasePopup>(true).ToList();
                }
                return uiPopupList;
            }
        }
        public List<BasePopup> OpenedPopupList => openedPopupList;
        
        [ReadOnly][SerializeField] private List<BasePanel> uiPanelList = new();
        [ReadOnly][SerializeField] private List<BasePanel> openedPanelList = new();
        [ReadOnly][SerializeField] List<BasePopup> uiPopupList = new();
        [ReadOnly][SerializeField] List<BasePopup> openedPopupList = new();

        [Button]
        public T OpenPanel<T>(object data = null, UnityAction done = null) where T : BasePanel
        {
            foreach (BasePanel panel in UIPanelList)
            {
                if (panel is T basePanel)
                {
                    if (!openedPanelList.Contains(basePanel))
                    {
                        openedPanelList.Add(basePanel);
                    }
                    basePanel.Open(data, done);
                    return basePanel;
                }
            }

            Utils.MakeLog(Utils.LogCategory.WARNING, $"Fail Open UI : {typeof(T).Name}");
            return null;
        }

        [Button]
        public void ClosePanel<T>(UnityAction done = null) where T : BasePanel
        {
            foreach (BasePanel panel in UIPanelList)
            {
                if (panel is T)
                {
                    panel.Close(done);
                    if (openedPanelList.Contains(panel))
                    {
                        openedPanelList.Remove(panel);
                    }
                    return;
                }
            }

            Utils.MakeLog(Utils.LogCategory.WARNING, $"Fail Close UI : {typeof(T).Name}");
        }

        public bool IsOpenedPanel<T>() where T : BasePanel
        {
            foreach (BasePanel panel in openedPanelList)
            {
                T p = panel as T;
                if(p != null)
                {
                    return true;
                }
            }

            return false;
        }

        public void CloseAllPanel()
        {
            List<BasePanel> tmpOpenedPanelList = openedPanelList.ToList();
            foreach (BasePanel panel in tmpOpenedPanelList)
            {
                panel.Close();
                if (openedPanelList.Contains(panel))
                {
                    openedPanelList.Remove(panel);
                }
            }
        }

        public T GetPanel<T>() where T : BasePanel
        {
            foreach (BasePanel panel in UIPanelList)
            {
                if (panel is T)
                {
                    return (T)panel;
                }
            }

            return null;
        }

        public bool HasPanel<T>() where T : BasePanel
        {
            foreach (BasePanel panel in uiPanelList)
            {
                if (panel is T)
                {
                    return true;
                }
            }

            return false;
        }

        public void OpenPopup<T>(object data = null) where T : BasePopup
        {
            foreach (BasePopup popup in UIPopupList)
            {
                if (popup is T)
                {
                    openedPopupList.Add(popup);
                    popup.Open(data);
                }
            }
        }

        public void ClosePopup<T>() where T : BasePopup
        {
            foreach (BasePopup popup in UIPopupList)
            {
                if (popup is T)
                {
                    popup.Close();
                    openedPopupList.Remove(popup);
                }
            }
        }

        public T GetPopup<T>() where T : BasePopup
        {
            foreach (BasePopup popup in UIPopupList)
            {
                if (popup is T)
                {
                    return (T)popup;
                }
            }

            return null;
        }

        public bool HasPopup<T>() where T : BasePopup
        {
            foreach (BasePopup popup in uiPopupList)
            {
                if (popup is T)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
