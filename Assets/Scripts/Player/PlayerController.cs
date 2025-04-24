using UnityEngine;
using UnityEngine.InputSystem;


public class PlayerController : BaseController
{
    protected override void Start()
    {
        base.Start();
        PrepareToRoll();
    }
    
    // New Input System 입력 처리 메서드
    
    // 점프 버튼 (주사위 굴림 또는 분기점 선택 확정)
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
        }
        else
        {
            if (isRolling)
            {
                // 주사위 굴림 시작
                StartRoll();
            }
        }
    }

    // 이동 입력 (분기점에서 경로 선택)
    void OnMove(InputValue value)
    {
        if (!allowInput)
            return;

        // 좌/우 입력에 따라 분기점 인덱스 조정
        // -1: 왼쪽, 1: 오른쪽
        if (value.Get<Vector2>().x != 0)
            SelectJunctionPath(-(int)value.Get<Vector2>().x);
    }

    // 취소 버튼 (주사위 굴림 취소)
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