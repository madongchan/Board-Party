using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

/// <summary>
/// BoardEvents 클래스 - 보드 게임의 모든 이벤트를 중앙에서 관리
/// 모든 컴포넌트 간 통신은 이 클래스의 이벤트를 통해 이루어집니다.
/// </summary>
public static class BoardEvents
{
    // 턴 관련 이벤트
    public static UnityEvent<BaseController> OnTurnStart = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController> OnTurnEnd = new UnityEvent<BaseController>();
    
    // 주사위 관련 이벤트
    public static UnityEvent<BaseController> OnRollPrepare = new UnityEvent<BaseController>(); // 주사위 굴림 준비 이벤트 추가
    public static UnityEvent<BaseController> OnRollStart = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController> OnRollJump = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController, int> OnRollDisplay = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController> OnRollEnd = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController> OnRollCancel = new UnityEvent<BaseController>();
    
    // 이동 관련 이벤트
    public static UnityEvent<BaseController, bool> OnMovementStart = new UnityEvent<BaseController, bool>();
    public static UnityEvent<BaseController, int> OnMovementUpdate = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController, SplineKnotIndex> OnKnotEnter = new UnityEvent<BaseController, SplineKnotIndex>();
    public static UnityEvent<BaseController, SplineKnotIndex> OnKnotLand = new UnityEvent<BaseController, SplineKnotIndex>();
    
    // 분기점 관련 이벤트
    public static UnityEvent<BaseController, bool> OnEnterJunction = new UnityEvent<BaseController, bool>();
    public static UnityEvent<BaseController, int> OnJunctionSelection = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController, SplineKnotIndex> OnDestinationKnot = new UnityEvent<BaseController, SplineKnotIndex>();
    
    // 스탯 관련 이벤트
    public static UnityEvent<BaseController, int> OnCoinsChanged = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController, int> OnStarsChanged = new UnityEvent<BaseController, int>();
    public static UnityEvent<BaseController> OnStatsInitialized = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController, int> OnCoinAnimation = new UnityEvent<BaseController, int>();
    
    // 애니메이션 관련 이벤트
    public static UnityEvent<BaseController> OnCelebrateAnimation = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController> OnSadAnimation = new UnityEvent<BaseController>();
    
    // 이벤트 처리 관련 이벤트
    public static UnityEvent<BaseController, SpaceEvent> OnEventStarted = new UnityEvent<BaseController, SpaceEvent>();
    public static UnityEvent<BaseController> OnEventCompleted = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController, bool> OnStarPurchaseDecision = new UnityEvent<BaseController, bool>();
    
    // 기타 이벤트
    public static UnityEvent<BaseController> OnInitialize = new UnityEvent<BaseController>();
    public static UnityEvent<BaseController, string> OnNPCStateChanged = new UnityEvent<BaseController, string>();
}
