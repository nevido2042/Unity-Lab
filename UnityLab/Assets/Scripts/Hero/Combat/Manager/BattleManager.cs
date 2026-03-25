using UnityEngine;
using Hero.Combat.Core;

namespace Hero.Combat.Manager
{
    public class BattleManager : MonoBehaviour
    {
        public static BattleManager Instance { get; private set; }

        [SerializeField] private int maxAgents = 2000;
        public int MaxAgents { get => maxAgents; set => maxAgents = value; }
        private AgentData[] _agents;

        // 타겟 탐색 주기를 늦춰서 최적화 (모든 프레임마다 거리 계산 금지)
        private float _targetSearchTimer = 0f;
        private readonly float targetSearchInterval = 0.5f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            _agents = new AgentData[maxAgents];
        }

        public int RegisterAgent(Vector3 spawnPosition, Vector3 spawnForward, int teamId)
        {
            for (int i = 0; i < maxAgents; i++)
            {
                if (!_agents[i].IsActive)
                {
                    _agents[i].Reset(spawnPosition, spawnForward, teamId);
                    return i;
                }
            }
            Debug.LogWarning("BattleManager: Max agents reached.");
            return -1;
        }

        public void UnregisterAgent(int index)
        {
            if (index >= 0 && index < maxAgents)
            {
                _agents[index].IsActive = false;
            }
        }

        public ref AgentData GetAgentDataRef(int index)
        {
            return ref _agents[index];
        }

        public AgentData GetAgentData(int index)
        {
            return _agents[index];
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _targetSearchTimer += dt;
            bool doTargetSearch = _targetSearchTimer >= targetSearchInterval;
            if (doTargetSearch) _targetSearchTimer = 0f;

            for (int i = 0; i < maxAgents; i++)
            {
                if (!_agents[i].IsActive) continue;
                
                // 사망 체크
                if (_agents[i].Health <= 0 && _agents[i].CurrentState != CombatState.Dead)
                {
                    _agents[i].CurrentState = CombatState.Dead;
                    _agents[i].TargetIndex = -1;
                    continue;
                }

                if (_agents[i].CurrentState == CombatState.Dead) continue;

                // 타이머 갱신 (애니메이션 상태 전환용)
                if (_agents[i].StateTimer > 0)
                {
                    _agents[i].StateTimer -= dt;
                    if (_agents[i].StateTimer <= 0)
                    {
                        ResolveStateTimer(i);
                    }
                }

                // AI 제어 로직
                if (!_agents[i].IsPlayerControlled && _agents[i].CurrentState == CombatState.Idle)
                {
                    if (doTargetSearch || _agents[i].TargetIndex == -1)
                    {
                        FindTargetFor(i);
                    }

                    ProcessAILogic(i, dt);
                }
            }
        }

        private void FindTargetFor(int agentIndex)
        {
            int myTeam = _agents[agentIndex].TeamId;
            Vector3 myPos = _agents[agentIndex].Position;
            float minDistSq = float.MaxValue;
            int bestTarget = -1;

            // 브루트포스 탐색 (최적화 시 Grid나 QuadTree 도입 필요)
            for (int j = 0; j < maxAgents; j++)
            {
                if (!_agents[j].IsActive || _agents[j].CurrentState == CombatState.Dead) continue;
                if (_agents[j].TeamId == myTeam) continue;

                float sqrDist = (myPos - _agents[j].Position).sqrMagnitude;
                if (sqrDist < minDistSq)
                {
                    minDistSq = sqrDist;
                    bestTarget = j;
                }
            }

            _agents[agentIndex].TargetIndex = bestTarget;
        }

        private void ProcessAILogic(int i, float dt)
        {
            int targetIdx = _agents[i].TargetIndex;
            if (targetIdx == -1) return;

            // 타겟이 죽었거나 비활성화면 타겟 초기화
            if (!_agents[targetIdx].IsActive || _agents[targetIdx].CurrentState == CombatState.Dead)
            {
                _agents[i].TargetIndex = -1;
                return;
            }

            Vector3 targetPos = _agents[targetIdx].Position;
            Vector3 dirToTarget = targetPos - _agents[i].Position;
            float dist = dirToTarget.magnitude;

            if (dist > 0.01f)
            {
                _agents[i].Forward = dirToTarget / dist;
            }

            if (dist > _agents[i].AttackRange)
            {
                // 이동
                _agents[i].Position += _agents[i].Forward * (_agents[i].MoveSpeed * dt);
            }
            else
            {
                // 공격 사거리 내 진입시 마운트앤블레이드 논리 판단
                DecideCombatAction(i, targetIdx);
            }
        }

