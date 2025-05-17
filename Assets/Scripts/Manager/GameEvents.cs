// GameEvents.cs (신규)
using UnityEngine.Events;
using UnityEngine.Splines;

public static class GameEvents
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
}
