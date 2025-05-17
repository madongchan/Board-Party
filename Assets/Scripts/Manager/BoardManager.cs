using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BoardManager 클래스 - 게임의 총괄 매니저
/// 싱글톤 패턴을 사용하여 모든 하위 매니저와 컴포넌트를 관리합니다.
/// </summary>
public class BoardManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static BoardManager instance;
    
    // 싱글톤 인스턴스 접근자
    public static BoardManager GetInstance()
    {
        return instance;
    }
    
    // 관리하는 매니저 컴포넌트들
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private VisualEffectsManager visualEffectsManager;
    [SerializeField] private CameraHandler cameraHandler;
    
    // 스플라인 노트 데이터 참조
    [SerializeField] private SplineKnotInstantiate splineKnotData;
    
    // 플레이어 변경 이벤트
    [HideInInspector] public UnityEvent<BaseController> OnPlayerChanged = new UnityEvent<BaseController>();
    
    /// <summary>
    /// Awake - 싱글톤 설정 및 초기화
    /// </summary>
    private void Awake()
    {
        // 싱글톤 설정
        if (instance == null)
        {
            instance = this;

            // 초기화 로직
            Initialize();
            
            StartGame();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 초기화
    /// </summary>
    private void Initialize()
    {
        // 컴포넌트 초기화
        InitializeComponents();
        
        // 이벤트 리스너 등록
        RegisterEventListeners();
        
        Debug.Log("BoardManager initialized");
    }
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 각 매니저 컴포넌트 초기화 (없으면 자동 생성)
        if (turnManager == null)
            turnManager = GetComponentInChildren<TurnManager>() ?? gameObject.AddComponent<TurnManager>();
            
        if (playerManager == null)
            playerManager = GetComponentInChildren<PlayerManager>() ?? gameObject.AddComponent<PlayerManager>();
            
        if (uiManager == null)
            uiManager = FindFirstObjectByType<UIManager>();
            
        if (visualEffectsManager == null)
            visualEffectsManager = FindFirstObjectByType<VisualEffectsManager>();
            
        if (cameraHandler == null)
            cameraHandler = FindFirstObjectByType<CameraHandler>();
            
        // 각 매니저 초기화
        turnManager?.Initialize();
        playerManager?.Initialize();
        uiManager?.Initialize();
        visualEffectsManager?.Initialize();
        cameraHandler?.Initialize();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    private void RegisterEventListeners()
    {
        // 필요한 이벤트 리스너 등록
        BoardEvents.OnTurnStart.AddListener(OnTurnStart);
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 리스너 해제
        BoardEvents.OnTurnStart.RemoveListener(OnTurnStart);
        
        // 싱글톤 인스턴스 정리
        if (instance == this)
        {
            instance = null;
        }
    }
    
    /// <summary>
    /// 턴 시작 이벤트 핸들러
    /// </summary>
    private void OnTurnStart(BaseController controller)
    {
        // 플레이어 변경 이벤트 발생
        OnPlayerChanged.Invoke(controller);
    }
    
    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        // 플레이어 초기화
        playerManager.Initialize();
        
        // 첫 턴 시작
        turnManager.StartFirstTurn();
        
        Debug.Log("Game started");
    }
    
    #region Getters
    
    /// <summary>
    /// TurnManager 가져오기
    /// </summary>
    public TurnManager GetTurnManager()
    {
        return turnManager;
    }
    
    /// <summary>
    /// PlayerManager 가져오기
    /// </summary>
    public PlayerManager GetPlayerManager()
    {
        return playerManager;
    }
    
    /// <summary>
    /// UIManager 가져오기
    /// </summary>
    public UIManager GetUIManager()
    {
        return uiManager;
    }
    
    /// <summary>
    /// VisualEffectsManager 가져오기
    /// </summary>
    public VisualEffectsManager GetVisualEffectsManager()
    {
        return visualEffectsManager;
    }
    
    /// <summary>
    /// CameraHandler 가져오기
    /// </summary>
    public CameraHandler GetCameraHandler()
    {
        return cameraHandler;
    }
    
    /// <summary>
    /// SplineKnotInstantiate 가져오기
    /// </summary>
    public SplineKnotInstantiate GetSplineKnotData()
    {
        return splineKnotData;
    }
    
    /// <summary>
    /// 현재 플레이어 가져오기
    /// </summary>
    public BaseController GetCurrentPlayer()
    {
        return turnManager.GetCurrentPlayer();
    }
    
    #endregion
    
    #region Convenience Methods
    
    /// <summary>
    /// 현재 플레이어가 NPC인지 확인
    /// </summary>
    public bool IsCurrentPlayerNPC()
    {
        return turnManager.IsCurrentPlayerNPC();
    }
    
    /// <summary>
    /// 현재 턴 종료
    /// </summary>
    public void EndCurrentTurn()
    {
        turnManager.EndCurrentTurn();
    }
    
    #endregion
}
