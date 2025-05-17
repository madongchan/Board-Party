using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

/// <summary>
/// BaseController 클래스 - 플레이어와 NPC의 공통 기능
/// 상태 관리, 이동, 주사위 굴림 등 공통 기능을 구현합니다.
/// </summary>
[RequireComponent(typeof(SplineKnotAnimate))]
public abstract class BaseController : MonoBehaviour
{
    // 컴포넌트 참조
    protected BaseStats stats;
    protected SplineKnotAnimate splineKnotAnimator;
    protected SplineKnotInstantiate splineKnotData;
    protected int roll = 0; // 주사위 결과
    
    // 상태 관리
    public BaseState currentState { get; protected set; }
    protected Dictionary<System.Type, BaseState> states = new Dictionary<System.Type, BaseState>();
    
    // 파라미터
    [Header("Parameters")]
    [SerializeField] protected float jumpDelay = .5f;
    [SerializeField] protected float resultDelay = .5f;
    [SerializeField] protected float startMoveDelay = .5f;
    
    // 이벤트
    [Header("Events")]
    [HideInInspector] public UnityEvent OnRollStart;
    [HideInInspector] public UnityEvent OnRollJump;
    [HideInInspector] public UnityEvent<int> OnRollDisplay;
    [HideInInspector] public UnityEvent OnRollEnd;
    [HideInInspector] public UnityEvent OnRollCancel;
    [HideInInspector] public UnityEvent<bool> OnMovementStart;
    [HideInInspector] public UnityEvent<int> OnMovementUpdate;
    [HideInInspector] public UnityEvent OnTurnEnd;
    
    // 상태 플래그
    [Header("States")]
    public bool isRolling;
    public bool allowInput = true;
    
    /// <summary>
    /// 컴포넌트 초기화
    /// </summary>
    public virtual void Initialize()
    {
        // 컴포넌트 참조 획득
        stats = GetComponent<BaseStats>();
        splineKnotAnimator = GetComponent<SplineKnotAnimate>();
        
        // 이벤트 리스너 등록
        splineKnotAnimator.OnDestinationKnot.AddListener(OnDestinationKnot);
        splineKnotAnimator.OnKnotEnter.AddListener(OnKnotEnter);
        splineKnotAnimator.OnKnotLand.AddListener(OnKnotLand);
        
        // BoardManager를 통해 SplineKnotData 참조 획득
        if (BoardManager.GetInstance() != null && BoardManager.GetInstance().SplineKnotData != null)
            splineKnotData = BoardManager.GetInstance().SplineKnotData;
        
        // 이벤트 등록
        RegisterEvents();
        
        // 상태 초기화
        InitializeStates();
        ChangeState<IdleState>(); // 초기 상태 설정
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 이벤트 해제
        UnregisterEvents();
    }
    
    /// <summary>
    /// 이벤트 등록
    /// </summary>
    protected virtual void RegisterEvents()
    {
        VisualEffectsManager visualEffectsManager = BoardManager.GetInstance().GetVisualEffectsManager();
        if (visualEffectsManager != null)
        {
            OnRollStart.AddListener(visualEffectsManager.OnRollStart);
            OnRollJump.AddListener(visualEffectsManager.OnRollJump);
            OnRollDisplay.AddListener(visualEffectsManager.OnRollDisplay);
            OnRollEnd.AddListener(visualEffectsManager.OnRollEnd);
            OnRollCancel.AddListener(visualEffectsManager.OnRollCancel);
            OnMovementStart.AddListener(visualEffectsManager.OnMovementStart);
        }
        
        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        if (uiManager != null)
        {
            OnRollStart.AddListener(uiManager.OnRollStart);
            OnRollDisplay.AddListener(uiManager.OnRollDisplay);
            OnRollEnd.AddListener(uiManager.OnRollEnd);
            OnRollCancel.AddListener(uiManager.OnRollCancel);
            OnMovementStart.AddListener(uiManager.OnMovementStart);
        }
        
        // BoardEvents에 이벤트 등록
        BoardEvents.OnTurnStart.AddListener(OnTurnStartEvent);
        BoardEvents.OnTurnEnd.AddListener(OnTurnEndEvent);
    }
    
