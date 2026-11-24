using UnityEngine;

/// <summary>
/// 3D玩家的胶囊体检测：脚底判定 + 墙体判定
/// 胶囊尺寸统一取黑板里的数值，和2D探测用的是同一份，两边判定口径一致
/// </summary>
public static class PlayerProbe3D
{
    // 探针球心相对角色底部抬起的量：抬一点，避免球体一开始就贴面/重叠导致漏检
    private const float ProbeLift = 0.02f;

    /// <summary>
    /// 胶囊中心：3D玩家的原点就是胶囊中心（Awake里会把控制器的center归零）
    /// </summary>
    public static Vector3 GetCenter(PlayerBlackboard blackboard)
    {
        if (blackboard.player3D == null)
        {
            return Vector3.zero;
        }

        Vector3 center = blackboard.controller3D != null ? blackboard.controller3D.center : Vector3.zero;
        return blackboard.player3D.transform.position + center;
    }

    /// <summary>
    /// 胶囊底部中心
    /// </summary>
    public static Vector3 GetBottom(PlayerBlackboard blackboard)
    {
        return GetCenter(blackboard) + Vector3.down * (blackboard.capsuleHeight * 0.5f);
    }

    /// <summary>
    /// 向下的胶囊体判定：胶囊底端正好贴着角色底部，向下探 groundCheckGap
    /// 踩到不超过 slopeLimit 的斜面才算落地，太陡的面交给掉落状态
    /// 自己身上的碰撞体一律忽略
    /// </summary>
    public static bool GroundCheck(PlayerBlackboard blackboard)
    {
        if (blackboard.player3D == null)
        {
            return false;
        }

        float radius = blackboard.capsuleRadius;
        // 球心抬到角色底部上方，胶囊底端就在脚下
        Vector3 feet = GetBottom(blackboard) + Vector3.up * (radius + ProbeLift);
        float distance = blackboard.groundCheckGap + ProbeLift;

        // 取全部命中再筛：最近的命中可能是自己身上的碰撞体，必须跳过再找真正的脚下地面
        RaycastHit[] hits = Physics.CapsuleCastAll(feet, feet, radius, Vector3.down, distance, blackboard.groundLayer, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        bool grounded = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i].collider))
            {
                continue;
            }

            if (Vector3.Angle(hits[i].normal, Vector3.up) > blackboard.slopeLimit)
            {
                continue;
            }

            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                grounded = true;
            }
        }

        return grounded;
    }

    /// <summary>
    /// 向移动方向做膨胀胶囊检测：胶囊半径膨胀 wallCheckSkin，还在贴住墙之前就判出来
    /// 撞到墙返回true，调用方据此拒绝这次移动
    /// </summary>
    public static bool WallCheck(PlayerBlackboard blackboard, Vector3 direction, float distance)
    {
        if (blackboard.player3D == null || direction == Vector3.zero)
        {
            return false;
        }

        float radius = blackboard.capsuleRadius + blackboard.wallCheckSkin;
        float half = Mathf.Max(0f, blackboard.capsuleHeight * 0.5f - blackboard.capsuleRadius);
        Vector3 center = GetCenter(blackboard);
        Vector3 pointTop = center + Vector3.up * half;
        Vector3 pointBottom = center - Vector3.up * half;

        // 取全部命中再筛：只取最近的那个，会被脚下的地面/坡面抢先命中，把真正的墙挡掉
        RaycastHit[] hits = Physics.CapsuleCastAll(pointTop, pointBottom, radius, direction, distance + blackboard.wallCheckSkin, blackboard.wallLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i].collider))
            {
                continue;
            }

            if (IsWall(blackboard, hits[i].normal, direction))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 3D位移：全部交给CharacterController，坡面、台阶、碰撞都由它解决
    /// </summary>
    public static void Move(PlayerBlackboard blackboard, Vector3 motion)
    {
        if (blackboard.controller3D == null)
        {
            return;
        }

        blackboard.controller3D.Move(motion);
    }

    // 挡路的墙：法线要迎着移动方向，而且够竖直
    // 只看法线竖直程度是不够的：起始就贴着的接触（脚下的地面）会给出零法线，也会被误判成墙
    // 另外法线朝下的面（斜坡低端那个端盖就是）也不是墙，不然走上斜坡时会撞在坡脚上过不去
    private static bool IsWall(PlayerBlackboard blackboard, Vector3 normal, Vector3 direction)
    {
        if (normal.y < -0.05f)
        {
            return false;
        }

        if (Vector3.Dot(normal, direction) > -0.01f)
        {
            return false;
        }

        return Mathf.Abs(normal.y) < Mathf.Cos(blackboard.slopeLimit * Mathf.Deg2Rad);
    }

    // 自己身上的碰撞体（模型、玩家自己的Collider、胶囊）不参与检测
    private static bool IsSelf(PlayerBlackboard blackboard, Collider collider)
    {
        if (collider == null || blackboard.player3D == null)
        {
            return false;
        }

        Transform body = blackboard.player3D.transform;
        return collider.transform == body || collider.transform.IsChildOf(body);
    }
}

