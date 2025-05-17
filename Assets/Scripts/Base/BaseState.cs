using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// BaseState 클래스 - 상태 패턴의 기본 클래스
/// 모든 상태 클래스의 부모 클래스입니다.
/// </summary>
public abstract class BaseState
{
    protected BaseController controller;
    
    public BaseState(BaseController controller)
    {
        this.controller = controller;
    }
    
    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

/// <summary>
/// IdleState 클래스 - 대기 상태
/// </summary>
public class IdleState : BaseState
{
    public IdleState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 대기 상태 진입 로직
    }
    
    public override void Update()
    {
        // 대기 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 대기 상태 종료 로직
    }
}

/// <summary>
/// TurnStartState 클래스 - 턴 시작 상태
/// </summary>
public class TurnStartState : BaseState
{
    public TurnStartState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 턴 시작 상태 진입 로직
        controller.PrepareToRoll();
        
        // NPC인 경우 자동으로 주사위 굴림 진행
        if (controller is NPCController)
        {
            // 약간의 지연 후 주사위 굴림 (시각적 효과를 위해)
            controller.StartCoroutine(DelayedRoll());
        }
    }
    
    private System.Collections.IEnumerator DelayedRoll()
    {
        yield return new WaitForSeconds(1.0f);
        controller.ChangeState<RollingState>();
    }
    
    public override void Update()
    {
        // 턴 시작 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 턴 시작 상태 종료 로직
    }
}

/// <summary>
/// RollingState 클래스 - 주사위 굴림 상태
/// </summary>
public class RollingState : BaseState
{
    public RollingState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 주사위 굴림 상태 진입 로직
        controller.StartRoll();
    }
    
    public override void Update()
    {
        // 주사위 굴림 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 주사위 굴림 상태 종료 로직
    }
}

/// <summary>
/// MovingState 클래스 - 이동 상태
/// </summary>
public class MovingState : BaseState
{
    public MovingState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 이동 상태 진입 로직
    }
    
    public override void Update()
    {
        // 이동 상태 업데이트 로직
        
        // 이동이 완료되면 이벤트 처리 상태로 전환
        if (!controller.GetComponent<SplineKnotAnimate>().isMoving)
        {
            controller.ChangeState<EventProcessingState>();
        }
    }
    
    public override void Exit()
    {
        // 이동 상태 종료 로직
    }
}

/// <summary>
/// JunctionDecisionState 클래스 - 분기점 결정 상태
/// </summary>
public class JunctionDecisionState : BaseState
{
    public JunctionDecisionState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 분기점 결정 상태 진입 로직
        
        // NPC인 경우 자동으로 경로 선택
        if (controller is NPCController)
        {
            NPCController npc = controller as NPCController;
            npc.SelectJunctionPath();
        }
    }
    
    public override void Update()
    {
        // 분기점 결정 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 분기점 결정 상태 종료 로직
    }
}

/// <summary>
/// EventProcessingState 클래스 - 이벤트 처리 상태
/// </summary>
public class EventProcessingState : BaseState
{
    public EventProcessingState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 이벤트 처리 상태 진입 로직
        
        // 현재 위치의 이벤트 처리
        SplineKnotAnimate animator = controller.GetComponent<SplineKnotAnimate>();
        SplineKnotIndex currentKnot = animator.currentKnot;
        
        // 이벤트 시작 알림
        SpaceEvent spaceEvent = BoardManager.GetInstance().SplineKnotData.GetEventAtKnot(currentKnot);
        if (spaceEvent != null)
        {
            BoardEvents.OnEventStarted.Invoke(controller, spaceEvent);
            spaceEvent.StartEvent(animator);
        }
        else
        {
            // 이벤트가 없으면 바로 턴 종료 상태로 전환
            controller.ChangeState<TurnEndState>();
        }
    }
    
    public override void Update()
    {
        // 이벤트 처리 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 이벤트 처리 상태 종료 로직
    }
}

/// <summary>
/// StarPurchaseDecisionState 클래스 - 별 구매 결정 상태
/// </summary>
public class StarPurchaseDecisionState : BaseState
{
    public StarPurchaseDecisionState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 별 구매 결정 상태 진입 로직
        
        // NPC인 경우 자동으로 별 구매 결정
        if (controller is NPCController)
        {
            NPCController npc = controller as NPCController;
            bool decision = npc.DecideStarPurchase();
            BoardEvents.OnStarPurchaseDecision.Invoke(controller, decision);
            
            // 결정 후 이벤트 처리 완료 상태로 전환
            controller.ChangeState<TurnEndState>();
        }
        // 플레이어인 경우 UI를 통해 결정
        else
        {
            // UI 표시는 StarSpace에서 처리
        }
    }
    
    public override void Update()
    {
        // 별 구매 결정 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 별 구매 결정 상태 종료 로직
    }
}

/// <summary>
/// TurnEndState 클래스 - 턴 종료 상태
/// </summary>
public class TurnEndState : BaseState
{
    public TurnEndState(BaseController controller) : base(controller) { }
    
    public override void Enter()
    {
        // 턴 종료 상태 진입 로직
        
        // 이벤트 처리 완료 알림
        BoardEvents.OnEventCompleted.Invoke(controller);
        
        // 턴 종료 후 대기 상태로 전환
        controller.ChangeState<IdleState>();
    }
    
    public override void Update()
    {
        // 턴 종료 상태 업데이트 로직
    }
    
    public override void Exit()
    {
        // 턴 종료 상태 종료 로직
    }
}
