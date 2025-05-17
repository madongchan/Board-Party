using UnityEngine.Events;
using UnityEngine.Splines;

/// <summary>
/// BoardEvents 클래스 - 게임 내 이벤트 정의 및 관리
/// 모든 게임 이벤트를 중앙화하여 관리합니다.
/// </summary>
public static class BoardEvents
{
    // 턴 관련 이벤트
    public static UnityEvent<BaseController> OnTurnStart = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController> OnTurnEnd = new UnityEvent<BaseController>();

    // 주사위 관련 이벤트
    public static UnityEvent<BaseController, int> OnDiceRolled = new UnityEvent<BaseController, int>();

    // 이동 관련 이벤트
    public static UnityEvent<BaseController, SplineKnotIndex> OnKnotReached = new UnityEvent<BaseController, SplineKnotIndex>();
    public static UnityEvent<BaseController, SplineKnotIndex> OnJunctionReached = new UnityEvent<BaseController, SplineKnotIndex>();

    // 아이템/이벤트 관련 이벤트
    public static UnityEvent<BaseController, int> OnCoinsChanged = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController, int> OnStarsChanged = new UnityEvent<BaseController, int>();

    // 이벤트 처리 관련 이벤트
    public static UnityEvent<BaseController, SpaceEvent> OnEventStarted = new UnityEvent<BaseController, SpaceEvent>();
    public static UnityEvent<BaseController> OnEventCompleted = new UnityEvent<BaseController>();

    // 별 구매 관련 이벤트
    public static UnityEvent<BaseController, bool> OnStarPurchaseDecision = new UnityEvent<BaseController, bool>();

    /// <summary>
    /// 이벤트 초기화 메서드
    /// </summary>
    public static void InitializeEvents()
    {
        // 이벤트 초기화 로직 (필요시)
    }

    /// <summary>
    /// 이벤트 정리 메서드 (씬 전환 등에 사용)
    /// </summary>
    public static void ClearAllEvents()
    {
        OnTurnStart.RemoveAllListeners();
        OnTurnEnd.RemoveAllListeners();
        OnDiceRolled.RemoveAllListeners();
        OnKnotReached.RemoveAllListeners();
        OnJunctionReached.RemoveAllListeners();
        OnCoinsChanged.RemoveAllListeners();
        OnStarsChanged.RemoveAllListeners();
        OnEventStarted.RemoveAllListeners();
        OnEventCompleted.RemoveAllListeners();
        OnStarPurchaseDecision.RemoveAllListeners();
    }
}