/// <summary>
/// 2D玩家的胶囊体检测：脚底判定 + 墙体判定 + 竖直贴面移动
/// 2D玩家没有刚体，位移全部手算，所以向下移动要先探面再走，不然会穿地
/// </summary>
public static class PlayerProbe2D
{
    // 探针球心相对角色底部抬起的量：抬一点，避免球体一开始就贴面/重叠导致漏检
    private const float ProbeLift = 0.02f;

    /// <summary>
    /// 胶囊中心：2D玩家的原点就是胶囊中心，和3D玩家对齐
    /// </summary>
    public static Vector2 GetCenter(PlayerBlackboard blackboard)
    {
        if (blackboard.player2D == null)
        {
            return Vector2.zero;
        }

        Vector3 position = blackboard.player2D.transform.position;
        return new Vector2(position.x, position.y);
    }

    /// <summary>
    /// 胶囊底部中心
    /// </summary>
    public static Vector2 GetBottom(PlayerBlackboard blackboard)
    {
        return GetCenter(blackboard) + Vector2.down * (blackboard.capsuleHeight * 0.5f);
    }

    /// <summary>
    /// 胶囊顶部中心
    /// </summary>
    public static Vector2 GetTop(PlayerBlackboard blackboard)
    {
        return GetCenter(blackboard) + Vector2.up * (blackboard.capsuleHeight * 0.5f);
    }

