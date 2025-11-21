using System;
using Network;
using Utils;
using Core.UI.Widget;
using UnityEngine;
using UnityEngine.Assertions;
using Users;

namespace .Core
{
    public class PointRefresher
    {
        public struct State
        {
            public PointType PointType;
            public Action ReflashCallback;
        }

        private readonly State _state;

        private delegate void ProductionChargeTimeDelegate();
        private event ProductionChargeTimeDelegate OnProductionChargeTimeUpdate;
        private readonly CommonText _refreshTimeText;

        private double _productionChargeTime;
        public long _updateLastTime;

        public PointRefresher(CommonText refreshTimeText, State state)
        {
            _refreshTimeText = refreshTimeText;
            _state = state;
        }

        public void Refresh()
        {
            _productionChargeTime = 0;
            OnProductionChargeTimeUpdate = null;
            _refreshTimeText.gameObject.SetActive(false);
            if (User.My.InventoryInfo.Points[_state.PointType].Count < User.My.WriterInfo.WriterMaxAp)
            {
                VirtualAp();
            }
        }

        public void OnLateUpdate()
        {
            if (OnProductionChargeTimeUpdate == null)
            {
                return;
            }
            
            OnProductionChargeTimeUpdate?.Invoke();
        }

        private void VirtualAp()
        {
            var production = User.My.InventoryInfo.Points[_state.PointType];
            Assert.IsNotNull(production);

            OnProductionChargeTimeUpdate += UpdateProductionChargeTime;
            
            _productionChargeTime = production.Renewed().AutoChargeTime;
            
            _refreshTimeText.gameObject.SetActive(true);
        }

        private void UpdateProductionChargeTime()
        {
            _productionChargeTime -= Time.deltaTime;
            if (_productionChargeTime <= 0)
            {
                Refresh();
                _state.ReflashCallback?.Invoke();
                return;
            }

            _refreshTimeText.SetText(TimeSpan.FromSeconds(_productionChargeTime).ToString(@"mm\:ss"));
        }
    }
}