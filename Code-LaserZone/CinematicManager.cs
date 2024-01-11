using System;
using System.Collections;
using System.Collections.Generic;
using AlmostEngine.Screenshot.Extra;
using Cinemachine;
using DG.Tweening;
using Sirenix.OdinInspector;
using UniRx;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class CinematicProcessContainer
{
    public List<TaskItem> taskItemList;

    private int _currentIndex;

    public int CurrentIndex
    {
        get => _currentIndex;
        set => _currentIndex = value;
    }

    public Coroutine _cinematicProcessRoutine;
    public Coroutine _taskProcessRoutine;

    public TaskItem GetCurrentTask()
    {
        return taskItemList[CurrentIndex];
    }
}

public class CinematicManager : MonoBehaviour, ISfxSoundPlayable
{
    private static CinematicManager _instance;

    public static CinematicManager Instance
    {
        get => _instance;
    }

    public event Action EventBeginCinematic;
    public event Action<bool> EventEndCinematic;
    public event Action EventEndCinematicCameraCtrl;
    
    private CinematicViewCondition _cinematicViewCondition = null;

    private CinematicProcessContainer _mainCinematic;
    public CinematicProcessContainer MainCinematic => _mainCinematic;
    private List<CinematicProcessContainer> _subCinematicList;
    public List<CinematicProcessContainer> SubCinematicList => _subCinematicList;

    public bool IsCinematicViewer { get; set; }
    private bool _isSkip;

    public bool IsSkip
    {
        get => _isSkip;
        set
        {
            _isSkip = value;
        }
    }

    public CinematicProcessContainer GetCurrentSubCinematicProcess()
    {
        if (_subCinematicList.Count == 0)
        {
            return null;
        }

        return _subCinematicList[_subCinematicList.Count - 1];
    }

    private void Awake()
    {
        _instance = this;
        SetUpSoundSystem();
        _mainCinematic = new CinematicProcessContainer();
        _subCinematicList = new List<CinematicProcessContainer>();
    }
    
    #region Sound

    private SoundSystem _soundSystem;
    public SoundSystem SoundSystem => _soundSystem;

    private const int SoundFireBullet = 4000;
    
    public void SetUpSoundSystem()
    {
        _soundSystem = new SoundSystem(GetComponents<AudioSource>(), 0);
        SoundSystem.AddSoundData(SoundFireBullet);
    }

    #endregion

    private void Start()
    {
        GameManager.Instance.EventIsCinematicChanged += OnIsCinematicChanged;
    }

    private void OnDestroy()
    {
        GameManager.Instance.EventIsCinematicChanged -= OnIsCinematicChanged;
    }

    private void OnIsCinematicChanged(bool isCinematic)
    {
        
    }

    private string _fileName;
    
    [Button]
    public void BeginCinematic(string fileName, int beginIndex = 0, bool isSub = false)
    {
        IsSkip = false;
        if (GameDataObject.GetCurrentData().IsFirstPlay == false)
        {
            if (GameManager.Instance.IsCinematic)
            {
                if (isSub == false)
                {
                    return;
                }
            }
        }

        if (_skipTween != null)
        {
            _skipTween.Kill();
            FadeManager.FadeIn(0.3f);
            GameManager.Player.EnablePlayer();
            _skipTween = null;
        }

        // TODO DEBUG
        LZUtils.MakeLog($"BeginCinematic : {fileName}");
        _fileName = fileName;
        //Debug.Log($"BeginCinematic {fileName}");
        GameManager.Player.UiDashObject.SetActive(false);
        GameManager.Player.hpSliderCanvas.DisableSlider();
        
        CameraCtrl cameraCtrl = GameManager.Instance.cameraCtrl;
        cameraCtrl.SetSoftZone(0f, 1f);
        cameraCtrl.SetDeadZone(0f, 1f);
        cameraCtrl.SetLookAheadTime(0f, 4f);
        cameraCtrl.SetLookAheadSmoothing(0f, 4f);
        cameraCtrl.CameraFreeTargetTransform.parent = GameManager.Player.transform;
        cameraCtrl.CameraFreeTargetTransform.localPosition = Vector3.zero;
        GameUiManager.Instance.uiInGameOption.CanShowOption = false;

        CinematicDataObject cinematicDataObject = Resources.Load<CinematicDataObject>($"CinematicDatas/{fileName}");

        if (isSub)
        {
            CinematicProcessContainer subCinematic = new CinematicProcessContainer();
            _subCinematicList.Add(subCinematic);
            if (TaskParser.Load(cinematicDataObject, out subCinematic.taskItemList))
            {
                subCinematic._cinematicProcessRoutine = StartCoroutine(CoCinematicProcess(beginIndex, true));
            }
            InputManager.EnablePlayerInput();
        }
        else
        {
            _endCinematicTween?.Kill();
            _changeUiTween?.Kill();
            InputManager.EnablePlayerInput();

            GameManager.Instance.IsCinematic = true;
            GameDataObject.GameData gameData = ScriptableObjectManager.GameDataObject.GetCurrentDataInstance();
            _cinematicViewCondition = gameData.FindCinematicViewCondition(fileName);
            
            Vector3 playerPos = GameManager.Player.transform.position;
            playerPos.z = -10f;
            GameManager.Instance.cameraCtrl.transform.DOMove(playerPos, 0.3f)
                .OnComplete(() =>
                {
                    if (TaskParser.Load(cinematicDataObject, out _mainCinematic.taskItemList))
                    {
                        GameUiManager.Instance.ChangeUiMode(UiModeType.Hide);
                        EventBeginCinematic?.Invoke();
                        _mainCinematic._cinematicProcessRoutine = StartCoroutine(CoCinematicProcess(beginIndex));
                    }
                });
        }
    }

