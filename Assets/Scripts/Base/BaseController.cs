using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BaseController 클래스 - 모든 캐릭터 컨트롤러의 기본 클래스
/// 상태 관리, 이벤트 처리, 이동 등 공통 기능을 구현합니다.
/// </summary>
public abstract class BaseController : MonoBehaviour
{
    // 상태 관리
    protected BaseState currentState;
    protected Dictionary<System.Type, BaseState> states = new Dictionary<System.Type, BaseState>();
    
    // 컴포넌트 참조
    protected BaseStats stats;
    protected BaseVisualHandler visualHandler;
    protected SplineKnotAnimate splineKnotAnimate;
    
    // 고유 ID (데이터 영속성용)
    [SerializeField] protected string uniqueId;
    public string GetUniqueId() => uniqueId;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public virtual void Initialize()
    {
        // 고유 ID가 없으면 생성
        if (string.IsNullOrEmpty(uniqueId))
        {
            uniqueId = System.Guid.NewGuid().ToString();
        }
        
        // 컴포넌트 참조 획득
        stats = GetComponent<BaseStats>();
        visualHandler = GetComponent<BaseVisualHandler>();
        splineKnotAnimate = GetComponent<SplineKnotAnimate>();
        
        // 컴포넌트 초기화
        stats?.Initialize();
        visualHandler?.Initialize();
        
        // 상태 초기화
        InitializeStates();
        
        // 이벤트 등록
        RegisterEventListeners();
    }
    
    /// <summary>
    /// 상태 초기화
    /// </summary>
    protected virtual void InitializeStates()
    {
        // 상태 생성 및 등록 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 해제
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 이벤트 해제
        UnregisterEventListeners();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    protected virtual void RegisterEventListeners()
    {
        // BoardEvents에 이벤트 등록
        BoardEvents.OnTurnStart.AddListener(OnTurnStart);
        BoardEvents.OnTurnEnd.AddListener(OnTurnEnd);
        BoardEvents.OnEventStarted.AddListener(OnEventStarted);
        BoardEvents.OnEventCompleted.AddListener(OnEventCompleted);
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    protected virtual void UnregisterEventListeners()
    {
        // BoardEvents에서 이벤트 해제
        BoardEvents.OnTurnStart.RemoveListener(OnTurnStart);
        BoardEvents.OnTurnEnd.RemoveListener(OnTurnEnd);
        BoardEvents.OnEventStarted.RemoveListener(OnEventStarted);
        BoardEvents.OnEventCompleted.RemoveListener(OnEventCompleted);
    }
    
    /// <summary>
    /// 턴 시작 이벤트 처리
    /// </summary>
    protected virtual void OnTurnStart(BaseController controller)
    {
        if (controller != this) return;
        
        // 턴 시작 처리 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 턴 종료 이벤트 처리
    /// </summary>
    protected virtual void OnTurnEnd(BaseController controller)
    {
        if (controller != this) return;
        
        // 턴 종료 처리 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 이벤트 시작 처리
    /// </summary>
    protected virtual void OnEventStarted(BaseController controller, SpaceEvent spaceEvent)
    {
        if (controller != this) return;
        
        // 이벤트 시작 처리 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 이벤트 완료 처리
    /// </summary>
    protected virtual void OnEventCompleted(BaseController controller)
    {
        if (controller != this) return;
        
        // 이벤트 완료 처리 (하위 클래스에서 구현)
    }
    
    /// <summary>
    /// 상태 변경
    /// </summary>
    public virtual void ChangeState<T>() where T : BaseState
    {
        // 현재 상태 종료
        currentState?.Exit();
        
        // 새 상태로 변경
        if (states.TryGetValue(typeof(T), out BaseState newState))
        {
            currentState = newState;
            currentState.Enter();
        }
        else
        {
            Debug.LogError($"State {typeof(T).Name} not found!");
        }
    }
    
    /// <summary>
    /// 주사위 굴리기
    /// </summary>
    public virtual void RollDice()
    {
        // BoardEvents를 통해 주사위 굴림 시작 이벤트 발생
        BoardEvents.OnRollStart.Invoke(this);
    }
    
    /// <summary>
    /// 이동 시작
    /// </summary>
    public virtual void StartMovement(int steps)
    {
        // 이동 가능한지 확인
        if (splineKnotAnimate == null || splineKnotAnimate.isMoving)
            return;
            
        // BoardEvents를 통해 이동 시작 이벤트 발생
        BoardEvents.OnMovementStart.Invoke(this, true);
        
        // 이동 시작
        splineKnotAnimate.Animate(steps);
    }
    
    /// <summary>
    /// 별 구매 결정
    /// </summary>
    public virtual void MakeStarPurchaseDecision(bool purchase)
    {
        // BoardEvents를 통해 별 구매 결정 이벤트 발생
        BoardEvents.OnStarPurchaseDecision.Invoke(this, purchase);
    }
    
    /// <summary>
    /// 업데이트 (상태 처리)
    /// </summary>
    protected virtual void Update()
    {
        // 현재 상태 업데이트
        currentState?.Update();
    }

    public int GetStarCount()
    {
        return stats != null ? stats.Stars : 0;
    }

    public int GetCoinCount()
    {
        return stats != null ? stats.Coins : 0;
    }
}