    /// <summary>
    /// 이벤트 해제
    /// </summary>
    protected virtual void UnregisterEvents()
    {
        VisualEffectsManager visualEffectsManager = BoardManager.GetInstance().GetVisualEffectsManager();
        if (visualEffectsManager != null)
        {
            OnRollStart.RemoveListener(visualEffectsManager.OnRollStart);
            OnRollJump.RemoveListener(visualEffectsManager.OnRollJump);
            OnRollDisplay.RemoveListener(visualEffectsManager.OnRollDisplay);
            OnRollEnd.RemoveListener(visualEffectsManager.OnRollEnd);
            OnRollCancel.RemoveListener(visualEffectsManager.OnRollCancel);
            OnMovementStart.RemoveListener(visualEffectsManager.OnMovementStart);
        }
        
        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        if (uiManager != null)
        {
            OnRollStart.RemoveListener(uiManager.OnRollStart);
            OnRollDisplay.RemoveListener(uiManager.OnRollDisplay);
            OnRollEnd.RemoveListener(uiManager.OnRollEnd);
            OnRollCancel.RemoveListener(uiManager.OnRollCancel);
            OnMovementStart.RemoveListener(uiManager.OnMovementStart);
        }
        
        // BoardEvents에서 이벤트 해제
        BoardEvents.OnTurnStart.RemoveListener(OnTurnStartEvent);
        BoardEvents.OnTurnEnd.RemoveListener(OnTurnEndEvent);
    }
    
