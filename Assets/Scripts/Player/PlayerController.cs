using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController 클래스 - 플레이어 캐릭터 제어
/// 사용자 입력을 처리하고 플레이어 캐릭터를 제어합니다.
/// </summary>
public class PlayerController : BaseController
{
    // 입력 시스템
    [SerializeField] private InputAction rollAction;
    [SerializeField] private InputAction confirmAction;
    [SerializeField] private InputAction cancelAction;
    [SerializeField] private InputAction navigationAction;
    
    // 입력 상태
    private bool canRoll = false;
    private bool canConfirm = false;
    private bool canNavigate = false;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        
        // 입력 액션 활성화
        EnableInputActions();
        
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
    /// 컴포넌트 활성화 시 입력 활성화
    /// </summary>
    private void OnEnable()
    {
        EnableInputActions();
    }
    
    /// <summary>
    /// 컴포넌트 비활성화 시 입력 비활성화
    /// </summary>
    private void OnDisable()
    {
        DisableInputActions();
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 정리
    /// </summary>
    protected override void OnDestroy()
    {
        base.OnDestroy();
        DisableInputActions();
    }
    
    /// <summary>
    /// 입력 액션 활성화
    /// </summary>
    private void EnableInputActions()
    {
        if (rollAction != null)
        {
            rollAction.Enable();
            rollAction.performed += OnRollActionPerformed;
        }
        
        if (confirmAction != null)
        {
            confirmAction.Enable();
            confirmAction.performed += OnConfirmActionPerformed;
        }
        
        if (cancelAction != null)
        {
            cancelAction.Enable();
            cancelAction.performed += OnCancelActionPerformed;
        }
        
        if (navigationAction != null)
        {
            navigationAction.Enable();
            navigationAction.performed += OnNavigationActionPerformed;
        }
    }
    
    /// <summary>
    /// 입력 액션 비활성화
    /// </summary>
    private void DisableInputActions()
    {
        if (rollAction != null)
        {
            rollAction.Disable();
            rollAction.performed -= OnRollActionPerformed;
        }
        
        if (confirmAction != null)
        {
            confirmAction.Disable();
            confirmAction.performed -= OnConfirmActionPerformed;
        }
        
        if (cancelAction != null)
        {
            cancelAction.Disable();
            cancelAction.performed -= OnCancelActionPerformed;
        }
        
        if (navigationAction != null)
        {
            navigationAction.Disable();
            navigationAction.performed -= OnNavigationActionPerformed;
        }
    }
    
    /// <summary>
    /// 주사위 굴림 액션 처리
    /// </summary>
    private void OnRollActionPerformed(InputAction.CallbackContext context)
    {
        if (canRoll && currentState is TurnStartState)
        {
            RollDice();
        }
    }
    
    /// <summary>
    /// 확인 액션 처리
    /// </summary>
    private void OnConfirmActionPerformed(InputAction.CallbackContext context)
    {
        if (!canConfirm) return;
        
        // 분기점에서 확인
        if (splineKnotAnimate != null && splineKnotAnimate.inJunction)
        {
            splineKnotAnimate.ConfirmJunctionSelection();
        }
        
        // 별 구매 확인
        if (currentState is EventProcessingState)
        {
            MakeStarPurchaseDecision(true);
        }
    }
    
    /// <summary>
    /// 취소 액션 처리
    /// </summary>
    private void OnCancelActionPerformed(InputAction.CallbackContext context)
    {
        // 별 구매 취소
        if (currentState is EventProcessingState)
        {
            MakeStarPurchaseDecision(false);
        }
    }
    
    /// <summary>
    /// 네비게이션 액션 처리
    /// </summary>
    private void OnNavigationActionPerformed(InputAction.CallbackContext context)
    {
        if (!canNavigate) return;
        
        // 분기점에서 방향 선택
        if (splineKnotAnimate != null && splineKnotAnimate.inJunction)
        {
            Vector2 direction = context.ReadValue<Vector2>();
            
            if (direction.x > 0.5f)
                splineKnotAnimate.AddToJunctionIndex(1);
            else if (direction.x < -0.5f)
                splineKnotAnimate.AddToJunctionIndex(-1);
        }
    }
    
    /// <summary>
    /// 턴 시작 이벤트 처리
    /// </summary>
    protected override void OnTurnStart(BaseController controller)
    {
        if (controller != this) return;
        
        // 플레이어 턴 시작
        ChangeState<TurnStartState>();
        
        // 입력 허용
        canRoll = true;
        canConfirm = true;
        canNavigate = true;
    }
    
    /// <summary>
    /// 턴 종료 이벤트 처리
    /// </summary>
    protected override void OnTurnEnd(BaseController controller)
    {
        if (controller != this) return;
        
        // 플레이어 턴 종료
        ChangeState<IdleState>();
        
        // 입력 제한
        canRoll = false;
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
            // UI 표시 등의 처리
            // 플레이어는 직접 입력으로 결정
        }
    }
}
