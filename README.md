# Portfolio Code Style

Unity 게임 개발 경험을 보여주는 코드 스타일 포트폴리오입니다.

## Tech Stack

- **Engine**: Unity
- **Language**: C#
- **Libraries**: Addressables, UniTask, Odin Inspector

## Project Structure

### Y2019_2021_Code-LaserZone
VR 게임 "LaserZone" 프로젝트에서 작성한 코드입니다.

| File | Description |
|------|-------------|
| `CinematicManager.cs` | 시네마틱 시퀀스 관리 |
| `LzObject.cs` | 게임 오브젝트 기본 클래스 |
| `HealthPoint.cs` | HP 시스템 |
| `TaskItem.cs` | 태스크 시스템 기본 클래스 |
| `TaskItemCinematic.cs` | 시네마틱 태스크 |
| `TaskItemSendMessage.cs` | 메시지 전송 태스크 |

### Y2022_2023_Code-Bot
게임 AI 봇 시스템 코드입니다.

| File | Description |
|------|-------------|
| `BaseBot.cs` | 봇 기본 추상 클래스 |
| `Bot.cs` | 봇 구현체 |
| `CommonBot.cs` | 공용 봇 로직 |

### Y2022_2023_Code-UI
UI 프레임워크 코드입니다.

| File | Description |
|------|-------------|
| `UIManager.cs` | UI 전체 관리 매니저 |
| `BasePanel.cs` | 패널 기본 클래스 |
| `BasePopup.cs` | 팝업 기본 클래스 |
| `TopPanel.cs` | 상단 UI 패널 |
| `RewardItemController.cs` | 보상 아이템 컨트롤러 |
| `RewardItemElement.cs` | 보상 아이템 요소 |
| `ShareEventPopup.cs` | 공유 이벤트 팝업 |

### Y2024_2025_Code
최신 프로젝트 코드입니다.

#### Code-AssetManager
Addressables 기반 에셋 관리 시스템입니다.

| File | Description |
|------|-------------|
| `AssetManager.cs` | 에셋 매니저 메인 클래스 |
| `AssetManager.AssetPool.cs` | 에셋 풀 관리 |
| `AssetManager.ObjectPool.cs` | 오브젝트 풀 관리 |
| `AssetManager.LoadAsset.cs` | 에셋 로드 기능 |
| `AssetManager.Instantiate.cs` | 인스턴스 생성 |
| `AssetManager.Provider.cs` | 에셋 제공자 |
| `AssetManager.Singleton.cs` | 싱글톤 에셋 관리 |

#### Code-EventCommon
이벤트 시스템 공통 코드입니다.

| File | Description |
|------|-------------|
| `UIEventCommonContent.cs` | 이벤트 콘텐츠 기본 클래스 |
| `UIEventCommonMissionPanel.cs` | 이벤트 미션 패널 |
| `UIEventSampleMainPanel.cs` | 샘플 이벤트 메인 패널 |

#### Code-ProductionSlot
생산 슬롯 시스템입니다.

| File | Description |
|------|-------------|
| `ProductionSlot.cs` | 생산 슬롯 메인 클래스 |
| `BaseProduction.cs` | 생산 기본 클래스 |
| `ItemProduction.cs` | 아이템 생산 |
| `PointProduction.cs` | 포인트 생산 |
| `PointRefresher.cs` | 포인트 갱신 |

## Code Highlights

### Partial Class 활용
`AssetManager`는 기능별로 partial class를 분리하여 관리합니다.
```csharp
public partial class AssetManager : MonoSingletonDontDestroyed<AssetManager>
```

### 제네릭 UI 시스템
타입 안전한 UI 관리를 위해 제네릭 메서드를 사용합니다.
```csharp
public T OpenPanel<T>(object data = null, UnityAction done = null) where T : BasePanel
```

### 비동기 처리
UniTask를 활용한 비동기 봇 처리 시스템입니다.
```csharp
protected abstract UniTaskVoid BotProcessAsync();
protected abstract UniTaskVoid ChangeDecisionProcessAsync();
```

## Contact

- Gmail: haro7488@gmail.com
