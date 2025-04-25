using DG.Tweening;
using TMPro;
using UnityEngine;

public class RollUI : MonoBehaviour
{
    private BaseController currentController;
    private Transform currentDice;
    
    private TextMeshProUGUI rollTextMesh;
    public AnimationCurve scaleEase;

    [Header("Parameters")]
    [SerializeField] private Vector3 textOffset;
    [SerializeField] private float followSmoothness = 5;

    private bool rolling = false;
    private bool isActive = false;

    void Start()
    {
        rollTextMesh = GetComponentInChildren<TextMeshProUGUI>();
        rollTextMesh.gameObject.SetActive(false);
        
        // GameManager가 초기화된 후 현재 플레이어 설정
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.AddListener(SetCurrentPlayer);
            SetCurrentPlayer(GameManager.Instance.GetCurrentPlayer());
        }
    }
    
    void OnDestroy()
    {
        // 이벤트 리스너 해제
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerChanged.RemoveListener(SetCurrentPlayer);
        }
        
        UnregisterEvents();
    }
    
    // 현재 플레이어/NPC 설정
    public void SetCurrentPlayer(BaseController controller)
    {
        if (controller == null) return;
        
        // 이전 이벤트 리스너 해제
        UnregisterEvents();
        
        // 새 컨트롤러 참조 설정
        currentController = controller;
        
        // 주사위 참조 획득
        BaseVisualHandler visualHandler = controller.GetComponentInChildren<BaseVisualHandler>();
        if (visualHandler != null)
        {
            // BaseVisualHandler에 주사위 Transform을 가져오는 메서드 추가 필요
            currentDice = visualHandler.GetDiceTransform();
        }
        
        // 새 이벤트 리스너 등록
        if (currentController != null)
        {
            currentController.OnRollEnd.AddListener(OnRollEnd);
            currentController.OnRollDisplay.AddListener(OnRollUpdate);
            currentController.OnMovementStart.AddListener(OnMovementUpdate);
            currentController.OnMovementUpdate.AddListener(OnRollUpdate);
            
            // NPC인 경우 UI 활성화 여부 설정
            isActive = !(currentController is NPCController);
            gameObject.SetActive(isActive);
        }
    }
    
    // 이벤트 리스너 해제
    private void UnregisterEvents()
    {
        if (currentController != null)
        {
            currentController.OnRollEnd.RemoveListener(OnRollEnd);
            currentController.OnRollDisplay.RemoveListener(OnRollUpdate);
            currentController.OnMovementStart.RemoveListener(OnMovementUpdate);
            currentController.OnMovementUpdate.RemoveListener(OnRollUpdate);
            currentController = null;
        }
    }

    private void OnMovementUpdate(bool arg0)
    {
        rolling = false;
    }

    private void OnEnable()
    {
        rolling = true;
    }

    private void LateUpdate()
    {
        if (!isActive || currentController == null || currentDice == null) return;
        
        float movementBlend = Mathf.Pow(0.5f, Time.deltaTime * followSmoothness);
        Vector3 targetPosition = rolling ? currentDice.position : currentController.transform.position + textOffset;
        Vector3 screenPosition = Camera.main.WorldToScreenPoint(targetPosition);
        rollTextMesh.transform.position = Vector3.Lerp(rollTextMesh.transform.position, screenPosition, movementBlend);
    }

    private void OnRollUpdate(int roll)
    {
        if (!isActive) return;
        
        if (roll == 0)
            rollTextMesh.gameObject.SetActive(false);
        rollTextMesh.text = roll.ToString();
    }

    private void OnRollEnd()
    {
        if (!isActive) return;
        
        rollTextMesh.gameObject.SetActive(true);

        rollTextMesh.transform.DOComplete();
        rollTextMesh.transform.DOScale(0, .2f).From().SetEase(scaleEase);
    }
}
