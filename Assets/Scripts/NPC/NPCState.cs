using UnityEngine;

public abstract class NPCState
{
    protected NPCController npcController;

    public NPCState(NPCController controller)
    {
        npcController = controller;
    }

    public abstract void Enter();
    public abstract void Update();
    public abstract void Exit();
}

// 대기 상태: 자신의 턴을 기다리는 상태
// 대기 상태: 자신의 턴을 기다리는 상태
public class IdleState : NPCState
{
    public IdleState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 대기 상태로 진입했습니다.");
    }

    public override void Update()
    {
        
    }

    public override void Exit()
    {
        // 대기 상태 종료 시 필요한 정리
    }
}

// 턴 시작 상태: 자신의 턴이 시작된 상태
public class TurnStartState : NPCState
{
    private float stateTimer = 0f;
    private float Delay = 1f;

    public TurnStartState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}의 턴이 시작되었습니다.");
        // 주사위 굴림 시작
        npcController.StartRoll();
    }

    public override void Update()
    {

        stateTimer += Time.deltaTime;
        if (stateTimer >= Delay)
        {
            npcController.ChangeState<RollingState>();
        }
    }

    public override void Exit()
    {
        // 턴 시작 상태 종료 시 필요한 정리
    }
}

// 주사위 굴림 상태: 주사위를 굴리는 상태
public class RollingState : NPCState
{
    private bool isRollEnd = false; // 주사위 굴림 여부
    private float stateTimer = 0f;
    private float Delay = 1f;
    public RollingState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 주사위 결정합니다.");
    }

    public override void Update()
    {
        stateTimer += Time.deltaTime;
        if (stateTimer >= Delay && !isRollEnd)
        {
            // 주사위 굴림 시작
            npcController.ConfirmRoll();
            isRollEnd = true;
        }

        // 주사위 굴림이 끝났는지 확인
        if (!npcController.isRolling && isRollEnd)
        {
            // 이동 상태로 전환
            npcController.ChangeState<MovingState>();
        }
    }

    public override void Exit()
    {
        // 주사위 굴림 상태 종료 시 필요한 정리
    }
}

// 이동 상태: 주사위 결과에 따라 이동하는 상태
public class MovingState : NPCState
{
    SplineKnotAnimate splineKnotAnimator;
    public MovingState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 이동을 시작합니다.");
        splineKnotAnimator = npcController.GetComponent<SplineKnotAnimate>();
        if (splineKnotAnimator == null)
        {
            Debug.LogError($"{npcController.name}에 SplineKnotAnimate 컴포넌트가 없습니다. 이동 상태를 종료합니다.");
            npcController.ChangeState<IdleState>();
            return;
        }
    }

    public override void Update()
    {
        npcController.Move();
        // 분기점에 도달했는지 확인
        if (splineKnotAnimator.inJunction)
        {
            npcController.ChangeState<JunctionDecisionState>();
            return;
        }

        // 이동이 완료되었는지 확인
        if (!splineKnotAnimator.isMoving)
        {
            // 별 구매 공간에 도착했는지 확인
            // 이 부분은 게임 로직에 따라 구현해야 함
            bool isOnStarSpace = CheckIfOnStarSpace();
            if (isOnStarSpace)
            {
                npcController.ChangeState<StarPurchaseDecisionState>();
                return;
            }

            // 이벤트 처리 상태로 전환
            npcController.ChangeState<EventProcessingState>();
        }
    }

    public override void Exit()
    {
        // 이동 상태 종료 시 필요한 정리
    }

    // 별 구매 공간에 있는지 확인하는 메서드
    private bool CheckIfOnStarSpace()
    {
        // 게임 로직에 따라 구현
        // 예: 현재 노드의 타입이 별 구매 공간인지 확인
        return false; // 임시 반환값
    }
}

// 분기점 선택 상태: 분기점에서 경로를 선택하는 상태
public class JunctionDecisionState : NPCState
{
    private float decisionTimer = 0f;
    private float decisionDelay = 1.0f; // 경로 선택까지의 지연 시간
    private bool hasSelectedPath = false;

