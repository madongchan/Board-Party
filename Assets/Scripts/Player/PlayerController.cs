using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// PlayerController 클래스 - 사용자 플레이어 제어
/// 사용자 입력을 처리하고 플레이어 동작을 구현합니다.
/// </summary>
public class PlayerController : BaseController
{
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        PrepareToRoll();
    }
    
    // New Input System 입력 처리 메서드
    
    /// <summary>
    /// 점프 버튼 (주사위 굴림 또는 분기점 선택 확정)
    /// </summary>
    void OnJump()
    {
        if (!allowInput)
            return;

        if (splineKnotAnimator.isMoving)
            return;

        if (splineKnotAnimator.inJunction)
        {
            // 분기점 선택 확정
            ConfirmJunctionSelection();
            // 분기점 결정 상태로 전환
            ChangeState<JunctionDecisionState>();
        }
        else
        {
            if (isRolling)
            {
                // 주사위 굴림 시작
                StartRoll();
                // 주사위 굴림 상태로 전환
                ChangeState<RollingState>();
            }
        }
    }

    /// <summary>
    /// 이동 입력 (분기점에서 경로 선택)
    /// </summary>
    void OnMove(InputValue value)
    {
        if (!allowInput)
            return;

        // 좌/우 입력에 따라 분기점 인덱스 조정
        // -1: 왼쪽, 1: 오른쪽
        if (value.Get<Vector2>().x != 0)
            SelectJunctionPath(-(int)value.Get<Vector2>().x);
    }

    /// <summary>
    /// 취소 버튼 (주사위 굴림 취소)
    /// </summary>
    void OnCancel()
    {
        if (!allowInput)
            return;

        if (!isRolling)
            return;

        // 주사위 굴림 취소
        CancelRoll();
    }
}
