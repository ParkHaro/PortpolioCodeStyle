using Cysharp.Threading.Tasks;
using Core.UI.Widget;
using Users;

namespace Content.EventCommon
{
    public class UIEventSampleSubContent : UIContent
    {
        public struct ContentState
        {
            public EventGroupItem EventGroupItem { get; init; }
        }

        private ContentState _state;
        
        public async UniTask OnEnter(ContentState state)
        {
            _state = state;
        }

        public void OnExit()
        {
            _state = default;
        }
    }
}