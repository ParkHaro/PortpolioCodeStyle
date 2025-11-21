using Provider;
using UnityEngine.AddressableAssets;

namespace Manager
{
    public partial class AssetManager
    {
        private ProviderContainer _provider;
        private ProviderContainer Provider => _provider ??= new ProviderContainer();
        
        public class ProviderContainer
        {
            private static GameModeProvider _gameModeProvider;
            public static GameModeProvider GameModeProvider => _gameModeProvider ??=
                Addressables.LoadAssetAsync<GameModeProvider>("Assets/Data/Providers/GameModeProvider.asset").WaitForCompletion();
            private static UIPanelProvider _uiPanelProvider;
            public static UIPanelProvider UIPanelProvider => _uiPanelProvider ??=
                Addressables.LoadAssetAsync<UIPanelProvider>("Assets/Data/Providers/UIPanelProvider.asset").WaitForCompletion();
            private static UIOverlayProvider _uiOverlayProvider;
            public static UIOverlayProvider UIOverlayProvider => _uiOverlayProvider ??=
                Addressables.LoadAssetAsync<UIOverlayProvider>("Assets/Data/Providers/UIOverlayProvider.asset").WaitForCompletion();
            private static UISlotModuleProvider _uiSlotModuleProvider;
            public static UISlotModuleProvider UISlotModuleProvider => _uiSlotModuleProvider ??=
                Addressables.LoadAssetAsync<UISlotModuleProvider>("Assets/Data/Providers/UISlotModuleProvider.asset").WaitForCompletion();

            public void ReleaseLoadedProviderAssets()
            {
                UIPanelProvider.ReleaseAllRegisteredAssets();
                UIOverlayProvider.ReleaseAllRegisteredAssets();
                UISlotModuleProvider.ReleaseAllRegisteredAssets();
            }
        }
    }
}