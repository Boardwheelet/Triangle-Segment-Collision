using UnityEngine;

/// <summary>
/// 3차원 선분-삼각형 교차 판정 알고리즘
/// 법선벡터 → t값 계산 → Barycentric Coordinate 내부 판정
/// (Python/C++ 구현과 동일한 수학적 흐름)
/// </summary>
public static class SegmentTriangleIntersector
{
    private const float Epsilon = 1e-6f;

    public enum Result
    {
        Intersects,       // ✅ 교차함
        Parallel,         // ❌ 평행 (다른 평면)
        Coplanar,         // ⚠️ 동일 평면 (교점 무한)
        OutOfSegment,     // ❌ 선분 범위 밖 (t < 0 or t > 1)
        OutsideTriangle   // ❌ 삼각형 밖
    }

    /// <summary>
    /// 선분 PQ 와 삼각형 ABC 의 교차를 판정합니다.
    /// </summary>
    /// <param name="A">삼각형 꼭짓점 A</param>
    /// <param name="B">삼각형 꼭짓점 B</param>
    /// <param name="C">삼각형 꼭짓점 C</param>
    /// <param name="P">선분 시작점</param>
    /// <param name="Q">선분 끝점</param>
    /// <param name="hitPoint">교점 좌표 (교차 없을 때는 평면과의 교점 후보)</param>
    /// <param name="t">매개변수 t (0~1 범위면 선분 위)</param>
    /// <param name="alpha">Barycentric 좌표 α</param>
    /// <param name="beta">Barycentric 좌표 β</param>
    /// <param name="gamma">Barycentric 좌표 γ = 1 - α - β</param>
    public static Result Check(
        Vector3 A, Vector3 B, Vector3 C,
        Vector3 P, Vector3 Q,
        out Vector3 hitPoint,
        out float t,
        out float alpha, out float beta, out float gamma)
    {
        hitPoint = Vector3.zero;
        t = 0f;
        alpha = beta = gamma = 0f;

        // ── STEP 1: 삼각형 변벡터 & 법선벡터 계산 ──
        // u = B - A,  v = C - A
        // n = u × v  (삼각형 평면에 수직인 법선벡터)
        Vector3 u = B - A;
        Vector3 v = C - A;
        Vector3 n = Vector3.Cross(u, v);

        // ── STEP 2: 선분 방향벡터 & 평행 판정 ──
        // d = Q - P
        // denom = n · d
        // denom ≈ 0  →  선분이 평면과 평행
        Vector3 d = Q - P;
        float denom = Vector3.Dot(n, d);

        if (Mathf.Abs(denom) < Epsilon)
        {
            // 선분이 삼각형 평면 위에 있으면 Coplanar, 아니면 Parallel
            bool onPlane = Mathf.Abs(Vector3.Dot(n, P - A)) < Epsilon;
            return onPlane ? Result.Coplanar : Result.Parallel;
        }

        // ── STEP 3: 매개변수 t 계산 → 교점 후보 구하기 ──
        // t = n · (A - P) / denom
        // 교점 후보 = P + t * d
        t = Vector3.Dot(n, A - P) / denom;
        Vector3 point = P + t * d;
        hitPoint = point;

        // ── STEP 4: 선분 범위 판정 (0 ≤ t ≤ 1) ──
        if (t < 0f || t > 1f)
            return Result.OutOfSegment;

        // ── STEP 5: Barycentric Coordinate 내부 판정 ──
        // w = point - A
        // α, β, γ 를 계산해서 셋 다 ≥ 0 이면 삼각형 내부
        Vector3 w = point - A;

        float uu = Vector3.Dot(u, u);
        float uv = Vector3.Dot(u, v);
        float vv = Vector3.Dot(v, v);
        float wu = Vector3.Dot(w, u);
        float wv = Vector3.Dot(w, v);

        float denomBary = uu * vv - uv * uv;

        alpha = (vv * wu - uv * wv) / denomBary;
        beta  = (uu * wv - uv * wu) / denomBary;
        gamma = 1f - alpha - beta;

        if (alpha >= 0f && beta >= 0f && gamma >= 0f)
            return Result.Intersects;
        else
            return Result.OutsideTriangle;
    }
}
