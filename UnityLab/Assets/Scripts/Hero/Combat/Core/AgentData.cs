using UnityEngine;

namespace Hero.Combat.Core
{
    /// <summary>
    /// 대규모 전투 시뮬레이션에서 개별 유닛의 상태를 캐싱하는 구조체
    /// BattleManager 단일 Update 루프에서 배열 형태로 처리하기 위해 활용
    /// </summary>
    [System.Serializable]
    public struct AgentData
    {
        public bool IsActive;             // 오브젝트 풀에서 활성화 여부
        public bool IsPlayerControlled;   // 플레이어 빙의 여부
        public int TeamId;                // 0: 아군, 1: 적 (등등)

        public float Health;
        public float MaxHealth;
        public float MoveSpeed;
        public float AttackRange;
        public float AttackDamage;

        public Vector3 Position;
        public Vector3 Forward;

        public CombatState CurrentState;
        public AttackDirection CurrentDirection;
        public float StateTimer;          // 현재 상태 이탈을 위한 남은 시간

        public int TargetIndex;           // BattleManager 배열 내 적 타겟 인덱스 (-1이면 없음)

        public void Reset(Vector3 spawnPosition, Vector3 spawnForward, int teamId)
        {
            IsActive = true;
            IsPlayerControlled = false;
            TeamId = teamId;
            
            MaxHealth = 100f;
            Health = MaxHealth;
            MoveSpeed = 3f;
            AttackRange = 2f;
            AttackDamage = 15f;

            Position = spawnPosition;
            Forward = spawnForward;

            CurrentState = CombatState.Idle;
            CurrentDirection = AttackDirection.None;
            StateTimer = 0f;
            TargetIndex = -1;
        }
    }
}
