using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// CameraHandler 클래스 - 카메라 시스템 관리
/// 플레이어 추적, 줌 효과, 카메라 전환 등을 처리합니다.
/// BoardEvents 기반 이벤트 시스템을 사용하여 이벤트를 처리합니다.
/// </summary>
public class CameraHandler : MonoBehaviour
{
    [Header("Camera References")]
    [SerializeField] private CinemachineCamera defaultCamera;
    [SerializeField] private CinemachineCamera zoomCamera;
    [SerializeField] private CinemachineCamera junctionCamera;
    [SerializeField] private CinemachineCamera boardCamera;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    
    [Header("Camera States")]
    private bool isZoomed = false;
    
    // 현재 활성화된 캐릭터
    private BaseController currentController;
    
    // BoardManager 참조
    private BoardManager boardManager;
    
    // Public property to check blending status
    public bool IsBlending => cinemachineBrain != null && cinemachineBrain.IsBlending;
    
    /// <summary>
    /// 초기화
    /// </summary>
    public void Initialize()
    {
        // BoardManager 참조 획득
        boardManager = BoardManager.GetInstance();
        
        // CinemachineBrain이 할당되지 않았다면 자동으로 찾기
        if (cinemachineBrain == null)
        {
            cinemachineBrain = FindFirstObjectByType<CinemachineBrain>();
            if (cinemachineBrain == null)
            {
                Debug.LogError("CinemachineBrain을 찾을 수 없습니다. 메인 카메라에 CinemachineBrain 컴포넌트가 부착되어 있는지 확인하세요.");
            }
        }
        
        // 이벤트 리스너 등록
        RegisterEventListeners();
        
        // 현재 플레이어 설정
        SetCurrentPlayer(boardManager.GetCurrentPlayer());
        
        Debug.Log("CameraHandler initialized");
    }
    
    /// <summary>
    /// 컴포넌트 제거 시 이벤트 리스너 해제
    /// </summary>
    private void OnDestroy()
    {
        // 이벤트 리스너 해제
        UnregisterEventListeners();
    }
    
    /// <summary>
    /// 이벤트 리스너 등록
    /// </summary>
    private void RegisterEventListeners()
    {
        // 턴 관련 이벤트 리스너 등록
        BoardEvents.OnTurnStart.AddListener(OnTurnStart);
        
        // 주사위 관련 이벤트 리스너 등록
        BoardEvents.OnRollStart.AddListener(OnRollStart);
        BoardEvents.OnRollCancel.AddListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 등록
        BoardEvents.OnMovementStart.AddListener(OnMovementStart);
        
        // 분기점 관련 이벤트 리스너 등록
        BoardEvents.OnEnterJunction.AddListener(OnEnterJunction);
    }
    
    /// <summary>
    /// 이벤트 리스너 해제
    /// </summary>
    private void UnregisterEventListeners()
    {
        // 턴 관련 이벤트 리스너 해제
        BoardEvents.OnTurnStart.RemoveListener(OnTurnStart);
        
        // 주사위 관련 이벤트 리스너 해제
        BoardEvents.OnRollStart.RemoveListener(OnRollStart);
        BoardEvents.OnRollCancel.RemoveListener(OnRollCancel);
        
        // 이동 관련 이벤트 리스너 해제
        BoardEvents.OnMovementStart.RemoveListener(OnMovementStart);
        
        // 분기점 관련 이벤트 리스너 해제
        BoardEvents.OnEnterJunction.RemoveListener(OnEnterJunction);
    }
    
    /// <summary>
    /// 턴 시작 이벤트 핸들러
    /// </summary>
    private void OnTurnStart(BaseController controller)
    {
        // 현재 플레이어 설정
        SetCurrentPlayer(controller);
    }
    
    /// <summary>
    /// 현재 플레이어 설정
    /// </summary>
    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;
        
        currentController = controller;
        
        // 카메라 타겟 설정
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
        
        // 기본 카메라로 시작
        ZoomCamera(false);
        
        Debug.Log($"Camera following player: {controller.name}");
    }
    
    /// <summary>
    /// 주사위 굴림 시작 이벤트 핸들러
    /// </summary>
    private void OnRollStart(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 분기점 카메라가 활성화되어 있으면 무시
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        
        // 주사위 굴림 시 줌 인
        ZoomCamera(true);
    }
    
    /// <summary>
    /// 주사위 굴림 취소 이벤트 핸들러
    /// </summary>
    private void OnRollCancel(BaseController controller)
    {
        if (controller != currentController) return;
        
        // 분기점 카메라가 활성화되어 있으면 무시
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        
        // 주사위 굴림 취소 시 줌 아웃
        ZoomCamera(false);
    }
    
    /// <summary>
    /// 이동 시작 이벤트 핸들러
    /// </summary>
    private void OnMovementStart(BaseController controller, bool started)
    {
        if (controller != currentController) return;
        
        // 분기점 카메라가 활성화되어 있으면 무시
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        
        if (started)
        {
            // 이동 시작 시 줌 아웃
            ZoomCamera(false);
        }
        else
        {
            // 이동 종료 시 줌 인 (선택적)
            // ZoomCamera(true);
        }
    }
    
    /// <summary>
    /// 분기점 진입 이벤트 핸들러
    /// </summary>
    private void OnEnterJunction(BaseController controller, bool entered)
    {
        if (controller != currentController) return;
        
        if (junctionCamera == null) return;
        
        // 분기점 카메라 우선순위 설정
        junctionCamera.Priority = entered ? 10 : -1;
        
        if (entered)
        {
            // 분기점 진입 시 다른 카메라 비활성화
            if (defaultCamera != null) defaultCamera.Priority = -1;
            if (zoomCamera != null) zoomCamera.Priority = -1;
        }
        else
        {
            // 분기점 종료 시 이전 카메라 상태 복원
            ZoomCamera(isZoomed);
        }
    }
    
    /// <summary>
    /// 카메라 줌 설정
    /// </summary>
    public void ZoomCamera(bool zoom)
    {
        if (defaultCamera == null || zoomCamera == null) return;
        
        // 분기점 카메라가 활성화되어 있으면 무시
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) return;
        
        // 카메라 우선순위 설정
        defaultCamera.Priority = zoom ? -1 : 1;
        zoomCamera.Priority = zoom ? 1 : -1;
        
        // 줌 상태 저장
        isZoomed = zoom;
    }
    
    /// <summary>
    /// 착지 후 줌 시퀀스 트리거
    /// </summary>
    public void TriggerPostLandZoom()
    {
        StartCoroutine(ZoomSequenceAfterLand());
    }
    
    /// <summary>
    /// 착지 후 줌 시퀀스 코루틴
    /// </summary>
    private IEnumerator ZoomSequenceAfterLand()
    {
        // 분기점 카메라가 활성화되어 있으면 무시
        if (junctionCamera != null && junctionCamera.Priority.Value > 0) yield break;
        
        // 줌 인
        ZoomCamera(true);
        
        // 블렌딩이 완료될 때까지 대기
        yield return new WaitUntil(() => !IsBlending);
        
        // 선택적 딜레이
        // yield return new WaitForSeconds(1.0f);
        
        // 다음 이동이나 턴 변경에서 줌 아웃 처리
    }
}