    private Tween _caputreTween;
    private IDisposable _skipObserver;

    private IEnumerator CoCinematicProcess(int beginIndex, bool isSub = false)
    {
        while (IllustrationManager.Instance.IsPlaying)
        {
            yield return null;
        }
        
        CinematicProcessContainer container = isSub ? GetCurrentSubCinematicProcess() : _mainCinematic;

        _caputreTween?.Kill();
        // 캡쳐
        _caputreTween = DOVirtual.DelayedCall(1f, () => { CaptureScreenshot(); });

        if (isSub == false)
        {
            // 스킵 감시
            ActivateSkipProcess();
        }

        container.CurrentIndex = beginIndex;
        while (container.CurrentIndex < container.taskItemList.Count)
        {
            if (IsSkip)
            {
                break;
            }
            container._taskProcessRoutine = StartCoroutine(container.GetCurrentTask().CoProcess());
            yield return container._taskProcessRoutine;
            container.CurrentIndex++;
        }
        
        EndCinematic(container, isSub, IsSkip);
    }

    private void CaptureScreenshot()
    {
        if (_cinematicViewCondition == null)
        {
            return;
        }
        
        GameManager.Instance.ScreenshotCutter.m_SelectionArea.position =
            Camera.main.WorldToScreenPoint(GameManager.Player.transform.position);
        GameManager.Instance.ScreenshotManager.m_Config.m_FileName =
            $"CinematicImage_{_cinematicViewCondition.index}";
        GameManager.Instance.ScreenshotManager.Capture();
    }

    public void ActivateSkipProcess()
    {
        Debug.Log("ActivateSkipProcess");
        if (!GameManager.Instance.isReviewing)
        {
            if (!GameManager.Instance.IsCinematic)
            {
                return;
            }
        }
        
        Debug.Log("ActivateSkipProcess Begin");
        DeactivateSkipProcess();

        _skipObserver?.Dispose();
        _skipObserver = Observable.EveryUpdate()
            .Where(n => GameManager.Player.InputSystem.ExitButtonPressed)
            .First()
            .Subscribe(n =>
            {
                _caputreTween.Kill();
                CaptureScreenshot();
                GameUiManager.Instance.UiPopupSkipCinematic.Show();
            });
        ;
    }

    public void DeactivateSkipProcess()
    {
        Debug.Log("DeactivateSkipProcess");
        if (GameManager.Instance.isReviewing)
        {
            return;
        }
        Debug.Log("DeactivateSkipProcess Begin");
        
        _skipObserver?.Dispose();
        _skipObserver = Observable.EveryUpdate()
            .Where(n => GameManager.Player.InputSystem.ExitButtonPressed)
            .Subscribe(n =>
            {
                CinematicToast.ShowToast(StringDataObject.GetStringData(601), 0.5f);
            });
    }

    private Tween _skipTween;
    
    [Button]
    public void SkipCinematic()
    {
        // TODO DEBUG
        LZUtils.MakeLog($"SkipCinematic : {_fileName}");

        GameManager.Player.DisablePlayer();
        FadeManager.FadeOut(0.3f);
        IsSkip = true;
        DOVirtual.DelayedCall(0.3f, () =>
        {
            if (GameManager.Player.AppearParticleInstance != null)
            {
                Destroy(GameManager.Player.AppearParticleInstance);
            }

            if (_mainCinematic._cinematicProcessRoutine != null)
            {
                StopCoroutine(_mainCinematic._cinematicProcessRoutine);
                _mainCinematic._cinematicProcessRoutine = null;
            }

            if (_mainCinematic._taskProcessRoutine != null)
            {
                StopCoroutine(_mainCinematic._taskProcessRoutine);
                _mainCinematic._taskProcessRoutine = null;
            }

            for (int i = 0; i < _mainCinematic.taskItemList.Count; i++)
            {
                _mainCinematic.taskItemList[i].Skip();
            }

            for (int i = 0; i < _subCinematicList.Count; i++)
            {
                for (int j = 0; j < _subCinematicList[i].taskItemList.Count; j++)
                {
                    _subCinematicList[i].taskItemList[j].Skip();
                }
            }
            
            EndCinematic(_mainCinematic, false, IsSkip);
        });

        _skipTween = DOVirtual.DelayedCall(1f, () =>
        {
            GameManager.Player.EnablePlayer();
            if (GameManager.Instance.isReviewing == false)
            {
                FadeManager.FadeIn(0.3f);
            }

            _skipTween = null;
        });
    }

