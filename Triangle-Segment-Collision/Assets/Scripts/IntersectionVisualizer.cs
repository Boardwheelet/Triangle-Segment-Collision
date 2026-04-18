using UnityEngine;
using TMPro;

/// <summary>
/// 선분-삼각형 교차 판정 시각화 컨트롤러
/// Inspector에서 포인트 오브젝트들을 연결하면 실시간으로 결과를 보여줍니다.
/// </summary>
public class IntersectionVisualizer : MonoBehaviour
{
    [Header("── 삼각형 꼭짓점 ──")]
    public Transform pointA;
    public Transform pointB;
    public Transform pointC;

    [Header("── 선분 끝점 ──")]
    public Transform pointP;
    public Transform pointQ;

    [Header("── 시각화 오브젝트 ──")]
    public GameObject intersectionMarker;   // 교점 위치에 표시할 구
    public LineRenderer segmentLine;        // 선분 PQ 렌더링
    public LineRenderer triangleOutline;    // 삼각형 테두리 렌더링

    [Header("── UI ──")]
    public TextMeshProUGUI resultText;      // 판정 결과 텍스트
    public TextMeshProUGUI coordText;       // 좌표 정보 텍스트

    // 삼각형 면 (반투명 메시)
    private Mesh _triMesh;
    private MeshFilter _triFilter;
    private MeshRenderer _triRenderer;

    // 결과별 색상
    private static readonly Color ColIntersects      = new Color(0.2f, 0.9f, 0.3f);
    private static readonly Color ColParallel        = new Color(0.9f, 0.3f, 0.3f);
    private static readonly Color ColOutOfSegment    = new Color(1.0f, 0.8f, 0.1f);
    private static readonly Color ColOutsideTriangle = new Color(1.0f, 0.6f, 0.1f);
    private static readonly Color ColCoplanar        = new Color(0.3f, 0.8f, 1.0f);

    void Start()
    {
        BuildTriangleMesh();

        // LineRenderer 기본 설정
        SetupLine(segmentLine, Color.red, 0.05f);
        SetupLine(triangleOutline, new Color(0.3f, 0.6f, 1f), 0.04f);
        triangleOutline.positionCount = 4; // 삼각형 3변 + 닫힘

        if (intersectionMarker != null)
            intersectionMarker.SetActive(false);
    }

    void Update()
    {
        if (!ValidateRefs()) return;

        Vector3 A = pointA.position, B = pointB.position, C = pointC.position;
        Vector3 P = pointP.position, Q = pointQ.position;

        // 삼각형 면 & 테두리 업데이트
        UpdateTriangleMesh(A, B, C);
        UpdateTriangleOutline(A, B, C);

        // 선분 업데이트
        segmentLine.SetPosition(0, P);
        segmentLine.SetPosition(1, Q);

        // ── 교차 판정 실행 ──
        var result = SegmentTriangleIntersector.Check(
            A, B, C, P, Q,
            out Vector3 hitPoint, out float t,
            out float alpha, out float beta, out float gamma);

        // 결과 시각화
        ApplyResult(result, hitPoint, t, alpha, beta, gamma);

        // 좌표 정보 업데이트
        UpdateCoordText(A, B, C, P, Q);
    }

    // ── 결과 적용 ──────────────────────────────────────────────────────────────
    private SegmentTriangleIntersector.Result _lastResult = (SegmentTriangleIntersector.Result)(-1);

