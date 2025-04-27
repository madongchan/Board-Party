using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Splines;

[RequireComponent(typeof(SplineKnotAnimate))]
public abstract class BaseController : MonoBehaviour
{
    protected BaseStats stats;
    protected SplineKnotAnimate splineKnotAnimator;
    protected SplineKnotInstantiate splineKnotData;
    [SerializeField] protected int roll = 0;

    [Header("Parameters")]
    [SerializeField] protected float jumpDelay = .5f;
    [SerializeField] protected float resultDelay = .5f;
    [SerializeField] protected float startMoveDelay = .5f;

    [Header("Events")]
    [HideInInspector] public UnityEvent OnRollStart;
    [HideInInspector] public UnityEvent OnRollJump;
    [HideInInspector] public UnityEvent<int> OnRollDisplay;
    [HideInInspector] public UnityEvent OnRollEnd;
    [HideInInspector] public UnityEvent OnRollCancel;
    [HideInInspector] public UnityEvent<bool> OnMovementStart;
    [HideInInspector] public UnityEvent<int> OnMovementUpdate;

    [Header("States")]
    public bool isRolling;
    public bool allowInput = true;

    protected virtual void Start()
    {
        stats = GetComponent<BaseStats>();
        splineKnotAnimator = GetComponent<SplineKnotAnimate>();

        splineKnotAnimator.OnDestinationKnot.AddListener(OnDestinationKnot);
        splineKnotAnimator.OnKnotEnter.AddListener(OnKnotEnter);
        splineKnotAnimator.OnKnotLand.AddListener(OnKnotLand);

        // GameManager를 통해 SplineKnotData 참조 획득
        if (GameManager.Instance != null && GameManager.Instance.SplineKnotData != null)
            splineKnotData = GameManager.Instance.SplineKnotData;

        // 매니저 클래스에 이벤트 등록 (추가)
        if (VisualEffectsManager.Instance != null)
        {
            OnRollStart.AddListener(VisualEffectsManager.Instance.OnRollStart);
            OnRollJump.AddListener(VisualEffectsManager.Instance.OnRollJump);
            OnRollDisplay.AddListener(VisualEffectsManager.Instance.OnRollDisplay);
            OnRollEnd.AddListener(VisualEffectsManager.Instance.OnRollEnd);
            OnRollCancel.AddListener(VisualEffectsManager.Instance.OnRollCancel);
            OnMovementStart.AddListener(VisualEffectsManager.Instance.OnMovementStart);
        }

        if (UIManager.Instance != null)
        {
            OnRollStart.AddListener(UIManager.Instance.OnRollStart);
            OnRollDisplay.AddListener(UIManager.Instance.OnRollDisplay);
            OnRollEnd.AddListener(UIManager.Instance.OnRollEnd);
            OnRollCancel.AddListener(UIManager.Instance.OnRollCancel);
            OnMovementStart.AddListener(UIManager.Instance.OnMovementStart);
        }
    }

    protected virtual void OnDestroy()
    {
        if (VisualEffectsManager.Instance != null)
        {
            OnRollStart.RemoveListener(VisualEffectsManager.Instance.OnRollStart);
            OnRollJump.RemoveListener(VisualEffectsManager.Instance.OnRollJump);
            OnRollDisplay.RemoveListener(VisualEffectsManager.Instance.OnRollDisplay);
            OnRollEnd.RemoveListener(VisualEffectsManager.Instance.OnRollEnd);
            OnRollCancel.RemoveListener(VisualEffectsManager.Instance.OnRollCancel);
            OnMovementStart.RemoveListener(VisualEffectsManager.Instance.OnMovementStart);
        }

        if (UIManager.Instance != null)
        {
            OnRollStart.RemoveListener(UIManager.Instance.OnRollStart);
            OnRollDisplay.RemoveListener(UIManager.Instance.OnRollDisplay);
            OnRollEnd.RemoveListener(UIManager.Instance.OnRollEnd);
            OnRollCancel.RemoveListener(UIManager.Instance.OnRollCancel);
            OnMovementStart.RemoveListener(UIManager.Instance.OnMovementStart);
        }
    }

    protected virtual void OnDestinationKnot(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;

        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        if (data.skipStepCount)
            splineKnotAnimator.SkipStepCount = true;
    }

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
            yield return new WaitForSeconds(2);

            // GameManager를 통한 턴 종료 처리
            GameManager.Instance.EndCurrentTurn();
        }
    }

    protected virtual void OnKnotEnter(SplineKnotIndex index)
    {
        if (splineKnotData == null) return;

        SplineKnotData data = splineKnotData.splineDatas[index.Spline].knots[index.Knot];
        data.EnterKnot(splineKnotAnimator);
        OnMovementUpdate.Invoke(splineKnotAnimator.Step);
    }

    public virtual void PrepareToRoll()
    {
        isRolling = true;
        OnRollStart.Invoke();
    }

    protected virtual IEnumerator RollSequence()
    {
        allowInput = false;
        OnRollJump.Invoke();

        roll = Random.Range(1, 11);

        yield return new WaitForSeconds(jumpDelay);

        OnRollDisplay.Invoke(roll);

        yield return new WaitForSeconds(resultDelay);

        isRolling = false;
        OnRollEnd.Invoke();

        yield return new WaitForSeconds(startMoveDelay);

        splineKnotAnimator.Animate(roll);

        OnMovementStart.Invoke(true);
        OnMovementUpdate.Invoke(roll);
        allowInput = true;
    }

    public virtual void AllowInput(bool allow)
    {
        allowInput = allow;
    }

    // 분기점에서 경로 선택 메서드 (공통)
    public virtual void SelectJunctionPath(int direction)
    {
        if (splineKnotAnimator != null && splineKnotAnimator.inJunction)
        {
            splineKnotAnimator.AddToJunctionIndex(direction);
        }
    }

    // 분기점 선택 확정 메서드 (공통)
    public virtual void ConfirmJunctionSelection()
    {
        if (splineKnotAnimator != null && splineKnotAnimator.inJunction)
        {
            splineKnotAnimator.inJunction = false;
        }
    }

    // 주사위 굴림 시작 메서드 (공통)
    public virtual void StartRoll()
    {
        if (!allowInput || splineKnotAnimator.isMoving || !isRolling)
            return;

        StartCoroutine(RollSequence());
    }

    // 주사위 굴림 취소 메서드 (공통)
    public virtual void CancelRoll()
    {
        if (!allowInput || !isRolling)
            return;

        isRolling = false;
        OnRollCancel.Invoke();

        // GameManager를 통한 턴 관리
        GameManager.Instance.EndCurrentTurn();
    }

    public virtual BaseStats GetStats()
    {
        return stats;
    }
}
