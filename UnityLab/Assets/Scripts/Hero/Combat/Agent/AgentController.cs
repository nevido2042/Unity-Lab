using UnityEngine;
using Hero.Combat.Core;
using Hero.Combat.Manager;

namespace Hero.Combat.Agent
{
    [RequireComponent(typeof(Animator))]
    public class AgentController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [field: SerializeField] public int AgentIndex { get; private set; } = -1;

        // Animator Hashes for performance
        private readonly int _velocityXHash = Animator.StringToHash("VelocityX");
        private readonly int _velocityZHash = Animator.StringToHash("VelocityZ");
        private readonly int _attackHash = Animator.StringToHash("Attack");
        private readonly int _blockHash = Animator.StringToHash("Block");
        private readonly int _hitHash = Animator.StringToHash("Hit");
        private readonly int _deadHash = Animator.StringToHash("Dead");
        private readonly int _dirHash = Animator.StringToHash("Direction"); // 1:Left, 2:Right, 3:Up, 4:Thrust

        // 상태 캐싱 (중복 Animator Trigger 방지용)
        private CombatState _previousState = CombatState.Idle;
        private Vector3 _previousPos;

        public void Initialize(int index)
        {
            AgentIndex = index;
            if (_animator == null) _animator = GetComponent<Animator>();
            _previousPos = transform.position;
            _previousState = CombatState.Idle;
            if (_animator != null && _animator.runtimeAnimatorController != null)
            {
                _animator.Rebind();
                _animator.Update(0f);
            }
        }

        private void Update()
        {
            // 1. 유효성 검사: 인덱스가 할당되지 않았거나 배틀 매니저가 없으면 중단
            if (AgentIndex == -1 || BattleManager.Instance == null) return;

            // 2. 데이터 검색: 배틀 매니저로부터 이 에이전트의 최신 데이터(구조체)를 가져옴
            AgentData data = BattleManager.Instance.GetAgentData(AgentIndex);

            // 3. 활성화 상태 동기화: 유닛이 비활성 상태라면 게임 오브젝트도 비활성화
            if (!data.IsActive)
            {
                gameObject.SetActive(false);
                return;
            }

            // 4. 트랜스폼(위치/회전) 동기화
            // 로컬 이동 속도 계산: 이전 위치와 현재 위치의 차이를 로컬 좌표계로 변환 (전후좌우 애니메이션용)
            Vector3 worldVelocity = (data.Position - _previousPos) / Time.deltaTime;
            Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);
            
            // 위치 동기화: Lerp를 이용해 부드럽게 실제 데이터상의 위치로 이동 (프레임간 부드러운 연결)
            transform.position = Vector3.Lerp(transform.position, data.Position, Time.deltaTime * 10f);
            
            // 회전 동기화: LookRotation과 Slerp를 이용해 유닛이 바라보는 방향(Forward)으로 부드럽게 회전
            if (data.Forward != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(data.Forward), Time.deltaTime * 10f);
            }
            
            _previousPos = data.Position;

            // 5. 애니메이션 동기화 (AnimatorController가 있는 경우에만 처리)
            if (_animator == null || _animator.runtimeAnimatorController == null) return;

            // 로컬 방향 성분을 2D Blend Tree 파라미터로 전달
            _animator.SetFloat(_velocityXHash, localVelocity.x);
            _animator.SetFloat(_velocityZHash, localVelocity.z);

            // 상태 변화 감지: 전투 상태가 변경되었을 때만 트리거 및 파라미터 업데이트 (매 프레임 호출 방지)
            if (_previousState != data.CurrentState)
            {
                _previousState = data.CurrentState;
                
                // 공격/방어 방향(Direction) 업데이트
                _animator.SetInteger(_dirHash, (int)data.CurrentDirection);

                // 현재 상태에 따른 애니메이션 트리거 실행
                switch (data.CurrentState)
                {
                    case CombatState.Windup:
                        _animator.SetTrigger(_attackHash); // 공격 준비 시작
                        break;
                    case CombatState.Block:
                        _animator.SetTrigger(_blockHash);  // 방어 태세
                        break;
                    case CombatState.HitStun:
                        _animator.SetTrigger(_hitHash);    // 피격 모션
                        break;
                    case CombatState.Dead:
                        _animator.SetTrigger(_deadHash);   // 사망 애니메이션
                        break;
                    case CombatState.Idle:
                    case CombatState.Release:
                    case CombatState.Recovery:
                        // 대기, 공격 방출, 후딜레이는 별도의 트리거 없이 상태 전이 구조에서 자연스럽게 처리됨
                        break;
                }
            }
        }
    }
}
