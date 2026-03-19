// Copyright CodeGamified 2025-2026
// MIT License — Royale
using System.Collections.Generic;
using UnityEngine;

namespace Royale.Game
{
    /// <summary>
    /// Top-down renderer — builds 3D visuals for arena elements.
    /// Players are colored circles, buildings are grey cubes, zone is a ring, etc.
    /// </summary>
    public class RoyaleRenderer : MonoBehaviour
    {
        private RoyaleArena _arena;
        private RoyaleZone _zone;
        private RoyaleMatchManager _match;

        // Visual roots
        private readonly List<GameObject> _buildingVisuals = new List<GameObject>();
        private readonly List<GameObject> _rockVisuals = new List<GameObject>();
        private readonly List<GameObject> _treeVisuals = new List<GameObject>();
        private readonly Dictionary<int, GameObject> _playerVisuals = new Dictionary<int, GameObject>();
        private readonly Dictionary<int, GameObject> _playerAimLines = new Dictionary<int, GameObject>();
        private GameObject _zoneRingOuter;
        private LineRenderer _zoneLineRenderer;

        // Materials
        private Material _playerMat;
        private Material _enemyMat;
        private Material _deadMat;

        public void Initialize(RoyaleArena arena, RoyaleZone zone, RoyaleMatchManager match)
        {
            _arena = arena;
            _zone = zone;
            _match = match;

            CreateMaterials();
            BuildStaticVisuals();
            BuildPlayerVisuals();
            BuildZoneVisual();
        }

        private void CreateMaterials()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");

            _playerMat = new Material(shader);
            _playerMat.color = new Color(0.2f, 1.0f, 0.3f);
            _playerMat.EnableKeyword("_EMISSION");
            _playerMat.SetColor("_EmissionColor", new Color(0.2f, 1.0f, 0.3f) * 0.5f);

            _enemyMat = new Material(shader);
            _enemyMat.color = new Color(1.0f, 0.4f, 0.2f);
            _enemyMat.EnableKeyword("_EMISSION");
            _enemyMat.SetColor("_EmissionColor", new Color(1.0f, 0.4f, 0.2f) * 0.3f);

            _deadMat = new Material(shader);
            _deadMat.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        private void BuildStaticVisuals()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Sprites/Default");

