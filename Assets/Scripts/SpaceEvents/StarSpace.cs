using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// StarSpace 클래스 - 별 획득 이벤트 처리
/// 별 구매 UI 표시, 별 구매 결정 처리 등을 담당합니다.
/// </summary>
public class StarSpace : SpaceEvent
{
    // 컴포넌트 참조
    private CameraHandler cameraHandler;
    private PlayableDirector starTimelineDirector;
    private SplineKnotAnimate currentSplineKnotAnimator;
    private BaseController currentController;

    // 별 관련 설정
    [SerializeField] private int starCost = 20;
    [SerializeField] private Transform starTransform;

    /// <summary>
    /// 초기화
    /// </summary>
    private void Start()
    {
        cameraHandler = FindAnyObjectByType<CameraHandler>();
        starTimelineDirector = FindAnyObjectByType<PlayableDirector>();

        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        uiManager.starConfirmButton.onClick.AddListener(OnStarBuyClick);
        uiManager.starCancelButton.onClick.AddListener(OnStarCancel);

        // 별 구매 결정 이벤트 리스너 등록
        BoardEvents.OnStarPurchaseDecision.AddListener(OnStarPurchaseDecision);
    }

    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        // 별 구매 결정 이벤트 리스너 해제
        BoardEvents.OnStarPurchaseDecision.RemoveListener(OnStarPurchaseDecision);
    }

    /// <summary>
    /// 별 구매 취소 처리
    /// </summary>
    private void OnStarCancel()
    {
        currentSplineKnotAnimator.Paused = false;
        FocusOnStar(false);

        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        uiManager.FadeRollText(false);

        // 이벤트 처리 완료 알림
        if (currentController != null)
        {
            currentController.ChangeState<TurnEndState>();
        }
    }

    /// <summary>
    /// 별 구매 버튼 클릭 처리
    /// </summary>
    private void OnStarBuyClick()
    {
        if (currentController == null) return;

        // BaseStats playerStats = currentController.GetStats();
        // if (playerStats.Coins < starCost)
        //     return;

        // StartCoroutine(StarSequence(playerStats));
    }

    /// <summary>
    /// 별 구매 결정 이벤트 처리
    /// </summary>
    private void OnStarPurchaseDecision(BaseController controller, bool decision)
    {
        if (controller != currentController) return;

        // NPC가 별을 구매하기로 결정한 경우
        // if (decision)
        // {
        //     BaseStats npcStats = controller.GetStats();
        //     StartCoroutine(StarSequence(npcStats));
        // }
        // else
        // {
        //     // 구매하지 않기로 결정한 경우
        //     OnStarCancel();
        // }
    }

    /// <summary>
    /// 별 구매 시퀀스
    /// </summary>
    private IEnumerator StarSequence(BaseStats stats)
    {
        stats.AddCoins(-starCost);
        stats.UpdateStats();

        cameraHandler.ZoomCamera(false);

        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        uiManager.ShowStarPurchaseUI(false);

        starTransform.DOScale(0, .1f);
        starTimelineDirector.Play();
        yield return new WaitUntil(() => starTimelineDirector.state == PlayState.Paused);

        stats.AddStars(1);
        stats.UpdateStats();

        FocusOnStar(false);
        starTransform.DOScale(1, .5f).SetEase(Ease.OutBack);
        currentSplineKnotAnimator.Paused = false;
        uiManager.FadeRollText(false);

        // 이벤트 처리 완료 알림
        if (currentController != null)
        {
            currentController.ChangeState<TurnEndState>();
        }
    }

    /// <summary>
    /// 이벤트 시작
    /// </summary>
    public override void StartEvent(SplineKnotAnimate animator)
    {
        base.StartEvent(animator);
        currentSplineKnotAnimator = animator;
        currentController = animator.GetComponent<BaseController>();

        FocusOnStar(true);

        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        uiManager.FadeRollText(true);

        // NPC인 경우 자동으로 별 구매 결정 상태로 전환
        if (currentController is NPCController)
        {
            //currentController.ChangeState<StarPurchaseDecisionState>();
        }
        else
        {
            // 플레이어는 UI를 통해 결정
            // 현재 상태 유지
        }
    }

    /// <summary>
    /// 별에 포커스
    /// </summary>
    public void FocusOnStar(bool focus)
    {
        FindAnyObjectByType<CameraHandler>().ZoomCamera(focus);

        UIManager uiManager = BoardManager.GetInstance().GetUIManager();
        uiManager.ShowStarPurchaseUI(focus);

        if (focus)
            currentSplineKnotAnimator.transform.GetChild(0).DOLookAt(starTransform.position, .5f, AxisConstraint.Y);
        else
            currentSplineKnotAnimator.transform.GetChild(0).DOLocalRotate(Vector3.zero, .3f);
    }
}
