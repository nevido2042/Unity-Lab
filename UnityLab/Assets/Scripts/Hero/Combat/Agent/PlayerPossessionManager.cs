using UnityEngine;
using UnityEngine.InputSystem;
using Hero.Combat.Core;
using Hero.Combat.Manager;

namespace Hero.Combat.Agent
{
    public enum PossessionState
    {
        Ghost,          // 자유 카메라 혹은 기본 관전 (아직 특정 유닛 선택 안됨)
        Spectating,     // 특정 유닛 TPS 관전 (AI가 제어 중)
        Possessed       // 유닛 직접 조작
    }

    public class PlayerPossessionManager : MonoBehaviour
    {
        public static PlayerPossessionManager Instance { get; private set; }

        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _ghostMoveSpeed = 20f;
        [SerializeField] private float _mouseSensitivity = 0.2f;

        [Header("State")]
        [SerializeField] private PossessionState _currentState = PossessionState.Ghost;
        [SerializeField] private int _currentAgentIndex = -1;

        private Transform _cameraAnchor;
        private float _yaw = 0f;
        private float _pitch = 20f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            if (_cameraAnchor == null)
            {
                _cameraAnchor = new GameObject("CameraAnchor").transform;
            }
        }

        private void Start()
        {
            EnterGhostMode();
        }

        private void Update()
        {
            // 유니티 창이 포커스되어 있을 때만 입력 처리
            if (!Application.isFocused) return;

            // 마우스 회전 업데이트
            // (커서 잠금 로직이 제거되어 화면 끝에 닿으면 회전이 멈출 수 있습니다.)
            if (Mouse.current != null)
            {
                Vector2 delta = Mouse.current.delta.ReadValue();
                if (delta.sqrMagnitude > 0.001f)
                {
                    _yaw += delta.x * _mouseSensitivity;
                    _pitch -= delta.y * _mouseSensitivity;
                    _pitch = Mathf.Clamp(_pitch, -80f, 80f);
                }
            }

            // Tab 키: 고스트 <> 관전 전환
            if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
            {
                if (_currentState == PossessionState.Ghost)
                {
                    CycleAgent(1); // 관전 모드로 진입 (첫 번째 유닛)
                }
                else
                {
                    EnterGhostMode(); // 고스트 모드로 복귀
                }
            }

            // 회전값을 먼저 적용 (이동 로직에서 참조하기 위함)
            if (_playerCamera != null)
            {
                _playerCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }

            switch (_currentState)
            {
                case PossessionState.Ghost:
                    HandleGhostInput();
                    HandleSelectionInput();
                    break;
                case PossessionState.Spectating:
                    HandleSpectatorInput();
                    HandleSelectionInput();
                    break;
                case PossessionState.Possessed:
                    HandleCombatInput();
                    break;
            }

            // 위치 추적 업데이트
            UpdateCameraFollow();
        }

        // LockCursor 메서드 삭제 (크래시 방지)

        private void EnterGhostMode()
        {
            _currentState = PossessionState.Ghost;
            ReleaseAgent();
            
            if (_playerCamera != null)
            {
                _playerCamera.transform.SetParent(null);
                // 현재 위치는 유지하거나 초기화
                _yaw = _playerCamera.transform.eulerAngles.y;
                _pitch = _playerCamera.transform.eulerAngles.x;
            }
        }

        private void HandleGhostInput()
        {
            if (Keyboard.current == null || _playerCamera == null) return;

            Vector3 inputMove = Vector3.zero;
            if (Keyboard.current.wKey.isPressed) inputMove += Vector3.forward;
            if (Keyboard.current.sKey.isPressed) inputMove += Vector3.back;
            if (Keyboard.current.aKey.isPressed) inputMove += Vector3.left;
            if (Keyboard.current.dKey.isPressed) inputMove += Vector3.right;
            if (Keyboard.current.qKey.isPressed) inputMove += Vector3.down;
            if (Keyboard.current.eKey.isPressed) inputMove += Vector3.up;

            if (inputMove.sqrMagnitude > 0.01f)
            {
                // 카메라의 로컬 방향을 월드로 변환하여 이동 (Fly-cam 방식)
                Vector3 worldMove = _playerCamera.transform.TransformDirection(inputMove);
                _playerCamera.transform.position += worldMove * (_ghostMoveSpeed * Time.deltaTime);
            }
        }

        private void HandleSelectionInput()
        {
            if (Mouse.current == null) return;

            bool isClick = Mouse.current.leftButton.wasPressedThisFrame;
            bool isRightClick = Mouse.current.rightButton.wasPressedThisFrame;

            if (isClick || isRightClick)
            {
                if (_currentState == PossessionState.Spectating)
                {
                    CycleAgent(isClick ? -1 : 1);
                }
            }
        }