            // Buildings
            foreach (var bld in _arena.Buildings)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Building";
                go.transform.SetParent(transform, false);
                float w = bld.MaxX - bld.MinX;
                float h = bld.MaxZ - bld.MinZ;
                float cx = (bld.MinX + bld.MaxX) / 2f;
                float cz = (bld.MinZ + bld.MaxZ) / 2f;
                go.transform.localPosition = new Vector3(cx, 1f, cz);
                go.transform.localScale = new Vector3(w, 2f, h);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.25f, 0.25f, 0.28f);
                    rend.material = mat;
                }
                _buildingVisuals.Add(go);
            }

            // Rocks
            foreach (var rock in _arena.Rocks)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.name = "Rock";
                go.transform.SetParent(transform, false);
                float r = rock.z;
                go.transform.localPosition = new Vector3(rock.x, r * 0.5f, rock.y);
                go.transform.localScale = new Vector3(r * 2f, r, r * 2f);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.4f, 0.4f, 0.38f);
                    rend.material = mat;
                }
                _rockVisuals.Add(go);
            }

            // Trees
            foreach (var tree in _arena.Trees)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = "Tree";
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(tree.x, 1f, tree.y);
                go.transform.localScale = new Vector3(0.6f, 2f, 0.6f);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.1f, 0.35f, 0.1f);
                    rend.material = mat;
                }
                _treeVisuals.Add(go);

                // Foliage top
                var top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                top.name = "Foliage";
                top.transform.SetParent(go.transform, false);
                top.transform.localPosition = new Vector3(0f, 0.7f, 0f);
                top.transform.localScale = new Vector3(3f, 1.5f, 3f);
                var tcol = top.GetComponent<Collider>();
                if (tcol != null) Destroy(tcol);
                var trend = top.GetComponent<Renderer>();
                if (trend != null)
                {
                    var mat = new Material(shader);
                    mat.color = new Color(0.08f, 0.3f, 0.08f);
                    trend.material = mat;
                }
            }
        }

        private void BuildPlayerVisuals()
        {
            foreach (var player in _match.Players)
            {
                // Player circle (cylinder, flat)
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = $"PlayerVis_{player.PlayerIndex}";
                go.transform.SetParent(transform, false);
                go.transform.localScale = new Vector3(1f, 0.1f, 1f);

                var col = go.GetComponent<Collider>();
                if (col != null) Destroy(col);

                var rend = go.GetComponent<Renderer>();
                if (rend != null)
                    rend.material = player.IsCodeControlled ? _playerMat : _enemyMat;

                _playerVisuals[player.PlayerIndex] = go;

                // Aim direction line
                var aimGo = new GameObject($"AimLine_{player.PlayerIndex}");
                aimGo.transform.SetParent(go.transform, false);
                var lr = aimGo.AddComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.02f;
                var lrMat = new Material(Shader.Find("Sprites/Default")
                    ?? Shader.Find("Unlit/Color"));
                lrMat.color = player.IsCodeControlled
                    ? new Color(0.2f, 1f, 0.3f, 0.6f)
                    : new Color(1f, 0.4f, 0.2f, 0.4f);
                lr.material = lrMat;
                _playerAimLines[player.PlayerIndex] = aimGo;
            }
        }

        private void BuildZoneVisual()
        {
            _zoneRingOuter = new GameObject("ZoneRing");
            _zoneRingOuter.transform.SetParent(transform, false);
            _zoneLineRenderer = _zoneRingOuter.AddComponent<LineRenderer>();
            _zoneLineRenderer.useWorldSpace = true;
            _zoneLineRenderer.loop = true;
            _zoneLineRenderer.startWidth = 0.5f;
            _zoneLineRenderer.endWidth = 0.5f;
            _zoneLineRenderer.positionCount = 64;

            var mat = new Material(Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color"));
            mat.color = new Color(0.2f, 0.6f, 1.0f, 0.8f);
            _zoneLineRenderer.material = mat;
        }

        private void Update()
        {
            UpdatePlayerVisuals();
            UpdateZoneVisual();
        }

        private void UpdatePlayerVisuals()
        {
            foreach (var player in _match.Players)
            {
                if (!_playerVisuals.TryGetValue(player.PlayerIndex, out var go)) continue;

                if (!player.IsAlive)
                {
                    go.SetActive(false);
                    continue;
                }

                go.SetActive(true);
                go.transform.position = new Vector3(player.posX, 0.25f, player.posZ);

                // Aim line
                if (_playerAimLines.TryGetValue(player.PlayerIndex, out var aimGo))
                {
                    var lr = aimGo.GetComponent<LineRenderer>();
                    if (lr != null)
                    {
                        float rad = player.facing * Mathf.Deg2Rad;
                        Vector3 start = new Vector3(player.posX, 0.3f, player.posZ);
                        Vector3 end = start + new Vector3(
                            Mathf.Cos(rad) * 1.5f, 0f, Mathf.Sin(rad) * 1.5f);
                        lr.SetPosition(0, start);
                        lr.SetPosition(1, end);
                    }
                }
            }
        }

        private void UpdateZoneVisual()
        {
            if (_zone == null || _zoneLineRenderer == null) return;

            float cx = _zone.CenterX;
            float cz = _zone.CenterZ;
            float r = _zone.CurrentRadius;

            for (int i = 0; i < 64; i++)
            {
                float angle = (i / 64f) * Mathf.PI * 2f;
                float x = cx + Mathf.Cos(angle) * r;
                float z = cz + Mathf.Sin(angle) * r;
                _zoneLineRenderer.SetPosition(i, new Vector3(x, 0.5f, z));
            }
        }
    }
}
