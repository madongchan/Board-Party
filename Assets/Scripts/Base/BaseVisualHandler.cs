using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;

/// <summary>
/// BaseVisualHandler 클래스 - 캐릭터의 시각적 효과 관리
/// 애니메이션, 이펙트 등 시각적 요소를 처리합니다.
/// </summary>
public abstract class BaseVisualHandler : MonoBehaviour
{
    // 컴포넌트 참조
    protected BaseController baseController;
    protected BaseStats baseStats;
    protected Animator animator;
    
    // 애니메이션 파라미터
    protected readonly int IdleHash = Animator.StringToHash("Idle");
    protected readonly int WalkHash = Animator.StringToHash("Walk");
    protected readonly int JumpHash = Animator.StringToHash("Jump");
    protected readonly int CelebrateHash = Animator.StringToHash("Celebrate");
    protected readonly int SadHash = Animator.StringToHash("Sad");
    
    // 이동 관련 변수
    [SerializeField] protected float jumpHeight = 1f;
    [SerializeField] protected float jumpDuration = 0.5f;
    protected Vector3 originalPosition;
    protected Sequence jumpSequence;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public virtual void Initialize()
    {
        // 컴포넌트 참조 획득
        baseController = GetComponent<BaseController>();
        baseStats = GetComponent<BaseStats>();
        animator = GetComponentInChildren<Animator>();
        
        // 원래 위치 저장
        originalPosition = transform.position;
        
        // 이벤트 등록
        RegisterEventListeners();
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 해제
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 이벤트 해제
        UnregisterEventListeners();
        
        // 실행 중인 트윈 정리
        jumpSequence?.Kill();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    protected virtual void RegisterEventListeners()
    {
        // BoardEvents에 이벤트 등록
        BoardEvents.OnRollJump.AddListener(OnRollJump);
        BoardEvents.OnMovementStart.AddListener(OnMovementStart);
        BoardEvents.OnMovementUpdate.AddListener(OnMovementUpdate);
        BoardEvents.OnKnotLand.AddListener(OnKnotLand);
        BoardEvents.OnCelebrateAnimation.AddListener(OnCelebrateAnimation);
        BoardEvents.OnSadAnimation.AddListener(OnSadAnimation);
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    protected virtual void UnregisterEventListeners()
    {
        // BoardEvents에서 이벤트 해제
        BoardEvents.OnRollJump.RemoveListener(OnRollJump);
        BoardEvents.OnMovementStart.RemoveListener(OnMovementStart);
        BoardEvents.OnMovementUpdate.RemoveListener(OnMovementUpdate);
        BoardEvents.OnKnotLand.RemoveListener(OnKnotLand);
        BoardEvents.OnCelebrateAnimation.RemoveListener(OnCelebrateAnimation);
        BoardEvents.OnSadAnimation.RemoveListener(OnSadAnimation);
    }
    
    /// <summary>
    /// 주사위 굴림 점프 애니메이션
    /// </summary>
    protected virtual void OnRollJump(BaseController controller)
    {
        if (controller != baseController) return;
        
        // 점프 애니메이션 실행
        PlayJumpAnimation();
    }
    
    /// <summary>
    /// 이동 시작 애니메이션
    /// </summary>
    protected virtual void OnMovementStart(BaseController controller, bool isMoving)
    {
        if (controller != baseController) return;
        
        // 이동 애니메이션 설정
        if (animator != null)
        {
            animator.SetBool(WalkHash, isMoving);
            animator.SetBool(IdleHash, !isMoving);
        }
    }
    
    /// <summary>
    /// 이동 업데이트 애니메이션
    /// </summary>
    protected virtual void OnMovementUpdate(BaseController controller, int remainingSteps)
    {
        if (controller != baseController) return;
        
        // 필요한 경우 이동 중 애니메이션 업데이트
    }
    
    /// <summary>
    /// 노트 착지 애니메이션
    /// </summary>
    protected virtual void OnKnotLand(BaseController controller, SplineKnotIndex knotIndex)
    {
        if (controller != baseController) return;
        
        // 착지 애니메이션 설정
        if (animator != null)
        {
            animator.SetBool(WalkHash, false);
            animator.SetBool(IdleHash, true);
        }
    }
    
    /// <summary>
    /// 축하 애니메이션
    /// </summary>
    protected virtual void OnCelebrateAnimation(BaseController controller)
    {
        if (controller != baseController) return;
        
        // 축하 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger(CelebrateHash);
        }
    }
    
    /// <summary>
    /// 슬픔 애니메이션
    /// </summary>
    protected virtual void OnSadAnimation(BaseController controller)
    {
        if (controller != baseController) return;
        
        // 슬픔 애니메이션 실행
        if (animator != null)
        {
            animator.SetTrigger(SadHash);
        }
    }
    
    /// <summary>
    /// 점프 애니메이션 실행
    /// </summary>
    protected virtual void PlayJumpAnimation()
    {
        // 기존 시퀀스 정리
        jumpSequence?.Kill();
        
        // 새 점프 시퀀스 생성
        jumpSequence = DOTween.Sequence();
        
        // 점프 애니메이션 설정
        if (animator != null)
        {
            animator.SetTrigger(JumpHash);
        }
        
        // 점프 트윈 생성
        jumpSequence.Append(transform.DOLocalMoveY(originalPosition.y + jumpHeight, jumpDuration / 2f).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DOLocalMoveY(originalPosition.y, jumpDuration / 2f).SetEase(Ease.InQuad));
        
        // 시퀀스 실행
        jumpSequence.Play();
    }
}
