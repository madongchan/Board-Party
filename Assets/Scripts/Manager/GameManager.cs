using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// GameManager 클래스
public class GameManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static GameManager Instance { get; private set; }

    // 플레이어 목록 (사용자 플레이어 + NPC 플레이어들)
    [SerializeField] private List<BaseController> players = new List<BaseController>();
    public int currentPlayerIndex = -1;

    // 스플라인 노트 데이터 참조
    [SerializeField] private SplineKnotInstantiate splineKnotData;
    public SplineKnotInstantiate SplineKnotData => splineKnotData;

    [HideInInspector] public UnityEvent<BaseController> OnPlayerChanged;

    private void Awake()
    {
        // 싱글톤 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // 게임 시작
        StartGame();
    }

    // 게임 시작
    public void StartGame()
    {
        // 플레이어 목록이 Inspector에서 설정되어 있지 않으면 자동으로 찾기
        if (players.Count == 0)
        {
            // 태그로 플레이어 찾기
            GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject playerObject in playerObjects)
            {
                BaseController controller = playerObject.GetComponent<BaseController>();
                if (controller != null)
                    players.Add(controller);
            }

            GameObject[] npcObjects = GameObject.FindGameObjectsWithTag("NPC");
            foreach (GameObject npcObject in npcObjects)
            {
                BaseController controller = npcObject.GetComponent<BaseController>();
                if (controller != null)
                    players.Add(controller);
            }
        }

        // 플레이어 순서 랜덤화 (선택사항)
        //ShufflePlayers();

        // 첫 턴 시작
        currentPlayerIndex = -1;
        StartNextTurn();
    }

    // 다음 턴 시작
    public void StartNextTurn()
    {
        // 다음 플레이어 인덱스 계산
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        BaseController currentPlayer = players[currentPlayerIndex];

        // 플레이어 변경 이벤트 발생
        OnPlayerChanged.Invoke(currentPlayer);

        // 현재 플레이어가 NPC인지 확인
        NPCController npcController = currentPlayer as NPCController;
        if (npcController != null)
        {
            // NPC 턴 시작
            npcController.ChangeState<TurnStartState>();
            // UI 업데이트 (NPC 턴 표시)
            UIManager.Instance.UpdateAllUI();
        }
        else
        {
            // 사용자 플레이어 턴 시작
            PlayerController playerController = currentPlayer as PlayerController;
            // UI 업데이트 (플레이어 턴 표시)
            UIManager.Instance.UpdateAllUI();
            // 플레이어 주사위 굴림 준비
            playerController.PrepareToRoll();
        }
    }

    // 현재 턴 종료
    public void EndCurrentTurn()
    {
        // 현재 턴 종료 및 다음 턴 시작
        StartNextTurn();
    }

    // 현재 플레이어 가져오기
    public BaseController GetCurrentPlayer()
    {
        if (currentPlayerIndex >= 0 && currentPlayerIndex < players.Count)
            return players[currentPlayerIndex];
        return null;
    }

    // 현재 플레이어가 NPC인지 확인
    public bool IsCurrentPlayerNPC()
    {
        BaseController currentPlayer = GetCurrentPlayer();
        return currentPlayer != null && currentPlayer is NPCController;
    }

    // 플레이어 순서 랜덤화
    private void ShufflePlayers()
    {
        for (int i = 0; i < players.Count; i++)
        {
            BaseController temp = players[i];
            int randomIndex = Random.Range(i, players.Count);
            players[i] = players[randomIndex];
            players[randomIndex] = temp;
        }
    }
}
