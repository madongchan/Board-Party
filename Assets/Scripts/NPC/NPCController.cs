using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// NPCController 클래스 - NPC 플레이어 제어
/// AI 의사결정 로직과 NPC 동작을 구현합니다.
/// </summary>
public class NPCController : BaseController
{
    /// <summary>
    /// 초기화
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
    }

    /// <summary>
    /// NPC 주사위 굴림 시작
    /// </summary>
    public override void StartRoll()
    {
        PrepareToRoll();
    }

    /// <summary>
    /// 주사위 굴림 확정
    /// </summary>
    public void ConfirmRoll()
    {
        StartCoroutine(RollSequence());
    }

    /// <summary>
    /// 분기점 경로 선택
    /// </summary>
    public void SelectJunctionPath()
    {
        if (splineKnotAnimator.inJunction && splineKnotAnimator.walkableKnots.Count > 0)
        {
            // 분기점 선택 로직 개선
            int pathIndex = DecideJunctionPath(splineKnotAnimator.walkableKnots);

            // 선택한 경로 인덱스 설정
            splineKnotAnimator.junctionIndex = pathIndex;

            // 선택 이벤트 발생 (UI 업데이트 등을 위해)
            splineKnotAnimator.OnJunctionSelection.Invoke(pathIndex);

            // 약간의 지연 후 선택 확정 (시각적 효과를 위해)
            StartCoroutine(ConfirmJunctionAfterDelay(0.5f));
        }
    }

    /// <summary>
    /// 지연 후 분기점 선택 확정
    /// </summary>
    private IEnumerator ConfirmJunctionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ConfirmJunctionSelection();
    }

    /// <summary>
    /// 분기점 경로 선택 로직
    /// </summary>
    public int DecideJunctionPath(List<SplineKnotIndex> options)
    {
        if (options.Count == 0) return 0;

        // 기본 랜덤 선택 로직
        int randomIndex = Random.Range(0, options.Count);

        // 여기에 더 복잡한 의사결정 로직 추가 가능
        // 예: 별이 있는 방향 우선 선택, 더 짧은 경로 선택 등

        return randomIndex;
    }

    /// <summary>
    /// 별 구매 결정
    /// </summary>
    public bool DecideStarPurchase()
    {
        BaseStats npcStats = GetStats();

        // 기본 로직: 코인이 충분하면 구매
        if (npcStats.Coins >= 20)  // 별 가격이 20코인이라고 가정
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 이벤트 처리 완료
    /// </summary>
    public void EventProcessingComplete()
    {
        ChangeState<TurnEndState>();
    }
}
