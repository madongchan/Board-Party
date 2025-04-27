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
    protected int roll = 0; // 주사위 결과

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
            yield return new WaitForSeconds(1.5f);

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

    // 주사위 굴림 준비 메서드 (공통)
    public virtual void PrepareToRoll()
    {
        isRolling = true;
        OnRollStart.Invoke();
    }

    // 주사위 굴림 시퀀스 코루틴 (공통)
    // 주사위 점프, 결과 표시, 이동 시작을 포함한 시퀀스
    protected virtual IEnumerator RollSequence()
    {
        allowInput = false; // 입력을 비활성화합니다.
        OnRollJump.Invoke(); // 주사위 점프 이벤트를 호출합니다.

        roll = Random.Range(8, 10); // 1에서 9 사이의 랜덤 숫자를 생성하여 주사위 결과로 설정합니다.

        yield return new WaitForSeconds(jumpDelay); // 점프 딜레이 시간만큼 대기합니다.

        OnRollDisplay.Invoke(roll); // 주사위 결과를 표시하는 이벤트를 호출합니다.

        //yield return new WaitForSeconds(resultDelay); // 결과 딜레이 시간만큼 대기합니다.

        isRolling = false; // 주사위 굴림 상태를 비활성화합니다.
        OnRollEnd.Invoke(); // 주사위 굴림 종료 이벤트를 호출합니다.

        yield return new WaitForSeconds(startMoveDelay); // 이동 시작 딜레이 시간만큼 대기합니다.

        splineKnotAnimator.Animate(roll); // 주사위 결과에 따라 애니메이션을 실행합니다.

        OnMovementStart.Invoke(true); // 이동 시작 이벤트를 호출합니다.
        OnMovementUpdate.Invoke(roll); // 이동 업데이트 이벤트를 호출하며 주사위 결과를 전달합니다.
        allowInput = true; // 입력을 다시 활성화합니다.
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
