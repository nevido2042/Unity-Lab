using UnityEngine;

namespace Hero.Combat.Core
{
    public enum AttackDirection
    {
        None = 0,
        Left,
        Right,
        Up,
        Thrust
    }

    public enum CombatState
    {
        Idle = 0,
        Windup,      // 공격 준비 모션
        Release,     // 공격 타격 판정 중
        Recovery,    // 공격 후 딜레이
        Block,       // 방어 중
        HitStun,     // 피격 경직
        Dead         // 사망
    }
}
