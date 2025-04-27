using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraHandler : MonoBehaviour
{
    [Header("References")]
    private BaseController currentController;
    private SplineKnotAnimate currentSplineKnotAnimator;
    [SerializeField] private CinemachineCamera defaultCamera;
    [SerializeField] private CinemachineCamera zoomCamera;
    [SerializeField] private CinemachineCamera junctionCamera;
    [SerializeField] private CinemachineCamera boardCamera;
    // [SerializeField] private Volume depthOfFieldVolume;

    [Header("States")]
    private bool isZoomed = false;

    private void Start()
    {
        // GameManager의 플레이어 변경 이벤트 구독
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.AddListener(SetCurrentPlayer);
            SetCurrentPlayer(GameManager.Instance.GetCurrentPlayer());
        }
    }

    // 현재 플레이어 설정 메서드
    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;

        // 이전 이벤트 리스너 해제
        UnregisterEvents();

        // 새 컨트롤러 참조 설정
        currentController = controller;
        currentSplineKnotAnimator = controller.GetComponent<SplineKnotAnimate>();

        // 시네머신 카메라 타겟 설정
        defaultCamera.Follow = controller.transform;
        defaultCamera.LookAt = controller.transform;
        zoomCamera.Follow = controller.transform;
        zoomCamera.LookAt = controller.transform;

        // 이벤트 리스너 등록
        currentController.OnRollStart.AddListener(OnRollStart);
        currentController.OnRollCancel.AddListener(OnRollCancel);
        currentController.OnMovementStart.AddListener(OnMovementStart);

        if (currentSplineKnotAnimator != null)
        {
            currentSplineKnotAnimator.OnEnterJunction.AddListener(OnEnterJunction);
        }
    }

    private void UnregisterEvents()
    {
        if (currentController != null)
        {
            currentController.OnRollStart.RemoveListener(OnRollStart);
            currentController.OnRollCancel.RemoveListener(OnRollCancel);
            currentController.OnMovementStart.RemoveListener(OnMovementStart);
        }

        if (currentSplineKnotAnimator != null)
        {
            currentSplineKnotAnimator.OnEnterJunction.RemoveListener(OnEnterJunction);
        }
    }

    private void OnEnterJunction(bool junction)
    {
        junctionCamera.Priority = junction ? 10 : -1;
    }

    private void OnMovementStart(bool started)
    {
        if (!started)
        {
            StartCoroutine(ZoomSequence());
            IEnumerator ZoomSequence()
            {

                currentController.AllowInput(false);
                ZoomCamera(true);
                yield return new WaitForSeconds(1.5f);
                ZoomCamera(false);
                currentController.AllowInput(true);
            }
        }
        else
        {
            ZoomCamera(false);
        }
    }


    private void OnRollStart()
    {
        ZoomCamera(true);
    }

    private void OnRollCancel()
    {
        ZoomCamera(false);
    }

    private void Update()
    {
        //depthOfFieldVolume.weight = Mathf.Lerp(depthOfFieldVolume.weight, isZoomed ? 1 : -1, 10 * Time.deltaTime);
    }

    public void ZoomCamera(bool zoom)
    {
        defaultCamera.Priority = zoom ? -1 : 1;
        zoomCamera.Priority = zoom ? 1 : -1;
        isZoomed = zoom;
    }
}