        private void CycleAgent(int direction)
        {
            int maxTotal = BattleManager.Instance.MaxAgents;
            int startIdx = _currentAgentIndex == -1 ? 0 : _currentAgentIndex;
            int nextIdx = startIdx;

            for (int i = 0; i < maxTotal; i++)
            {
                nextIdx += direction;
                if (nextIdx < 0) nextIdx = maxTotal - 1;
                else if (nextIdx >= maxTotal) nextIdx = 0;

                AgentData data = BattleManager.Instance.GetAgentData(nextIdx);
                if (data.IsActive && data.Health > 0)
                {
                    SpectateAgent(nextIdx);
                    return;
                }
            }
        }

        public void SpectateAgent(int agentIndex)
        {
            if (_currentState == PossessionState.Possessed) ReleaseAgent();
            _currentAgentIndex = agentIndex;
            _currentState = PossessionState.Spectating;
        }

        private void HandleSpectatorInput()
        {
            // Enter 키: 관전 -> 빙의
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                StartPossession(_currentAgentIndex);
            }
        }

        private void StartPossession(int agentIndex)
        {
            if (agentIndex == -1) return;
            _currentState = PossessionState.Possessed;
            ref AgentData data = ref BattleManager.Instance.GetAgentDataRef(agentIndex);
            data.IsPlayerControlled = true;
        }

        public void ReleaseAgent()
        {
            if (_currentAgentIndex != -1)
            {
                ref AgentData data = ref BattleManager.Instance.GetAgentDataRef(_currentAgentIndex);
                data.IsPlayerControlled = false;
            }
        }

        private void HandleCombatInput()
        {
            if (_currentAgentIndex == -1 || BattleManager.Instance == null) return;
            
            // Enter 키: 빙의 -> 관전 복귀
            if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ReleaseAgent();
                _currentState = PossessionState.Spectating;
                return;
            }

            ref AgentData data = ref BattleManager.Instance.GetAgentDataRef(_currentAgentIndex);
            if (!data.IsActive || data.CurrentState == CombatState.Dead)
            {
                EnterGhostMode();
                return;
            }

            float h = 0f;
            float v = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed) h += 1f;
                if (Keyboard.current.sKey.isPressed) v -= 1f;
                if (Keyboard.current.wKey.isPressed) v += 1f;
            }

            Vector3 camForward = _playerCamera.transform.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = _playerCamera.transform.right;
            camRight.y = 0;
            camRight.Normalize();

            // 이동 (Idle 상태에서만 가능)
            if (data.CurrentState == CombatState.Idle)
            {
                Vector3 moveDir = camRight * h + camForward * v;
                if (moveDir.sqrMagnitude > 0.01f)
                {
                    moveDir.Normalize();
                    data.Position += moveDir * (data.MoveSpeed * Time.deltaTime);
                }
            }

            // 항시 정면 응시 (카메라 방향 기준)
            if (camForward != Vector3.zero)
            {
                data.Forward = camForward;
            }

            // 공격 조작
            if (data.CurrentState == CombatState.Idle && Mouse.current != null)
            {
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    AttackDirection dir = DetermineDirection(mouseDelta);
                    BattleManager.Instance.SetPlayerControlledAction(_currentAgentIndex, CombatState.Windup, dir, 0.5f);
                }
                else if (Mouse.current.rightButton.wasPressedThisFrame) // 방어를 우 클릭으로 바꿈
                {
                    AttackDirection dir = DetermineDirection(mouseDelta);
                    BattleManager.Instance.SetPlayerControlledAction(_currentAgentIndex, CombatState.Block, dir, 1.0f);
                }
            }
        }

        private AttackDirection DetermineDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                return delta.x > 0 ? AttackDirection.Right : AttackDirection.Left;
            }
            else
            {
                return delta.y > 0 ? AttackDirection.Up : AttackDirection.Thrust;
            }
        }

        private void UpdateCameraFollow()
        {
            if (_playerCamera == null) return;
            _playerCamera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

            if (_currentState == PossessionState.Ghost) return; // 위치는 번역으로 이미 이동됨

            if (_currentAgentIndex == -1) return;
            GameObject targetObj = FindAgentObject(_currentAgentIndex);
            if (targetObj == null) return;

            Vector3 targetPos = targetObj.transform.position + Vector3.up * 1.5f;
            Vector3 offset = _playerCamera.transform.rotation * new Vector3(0, 0, -3.5f); // 카메라 뒤쪽 일정거리
            
            _playerCamera.transform.position = Vector3.Lerp(_playerCamera.transform.position, targetPos + offset, Time.deltaTime * 20f);
        }

        private GameObject FindAgentObject(int index)
        {
             AgentController[] controllers = FindObjectsByType<AgentController>(FindObjectsSortMode.None);
             foreach(var c in controllers)
             {
                 if (c.AgentIndex == index) return c.gameObject;
             }
             return null;
        }

    }
}

