using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// TurnManager 클래스 - 턴 진행 로직 담당
/// 턴 상태 관리, 턴 시작/종료, 플레이어 전환 등을 처리합니다.
/// </summary>
public class TurnManager : MonoBehaviour
{
    // 현재 턴 플레이어 인덱스
    private int currentPlayerIndex = -1;

    // 턴 상태
    public enum TurnState { Idle, TurnStart, Rolling, Moving, EventProcessing, TurnEnd }
    public TurnState CurrentTurnState { get; private set; } = TurnState.Idle;

    // 턴 상태 변경 이벤트
    public UnityEvent<TurnState> OnTurnStateChanged = new UnityEvent<TurnState>();

    /// <summary>
    /// TurnManager 초기화
    /// </summary>
    public void Initialize()
    {
        // 이벤트 리스너 등록
        RegisterEventListeners();

        // 하위 컴포넌트 초기화 (필요시)
        // 예: 턴 표시기, 타이머 등
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
        // 턴 종료 이벤트 리스너 등록
        BoardEvents.OnEventCompleted.AddListener(OnEventProcessingCompleted);
    }

    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void UnregisterEventListeners()
    {
        // 턴 종료 이벤트 리스너 해제
        BoardEvents.OnEventCompleted.RemoveListener(OnEventProcessingCompleted);
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
        PlayerManager playerManager = BoardManager.GetInstance().GetPlayerManager();
        currentPlayerIndex = (currentPlayerIndex + 1) % playerManager.GetPlayerCount();
        BaseController currentPlayer = playerManager.GetPlayerAt(currentPlayerIndex);

        // 턴 상태 변경
        ChangeTurnState(TurnState.TurnStart);

        // 턴 시작 이벤트 발생
        BoardEvents.OnTurnStart.Invoke(currentPlayer);

        // 현재 플레이어가 NPC인지 확인하여 처리
        if (currentPlayer is NPCController)
        {
            NPCController npcController = currentPlayer as NPCController;
            npcController.ChangeState<TurnStartState>();
        }
        else
        {
            PlayerController playerController = currentPlayer as PlayerController;
            playerController.PrepareToRoll();
        }

        // UI 업데이트
        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        if (uiManager != null)
            uiManager.UpdateAllUI();
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
    }

    /// <summary>
    /// 이벤트 처리 완료 콜백
    /// </summary>
    private void OnEventProcessingCompleted(BaseController player)
    {
        // 현재 플레이어의 이벤트 처리가 완료되면 턴 종료
        if (player == GetCurrentPlayer())
        {
            EndCurrentTurn();
        }
    }

    /// <summary>
    /// 현재 플레이어 가져오기
    /// </summary>
    public BaseController GetCurrentPlayer()
    {
        PlayerManager playerManager = BoardManager.GetInstance().GetPlayerManager();
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