    void ApplyResult(SegmentTriangleIntersector.Result result,
        Vector3 hitPoint, float t, float alpha, float beta, float gamma)
    {
        string uiTitle = "", uiDetail = "";
        Color col = Color.white;

        switch (result)
        {
            case SegmentTriangleIntersector.Result.Intersects:
                ShowMarker(hitPoint, ColIntersects);
                uiTitle  = "✅  교차함!";
                uiDetail = $"교점: ({hitPoint.x:F3}, {hitPoint.y:F3}, {hitPoint.z:F3})\n"
                          + $"t = {t:F4}\n"
                          + $"α={alpha:F3}  β={beta:F3}  γ={gamma:F3}";
                col = ColIntersects;
                break;

            case SegmentTriangleIntersector.Result.Parallel:
                HideMarker();
                uiTitle  = "❌  평행 — 교점 없음";
                uiDetail = "선분과 삼각형 평면이 평행합니다.\n(법선벡터 n · d ≈ 0)";
                col = ColParallel;
                break;

            case SegmentTriangleIntersector.Result.Coplanar:
                HideMarker();
                uiTitle  = "⚠️  동일 평면 — 교점 무한";
                uiDetail = "선분이 삼각형과 같은 평면 위에 있습니다.";
                col = ColCoplanar;
                break;

            case SegmentTriangleIntersector.Result.OutOfSegment:
                HideMarker();
                uiTitle  = "❌  선분 범위 밖";
                uiDetail = $"직선은 평면과 만나지만\nt = {t:F4}  (0~1 범위 벗어남)";
                col = ColOutOfSegment;
                break;

            case SegmentTriangleIntersector.Result.OutsideTriangle:
                HideMarker();
                uiTitle  = "❌  삼각형 밖";
                uiDetail = $"교점이 삼각형 외부에 있습니다.\nα={alpha:F3}  β={beta:F3}  γ={gamma:F3}";
                col = ColOutsideTriangle;
                break;
        }

        SetResultText(uiTitle, uiDetail, col);

        // 결과가 바뀔 때만 Console에 출력 (매 프레임 도배 방지)
        if (result != _lastResult)
        {
            Debug.Log($"[교차 판정] {uiTitle}\n{uiDetail}");
            _lastResult = result;
        }
    }

    // ── 헬퍼 함수들 ─────────────────────────────────────────────────────────────
    void ShowMarker(Vector3 pos, Color col)
    {
        if (intersectionMarker == null) return;
        intersectionMarker.SetActive(true);
        intersectionMarker.transform.position = pos;
        var rend = intersectionMarker.GetComponent<Renderer>();
        if (rend != null) rend.material.color = col;
    }

    void HideMarker()
    {
        if (intersectionMarker != null)
            intersectionMarker.SetActive(false);
    }

    void SetResultText(string title, string detail, Color col)
    {
        if (resultText != null)
        {
            resultText.text = title;
            resultText.color = col;
        }
        if (coordText != null)
        {
            coordText.text = detail;
        }
    }

    void UpdateCoordText(Vector3 A, Vector3 B, Vector3 C, Vector3 P, Vector3 Q)
    {
        // 좌표 패널이 별도로 있으면 여기서 갱신 (선택사항)
    }

    void BuildTriangleMesh()
    {
        var go = new GameObject("TriangleFace");
        _triFilter   = go.AddComponent<MeshFilter>();
        _triRenderer = go.AddComponent<MeshRenderer>();

        var mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.3f, 0.6f, 1f, 0.25f);

        // 반투명 모드 설정
        mat.SetFloat("_Mode", 3);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.renderQueue = 3000;

        _triRenderer.material = mat;

        _triMesh = new Mesh { name = "TriMesh" };
        _triFilter.mesh = _triMesh;
    }

    void UpdateTriangleMesh(Vector3 A, Vector3 B, Vector3 C)
    {
        _triMesh.vertices  = new[] { A, B, C };
        _triMesh.triangles = new[] { 0, 1, 2, 0, 2, 1 }; // 양면 렌더링
        _triMesh.RecalculateNormals();
    }

    void UpdateTriangleOutline(Vector3 A, Vector3 B, Vector3 C)
    {
        triangleOutline.SetPosition(0, A);
        triangleOutline.SetPosition(1, B);
        triangleOutline.SetPosition(2, C);
        triangleOutline.SetPosition(3, A); // 닫힘
    }

    void SetupLine(LineRenderer lr, Color col, float width)
    {
        if (lr == null) return;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = lr.endColor = col;
        lr.useWorldSpace = true;
    }

    bool ValidateRefs()
    {
        return pointA != null && pointB != null && pointC != null
            && pointP != null && pointQ != null;
    }
}
