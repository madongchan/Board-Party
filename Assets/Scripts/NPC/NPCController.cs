using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

public class NPCController : BaseController
{
    // 상태 변경 이벤트 추가
    public delegate void StateChangedHandler(NPCState newState);
    public event StateChangedHandler OnStateChanged;
    
    // 현재 상태
    public NPCState currentState { get; private set; }
    private Dictionary<System.Type, NPCState> states = new Dictionary<System.Type, NPCState>();
    
    protected override void Start()
    {
        base.Start();
        
        // 상태 초기화
        states.Add(typeof(IdleState), new IdleState(this));
        states.Add(typeof(TurnStartState), new TurnStartState(this));
        states.Add(typeof(RollingState), new RollingState(this));
        states.Add(typeof(MovingState), new MovingState(this));
        states.Add(typeof(JunctionDecisionState), new JunctionDecisionState(this));
        states.Add(typeof(EventProcessingState), new EventProcessingState(this));
        states.Add(typeof(TurnEndState), new TurnEndState(this));
        states.Add(typeof(StarPurchaseDecisionState), new StarPurchaseDecisionState(this));
        
        // 초기 상태 설정
        ChangeState<IdleState>();
    }
    
    // 상태 변경 메서드
    public void ChangeState<T>() where T : NPCState
    {
        if (currentState != null)
            currentState.Exit();
            
        currentState = states[typeof(T)];
        currentState.Enter();
        
        // 상태 변경 이벤트 발생
        OnStateChanged?.Invoke(currentState);
    }
    
    // 현재 상태 업데이트
    private void Update()
    {
        if (currentState != null)
            currentState.Update();
    }
    
    // NPC 주사위 굴림 시작 (PlayerController의 OnJump 대응)
    public override void StartRoll()
    {
        PrepareToRoll();
        StartCoroutine(RollSequence());
    }
    
    // 분기점 경로 선택 (PlayerController의 OnMove 대응)
    public int DecideJunctionPath(List<SplineKnotIndex> options)
    {
        // 간단한 의사결정 로직 (랜덤 선택)
        return Random.Range(0, options.Count);
    }
    
    // 분기점에서 경로 선택 실행
    public void SelectJunctionPath()
    {
        if (splineKnotAnimator.inJunction && splineKnotAnimator.walkableKnots.Count > 0)
        {
            int pathIndex = DecideJunctionPath(splineKnotAnimator.walkableKnots);
            splineKnotAnimator.junctionIndex = pathIndex;
            
            // 분기점 선택 확정 (PlayerController의 OnJump 대응)
            ConfirmJunctionSelection();
        }
    }
    
    // 별 구매 결정
    public bool DecideStarPurchase()
    {
        NPCStats npcStats = GetComponent<NPCStats>();
        
        // 기본 로직: 코인이 충분하면 구매
        if (npcStats.Coins >= 20)  // 별 가격이 20코인이라고 가정
        {
            return true;
        }
        
        return false;
    }
    
    // 이벤트 처리 완료 후 호출
    public void EventProcessingComplete()
    {
        ChangeState<TurnEndState>();
    }
}