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
        //Get Current Player, this would need to be adjusted with multiplayer
        PlayerStats playerStats = FindAnyObjectByType<PlayerStats>();
        if (playerStats.Coins < starCost)
            return;


        StartCoroutine(StarSequence());

        IEnumerator StarSequence()
        {
            playerStats.AddCoins(-starCost);
            playerStats.UpdateStats();

            cameraHandler.ZoomCamera(false);
            UIManager.Instance.ShowStarPurchaseUI(false);
            starTransform.DOScale(0, .1f);
            starTimelineDirector.Play();
            yield return new WaitUntil(() => starTimelineDirector.state == PlayState.Paused);

            playerStats.AddStars(1);
            playerStats.UpdateStats();

            FocusOnStar(false);
            starTransform.DOScale(1, .5f).SetEase(Ease.OutBack);
            currentSplineKnotAnimator.Paused = false;
            UIManager.Instance.FadeRollText(false);
        }
    }

    public override void StartEvent(SplineKnotAnimate animator)
    {
        base.StartEvent(animator);
        currentSplineKnotAnimator = animator;
        FocusOnStar(true);
        UIManager.Instance.FadeRollText(true);
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
