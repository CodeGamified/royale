// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using UnityEngine;

namespace Royale.Game
{
    /// <summary>
    /// The battle royale arena — 200×200 flat top-down map with buildings,
    /// obstacles (rocks, trees), and loot crates.
    /// </summary>
    public class RoyaleArena : MonoBehaviour
    {
        public float MapSize { get; private set; } = 200f;
        public float HalfMap => MapSize / 2f;

        // Buildings — axis-aligned rectangles with doorways
        public List<Building> Buildings { get; } = new List<Building>();

        // Obstacles
        public List<Vector3> Rocks { get; } = new List<Vector3>();   // x, z, radius
        public List<Vector2> Trees { get; } = new List<Vector2>();   // x, z

        // Crates
        public List<RoyaleCrate> Crates { get; } = new List<RoyaleCrate>();

        public void Initialize(float mapSize)
        {
            MapSize = mapSize;
            GenerateBuildings();
            GenerateObstacles();
            BuildGroundPlane();
        }

        // ── Buildings ──

        public struct Building
        {
            public float MinX, MaxX, MinZ, MaxZ;
            public float DoorX, DoorZ, DoorWidth;

            public bool BlocksPoint(float x, float z)
            {
                if (x < MinX || x > MaxX || z < MinZ || z > MaxZ)
                    return false;
                // Doorway check
                float dxD = Mathf.Abs(x - DoorX);
                float dzD = Mathf.Abs(z - DoorZ);
                if (dxD < DoorWidth && dzD < DoorWidth)
                    return false;
                return true;
            }

            public bool BlocksCircle(float cx, float cz, float radius)
            {
                // If center is near doorway, allow through
                float dxD = Mathf.Abs(cx - DoorX);
                float dzD = Mathf.Abs(cz - DoorZ);
                if (dxD < DoorWidth + radius && dzD < DoorWidth + radius)
                    return false;

                float closestX = Mathf.Clamp(cx, MinX, MaxX);
                float closestZ = Mathf.Clamp(cz, MinZ, MaxZ);
                float dx = cx - closestX;
                float dz = cz - closestZ;
                return (dx * dx + dz * dz) <= radius * radius;
            }

            public Vector2 Eject(float cx, float cz, float radius)
            {
                float closestX = Mathf.Clamp(cx, MinX, MaxX);
                float closestZ = Mathf.Clamp(cz, MinZ, MaxZ);
                float dx = cx - closestX;
                float dz = cz - closestZ;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist < 1e-6f)
                {
                    float pushLeft  = cx - MinX;
                    float pushRight = MaxX - cx;
                    float pushDown  = cz - MinZ;
                    float pushUp    = MaxZ - cz;
                    float min = Mathf.Min(pushLeft, Mathf.Min(pushRight,
                                Mathf.Min(pushDown, pushUp)));
                    if (min == pushLeft)  return new Vector2(MinX - radius, cz);
                    if (min == pushRight) return new Vector2(MaxX + radius, cz);
                    if (min == pushDown)  return new Vector2(cx, MinZ - radius);
                    return new Vector2(cx, MaxZ + radius);
                }

                float pen = radius - dist;
                if (pen <= 0f) return new Vector2(cx, cz);
                float nx = dx / dist;
                float nz = dz / dist;
                return new Vector2(cx + nx * pen, cz + nz * pen);
            }
        }

        private void GenerateBuildings()
        {
            Buildings.Clear();
            float half = HalfMap;
            int count = 20;

            for (int i = 0; i < count; i++)
            {
                float w = Random.Range(4f, 10f);
                float h = Random.Range(4f, 10f);
                float cx = Random.Range(-half + w, half - w);
                float cz = Random.Range(-half + h, half - h);

                // Door on a random wall side
                float doorX, doorZ;
                float doorWidth = 1.5f;
                int side = Random.Range(0, 4);
                switch (side)
                {
                    case 0: doorX = cx; doorZ = cz + h / 2f; break;       // top
                    case 1: doorX = cx; doorZ = cz - h / 2f; break;       // bottom
                    case 2: doorX = cx - w / 2f; doorZ = cz; break;       // left
                    default: doorX = cx + w / 2f; doorZ = cz; break;      // right
                }

                Buildings.Add(new Building
                {
                    MinX = cx - w / 2f,
                    MaxX = cx + w / 2f,
                    MinZ = cz - h / 2f,
                    MaxZ = cz + h / 2f,
                    DoorX = doorX,
                    DoorZ = doorZ,
                    DoorWidth = doorWidth,
                });
            }
        }

