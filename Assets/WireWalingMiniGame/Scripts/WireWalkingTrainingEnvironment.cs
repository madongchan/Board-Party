using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// ML-Agents 학습 환경 관리자 (수정 버전)
/// 여러 개의 줄타기 환경을 생성하고 관리합니다.
/// 줄은 X축 방향, 균형 막대는 Z축 방향으로 배치됩니다.
/// </summary>
public class WireWalkingTrainingEnvironment : MonoBehaviour
{
    [Header("환경 설정")]
    [SerializeField] private GameObject npcAgentPrefab; // NPC 에이전트 프리팹
    [SerializeField] private GameObject wirePrefab; // 줄 프리팹
    [SerializeField] private GameObject windParticlePrefab; // 바람 파티클 프리팹
    [SerializeField] private int environmentCount = 5; // 환경 개수 (기본값 줄임)
    [SerializeField] private float environmentSpacing = 10f; // 환경 간격
    
    [Header("줄 설정")]
    [SerializeField] private float wireLength = 20f; // 줄 길이
    [SerializeField] private float wireHeight = 5f; // 줄 높이
    
    [Header("학습 시각화")]
    [SerializeField] private bool showDebugInfo = true; // 디버깅 정보 표시 여부
    [SerializeField] private float timeScale = 1.0f; // 시뮬레이션 속도 (1.0 = 실시간)
    
    // 생성된 환경 목록
    private List<GameObject> environments = new List<GameObject>();
    
    private void Start()
    {
        // 시뮬레이션 속도 설정
        Time.timeScale = timeScale;
        
        // 학습 환경 생성
        CreateTrainingEnvironments();
    }
    
    /// <summary>
    /// 학습 환경 생성
    /// </summary>
    private void CreateTrainingEnvironments()
    {
        for (int i = 0; i < environmentCount; i++)
        {
            // 환경 위치 계산
            Vector3 environmentPosition = new Vector3(0, 0, i * environmentSpacing);
            
            // 환경 생성
            GameObject environment = new GameObject($"Environment_{i}");
            environment.transform.position = environmentPosition;
            environment.transform.parent = transform;
            
            // 줄 생성 (X축 방향)
            GameObject wire = CreateWire(environment.transform, wireLength);
            
            // 시작점과 끝점 생성
            Transform wireStart = CreateWirePoint(environment.transform, "WireStart", new Vector3(-wireLength/2, wireHeight, 0));
            Transform wireEnd = CreateWirePoint(environment.transform, "WireEnd", new Vector3(wireLength/2, wireHeight, 0));
            
            // NPC 에이전트 생성
            GameObject npcAgent = CreateNPCAgent(environment.transform, wireStart, wireEnd, wireLength);
            
            // 바람 파티클 생성 (줄 기준 좌우에 배치)
            CreateWindParticles(environment.transform, npcAgent.transform);
            
            // 환경 목록에 추가
            environments.Add(environment);
        }
        
        Debug.Log($"{environmentCount}개의 학습 환경이 생성되었습니다.");
    }
    
    /// <summary>
    /// 줄 생성
    /// </summary>
    private GameObject CreateWire(Transform parent, float length)
    {
        GameObject wire = Instantiate(wirePrefab, parent);
        wire.name = "Wire";
        
        // 줄 위치 및 크기 설정 (X축 방향)
        wire.transform.localPosition = new Vector3(0, wireHeight, 0);
        
        // LineRenderer 설정 (줄 프리팹에 LineRenderer가 있다고 가정)
        LineRenderer lineRenderer = wire.GetComponent<LineRenderer>();
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, new Vector3(-length/2, 0, 0));
            lineRenderer.SetPosition(1, new Vector3(length/2, 0, 0));
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
        
