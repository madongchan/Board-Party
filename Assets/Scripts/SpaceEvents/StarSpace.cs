using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

public class StarSpace : SpaceEvent
{
    private CameraHandler cameraHandler;
    private PlayableDirector starTimelineDirector;
    private SplineKnotAnimate currentSplineKnotAnimator;


    [SerializeField] private int starCost = 20;
    [SerializeField] private Transform starTransform;

    private void Start()
    {
        cameraHandler = FindAnyObjectByType<CameraHandler>();
        starTimelineDirector = FindAnyObjectByType<PlayableDirector>();

        UIManager.Instance.starConfirmButton.onClick.AddListener(OnStarBuyClick);
        UIManager.Instance.starCancelButton.onClick.AddListener(OnStarCancel);

    }

    private void OnStarCancel()
    {
        currentSplineKnotAnimator.Paused = false;
        FocusOnStar(false);
        UIManager.Instance.FadeRollText(false);
    }

    private void OnStarBuyClick()
    {
        // Player 별 구매 로직
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats.Coins < starCost)
            return;

        StartCoroutine(StarSequence(playerStats));
    }

    // 별 구매 시퀀스를 BaseStats로 일반화하여 Player와 NPC 모두 사용 가능하게 함
    private IEnumerator StarSequence(BaseStats stats)
    {
        stats.AddCoins(-starCost);
        stats.UpdateStats();

        cameraHandler.ZoomCamera(false);
        UIManager.Instance.ShowStarPurchaseUI(false);
        starTransform.DOScale(0, .1f);
        starTimelineDirector.Play();
        yield return new WaitUntil(() => starTimelineDirector.state == PlayState.Paused);

        stats.AddStars(1);
        stats.UpdateStats();

        FocusOnStar(false);
        starTransform.DOScale(1, .5f).SetEase(Ease.OutBack);
        currentSplineKnotAnimator.Paused = false;
        UIManager.Instance.FadeRollText(false);
    }

    public override void StartEvent(SplineKnotAnimate animator)
    {
        base.StartEvent(animator);
        currentSplineKnotAnimator = animator;
        
        // 현재 캐릭터가 NPC인지 Player인지 확인
        NPCController npcController = currentSplineKnotAnimator.GetComponentInParent<NPCController>();
        
        if (npcController != null)
        {
            // NPC 별 구매 로직 처리
            HandleNPCStarPurchase(npcController);
        }
        else
        {
            // Player 별 구매 UI 표시
            FocusOnStar(true);
            UIManager.Instance.FadeRollText(true);
        }
    }

    // NPC의 별 구매 로직 처리
    private void HandleNPCStarPurchase(NPCController npcController)
    {
        // NPC가 별 구매를 결정하는지 확인
        bool willPurchase = npcController.DecideStarPurchase();
        
        // NPC Stats 가져오기
        NPCStats npcStats = npcController.GetComponent<NPCStats>();
        
        if (willPurchase && npcStats != null && npcStats.Coins >= starCost)
        {
            // 별 구매 시퀀스 시작
            StartCoroutine(NPCStarPurchaseSequence(npcStats));
        }
        else
        {
            // 별 구매 취소 및 이동 계속
            currentSplineKnotAnimator.Paused = false;
        }
    }

    // NPC 별 구매 시퀀스
    private IEnumerator NPCStarPurchaseSequence(NPCStats npcStats)
    {
        // 별 구매 시각적 효과를 위해 잠시 카메라 포커스
        FocusOnStar(true);
        yield return new WaitForSeconds(1.0f); // NPC가 별을 보는 시간
        
        // 별 구매 시퀀스 실행
        yield return StartCoroutine(StarSequence(npcStats));
    }

    public void FocusOnStar(bool focus)
    {
        FindAnyObjectByType<CameraHandler>().ZoomCamera(focus);
        UIManager.Instance.ShowStarPurchaseUI(focus);
        if (focus)
            currentSplineKnotAnimator.transform.GetChild(0).DOLookAt(starTransform.position, .5f, AxisConstraint.Y);
        else
            currentSplineKnotAnimator.transform.GetChild(0).DOLocalRotate(Vector3.zero, .3f);
    }
}
