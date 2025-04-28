using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraHandler : MonoBehaviour
{
    // Add Singleton pattern
    public static CameraHandler Instance { get; private set; }

    [Header("References")]
    private BaseController currentController;
    private SplineKnotAnimate currentSplineKnotAnimator;
    [SerializeField] private CinemachineCamera defaultCamera;
    [SerializeField] private CinemachineCamera zoomCamera;
    [SerializeField] private CinemachineCamera junctionCamera;
    [SerializeField] private CinemachineCamera boardCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    // [SerializeField] private Volume depthOfFieldVolume;

    [Header("States")]
    private bool isZoomed = false;

    // Public property to check blending status
    public bool IsBlending => cinemachineBrain != null && cinemachineBrain.IsBlending; // 수정된 부분

    private void Awake() // Modified for Singleton
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.AddListener(SetCurrentPlayer);
            SetCurrentPlayer(GameManager.Instance.GetCurrentPlayer());
        }
        // CinemachineBrain이 할당되지 않았다면 자동으로 찾기
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("CinemachineBrain을 찾을 수 없습니다. 메인 카메라에 CinemachineBrain 컴포넌트가 부착되어 있는지 확인하세요.");
            }
        }
    }

    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;
        UnregisterEvents();
        currentController = controller;
        currentSplineKnotAnimator = controller.GetComponent<SplineKnotAnimate>();

        if (defaultCamera != null)
        {
            defaultCamera.Follow = controller.transform;
            defaultCamera.LookAt = controller.transform;
        }
        if (zoomCamera != null)
        {
            zoomCamera.Follow = controller.transform;
            zoomCamera.LookAt = controller.transform;
        }
        if (junctionCamera != null)
        {
            junctionCamera.Follow = controller.transform;
            junctionCamera.LookAt = controller.transform;
        }

        RegisterEvents(); // Register after setting controller
        ZoomCamera(false); // Ensure default camera is active initially for the new player
    }

    private void RegisterEvents() // Added null checks
    {
        if (currentController != null)
        {
            currentController.OnRollStart.AddListener(OnRollStart);
            currentController.OnRollCancel.AddListener(OnRollCancel);
            currentController.OnMovementStart.AddListener(OnMovementStart);
        }
        if (currentSplineKnotAnimator != null)
        {
            currentSplineKnotAnimator.OnEnterJunction.AddListener(OnEnterJunction);
            // Removed OnKnotLand listener - BaseController will handle turn end logic
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
        if (junctionCamera == null) return;
        // Set junction camera target if needed
        // junctionCamera.LookAt = ...;
        junctionCamera.Priority = junction ? 10 : -1;
        // Optionally zoom out default/zoom cameras when junction cam is active
        if (junction)
        {
            if (defaultCamera != null) defaultCamera.Priority = -1;
            if (zoomCamera != null) zoomCamera.Priority = -1;
        }
        else
        {
            // Restore default/zoom priority based on isZoomed state
            ZoomCamera(isZoomed);
        }
    }

    // Modified OnMovementStart: Only handles camera switching based on movement state
    private void OnMovementStart(bool started)
    {
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return; // Don't switch if junction cam is active

        if (!started) // Movement ended
        {
            // Zoom in when movement stops (optional, could be triggered by OnKnotLand instead)
            // ZoomCamera(true); // Let BaseController decide when to zoom if needed
        }
        else // Movement started
        {
            ZoomCamera(false); // Zoom out when movement starts
        }
    }


    private void OnRollStart()
    {
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        ZoomCamera(true); // Zoom in for rolling
    }

    private void OnRollCancel()
    {
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        ZoomCamera(false); // Zoom out if roll is cancelled
    }

    // Update is not needed for DoF currently
    // private void Update()
    // {
    //     //depthOfFieldVolume.weight = Mathf.Lerp(depthOfFieldVolume.weight, isZoomed ? 1 : -1, 10 * Time.deltaTime);
    // }

    // ZoomCamera now just switches priorities
    public void ZoomCamera(bool zoom)
    {
        if (defaultCamera == null || zoomCamera == null) return;
        // Don't change if junction camera is active
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;

        defaultCamera.Priority = zoom ? -1 : 1;
        zoomCamera.Priority = zoom ? 1 : -1;
        isZoomed = zoom;
    }

    // Optional: Method to explicitly trigger zoom after landing
    public void TriggerPostLandZoom()
    {
        StartCoroutine(ZoomSequenceAfterLand());
    }

    private IEnumerator ZoomSequenceAfterLand()
    {
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) yield break; // Don't zoom if junction cam active

        ZoomCamera(true); // Zoom in
        yield return new WaitUntil(() => !IsBlending); // Wait for zoom-in blend
                                                       // Optional delay after zoom-in
                                                       // yield return new WaitForSeconds(1.0f);
                                                       // ZoomCamera(false); // Zoom out handled by next movement start or turn change
    }

    // Added OnDestroy for Singleton cleanup
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        // Ensure events are unregistered
        UnregisterEvents();
    }
}

