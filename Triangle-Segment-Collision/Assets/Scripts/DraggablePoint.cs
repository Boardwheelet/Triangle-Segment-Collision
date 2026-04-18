using UnityEngine;

/// <summary>
/// 마우스로 3D 포인트를 드래그할 수 있게 해주는 컴포넌트.
/// 각 꼭짓점 구(Sphere)에 붙여서 사용합니다.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DraggablePoint : MonoBehaviour
{
    [Tooltip("드래그 시 이동을 제한할 축. None이면 자유롭게 이동")]
    public DragAxis lockAxis = DragAxis.None;

    [Tooltip("드래그 평면의 법선 방향 (카메라 기준 자동 설정 권장)")]
    public bool autoDragPlane = true;

    public enum DragAxis { None, X, Y, Z }

    private Camera _cam;
    private bool   _dragging;
    private float  _dragDist;
    private Plane  _dragPlane;
    private Vector3 _lockedPos;

    void Start()
    {
        _cam = Camera.main;
    }

    void OnMouseDown()
    {
        _dragging = true;
        _dragDist = Vector3.Distance(_cam.transform.position, transform.position);

        if (autoDragPlane)
        {
            // 카메라가 바라보는 방향에 수직인 평면을 드래그 기준으로 사용
            _dragPlane = new Plane(-_cam.transform.forward, transform.position);
        }

        _lockedPos = transform.position;
    }

    void OnMouseUp()
    {
        _dragging = false;
    }

    void OnMouseDrag()
    {
        if (!_dragging) return;

        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos;

        if (autoDragPlane)
        {
            if (_dragPlane.Raycast(ray, out float enter))
                targetPos = ray.GetPoint(enter);
            else
                return;
        }
        else
        {
            targetPos = ray.GetPoint(_dragDist);
        }

        // 축 고정 처리
        switch (lockAxis)
        {
            case DragAxis.X: targetPos.x = _lockedPos.x; break;
            case DragAxis.Y: targetPos.y = _lockedPos.y; break;
            case DragAxis.Z: targetPos.z = _lockedPos.z; break;
        }

        transform.position = targetPos;
    }

    // 에디터에서 포인트 위치를 기즈모로 표시
    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, 0.12f);
    }
}
