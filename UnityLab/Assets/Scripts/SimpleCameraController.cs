using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// WASD 이동 및 우클릭 시점 변환을 지원하는 간단한 카메라 컨트롤러 (New Input System 사용)
/// </summary>
public class SimpleCameraController : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("기본 이동 속도")]
    public float movementSpeed = 10f;
    [Tooltip("Shift 키를 눌렀을 때의 이동 속도 배율")]
    public float fastMovementMultiplier = 2f;
    [Tooltip("Q/E 키를 이용한 상하 이동 속도")]
    public float verticalSpeed = 5f;

    [Header("회전 설정")]
    [Tooltip("마우스 감도")]
    public float mouseSensitivity = 0.1f;
    [Tooltip("상하 회전 제한 각도")]
    public float verticalLookLimit = 85f;

    private float _rotationX = 0f;
    private float _rotationY = 0f;
    private bool _isRotating = false;

    void Start()
    {
        // 현재 카메라의 회전값을 초기값으로 설정
        Vector3 euler = transform.eulerAngles;
        _rotationX = euler.y;
        _rotationY = -euler.x;
    }

    void Update()
    {
        // 에디터나 앱이 포커스된 상태에서만 입력 처리
        if (!Application.isFocused) return;

        HandleMovement();
        HandleRotation();
    }

    /// <summary>
    /// WASD 및 Q/E 키 입력을 처리하여 카메라를 이동시킵니다.
    /// </summary>
    private void HandleMovement()
    {
        if (Keyboard.current == null) return;

        float speed = movementSpeed;
        if (Keyboard.current.leftShiftKey.isPressed)
        {
            speed *= fastMovementMultiplier;
        }

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) moveInput.y += 1f;
        if (Keyboard.current.sKey.isPressed) moveInput.y -= 1f;
        if (Keyboard.current.aKey.isPressed) moveInput.x -= 1f;
        if (Keyboard.current.dKey.isPressed) moveInput.x += 1f;

        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y).normalized;
        Vector3 move = transform.rotation * direction * speed * Time.deltaTime;

        if (Keyboard.current.eKey.isPressed)
        {
            move.y += verticalSpeed * Time.deltaTime;
        }
        if (Keyboard.current.qKey.isPressed)
        {
            move.y -= verticalSpeed * Time.deltaTime;
        }

        transform.position += move;
    }

    /// <summary>
    /// 마우스 우클릭 시 시점 회전을 처리합니다.
    /// </summary>
    private void HandleRotation()
    {
        if (Mouse.current == null) return;

        // 마우스 우클릭 상태 확인
        bool rightClickPressed = Mouse.current.rightButton.isPressed;

        // [주의] Cursor.lockState가 일부 환경에서 크래시를 유발할 수 있어 제외했습니다.
        // 상태가 변경될 때만 마우스 커서 표시 여부 업데이트
        if (rightClickPressed && !_isRotating)
        {
            _isRotating = true;
            // Cursor.visible = false; // 커 서 숨김도 일단 제외하여 테스트
        }
        else if (!rightClickPressed && _isRotating)
        {
            _isRotating = false;
            // Cursor.visible = true;
        }

        // 회전 중일 때 마우스 델타 값 처리
        if (_isRotating)
        {
            // delta 값을 직접 읽어옵니다.
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            if (mouseDelta.sqrMagnitude > 0)
            {
                float mouseX = mouseDelta.x * mouseSensitivity;
                float mouseY = mouseDelta.y * mouseSensitivity;

                _rotationX += mouseX;
                _rotationY += mouseY;
                _rotationY = Mathf.Clamp(_rotationY, -verticalLookLimit, verticalLookLimit);

                transform.rotation = Quaternion.Euler(-_rotationY, _rotationX, 0f);
            }
        }
    }
}