    /// <summary>
    /// 向下的胶囊体判定：胶囊底端正好贴着角色底部，向下探 groundCheckGap
    /// 结果连同地面法线一起写回黑板，斜坡移动要用这个法线
    /// 自己身上的碰撞体一律忽略
    /// </summary>
    public static bool GroundCheck(PlayerBlackboard blackboard)
    {
        float radius = blackboard.capsuleRadius;
        // 球心抬到角色底部上方，胶囊底端就在脚下（多抬高一点：万一已经扎进面里，从内部出发会打空）
        float extra = 0.1f;
        Vector2 feet = GetBottom(blackboard) + Vector2.up * (radius + ProbeLift + extra);
        Vector2 size = new Vector2(radius * 2f, radius * 2f);
        float distance = blackboard.groundCheckGap + ProbeLift + extra;

        // 取全部命中再筛：最近的命中可能是自己身上的碰撞体，必须跳过再找真正的脚下地面
        RaycastHit2D[] hits = Physics2D.CapsuleCastAll(feet, size, CapsuleDirection2D.Vertical, 0f, Vector2.down, distance, blackboard.groundLayer);
        float best = float.MaxValue;
        bool grounded = false;
        Vector2 normal = Vector2.up;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i].collider))
            {
                continue;
            }

            // 太陡的面不算地面，交给掉落状态
            if (Vector2.Angle(hits[i].normal, Vector2.up) > blackboard.slopeLimit)
            {
                continue;
            }

            if (hits[i].distance < best)
            {
                best = hits[i].distance;
                normal = hits[i].normal;
                grounded = true;
            }
        }

        if (grounded)
        {
            blackboard.groundNormal = normal;
            return true;
        }

        // 投射都打空了才说明是真的脚下一空；扎在面里时上面的抬高已经把命中救回来了
        return false;
    }

    /// <summary>
    /// 脚底那块是不是已经和面重叠了：从内部出发的投射会打空，靠这个判断"卡在里面"
    /// </summary>
    public static bool IsTouching(PlayerBlackboard blackboard)
    {
        float radius = blackboard.capsuleRadius;
        Vector2 feet = GetBottom(blackboard) + Vector2.up * (radius + ProbeLift);
        Collider2D[] hits = Physics2D.OverlapCircleAll(feet, radius, blackboard.groundLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i]))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// 向移动方向做膨胀胶囊检测：胶囊半径膨胀 wallCheckSkin，还在贴住墙之前就判出来
    /// 撞到墙返回true，调用方据此拒绝这次移动
    /// </summary>
    public static bool WallCheck(PlayerBlackboard blackboard, Vector2 direction, float distance)
    {
        if (blackboard.player2D == null || direction == Vector2.zero)
        {
            return false;
        }

        float radius = blackboard.capsuleRadius + blackboard.wallCheckSkin;
        float height = Mathf.Max(blackboard.capsuleHeight, radius * 2f);
        Vector2 size = new Vector2(radius * 2f, height);

        // 取全部命中再筛：只取最近的那个，会被脚下的地面/坡面抢先命中，把真正的墙挡掉
        RaycastHit2D[] hits = Physics2D.CapsuleCastAll(GetCenter(blackboard), size, CapsuleDirection2D.Vertical, 0f, direction, distance + blackboard.wallCheckSkin, blackboard.wallLayer);
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i].collider))
            {
                continue;
            }

            // 注意：这里不能跳过"距离0"的命中。斜坡是个整块碰撞体，人一直贴着坡面走，
            // 跳掉贴着的命中等于把整块斜坡从墙检测里摘掉，人就能从坡的另一头直接穿过去
            if (IsWall(blackboard, hits[i].normal, direction))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 2D竖直向下移动：先探脚下，碰到能站的坡面就停在面上
    /// 墙和陡坡的侧面不拦下落，否则走到地板边缘会被侧面卡住、永远掉不下去
    /// 返回是否踩到了地面，掉落状态靠它判断有没有着地
    /// </summary>
    public static bool MoveDown(PlayerBlackboard blackboard, float distance)
    {
        if (distance <= 0f)
        {
            return false;
        }

        // 取全部命中里最近的那个可站面：只取最近命中会被旁边墙/陡坡的侧面抢先命中，落下去就穿地了
        float gap = NearestWalkable(blackboard, Vector2.down, distance);

        if (gap < 0f)
        {
            // 已经扎在面里了就别再往下压，压下去只会越陷越深，人就卡在坡里出不来了
            if (IsTouching(blackboard))
            {
                return true;
            }

            Translate(blackboard, Vector2.down * distance);
            return false;
        }

        Translate(blackboard, Vector2.down * Mathf.Min(distance, gap));
        return gap <= distance;
    }

    /// <summary>
    /// 2D竖直向上移动：撞到头顶就停，返回是否撞上（撞上就把竖直速度清掉，交给掉落）
    /// 只有法线朝下的面算天花板，擦到侧面的墙不算撞头
    /// </summary>
    public static bool MoveUp(PlayerBlackboard blackboard, float distance)
    {
        if (distance <= 0f)
        {
            return false;
        }

        float gap = NearestCeiling(blackboard, distance);

        if (gap < 0f)
        {
            Translate(blackboard, Vector2.up * distance);
            return false;
        }

        Translate(blackboard, Vector2.up * Mathf.Min(distance, gap));
        return gap <= distance;
    }

    /// <summary>
    /// 2D空中水平移动：按黑板里继承下来的水平惯性走，先撞墙检测，撞上就把惯性清掉
    /// 衰减用黑板里的同一个公式，和3D跳出去的弧线一致
    /// </summary>
    public static void MoveAir(PlayerBlackboard blackboard)
    {
        blackboard.DecayAirVelocity();

        float speed = blackboard.horizontalVelocity.x;

        if (Mathf.Abs(speed) < 0.001f)
        {
            return;
        }

        Vector2 direction = new Vector2(Mathf.Sign(speed), 0f);
        float distance = Mathf.Abs(speed) * blackboard.deltaTime;

        if (WallCheck(blackboard, direction, distance))
        {
            blackboard.horizontalVelocity = Vector3.zero;
            return;
        }

        Translate(blackboard, new Vector2(speed * blackboard.deltaTime, 0f));
    }

    /// <summary>
    /// 2D位移：直接改transform
    /// </summary>
    public static void Translate(PlayerBlackboard blackboard, Vector2 delta)
    {
        if (blackboard.player2D == null)
        {
            return;
        }

        Vector3 position = blackboard.player2D.transform.position;
        blackboard.player2D.transform.position = new Vector3(position.x + delta.x, position.y + delta.y, position.z);
    }

    // 向下投射，返回最近的可站面离角色底部的距离；没有可站面返回-1
    private static float NearestWalkable(PlayerBlackboard blackboard, Vector2 direction, float distance)
    {
        float radius = blackboard.capsuleRadius;
        Vector2 feet = GetBottom(blackboard) + Vector2.up * (radius + ProbeLift);
        return NearestSurface(blackboard, feet, direction, distance, true);
    }

    // 向上投射，返回最近的头顶面离角色顶部的距离；没有返回-1
    private static float NearestCeiling(PlayerBlackboard blackboard, float distance)
    {
        float radius = blackboard.capsuleRadius;
        Vector2 head = GetTop(blackboard) - Vector2.up * (radius + ProbeLift);
        return NearestSurface(blackboard, head, Vector2.up, distance, false);
    }

    // 投射一次，取所有命中里最近的有效面距离（已扣掉探针抬升）；没有返回-1
    private static float NearestSurface(PlayerBlackboard blackboard, Vector2 origin, Vector2 direction, float distance, bool walkable)
    {
        float radius = blackboard.capsuleRadius;
        Vector2 size = new Vector2(radius * 2f, radius * 2f);
        RaycastHit2D[] hits = Physics2D.CapsuleCastAll(origin, size, CapsuleDirection2D.Vertical, 0f, direction, distance + ProbeLift, blackboard.groundLayer);

        float floor = Mathf.Cos(blackboard.slopeLimit * Mathf.Deg2Rad);
        float best = -1f;
        for (int i = 0; i < hits.Length; i++)
        {
            if (IsSelf(blackboard, hits[i].collider))
            {
                continue;
            }

            // 往下只认能站的坡面，往上只认法线朝下的天花板，侧面擦到的墙两边都不算
            bool valid = walkable
                ? Vector2.Angle(hits[i].normal, Vector2.up) <= blackboard.slopeLimit
                : hits[i].normal.y <= -floor;

            if (!valid)
            {
                continue;
            }

            float gap = Mathf.Max(0f, hits[i].distance - ProbeLift);
            if (best < 0f || gap < best)
            {
                best = gap;
            }
        }

        return best;
    }

    // 脚底胶囊向下投射
    private static RaycastHit2D FeetCast(PlayerBlackboard blackboard, float distance)
    {
        float radius = blackboard.capsuleRadius;
        Vector2 feet = GetBottom(blackboard) + Vector2.up * (radius + ProbeLift);
        Vector2 size = new Vector2(radius * 2f, radius * 2f);
        return Physics2D.CapsuleCast(feet, size, CapsuleDirection2D.Vertical, 0f, Vector2.down, distance, blackboard.groundLayer);
    }

    // 挡路的墙：法线要迎着移动方向，而且够竖直
    // 另外两种特殊情况：
    //  - 法线朝下的面（天花板/斜坡的底面）：胶囊都能撞上，说明这块地方人钻不过去，算墙（不然能从斜坡下面穿过去）
    //  - 其它斜朝下的面（斜坡低端那个端盖）不算墙，不然上坡会撞在坡脚上
    private static bool IsWall(PlayerBlackboard blackboard, Vector2 normal, Vector2 direction)
    {
        float cos = Mathf.Cos(blackboard.slopeLimit * Mathf.Deg2Rad);

        if (normal.y < -cos)
        {
            return true;
        }

        if (normal.y < -0.05f)
        {
            return false;
        }

        if (Vector2.Dot(normal, direction) > -0.01f)
        {
            return false;
        }

        return Mathf.Abs(normal.y) < cos;
    }

    // 自己身上的碰撞体（模型、玩家自己的Collider）不参与检测
    private static bool IsSelf(PlayerBlackboard blackboard, Collider2D collider)
    {
        if (collider == null || blackboard.player2D == null)
        {
            return false;
        }

        Transform body = blackboard.player2D.transform;
        return collider.transform == body || collider.transform.IsChildOf(body);
    }
}