using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 에디터 메뉴에서 씬 전체를 자동으로 구성해주는 도구.
/// 메뉴: Tools → Setup Intersection Scene
/// </summary>
public static class SceneSetupTool
{
    [MenuItem("Tools/Setup Intersection Scene")]
    public static void SetupScene()
    {
        // ── 기존 오브젝트 정리 ──
        DestroyIfExists("Point_A"); DestroyIfExists("Point_B"); DestroyIfExists("Point_C");
        DestroyIfExists("Point_P"); DestroyIfExists("Point_Q");
        DestroyIfExists("IntersectionMarker");
        DestroyIfExists("SegmentLine");
        DestroyIfExists("TriangleOutline");
        DestroyIfExists("Visualizer");
        DestroyIfExists("Canvas");

        // ── 1. 삼각형 꼭짓점 생성 ──
        var matA = MakeMat(new Color(0.4f, 0.5f, 1.0f)); // 파란계열
        var matB = MakeMat(new Color(0.3f, 0.85f, 0.5f));
        var matC = MakeMat(new Color(1.0f, 0.5f, 0.3f));

        Transform tA = MakePoint("Point_A", new Vector3( 2f, 0f, 0f), matA);
        Transform tB = MakePoint("Point_B", new Vector3(-1f, 2f, 0f), matB);
        Transform tC = MakePoint("Point_C", new Vector3(-1f,-1f, 2f), matC);

        // ── 2. 선분 끝점 생성 ──
        var matP = MakeMat(new Color(0.9f, 0.3f, 0.3f));
        var matQ = MakeMat(new Color(1.0f, 0.7f, 0.1f));

        Transform tP = MakePoint("Point_P", new Vector3(-2f, 1f, 1f), matP);
        Transform tQ = MakePoint("Point_Q", new Vector3( 3f, 0f, 0f), matQ);

        // ── 3. 교점 마커 ──
        var markerMat = MakeMat(Color.green);
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.name = "IntersectionMarker";
        marker.transform.localScale = Vector3.one * 0.22f;
        marker.GetComponent<Renderer>().material = markerMat;
        marker.SetActive(false);

        // ── 4. 선분 LineRenderer ──
        var segGo = new GameObject("SegmentLine");
        var segLR = segGo.AddComponent<LineRenderer>();
        segLR.positionCount = 2;
        segLR.startWidth = segLR.endWidth = 0.05f;
        segLR.material = new Material(Shader.Find("Sprites/Default"));
        segLR.startColor = segLR.endColor = Color.red;
        segLR.useWorldSpace = true;

        // ── 5. 삼각형 테두리 LineRenderer ──
        var triGo = new GameObject("TriangleOutline");
        var triLR = triGo.AddComponent<LineRenderer>();
        triLR.positionCount = 4;
        triLR.startWidth = triLR.endWidth = 0.04f;
        triLR.material = new Material(Shader.Find("Sprites/Default"));
        triLR.startColor = triLR.endColor = new Color(0.3f, 0.6f, 1f);
        triLR.useWorldSpace = true;
        triLR.loop = false;

        // ── 6. Canvas & TextMeshPro UI ──
        var canvas = new GameObject("Canvas");
        var canvasComp = canvas.AddComponent<Canvas>();
        canvasComp.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.AddComponent<UnityEngine.UI.CanvasScaler>();
        canvas.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        var resultTextGo  = CreateTMPText(canvas, "ResultText",
            new Vector2(-180, -40), new Vector2(360, 60), 22, TextAlignmentOptions.Left);
        var detailTextGo  = CreateTMPText(canvas, "DetailText",
            new Vector2(-180, -130), new Vector2(360, 120), 16, TextAlignmentOptions.Left);

        // ── 7. Visualizer 오브젝트에 컴포넌트 연결 ──
        var vizGo = new GameObject("Visualizer");
        var viz = vizGo.AddComponent<IntersectionVisualizer>();

        viz.pointA = tA; viz.pointB = tB; viz.pointC = tC;
        viz.pointP = tP; viz.pointQ = tQ;
        viz.intersectionMarker  = marker;
        viz.segmentLine         = segLR;
        viz.triangleOutline     = triLR;
        viz.resultText          = resultTextGo.GetComponent<TMPro.TextMeshProUGUI>();
        viz.coordText           = detailTextGo.GetComponent<TMPro.TextMeshProUGUI>();

        // ── 8. 카메라 위치 조정 ──
        var cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(3f, 3f, -6f);
            cam.transform.LookAt(Vector3.zero);
        }

        // ── 9. DraggablePoint 자동 부착 ──
        foreach (Transform pt in new[] { tA, tB, tC, tP, tQ })
            pt.gameObject.AddComponent<DraggablePoint>();

        Debug.Log("✅ Intersection Scene 자동 구성 완료!");
        Selection.activeGameObject = vizGo;
    }

    // ── 헬퍼 ──────────────────────────────────────────────────────────────────
    static Transform MakePoint(string name, Vector3 pos, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.18f;
        go.GetComponent<Renderer>().material = mat;
        return go.transform;
    }

    static Material MakeMat(Color col)
    {
        var mat = new Material(Shader.Find("Standard"));
        mat.color = col;
        return mat;
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static GameObject CreateTMPText(GameObject parent, string name,
        Vector2 anchoredPos, Vector2 size, int fontSize, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.text = "";

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(20, anchoredPos.y);
        rect.sizeDelta = size;

        return go;
    }
}
#endif