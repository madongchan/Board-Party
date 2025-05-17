using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// BoardManager 클래스 - 게임의 총괄 매니저
/// 유일한 싱글톤으로 모든 하위 매니저와 컴포넌트를 초기화하고 관리합니다.
/// </summary>
public class BoardManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    private static BoardManager Instance;

    // 직접 관리하는 하위 매니저들
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private VisualEffectsManager visualEffectsManager;

    // 스플라인 노트 데이터 참조
    [SerializeField] private SplineKnotInstantiate splineKnotData;
    public SplineKnotInstantiate SplineKnotData => splineKnotData;

    // 이벤트
    [HideInInspector] public UnityEvent<BaseController> OnPlayerChanged;

    // 게임 상태
    private bool isGameEnded = false;
    public bool IsGameEnded => isGameEnded;

    /// <summary>
    /// 싱글톤 인스턴스 초기화
    /// </summary>
    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 게임 시작 시 초기화 및 게임 시작
    /// </summary>
    private void Start()
    {
        // 컴포넌트 초기화 (없으면 자동 생성)
        InitializeComponents();

        // 게임 시작
        StartGame();
    }

    /// <summary>
    /// 하위 매니저 컴포넌트 초기화
    /// </summary>
    private void InitializeComponents()
    {
        // 각 매니저 컴포넌트 초기화
        if (turnManager == null)
            turnManager = gameObject.AddComponent<TurnManager>();

        if (playerManager == null)
            playerManager = gameObject.AddComponent<PlayerManager>();

        // 다른 필요한 컴포넌트 초기화
        if (uiManager == null && UIManager.Instance != null)
            uiManager = UIManager.Instance;

        if (visualEffectsManager == null && VisualEffectsManager.Instance != null)
            visualEffectsManager = VisualEffectsManager.Instance;

        // 각 매니저 초기화
        playerManager.Initialize();
        turnManager.Initialize();

        // 기타 매니저 초기화 (필요시)
    }

    /// <summary>
    /// 게임 시작
    /// </summary>
    public void StartGame()
    {
        // 첫 턴 시작
        turnManager.StartFirstTurn();
    }

    /// <summary>
    /// 싱글톤 인스턴스 접근자
    /// </summary>
    public static BoardManager GetInstance()
    {
        return Instance;
    }

    /// <summary>
    /// 현재 플레이어 가져오기
    /// </summary>
    public BaseController GetCurrentPlayer()
    {
        return turnManager.GetCurrentPlayer();
    }

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

    /// <summary>
    /// 게임 종료 체크
    /// </summary>
    public void CheckGameEnd()
    {
        // 게임 종료 조건 체크 로직
        // 예: 모든 플레이어가 목표 지점에 도달했는지, 특정 조건이 만족되었는지 등

        // if (/* 게임 종료 조건 */)
        // {
        //     isGameEnded = true;
        //     // 게임 종료 처리
        // }
    }

    /// <summary>
    /// PlayerManager 접근자
    /// </summary>
    public PlayerManager GetPlayerManager()
    {
        return playerManager;
    }

    /// <summary>
    /// TurnManager 접근자
    /// </summary>
    public TurnManager GetTurnManager()
    {
        return turnManager;
    }

    /// <summary>
    /// UIManager 접근자
    /// </summary>
    public UIManager GetUIManager()
    {
        return uiManager;
    }

    /// <summary>
    /// VisualEffectsManager 접근자
    /// </summary>
    public VisualEffectsManager GetVisualEffectsManager()
    {
        return visualEffectsManager;
    }
}