        private void DecideCombatAction(int i, int targetIdx)
        {
            // 간단한 AI: 적이 방어 중이지 않거나, 나의 공격 쿨타임이 초기화된 Idle 상태라면 공격
            // 적이 나와 같은 방향으로 방어하지 않는 공격 고르기 시뮬레이션 등
            
            ref AgentData myData = ref _agents[i];
            ref AgentData targetData = ref _agents[targetIdx];

            // 적이 특정 방향 공격(Windup) 중이라면, 같은 방향으로 방어 시도 (일정 확률)
            if (targetData.CurrentState == CombatState.Windup)
            {
                if (Random.value > 0.3f) // 70% 확률로 가드
                {
                    myData.CurrentState = CombatState.Block;
                    myData.CurrentDirection = targetData.CurrentDirection;
                    myData.StateTimer = 1.0f; // 가드 유지 시간
                    return;
                }
            }

            // 그렇지 않으면 공격 시도
            myData.CurrentState = CombatState.Windup;
            myData.CurrentDirection = (AttackDirection)Random.Range(1, 5); // 1~4 (좌, 우, 상, 찌르기)
            myData.StateTimer = 0.5f; // Windup 시간 (애니메이션 길이에 맞춤)
        }

        private void ResolveStateTimer(int i)
        {
            ref AgentData data = ref _agents[i];
            
            switch (data.CurrentState)
            {
                case CombatState.Windup:
                    // Windup 끝나면 Release (타격 판정) 돌입
                    data.CurrentState = CombatState.Release;
                    data.StateTimer = 0.2f; // 히트 판정 활성화 시간
                    EvaluateAttack(i);
                    break;
                case CombatState.Release:
                    data.CurrentState = CombatState.Recovery;
                    data.StateTimer = 0.5f; // 후딜레이
                    break;
                case CombatState.Recovery:
                case CombatState.HitStun:
                case CombatState.Block:
                    data.CurrentState = CombatState.Idle;
                    data.CurrentDirection = AttackDirection.None;
                    data.StateTimer = 0f;
                    break;
            }
        }

        private void EvaluateAttack(int attackerIdx)
        {
            ref AgentData attacker = ref _agents[attackerIdx];
            int targetIdx = attacker.TargetIndex;
            if (targetIdx == -1) return;

            ref AgentData target = ref _agents[targetIdx];
            if (!target.IsActive || target.CurrentState == CombatState.Dead) return;

            float dist = Vector3.Distance(attacker.Position, target.Position);
            if (dist <= attacker.AttackRange + 0.5f) // 약간의 허용 범위
            {
                // 마운트 앤 블레이드류 방향 가드 판정
                bool isBlocked = (target.CurrentState == CombatState.Block && target.CurrentDirection == attacker.CurrentDirection);

                if (!isBlocked)
                {
                    // 타격 성공
                    target.Health -= attacker.AttackDamage;
                    target.CurrentState = CombatState.HitStun;
                    target.StateTimer = 0.4f; // 피격 경직 시간
                }
                else
                {
                    // 막힘 처리 (파티클 재생, 효과음 등을 위해 이벤트 추가 가능)
                    // TODO: Blocked Event
                }
            }
        }

        // 플레이어 빙의 시 조작 처리용
        public void SetPlayerControlledAction(int agentIndex, CombatState state, AttackDirection direction, float timer)
        {
            if (agentIndex < 0 || agentIndex >= maxAgents) return;
            if (!_agents[agentIndex].IsActive || _agents[agentIndex].CurrentState == CombatState.Dead) return;

            _agents[agentIndex].CurrentState = state;
            _agents[agentIndex].CurrentDirection = direction;
            _agents[agentIndex].StateTimer = timer;

            if (state == CombatState.Windup)
            {
                // 플레이어도 Windup 시간 적용 후 Update 루프에서 자동으로 Release-EvaluateAttack 이 호출됨
                // 방어나 다른 액션은 Update에서 StateTimer가 줄어들며 Idle로 돌아감
            }
        }
    }
}