        return wire;
    }
    
    /// <summary>
    /// 줄 포인트 생성
    /// </summary>
    private Transform CreateWirePoint(Transform parent, string name, Vector3 position)
    {
        GameObject point = new GameObject(name);
        point.transform.parent = parent;
        point.transform.localPosition = position;
        return point.transform;
    }
    
    /// <summary>
    /// NPC 에이전트 생성
    /// </summary>
    private GameObject CreateNPCAgent(Transform parent, Transform wireStart, Transform wireEnd, float wireLength)
    {
        GameObject npcAgent = Instantiate(npcAgentPrefab, parent);
        npcAgent.name = "NPCAgent";
        
        // 시작 위치 설정
        npcAgent.transform.position = wireStart.position;
        
        // NPCWireWalkerHorizontalPole 컴포넌트 설정
        NPCWireWalkerHorizontalPole npcController = npcAgent.GetComponent<NPCWireWalkerHorizontalPole>();
        if (npcController != null)
        {
            // 리플렉션을 사용하여 private 필드에 접근하는 대신, 
            // 공개 메서드나 속성을 통해 설정하는 것이 좋습니다.
            // 여기서는 예시로 직접 필드를 설정하는 방식을 보여줍니다.
            
            // SerializedField 변수 설정을 위한 코드
            // 실제로는 Inspector에서 설정하거나 공개 메서드를 통해 설정하는 것이 좋습니다.
            var wireStartField = npcController.GetType().GetField("wireStart", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wireEndField = npcController.GetType().GetField("wireEnd", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var wireLengthField = npcController.GetType().GetField("wireLength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (wireStartField != null) wireStartField.SetValue(npcController, wireStart);
            if (wireEndField != null) wireEndField.SetValue(npcController, wireEnd);
            if (wireLengthField != null) wireLengthField.SetValue(npcController, wireLength);
        }
        
        return npcAgent;
    }
    
    /// <summary>
    /// 바람 파티클 생성
    /// </summary>
    private void CreateWindParticles(Transform parent, Transform targetTransform)
    {
        // 왼쪽 바람 파티클 (줄 기준 왼쪽, Z축 음수 방향)
        GameObject leftWindParticle = Instantiate(windParticlePrefab, parent);
        leftWindParticle.name = "LeftWindParticle";
        leftWindParticle.transform.localPosition = new Vector3(0, wireHeight, -5); // Z축 음수 방향
        leftWindParticle.transform.localRotation = Quaternion.Euler(0, 0, 0); // Z축 양수 방향으로 향하게
        
        // 오른쪽 바람 파티클 (줄 기준 오른쪽, Z축 양수 방향)
        GameObject rightWindParticle = Instantiate(windParticlePrefab, parent);
        rightWindParticle.name = "RightWindParticle";
        rightWindParticle.transform.localPosition = new Vector3(0, wireHeight, 5); // Z축 양수 방향
        rightWindParticle.transform.localRotation = Quaternion.Euler(0, 180, 0); // Z축 음수 방향으로 향하게
        
        // HorizontalPoleBalanceController 찾기
        HorizontalPoleBalanceController poleController = targetTransform.GetComponentInChildren<HorizontalPoleBalanceController>();
        if (poleController != null)
        {
            // 파티클 시스템 참조 설정
            var leftParticleField = poleController.GetType().GetField("leftWindParticle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rightParticleField = poleController.GetType().GetField("rightWindParticle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (leftParticleField != null) leftParticleField.SetValue(poleController, leftWindParticle.GetComponent<ParticleSystem>());
            if (rightParticleField != null) rightParticleField.SetValue(poleController, rightWindParticle.GetComponent<ParticleSystem>());
        }
    }
    
    /// <summary>
    /// 모든 환경 리셋
    /// </summary>
    public void ResetAllEnvironments()
    {
        foreach (GameObject environment in environments)
        {
            NPCWireWalkerHorizontalPole npcController = environment.GetComponentInChildren<NPCWireWalkerHorizontalPole>();
            if (npcController != null)
            {
                npcController.EndEpisode();
            }
        }
    }
    
    /// <summary>
    /// 시뮬레이션 속도 변경
    /// </summary>
    public void SetTimeScale(float scale)
    {
        timeScale = Mathf.Clamp(scale, 0.1f, 10f);
        Time.timeScale = timeScale;
        Debug.Log($"시뮬레이션 속도가 {timeScale}배로 변경되었습니다.");
    }
    
    /// <summary>
    /// 디버깅 정보 표시 설정
    /// </summary>
    public void SetDebugInfoVisibility(bool visible)
    {
        showDebugInfo = visible;
        
        // 모든 NPC 에이전트에 설정 전달
        foreach (GameObject environment in environments)
        {
            NPCWireWalkerHorizontalPole npcController = environment.GetComponentInChildren<NPCWireWalkerHorizontalPole>();
            if (npcController != null)
            {
                // 디버깅 정보 표시 설정 (리플렉션 사용)
                var debugField = npcController.GetType().GetField("_showDebugInfo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (debugField != null) debugField.SetValue(npcController, showDebugInfo);
            }
        }
    }
}
