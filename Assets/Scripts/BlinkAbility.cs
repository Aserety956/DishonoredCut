using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Blink (телепорт) как в Dishonored: можно телепортироваться и в воздух.
/// Логика:
/// 1) Мы целимся (hold aimKey) -> считаем точку назначения на луче камеры.
/// 2) Если луч упёрся в препятствие -> точка чуть ПЕРЕД препятствием.
///    Если не упёрся -> точка в воздухе на maxDistance.
/// 3) Проверяем, что капсула игрока помещается в точке (CheckCapsule).
/// 4) По нажатию castKey -> телепорт (CharacterController выключаем на момент смены позиции).
/// </summary>
public class BlinkAbility : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform marker;

    [Header("Distance")]
    [SerializeField] private float minDistance = 1.5f;
    [SerializeField] private float maxDistance = 15f;

    [Tooltip("Текущая выбранная дальность блинка (меняется колесом).")]
    [SerializeField] private float aimDistance = 10f;

    [Tooltip("Насколько меняется aimDistance за один щелчок колеса.")]
    [SerializeField] private float scrollStep = 1.5f;

    [Header("Collision")]
    [Tooltip("Слои, которые ОСТАНАВЛИВАЮТ луч (стены/пол/потолок/объекты).")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Tooltip("Слои, которые НЕЛЬЗЯ пересекать капсулой в точке телепорта (обычно стены/мебель).")]
    [SerializeField] private LayerMask overlapMask = ~0;

    [Tooltip("Отступ назад от поверхности, чтобы не оказаться внутри коллайдера.")]
    [SerializeField] private float backoffFromHit = 0.12f;

    [Tooltip("Чуть приподнимаем точку, чтобы не 'впечатываться' в пол.")]
    [SerializeField] private float standUpOffset = 0.05f;

    [Header("Marker")]
    [SerializeField] private bool showMarker = true;
    [SerializeField] private float markerOffsetY = 0.02f;
    [SerializeField] private float markerScaleValid = 0.25f;
    [SerializeField] private float markerScaleInvalid = 0.15f;

    [Header("Input")]
    [Tooltip("Удержание ПКМ = прицеливание.")]
    [SerializeField] private bool aimWithRightMouse = true;

    [Header("Debug")]
    [SerializeField] private bool debugDraw = false;
    [SerializeField] private bool debugLogBlocked = false;

    private bool isAiming;
    private bool hasValidPoint;
    private Vector3 targetPoint;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = Camera.main;
    }

    private void Awake()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (playerCamera == null) playerCamera = Camera.main;

        aimDistance = Mathf.Clamp(aimDistance, minDistance, maxDistance);

        if (marker != null) marker.gameObject.SetActive(false);
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null || playerCamera == null)
            return;

        // 1) Вход/выход из прицеливания (ПКМ удержание)
        if (aimWithRightMouse)
        {
            if (mouse.rightButton.wasPressedThisFrame) isAiming = true;

            // ВАЖНО: на отпускании сначала каст, потом выключаем прицел
            if (mouse.rightButton.wasReleasedThisFrame)
            {
                if (isAiming && hasValidPoint)
                    TeleportTo(targetPoint);

                isAiming = false;
                hasValidPoint = false;
                if (marker != null) marker.gameObject.SetActive(false);
                return;
            }
        }

        if (!isAiming)
            return;

        // 2) Колёсиком регулируем дальность (чтобы можно было блинкать близко)
        UpdateAimDistance(mouse);

        // 3) Считаем точку
        UpdateTargetPoint();

        // 4) Маркер
        UpdateMarker();
    }

    private void UpdateAimDistance(Mouse mouse)
    {
        // scroll.y обычно +/-120 за "щелчок"
        float scrollY = mouse.scroll.ReadValue().y;

        if (Mathf.Abs(scrollY) < 0.01f)
            return;

        float steps = scrollY / 5f; // нормализуем к "щелчкам"
        aimDistance += steps * scrollStep;
        aimDistance = Mathf.Clamp(aimDistance, minDistance, maxDistance);
    }

    private void UpdateTargetPoint()
    {
        hasValidPoint = false;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // 1) Точка в воздухе на выбранной дальности
        Vector3 candidate = ray.origin + ray.direction * aimDistance;

        // 2) Если есть препятствие ДО aimDistance — обрезаем до hit.point
        if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            candidate = hit.point - ray.direction * backoffFromHit;
        }

        // 3) Чуть вверх, чтобы не пересекать пол
        candidate += Vector3.up * standUpOffset;

        // 4) Проверка "влезает ли капсула"
        if (!CanStandAt(candidate))
        {
            if (debugLogBlocked)
                Debug.Log($"Blink blocked at {candidate} (overlap). Check overlapMask excludes Ground & Player.");

            targetPoint = candidate;
            return;
        }

        targetPoint = candidate;
        hasValidPoint = true;

        if (debugDraw)
        {
            Debug.DrawRay(ray.origin, ray.direction * aimDistance, Color.cyan, 0.02f);
            Debug.DrawLine(ray.origin, targetPoint, Color.green, 0.02f);
        }
    }

    private bool CanStandAt(Vector3 position)
    {
        if (controller == null) return true;

        float radius = controller.radius;
        float height = controller.height;

        Vector3 centerWorld = position + controller.center;

        float halfHeight = height * 0.5f;
        float cylinderHalf = Mathf.Max(0f, halfHeight - radius);

        Vector3 top = centerWorld + Vector3.up * cylinderHalf;
        Vector3 bottom = centerWorld + Vector3.down * cylinderHalf;

        bool blocked = Physics.CheckCapsule(bottom, top, radius, overlapMask, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    private void UpdateMarker()
    {
        if (!showMarker || marker == null)
            return;

        marker.gameObject.SetActive(true);
        marker.position = targetPoint + Vector3.up * markerOffsetY;

        float s = hasValidPoint ? markerScaleValid : markerScaleInvalid;
        marker.localScale = new Vector3(s, s, s);
    }

    private void TeleportTo(Vector3 position)
    {
        if (controller == null)
        {
            transform.position = position;
            return;
        }

        bool wasEnabled = controller.enabled;
        controller.enabled = false;

        transform.position = position;

        controller.enabled = wasEnabled;
    }
}

