// LevelGizmos.cs
// 白盒期用的辅助线：把 X = 0 的“投影平面”、0.5 m 网格、跳跃高度画出来。
// 只在 Scene 视图可见，不影响打包。

using UnityEngine;

public class LevelGizmos : MonoBehaviour
{
    public bool drawCenterPlane = true;
    public Vector3 planeCenter = new Vector3(0f, 5f, 24f);
    public Vector2 planeSize = new Vector2(48f, 10f); // Z, Y

    public bool drawDepthLines = true;
    public float depthLineStep = 4f;

    public bool drawJumpGuide = true;
    public float jumpHeight = 1.6f;
    public float doubleJumpHeight = 3.0f;

    private void OnDrawGizmos()
    {
        if (drawCenterPlane)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.5f);
            Vector3 a = planeCenter + new Vector3(0f, -planeSize.y * 0.5f, -planeSize.x * 0.5f);
            Vector3 b = planeCenter + new Vector3(0f, -planeSize.y * 0.5f, planeSize.x * 0.5f);
            Vector3 c = planeCenter + new Vector3(0f, planeSize.y * 0.5f, planeSize.x * 0.5f);
            Vector3 d = planeCenter + new Vector3(0f, planeSize.y * 0.5f, -planeSize.x * 0.5f);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
        }

        if (drawDepthLines)
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.18f);
            float half = planeSize.x * 0.5f;
            for (float z = -half; z <= half; z += depthLineStep)
            {
                Vector3 p = planeCenter + new Vector3(0f, 0f, z);
                Gizmos.DrawLine(p + Vector3.down * planeSize.y * 0.5f, p + Vector3.up * planeSize.y * 0.5f);
            }
        }

        if (drawJumpGuide)
        {
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.7f);
            Vector3 origin = transform.position;
            Gizmos.DrawLine(origin, origin + Vector3.up * jumpHeight);
            Gizmos.color = new Color(0.3f, 1f, 0.7f, 0.5f);
            Gizmos.DrawLine(origin, origin + Vector3.up * doubleJumpHeight);
        }
    }
}
