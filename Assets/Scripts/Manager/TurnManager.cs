using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TurnManager 클래스 - 턴 진행 로직 담당
/// 턴 상태 관리, 턴 시작/종료, 플레이어 전환 등을 처리합니다.
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class TurnManager : MonoBehaviour
{
    // 현재 턴 플레이어 인덱스
    private int currentPlayerIndex = -1;
    
    // 턴 상태
    public enum TurnState { Idle, TurnStart, Rolling, Moving, EventProcessing, TurnEnd }
    public TurnState CurrentTurnState { get; private set; } = TurnState.Idle;
    
    // 턴 상태 변경 이벤트 (UI 업데이트용)
    [HideInInspector] public UnityEvent<TurnState> OnTurnStateChanged = new UnityEvent<TurnState>();
    
    // BoardManager 참조
    private BoardManager boardManager;
    
    /// <summary>
    /// TurnManager 초기화
    /// </summary>
    public void Initialize()
    {
        // BoardManager 참조 획득
        boardManager = BoardManager.GetInstance();
        
        // 이벤트 리스너 등록
        RegisterEventListeners();
        
        // 하위 컴포넌트 초기화 (필요시)
        // 예: 턴 표시기, 타이머 등
        
        Debug.Log("TurnManager initialized");
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 리스너 해제
        UnregisterEventListeners();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    private void RegisterEventListeners()
    {
        // 이벤트 처리 완료 이벤트 리스너 등록
        BoardEvents.OnEventCompleted.AddListener(OnEventProcessingCompleted);
        
        // 주사위 굴림 관련 이벤트 리스너 등록
        BoardEvents.OnRollStart.AddListener(OnRollStart);
        BoardEvents.OnRollEnd.AddListener(OnRollEnd);
        
        // 이동 관련 이벤트 리스너 등록
        BoardEvents.OnMovementStart.AddListener(OnMovementStart);
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void UnregisterEventListeners()
    {
        // 이벤트 처리 완료 이벤트 리스너 해제
        BoardEvents.OnEventCompleted.RemoveListener(OnEventProcessingCompleted);
        
        // 주사위 굴림 관련 이벤트 리스너 해제
        BoardEvents.OnRollStart.RemoveListener(OnRollStart);
        BoardEvents.OnRollEnd.RemoveListener(OnRollEnd);
        
        // 이동 관련 이벤트 리스너 해제
        BoardEvents.OnMovementStart.RemoveListener(OnMovementStart);
    }
    
    /// <summary>
    /// 첫 턴 시작
    /// </summary>
    public void StartFirstTurn()
    {
        currentPlayerIndex = -1;
        StartNextTurn();
    }
    
    /// <summary>
    /// 다음 턴 시작
    /// </summary>
    public void StartNextTurn()
    {
        // 다음 플레이어 인덱스 계산
        PlayerManager playerManager = boardManager.GetPlayerManager();
        currentPlayerIndex = (currentPlayerIndex + 1) % playerManager.GetPlayerCount();
        BaseController currentPlayer = playerManager.GetPlayerAt(currentPlayerIndex);
        
        // 턴 상태 변경
        ChangeTurnState(TurnState.TurnStart);
        
        // 턴 시작 이벤트 발생
        BoardEvents.OnTurnStart.Invoke(currentPlayer);
        
        // 현재 플레이어가 NPC인지 확인하여 처리
        if (currentPlayer is NPCController)
        {
            // NPC는 TurnStartState로 상태 전환
            NPCController npcController = currentPlayer as NPCController;
            npcController.ChangeState<TurnStartState>();
        }
        else if (currentPlayer is PlayerController)
        {
            // 플레이어는 TurnStartState로 상태 전환
            PlayerController playerController = currentPlayer as PlayerController;
            playerController.ChangeState<TurnStartState>();
            
            // 주사위 굴림 준비 이벤트 발생
            BoardEvents.OnRollPrepare.Invoke(currentPlayer);
        }
        
        // UI 업데이트
        UIManager uiManager = boardManager.GetUIManager();
        if (uiManager != null)
            uiManager.UpdateAllUI();
            
        Debug.Log($"Turn started for player {currentPlayerIndex}: {currentPlayer.name}");
    }
    
    /// <summary>
    /// 현재 턴 종료
    /// </summary>
    public void EndCurrentTurn()
    {
        // 턴 상태 변경
        ChangeTurnState(TurnState.TurnEnd);
        
        // 턴 종료 이벤트 발생
        BaseController currentPlayer = GetCurrentPlayer();
        BoardEvents.OnTurnEnd.Invoke(currentPlayer);
        
        Debug.Log($"Turn ended for player {currentPlayerIndex}: {currentPlayer.name}");
        
        // 다음 턴 시작
        StartNextTurn();
    }
    
    /// <summary>
    /// 턴 상태 변경
    /// </summary>
    public void ChangeTurnState(TurnState newState)
    {
        CurrentTurnState = newState;
        OnTurnStateChanged.Invoke(newState);
        
        Debug.Log($"Turn state changed to: {newState}");
    }
    
    /// <summary>
    /// 주사위 굴림 시작 이벤트 처리
    /// </summary>
    private void OnRollStart(BaseController controller)
    {
        if (controller == GetCurrentPlayer())
        {
            ChangeTurnState(TurnState.Rolling);
        }
    }
    
    /// <summary>
    /// 주사위 굴림 종료 이벤트 처리
    /// </summary>
    private void OnRollEnd(BaseController controller)
    {
        if (controller == GetCurrentPlayer())
        {
            // 이동 상태로 전환은 OnMovementStart에서 처리
        }
    }
    
    /// <summary>
    /// 이동 시작 이벤트 처리
    /// </summary>
    private void OnMovementStart(BaseController controller, bool started)
    {
        if (controller == GetCurrentPlayer() && started)
        {
            ChangeTurnState(TurnState.Moving);
        }
    }
    
    /// <summary>
    /// 이벤트 처리 완료 콜백
    /// </summary>
    private void OnEventProcessingCompleted(BaseController controller)
    {
        // 현재 플레이어의 이벤트 처리가 완료되면 턴 종료
        if (controller == GetCurrentPlayer())
        {
            EndCurrentTurn();
        }
    }
    
    /// <summary>
    /// 현재 플레이어 가져오기
    /// </summary>
    public BaseController GetCurrentPlayer()
    {
        PlayerManager playerManager = boardManager.GetPlayerManager();
        return playerManager.GetPlayerAt(currentPlayerIndex);
    }
    
    /// <summary>
    /// 현재 플레이어가 NPC인지 확인
    /// </summary>
    public bool IsCurrentPlayerNPC()
    {
        BaseController currentPlayer = GetCurrentPlayer();
        return currentPlayer != null && currentPlayer is NPCController;
    }
}
