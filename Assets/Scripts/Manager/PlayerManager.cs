using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PlayerManager 클래스 - 플레이어와 NPC 관리
/// 플레이어 목록 관리, 초기화, 접근 등을 담당합니다.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    // 플레이어 목록 (사용자 플레이어 + NPC 플레이어들)
    [SerializeField] private List<BaseController> players = new List<BaseController>();

    /// <summary>
    /// PlayerManager 초기화
    /// </summary>
    public void Initialize()
    {
        // 플레이어 목록 초기화
        InitializePlayers();

        // 각 플레이어 초기화
        foreach (BaseController player in players)
        {
            player.Initialize();
        }
    }

    /// <summary>
    /// 플레이어 목록 초기화
    /// </summary>
    private void InitializePlayers()
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
    }

    /// <summary>
    /// 플레이어 수 가져오기
    /// </summary>
    public int GetPlayerCount()
    {
        return players.Count;
    }

    /// <summary>
    /// 인덱스로 플레이어 가져오기
    /// </summary>
    public BaseController GetPlayerAt(int index)
    {
        if (index >= 0 && index < players.Count)
            return players[index];
        return null;
    }

    /// <summary>
    /// 플레이어 순서 랜덤화
    /// </summary>
    public void ShufflePlayers()
    {
        for (int i = 0; i < players.Count; i++)
        {
            BaseController temp = players[i];
            int randomIndex = Random.Range(i, players.Count);
            players[i] = players[randomIndex];
            players[randomIndex] = temp;
        }
    }

    public BaseController GetPlayerController()
    {
        return players.Find(player => player.CompareTag("Player"));
    }

    public List<BaseController> GetNPCControllers()
    {
        return players.FindAll(player => player.CompareTag("NPC"));
    }
}
