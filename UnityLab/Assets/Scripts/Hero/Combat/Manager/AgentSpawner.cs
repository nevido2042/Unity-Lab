using UnityEngine;
using Hero.Combat.Agent;

namespace Hero.Combat.Manager
{
    public class AgentSpawner : MonoBehaviour
    {
        [SerializeField] private int _agentsToSpawnPerTeam = 100;
        [SerializeField] private float _spawnRadius = 20f;
        [SerializeField] private Transform _team0SpawnPoint;
        [SerializeField] private Transform _team1SpawnPoint;

        private void Start()
        {
            SpawnTeam(0, _team0SpawnPoint != null ? _team0SpawnPoint.position : new Vector3(-20, 0, 0));
            SpawnTeam(1, _team1SpawnPoint != null ? _team1SpawnPoint.position : new Vector3(20, 0, 0));
        }

        private void SpawnTeam(int teamId, Vector3 centerSpawnPos)
        {
            for (int i = 0; i < _agentsToSpawnPerTeam; i++)
            {
                // 약간 랜덤한 위치에 스폰
                Vector2 randomCircle = Random.insideUnitCircle * _spawnRadius;
                Vector3 spawnPos = centerSpawnPos + new Vector3(randomCircle.x, 0, randomCircle.y);
                
                // 마주보도록 기본 포워드 벡터 설정 (중앙을 향하게)
                Vector3 forward = (Vector3.zero - spawnPos).normalized;
                if (forward == Vector3.zero) forward = Vector3.forward; // 중앙 스폰의 엣지 케이스 처리

                // 1. BattleManager Data 배열에 등록
                int agentIndex = BattleManager.Instance.RegisterAgent(spawnPos, forward, teamId);

                if (agentIndex != -1)
                {
                    // 2. ObjectPooler 에서 프리팹 꺼내서 배치
                    GameObject go = ObjectPooler.Instance.Spawn(spawnPos, Quaternion.LookRotation(forward));
                    
                    // 3. AgentController 세팅
                    AgentController controller = go.GetComponent<AgentController>();
                    if (controller != null)
                    {
                        controller.Initialize(agentIndex);
                    }
                    else
                    {
                        Debug.LogError("Spawned Agent missing AgentController");
                    }
                }
            }
        }
    }
}