    public JunctionDecisionState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 분기점에 도달했습니다.");
        decisionTimer = 0f;
        hasSelectedPath = false;
    }

    public override void Update()
    {
        SplineKnotAnimate splineKnotAnimator = npcController.GetComponent<SplineKnotAnimate>();

        // 분기점 상태 확인
        if (!splineKnotAnimator.inJunction)
        {
            // 이미 분기점을 벗어났으면 이동 상태로 전환
            npcController.ChangeState<MovingState>();
            return;
        }

        decisionTimer += Time.deltaTime;

        // 일정 시간 후 경로 선택
        if (decisionTimer >= decisionDelay && !hasSelectedPath)
        {
            // 경로 선택 실행
            npcController.SelectJunctionPath();
            hasSelectedPath = true;

            // 이 시점에서는 아직 상태를 변경하지 않음
            // ConfirmJunctionSelection이 호출된 후 SplineKnotAnimate에서
            // inJunction이 false로 설정되면 다음 Update에서 상태 변경됨
        }
    }

    public override void Exit()
    {
        // 분기점 선택 상태 종료 시 필요한 정리
    }
}

// 이벤트 처리 상태: 타일 이벤트를 처리하는 상태
public class EventProcessingState : NPCState
{
    private float eventTimer = 0f;
    private float eventProcessingTime = 2.0f; // 이벤트 처리 시간

    public EventProcessingState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 이벤트를 처리합니다.");
        eventTimer = 0f;
    }

    public override void Update()
    {
        eventTimer += Time.deltaTime;

        // 일정 시간 후 이벤트 처리 완료
        if (eventTimer >= eventProcessingTime)
        {
            // 턴 종료 상태로 전환
            npcController.ChangeState<TurnEndState>();
        }
    }

    public override void Exit()
    {
        // 이벤트 처리 상태 종료 시 필요한 정리
    }
}

// 턴 종료 상태: 자신의 턴을 마치는 상태
public class TurnEndState : NPCState
{
    private float endTurnTimer = 0f;
    private float endTurnDelay = 1.0f; // 턴 종료까지의 지연 시간
    private bool hasTurnEnded = false;

    public TurnEndState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}의 턴이 종료됩니다.");
        endTurnTimer = 0f;
        hasTurnEnded = false;
    }

    public override void Update()
    {
        endTurnTimer += Time.deltaTime;

        // 일정 시간 후 턴 종료
        if (endTurnTimer >= endTurnDelay && !hasTurnEnded)
        {
            hasTurnEnded = true;
            // GameManager에 턴 종료 알림
            GameManager.Instance.EndCurrentTurn();
        }
    }

    public override void Exit()
    {
        // 턴 종료 상태 종료 시 필요한 정리
    }
}

// 별 구매 결정 상태: 별을 구매할지 결정하는 상태
public class StarPurchaseDecisionState : NPCState
{
    private float decisionTimer = 0f;
    private float decisionDelay = 2.0f; // 결정까지의 지연 시간

    public StarPurchaseDecisionState(NPCController controller) : base(controller) { }

    public override void Enter()
    {
        UIManager.Instance.ShowNPCState($"NPC State : {this.GetType().Name}");
        Debug.Log($"{npcController.name}이(가) 별 구매를 고려합니다.");
        decisionTimer = 0f;
    }

    public override void Update()
    {
        decisionTimer += Time.deltaTime;

        // 일정 시간 후 별 구매 결정
        if (decisionTimer >= decisionDelay)
        {
            // 별 구매 결정
            bool decideToPurchase = npcController.DecideStarPurchase();

            if (decideToPurchase)
            {
                Debug.Log($"{npcController.name}이(가) 별을 구매합니다.");

                // 별 구매 처리
                NPCStats npcStats = npcController.GetComponent<NPCStats>();
                if (npcStats != null)
                {
                    npcStats.AddCoins(-20); // 별 가격이 20코인이라고 가정
                    npcStats.AddStars(1);
                    npcStats.UpdateStats();
                }
            }
            else
            {
                Debug.Log($"{npcController.name}이(가) 별 구매를 거부합니다.");
            }

            // 턴 종료 상태로 전환
            npcController.ChangeState<TurnEndState>();
        }
    }

    public override void Exit()
    {
        // 별 구매 결정 상태 종료 시 필요한 정리
    }
}
