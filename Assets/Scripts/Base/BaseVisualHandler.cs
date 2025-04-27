using UnityEngine;
using DG.Tweening;
using UnityEngine.Splines;
using System.Collections;

public abstract class BaseVisualHandler : MonoBehaviour
{
    protected Animator animator;
    protected BaseController baseController;
    protected BaseStats baseStats;
    protected SplineKnotAnimate splineKnotAnimator;
    protected SplineKnotInstantiate splineKnotData;

    [Header("References")]
    [SerializeField] protected Transform characterModel;

    [Header("Jump Parameters")]
    [SerializeField] protected int jumpPower = 1;
    [SerializeField] protected float jumpDuration = .4f;

    protected virtual void Start()
    {
        animator = GetComponentInChildren<Animator>();
        baseController = GetComponentInParent<BaseController>();
        baseStats = GetComponentInParent<BaseStats>();
        splineKnotAnimator = GetComponentInParent<SplineKnotAnimate>();

        // GameManager를 통해 SplineKnotData 참조 획득
        if (GameManager.Instance != null && GameManager.Instance.SplineKnotData != null)
            splineKnotData = GameManager.Instance.SplineKnotData;

        // 이벤트 리스너 등록
        RegisterEventListeners();
    }

    protected virtual void RegisterEventListeners()
    {
        if (baseController != null)
        {
            baseController.OnRollJump.AddListener(OnRollJump);
            baseController.OnMovementStart.AddListener(OnMovementStart);
        }

        if (splineKnotAnimator != null)
        {
            splineKnotAnimator.OnKnotLand.AddListener(OnKnotLand);
        }
    }

    protected virtual void OnRollJump()
    {
        characterModel.DOComplete();
        characterModel.DOJump(transform.position, jumpPower, 1, jumpDuration);
        animator.SetTrigger("RollJump");
        //transform.DOLocalMoveY(0.5f, 0.3f).SetLoops(1, LoopType.Yoyo);
    }

    protected virtual void OnMovementStart(bool movement)
    {
        if (movement)
        {
            transform.DOLocalRotate(Vector3.zero, .3f);
        }
        else
        {
            transform.DOLookAt(Camera.main.transform.position, .35f, AxisConstraint.Y);
        }
    }

    protected virtual void OnKnotLand(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;

        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        
        // 애니메이션만 처리 (파티클은 VisualEffectsManager에서 처리)
        animator.SetTrigger(data.coinGain > 0 ? "Happy" : "Sad");
    }

    protected virtual void Update()
    {
        if (animator != null && splineKnotAnimator != null)
        {
            float speed = splineKnotAnimator.isMoving ? 1 : 0;
            speed = splineKnotAnimator.Paused ? 0 : speed;
            float fadeSpeed = splineKnotAnimator.isMoving ? .1f : .05f;

            animator.SetFloat("Blend", speed, fadeSpeed, Time.deltaTime);
        }
    }

    // 공통 애니메이션 메서드
    public virtual void PlayJumpAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Jump");
    }

    public virtual void SetMovingAnimation(bool isMoving)
    {
        if (animator != null)
            animator.SetBool("Move", isMoving);
    }

    public virtual void PlayCelebrateAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Happy");
    }

    public virtual void PlaySadAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Sad");
    }
}
