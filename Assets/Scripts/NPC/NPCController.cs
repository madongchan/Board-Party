using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPCController 클래스 - NPC 캐릭터 제어
/// AI 로직을 통해 NPC 캐릭터를 자동으로 제어합니다.
/// </summary>
public class NPCController : BaseController
{
    // AI 설정
    [SerializeField] private float decisionDelay = 1.0f;
    [SerializeField] private float moveDelay = 0.5f;
    
    // 코루틴 참조
    private Coroutine turnCoroutine;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // 초기 상태 설정
        ChangeState<IdleState>();
    }
    
    /// <summary>
    /// 상태 초기화
    /// </summary>
    protected override void InitializeStates()
    {
        // 상태 생성 및 등록
        states[typeof(IdleState)] = new IdleState(this);
        states[typeof(TurnStartState)] = new TurnStartState(this);
        states[typeof(RollingState)] = new RollingState(this);
        states[typeof(MovingState)] = new MovingState(this);
        states[typeof(EventProcessingState)] = new EventProcessingState(this);
        states[typeof(TurnEndState)] = new TurnEndState(this);
    }
    
    /// <summary>
    /// 턴 시작 이벤트 처리
    /// </summary>
    protected override void OnTurnStart(BaseController controller)
    {
        if (controller != this) return;
        
        // NPC 턴 시작
        ChangeState<TurnStartState>();
        
        // 자동 턴 진행 시작
        turnCoroutine = StartCoroutine(ProcessTurn());
    }
    
    /// <summary>
    /// 턴 종료 이벤트 처리
    /// </summary>
    protected override void OnTurnEnd(BaseController controller)
    {
        if (controller != this) return;
        
        // NPC 턴 종료
        ChangeState<IdleState>();
        
        // 진행 중인 코루틴 정지
        if (turnCoroutine != null)
        {
            StopCoroutine(turnCoroutine);
            turnCoroutine = null;
        }
    }
    
    /// <summary>
    /// 자동 턴 진행 코루틴
    /// </summary>
    private IEnumerator ProcessTurn()
    {
        // 잠시 대기
        yield return new WaitForSeconds(decisionDelay);
        
        // 주사위 굴림
        RollDice();
        
        // 주사위 결과 대기
        yield return new WaitForSeconds(decisionDelay);
        
        // 이동 시작
        ChangeState<MovingState>();
        
        // 이동 완료 대기
        while (splineKnotAnimate != null && (splineKnotAnimate.isMoving || splineKnotAnimate.inJunction))
        {
            // 분기점에서 결정
            if (splineKnotAnimate.inJunction)
            {
                yield return new WaitForSeconds(moveDelay);
                
                // 랜덤 방향 선택
                int randomDirection = Random.Range(0, splineKnotAnimate.walkableKnots.Count);
                splineKnotAnimate.junctionIndex = randomDirection;
                
                yield return new WaitForSeconds(moveDelay);
                
                // 선택 확정
                splineKnotAnimate.ConfirmJunctionSelection();
            }
            
            yield return null;
        }
        
        // 이벤트 처리 대기
        yield return new WaitForSeconds(decisionDelay);
        
        // 턴 종료
        BoardEvents.OnTurnEnd.Invoke(this);
    }
    
    /// <summary>
    /// 이벤트 시작 처리
    /// </summary>
    protected override void OnEventStarted(BaseController controller, SpaceEvent spaceEvent)
    {
        if (controller != this) return;
        
        // 별 구매 이벤트 처리
        if (spaceEvent is StarSpace)
        {
            // NPC는 코인이 충분하면 자동으로 구매
            //int starPrice = BoardManager.GetInstance().GetStarPrice();
            //bool canBuy = stats != null && stats.Coins >= starPrice;
            
            //StartCoroutine(DelayedStarPurchase(canBuy));
        }
    }
    
    /// <summary>
    /// 지연된 별 구매 결정
    /// </summary>
    private IEnumerator DelayedStarPurchase(bool purchase)
    {
        yield return new WaitForSeconds(decisionDelay);
        MakeStarPurchaseDecision(purchase);
    }
}