    public void EndCinematic()
    {
        EndCinematic(_mainCinematic);
    }

    private Tween _endCinematicTween;
    private Tween _changeUiTween;
    
    [Button]
    public void EndCinematic(CinematicProcessContainer container, bool isSub = false, bool isSkip = false)
    {
        if (isSub == false)
        {
            // TODO DEBUG
            LZUtils.MakeLog($"EndCinematic : {_fileName}");

            InputManager.DisablePlayerInput();
            CameraCtrl cameraCtrl = GameManager.Instance.cameraCtrl;
            cameraCtrl.CameraFreeTargetTransform.parent = GameManager.Player.transform;
            cameraCtrl.CameraFreeTargetTransform.localPosition = Vector3.zero;
            Camera.main.transform.position = GameManager.Player.transform.position;

            if (GameManager.Instance.isReviewing == false)
            {
                cameraCtrl.ResetSoftZone(1f);
                cameraCtrl.ResetDeadZone(1f);
                cameraCtrl.ResetLookAheadTime(4f);
                cameraCtrl.ResetLookAheadSmoothing(4f);
            }
            EventEndCinematicCameraCtrl?.Invoke();

            DeactivateSkipProcess();
        }

        if (container == null)
        {
            container = _mainCinematic;
        }
        
        if (GameManager.Instance.isReviewing == false)
        {
            if (container._cinematicProcessRoutine != null)
            {
                StopCoroutine(container._cinematicProcessRoutine);
                container._cinematicProcessRoutine = null;
            }

            if (container._taskProcessRoutine != null)
            {
                StopCoroutine(container._taskProcessRoutine);
                container._taskProcessRoutine = null;
            }

            if (_subCinematicList.Exists(processContainer => processContainer == container))
            {
                _subCinematicList.Remove(container);
            }

            container.CurrentIndex = -1;
            if (isSub == false)
            {
                _changeUiTween = DOVirtual.DelayedCall(1f, () =>
                {
                    if (SceneManager.GetSceneByName("Lobby").isLoaded)
                    {
                        if (GameManager.Instance.CurrentGameData.IsFirstPlay)
                        {
                            GameUiManager.Instance.ChangeUiMode(UiModeType.Tutorial);
                        }
                        else
                        {
                            GameUiManager.Instance.ChangeUiMode(UiModeType.Lobby);
                        }
                    }
                    else
                    {
                        GameUiManager.Instance.ChangeUiMode(UiModeType.Game);
                    }
                });
                if (_cinematicViewCondition != null)
                {
                    _cinematicViewCondition.IsDone = true;
                    
                    // Achievement
                    SteamAchievementManager.Instance.CinematicAchievementProcess(_cinematicViewCondition.index);
                }
            }
        }

        if (isSub == false)
        {
            _endCinematicTween = DOVirtual.DelayedCall(1f, () =>
            {
                if (IsCinematicViewer == false)
                {
                    if (TutorialManager.Instance.IsTutorial == false)
                    {
                        GameUiManager.Instance.uiInGameOption.CanShowOption = true;
                    }
                }
                InputManager.EnablePlayerInput();
                GameManager.Instance.IsCinematic = false;
                EventEndCinematic?.Invoke(isSkip);

                _endCinematicTween = null;
            });
            _caputreTween?.Kill();
            _skipObserver?.Dispose();
            GameManager.Instance.cameraCtrl.SetFollowPlayer(true);
            if (GameManager.Instance.isReviewing == false)
            {
                if (_cinematicViewCondition != null)
                {
                    GameDataObject.GetCurrentData().LastCinematicIndex = _cinematicViewCondition.index;
                }
            }

            if (!GameManager.Instance.isReviewing)
            {
                if (ScriptableObjectManager.ConfigDataObject.IsShowHpBar)
                {
                    GameManager.Player.hpSliderCanvas.EnableSlider();
                    GameManager.Player.UiDashObject.SetActive(true);
                }
            }
        }
    }
}