    /// <summary>
    /// 상태 초기화
    /// </summary>
    protected virtual void InitializeStates()
    {
        states.Add(typeof(IdleState), new IdleState(this));
        states.Add(typeof(TurnStartState), new TurnStartState(this));
        states.Add(typeof(RollingState), new RollingState(this));
        states.Add(typeof(MovingState), new MovingState(this));
        states.Add(typeof(JunctionDecisionState), new JunctionDecisionState(this));
        states.Add(typeof(EventProcessingState), new EventProcessingState(this));
        states.Add(typeof(TurnEndState), new TurnEndState(this));
        states.Add(typeof(StarPurchaseDecisionState), new StarPurchaseDecisionState(this));
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    public virtual void ChangeState<T>() where T : BaseState
    {
        if (currentState != null)
            currentState.Exit();
        
        currentState = states[typeof(T)];
        currentState.Enter();
    }
    
    /// <summary>
    /// 현재 상태 업데이트
    /// </summary>
    protected virtual void Update()
    {
        if (currentState != null)
            currentState.Update();
    }
    
    /// <summary>
    /// 턴 시작 이벤트 처리
    /// </summary>
    protected virtual void OnTurnStartEvent(BaseController player)
    {
        if (player == this)
        {
            ChangeState<TurnStartState>();
        }
    }
    
    /// <summary>
    /// 턴 종료 이벤트 처리
    /// </summary>
    protected virtual void OnTurnEndEvent(BaseController player)
    {
        if (player == this)
        {
            // 필요한 경우 추가 로직
        }
    }
    
    /// <summary>
    /// 스플라인 노트 도착 처리
    /// </summary>
    protected virtual void OnDestinationKnot(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;
        
        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        if (data.skipStepCount)
            splineKnotAnimator.SkipStepCount = true;
    }
    
    /// <summary>
    /// 스플라인 노트 착지 처리
    /// </summary>
    protected virtual void OnKnotLand(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;
        
        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        
        StartCoroutine(DelayCoroutine());
        IEnumerator DelayCoroutine()
        {
            yield return new WaitForSeconds(.08f);
            data.Land(stats);
            OnMovementStart.Invoke(false);
            
            // 카메라 핸들러가 있는 경우 카메라 줌 효과 트리거
            if (CameraHandler.Instance != null)
            {
                CameraHandler.Instance.TriggerPostLandZoom();
                
                // 카메라 블렌딩이 완료될 때까지 대기
                yield return new WaitUntil(() => !CameraHandler.Instance.IsBlending);
                
                // 추가 대기 시간 (선택 사항)
                yield return new WaitForSeconds(2f);
            }
            else
            {
                // 카메라 핸들러가 없는 경우 기존 대기 시간 사용
                yield return new WaitForSeconds(1.5f);
            }
            
            // 이벤트 처리 상태로 전환
            ChangeState<EventProcessingState>();
        }
    }
    
    /// <summary>
    /// 스플라인 노트 진입 처리
    /// </summary>
    protected virtual void OnKnotEnter(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;
        
        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        data.EnterKnot(splineKnotAnimator);
        OnMovementUpdate.Invoke(splineKnotAnimator.Step);
    }
    
    /// <summary>
    /// 주사위 굴림 준비
    /// </summary>
    public virtual void PrepareToRoll()
    {
        isRolling = true;
        OnRollStart.Invoke();
    }
    
    /// <summary>
    /// 주사위 굴림 시퀀스
    /// </summary>
    protected virtual IEnumerator RollSequence()
    {
        allowInput = false; // 입력을 비활성화합니다.
        OnRollJump.Invoke(); // 주사위 점프 이벤트를 호출합니다.
        
        roll = Random.Range(1, 10); // 1에서 9 사이의 랜덤 숫자를 생성하여 주사위 결과로 설정합니다.
        
        yield return new WaitForSeconds(jumpDelay); // 점프 딜레이 시간만큼 대기합니다.
        
        OnRollDisplay.Invoke(roll); // 주사위 결과를 표시하는 이벤트를 호출합니다.
        
        yield return new WaitForSeconds(resultDelay); // 결과 딜레이 시간만큼 대기합니다.
        
        isRolling = false; // 주사위 굴림 상태를 비활성화합니다.
        OnRollEnd.Invoke(); // 주사위 굴림 종료 이벤트를 호출합니다.
        
        yield return new WaitForSeconds(startMoveDelay); // 이동 시작 딜레이 시간만큼 대기합니다.
        
        // 이동 상태로 전환
        ChangeState<MovingState>();
        
        splineKnotAnimator.Animate(roll); // 주사위 결과에 따라 애니메이션을 실행합니다.
        
        OnMovementStart.Invoke(true); // 이동 시작 이벤트를 호출합니다.
        OnMovementUpdate.Invoke(roll); // 이동 업데이트 이벤트를 호출하며 주사위 결과를 전달합니다.
        allowInput = true; // 입력을 다시 활성화합니다.
    }
    
    /// <summary>
    /// 입력 허용 설정
    /// </summary>
    public virtual void AllowInput(bool allow)
    {
        allowInput = allow;
    }
    
    /// <summary>
    /// 분기점에서 경로 선택
    /// </summary>
    public virtual void SelectJunctionPath(int direction)
    {
        if (splineKnotAnimator != null && splineKnotAnimator.inJunction)
        {
            splineKnotAnimator.AddToJunctionIndex(direction);
        }
    }
    
    /// <summary>
    /// 분기점 선택 확정
    /// </summary>
    public virtual void ConfirmJunctionSelection()
    {
        if (splineKnotAnimator != null && splineKnotAnimator.inJunction)
        {
            splineKnotAnimator.ConfirmJunctionSelection();
        }
    }
    
    /// <summary>
    /// 주사위 굴림 시작
    /// </summary>
    public virtual void StartRoll()
    {
        if (!allowInput || splineKnotAnimator.isMoving || !isRolling)
            return;
        
        StartCoroutine(RollSequence());
    }
    
    /// <summary>
    /// 주사위 굴림 취소
    /// </summary>
    public virtual void CancelRoll()
    {
        if (!allowInput || !isRolling)
            return;
        
        isRolling = false;
        OnRollCancel.Invoke();
        
        // 턴 종료 상태로 전환
        ChangeState<TurnEndState>();
    }
    
    /// <summary>
    /// 턴 종료
    /// </summary>
    public virtual void EndTurn()
    {
        if (!splineKnotAnimator.isMoving)
        {
            ChangeState<TurnEndState>();
        }
    }
    
    /// <summary>
    /// 스탯 가져오기
    /// </summary>
    public virtual BaseStats GetStats()
    {
        return stats;
    }
}