        private void GenerateObstacles()
        {
            Rocks.Clear();
            Trees.Clear();
            float half = HalfMap;

            // ~40 rocks
            for (int i = 0; i < 40; i++)
            {
                float x = Random.Range(-half + 2f, half - 2f);
                float z = Random.Range(-half + 2f, half - 2f);
                float r = Random.Range(0.5f, 1.5f);
                Rocks.Add(new Vector3(x, z, r));
            }

            // ~30 trees
            for (int i = 0; i < 30; i++)
            {
                float x = Random.Range(-half + 2f, half - 2f);
                float z = Random.Range(-half + 2f, half - 2f);
                Trees.Add(new Vector2(x, z));
            }
        }

        // ── Collision helpers ──

        /// <summary>Clamp position to map bounds and resolve collisions with buildings/rocks.</summary>
        public Vector2 ResolveCollision(float x, float z, float radius)
        {
            // Map bounds
            float limit = HalfMap - radius;
            x = Mathf.Clamp(x, -limit, limit);
            z = Mathf.Clamp(z, -limit, limit);

            // Building collision
            foreach (var bld in Buildings)
            {
                if (bld.BlocksCircle(x, z, radius))
                {
                    var ejected = bld.Eject(x, z, radius);
                    x = ejected.x;
                    z = ejected.y;
                }
            }

            // Rock collision
            foreach (var rock in Rocks)
            {
                float dx = x - rock.x;
                float dz = z - rock.y;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                float minDist = rock.z + radius;
                if (dist < minDist && dist > 0.001f)
                {
                    float pen = minDist - dist;
                    x += (dx / dist) * pen;
                    z += (dz / dist) * pen;
                }
            }

            return new Vector2(x, z);
        }

        /// <summary>Can a point see another point? Blocked by buildings and rocks.
        /// Trees have 50% block chance.</summary>
        public bool HasLineOfSight(float fromX, float fromZ, float toX, float toZ)
        {
            // Buildings
            foreach (var bld in Buildings)
            {
                if (SegmentIntersectsAABB(fromX, fromZ, toX, toZ,
                    bld.MinX, bld.MinZ, bld.MaxX, bld.MaxZ))
                    return false;
            }

            // Rocks
            foreach (var rock in Rocks)
            {
                if (SegmentIntersectsCircle(fromX, fromZ, toX, toZ,
                    rock.x, rock.y, rock.z))
                    return false;
            }

            return true;
        }

        // ── Line segment geometry ──

        private static bool SegmentIntersectsAABB(float ax, float az, float bx, float bz,
                                                   float minX, float minZ, float maxX, float maxZ)
        {
            float dx = bx - ax;
            float dz = bz - az;
            float tMin = 0f, tMax = 1f;

            float[] p = { -dx, dx, -dz, dz };
            float[] q = { ax - minX, maxX - ax, az - minZ, maxZ - az };

            for (int i = 0; i < 4; i++)
            {
                if (Mathf.Abs(p[i]) < 1e-8f)
                {
                    if (q[i] < 0f) return false;
                }
                else
                {
                    float t = q[i] / p[i];
                    if (p[i] < 0f)
                        tMin = Mathf.Max(tMin, t);
                    else
                        tMax = Mathf.Min(tMax, t);
                    if (tMin > tMax) return false;
                }
            }
            return true;
        }

        private static bool SegmentIntersectsCircle(float ax, float az, float bx, float bz,
                                                     float cx, float cz, float r)
        {
            float dx = bx - ax;
            float dz = bz - az;
            float fx = ax - cx;
            float fz = az - cz;

            float a = dx * dx + dz * dz;
            float b = 2f * (fx * dx + fz * dz);
            float c = fx * fx + fz * fz - r * r;

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return false;

            disc = Mathf.Sqrt(disc);
            float t1 = (-b - disc) / (2f * a);
            float t2 = (-b + disc) / (2f * a);

            return (t1 >= 0f && t1 <= 1f) || (t2 >= 0f && t2 <= 1f) ||
                   (t1 < 0f && t2 > 1f);
        }

        // ── Visual ground ──

        private void BuildGroundPlane()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ground.name = "Ground";
            ground.transform.SetParent(transform, false);
            ground.transform.localPosition = Vector3.zero;
            ground.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ground.transform.localScale = new Vector3(MapSize, MapSize, 1f);

            var col = ground.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var rend = ground.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                    ?? Shader.Find("Sprites/Default"));
                mat.color = new Color(0.12f, 0.15f, 0.08f);
                rend.material = mat;
            }
        }
    }
}
