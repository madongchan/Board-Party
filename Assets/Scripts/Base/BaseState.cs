using UnityEngine;

/// <summary>
/// BaseState 클래스 - 모든 상태의 기본 클래스
/// 상태 패턴을 구현하기 위한 추상 클래스입니다.
/// </summary>
public abstract class BaseState
{
    // 컨트롤러 참조
    protected BaseController controller;

    /// <summary>
    /// 생성자
    /// </summary>
    /// <param name="controller">이 상태를 소유하는 컨트롤러</param>
    public BaseState(BaseController controller)
    {
        this.controller = controller;
    }

    /// <summary>
    /// 상태 진입 시 호출
    /// </summary>
    public abstract void Enter();

    /// <summary>
    /// 상태 업데이트 시 호출
    /// </summary>
    public abstract void Update();

    /// <summary>
    /// 상태 종료 시 호출
    /// </summary>
    public abstract void Exit();
}

/// <summary>
/// IdleState 클래스 - 대기 상태
/// 캐릭터가 아무 행동도 하지 않는 상태입니다.
/// </summary>
public class IdleState : BaseState
{
    public IdleState(BaseController controller) : base(controller) { }

    public override void Enter()
    {
        // 대기 상태 진입 시 처리
    }

    public override void Update()
    {
        // 대기 상태 업데이트 시 처리
    }

    public override void Exit()
    {
        // 대기 상태 종료 시 처리
    }
}

/// <summary>
/// TurnStartState 클래스 - 턴 시작 상태
/// 캐릭터의 턴이 시작될 때의 상태입니다.
/// </summary>
public class TurnStartState : BaseState
{
    public TurnStartState(BaseController controller) : base(controller) { }

    public override void Enter()
    {
        // 턴 시작 상태 진입 시 처리
        controller.ChangeState<RollingState>();
    }

    public override void Update()
    {
        // 턴 시작 상태 업데이트 시 처리
    }

    public override void Exit()
    {
        // 턴 시작 상태 종료 시 처리
    }
}

/// <summary>
/// RollingState 클래스 - 주사위 굴림 상태
/// 캐릭터가 주사위를 굴리는 상태입니다.
/// </summary>
public class RollingState : BaseState
{
    private float rollTime = 0f;
    private float maxRollTime = 2f;
    private int rollResult;

    public RollingState(BaseController controller) : base(controller) { }

    public override void Enter()
    {

    }

    public override void Update()
    {

    }

    public override void Exit()
    {
        // 주사위 굴림 상태 종료 시 처리
        BoardEvents.OnRollEnd.Invoke(controller);

        // 이동 상태로 전환
        controller.ChangeState<MovingState>();
    }
}

/// <summary>
/// MovingState 클래스 - 이동 상태
/// 캐릭터가 보드 위를 이동하는 상태입니다.
/// </summary>
public class MovingState : BaseState
{
    private SplineKnotAnimate splineKnotAnimate;

    public MovingState(BaseController controller) : base(controller) { }

    public override void Enter()
    {
        // 이동 시작 이벤트 발생
        BoardEvents.OnMovementStart.Invoke(controller, true);

        // 이동 시작
        SplineKnotAnimate splineKnotAnimate = controller.GetComponent<SplineKnotAnimate>();
        if (splineKnotAnimate != null)
        {
            splineKnotAnimate.Animate(3);
        }
    }

    public override void Update()
    {
        // 이동 상태 업데이트 시 처리

        // 이동이 완료되면 이벤트 처리 상태로 전환
        if (splineKnotAnimate != null && !splineKnotAnimate.isMoving && !splineKnotAnimate.inJunction)
        {
            controller.ChangeState<EventProcessingState>();
        }
    }

    public override void Exit()
    {
        // 이동 상태 종료 시 처리

        // 이동 종료 이벤트 발생
        BoardEvents.OnMovementStart.Invoke(controller, false);
    }
}

/// <summary>
/// EventProcessingState 클래스 - 이벤트 처리 상태
/// 캐릭터가 도착한 타일의 이벤트를 처리하는 상태입니다.
/// </summary>
public class EventProcessingState : BaseState
{
    private SplineKnotAnimate splineKnotAnimate;
    private SplineKnotInstantiate splineKnotInstantiate;
    private float eventTime = 0f;
    private float maxEventTime = 1f;
    private bool eventProcessed = false;

    public EventProcessingState(BaseController controller) : base(controller) { }

    public override void Enter()
    {
        // 이벤트 처리 상태 진입 시 처리
        eventTime = 0f;
        eventProcessed = false;

        splineKnotAnimate = controller.GetComponent<SplineKnotAnimate>();

        // SplineKnotInstantiate 컴포넌트 찾기
        splineKnotInstantiate = GameObject.FindFirstObjectByType<SplineKnotInstantiate>();

        if (splineKnotAnimate != null && splineKnotInstantiate != null)
        {
            // 현재 노트의 이벤트 가져오기
            SpaceEvent spaceEvent = splineKnotInstantiate.GetEventAtKnot(splineKnotAnimate.currentKnot);

            if (spaceEvent != null)
            {
                // 이벤트 시작 이벤트 발생
                BoardEvents.OnEventStarted.Invoke(controller, spaceEvent);
            }
            else
            {
                // 이벤트가 없으면 바로 턴 종료 상태로 전환
                eventProcessed = true;
            }
        }
        else
        {
            // 필요한 컴포넌트가 없으면 바로 턴 종료 상태로 전환
            eventProcessed = true;
        }
    }

    public override void Update()
    {
        // 이벤트 처리 상태 업데이트 시 처리
        eventTime += Time.deltaTime;

        // 이벤트 처리 완료 또는 시간 초과 시 턴 종료 상태로 전환
        if (eventProcessed || eventTime > maxEventTime)
        {
            controller.ChangeState<TurnEndState>();
        }
    }

    public override void Exit()
    {
        // 이벤트 처리 상태 종료 시 처리

        // 이벤트 완료 이벤트 발생
        BoardEvents.OnEventCompleted.Invoke(controller);
    }
}

/// <summary>
/// TurnEndState 클래스 - 턴 종료 상태
/// 캐릭터의 턴이 종료될 때의 상태입니다.
/// </summary>
public class TurnEndState : BaseState
{
    private float endTime = 0f;
    private float maxEndTime = 1f;

    public TurnEndState(BaseController controller) : base(controller) { }

    public override void Enter()
    {
        // 턴 종료 상태 진입 시 처리
        endTime = 0f;

        // 턴 종료 이벤트 발생
        BoardEvents.OnTurnEnd.Invoke(controller);
    }

    public override void Update()
    {
        // 턴 종료 상태 업데이트 시 처리
        endTime += Time.deltaTime;

        // 일정 시간 후 대기 상태로 전환
        if (endTime > maxEndTime)
        {
            controller.ChangeState<IdleState>();
        }
    }

    public override void Exit()
    {
        // 턴 종료 상태 종료 시 처리
    }
}
