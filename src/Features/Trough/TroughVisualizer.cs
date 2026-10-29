using System;
using UnityEngine;
using VikingHusbandry.Configuration;

namespace VikingHusbandry.Features.Trough
{
    public class TroughVisualizer : MonoBehaviour
    {
        private Container? _container;
        private GameObject? _troughVisualObj;
        private GameObject? _feedMoundObj;
        private Renderer[] _chestRenderers = Array.Empty<Renderer>();
        private bool _isTroughVisualActive;
        private float _syncTimer;
        private static Material? s_woodMaterial;
        private static Material? s_strawMaterial;
        private static Material? s_strawAlphaMaterial;

        public static TroughVisualizer? Get(Container container)
        {
            if (container == null || !container) return null;
            return container.GetComponent<TroughVisualizer>();
        }

        public static TroughVisualizer? EnsureAttached(Container container)
        {
            if (container == null || !container) return null;
            TroughVisualizer? vis = container.GetComponent<TroughVisualizer>();
            if (vis == null)
            {
                vis = container.gameObject.AddComponent<TroughVisualizer>();
            }
            return vis;
        }

        private void Awake()
        {
            _container = GetComponent<Container>();
            if (_container == null || !_container) return;
            CacheChestRenderers();
        }

        private void Start()
        {
            if (_container != null && _container && _container.GetInventory() != null)
            {
                _container.GetInventory().m_onChanged += OnInventoryChanged;
            }

            SyncVisualState(immediate: true);
        }

        private void OnDestroy()
        {
            if (_container != null && _container && _container.GetInventory() != null)
            {
                _container.GetInventory().m_onChanged -= OnInventoryChanged;
            }

            if (_troughVisualObj != null)
            {
                Destroy(_troughVisualObj);
                _troughVisualObj = null;
            }
        }

        private void CacheChestRenderers()
        {
            Transform newT = transform.Find("New");
            if (newT != null)
            {
                _chestRenderers = newT.GetComponentsInChildren<Renderer>(true);
            }
            else
            {
                _chestRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        private void Update()
        {
            if (_container == null) return;

            _syncTimer += Time.deltaTime;
            if (_syncTimer >= 1.0f)
            {
                _syncTimer = 0f;
                SyncVisualState(immediate: false);
            }
        }

        public void SyncVisualState(bool immediate = false)
        {
            if (_container == null) return;
            bool shouldBeTrough = ModConfig.ModEnabled.Value && ModConfig.EnableTroughs.Value && TroughRegistry.IsTrough(_container);
            SetVisualState(shouldBeTrough, force: immediate);
        }

        public void SetVisualState(bool isTrough, bool force = false)
        {
            if (isTrough == _isTroughVisualActive && !force) return;
            _isTroughVisualActive = isTrough;

            if (isTrough)
            {
                EnsureTroughModelBuilt();
                if (_troughVisualObj != null)
                {
                    _troughVisualObj.SetActive(true);
                }
                SetChestRenderersVisible(false);
                UpdateFeedFill();
            }
            else
            {
                if (_troughVisualObj != null)
                {
                    _troughVisualObj.SetActive(false);
                }
                SetChestRenderersVisible(true);
            }
        }

        public void EnsureChestRenderersHidden()
        {
            if (_isTroughVisualActive)
            {
                SetChestRenderersVisible(false);
            }
        }

        private void SetChestRenderersVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && r.gameObject != null)
                {
                    if (r.transform.IsChildOf(transform) && r.gameObject.name.StartsWith("VH_"))
                    {
                        continue;
                    }
                    r.enabled = visible;
                }
            }
        }

        private void EnsureTroughModelBuilt()
        {
            if (_troughVisualObj != null) return;

            _troughVisualObj = new GameObject("VH_TroughVisual");
            _troughVisualObj.transform.SetParent(transform, false);
            _troughVisualObj.transform.localPosition = Vector3.zero;
            _troughVisualObj.transform.localRotation = Quaternion.identity;

            // Fetch fallback material from chest wood
            Material? chestMat = null;
            if (_chestRenderers != null)
            {
                foreach (var r in _chestRenderers)
                {
                    if (r != null && r.sharedMaterial != null)
                    {
                        chestMat = r.sharedMaterial;
                        break;
                    }
                }
            }

            Material? woodMat = GetWoodMaterial(chestMat);
            Material? strawMat = GetStrawMaterial(woodMat);
            Material? strawAlphaMat = GetStrawAlphaMaterial();

            // 1. Trough wooden structure (uses authentic Scandinavian timber planking)
            GameObject frameObj = new GameObject("VH_TroughFrame");
            frameObj.transform.SetParent(_troughVisualObj.transform, false);
            frameObj.transform.localPosition = Vector3.zero;
            frameObj.transform.localRotation = Quaternion.identity;

            MeshFilter mf = frameObj.AddComponent<MeshFilter>();
            mf.sharedMesh = TroughMeshBuilder.GetOrCreateTroughMesh();

            MeshRenderer mr = frameObj.AddComponent<MeshRenderer>();
            if (woodMat != null)
            {
                mr.sharedMaterial = woodMat;
            }

            // 2. Feed mound inside trough (solid, opaque golden straw thatch bed)
            _feedMoundObj = new GameObject("VH_FeedMound");
            _feedMoundObj.transform.SetParent(_troughVisualObj.transform, false);
            _feedMoundObj.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            _feedMoundObj.transform.localRotation = Quaternion.identity;

            MeshFilter feedMf = _feedMoundObj.AddComponent<MeshFilter>();
            feedMf.sharedMesh = TroughMeshBuilder.GetOrCreateFeedMesh();

            MeshRenderer feedMr = _feedMoundObj.AddComponent<MeshRenderer>();
            feedMr.sharedMaterial = strawMat;

            // 3. Wispy straw fringe cards (loose straw blades rising above the mound)
            if (strawAlphaMat != null)
            {
                GameObject tuftsObj = new GameObject("VH_FeedTufts");
                tuftsObj.transform.SetParent(_feedMoundObj.transform, false);
                tuftsObj.transform.localPosition = Vector3.zero;
                tuftsObj.transform.localRotation = Quaternion.identity;

                MeshFilter tuftsMf = tuftsObj.AddComponent<MeshFilter>();
                tuftsMf.sharedMesh = TroughMeshBuilder.GetOrCreateTuftsMesh();

                MeshRenderer tuftsMr = tuftsObj.AddComponent<MeshRenderer>();
                tuftsMr.sharedMaterial = strawAlphaMat;
            }
        }

        private static Material? GetWoodMaterial(Material? fallbackMat)
        {
            if (s_woodMaterial != null) return s_woodMaterial;

            if (ZNetScene.instance != null)
            {
                // 1. Primary: woodwall New/_Combined Mesh [high] (pure seamless timber material)
                GameObject wall = ZNetScene.instance.GetPrefab("woodwall");
                if (wall != null)
                {
                    Transform t = wall.transform.Find("New/_Combined Mesh [high]");
                    if (t != null)
                    {
                        Renderer r = t.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null)
                        {
                            s_woodMaterial = r.sharedMaterial;
                            return s_woodMaterial;
                        }
                    }

                    Renderer wallR = wall.GetComponentInChildren<Renderer>();
                    if (wallR != null && wallR.sharedMaterial != null)
                    {
                        s_woodMaterial = wallR.sharedMaterial;
                        return s_woodMaterial;
                    }
                }

                // 2. Fallback: wood_wall_half or wood_floor
                GameObject wallHalf = ZNetScene.instance.GetPrefab("wood_wall_half");
                if (wallHalf != null)
                {
                    Renderer r = wallHalf.GetComponentInChildren<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        s_woodMaterial = r.sharedMaterial;
                        return s_woodMaterial;
                    }
                }
            }

            return fallbackMat;
        }

        private static Material? GetStrawMaterial(Material? fallbackMat)
        {
            if (s_strawMaterial != null) return s_strawMaterial;

            if (ZNetScene.instance != null)
            {
                // 1. Primary: wood_roof_45 New/large_lod exclusively uses straw_roof
                GameObject roof = ZNetScene.instance.GetPrefab("wood_roof_45");
                if (roof != null)
                {
                    Transform lod = roof.transform.Find("New/large_lod");
                    if (lod != null)
                    {
                        Renderer r = lod.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null)
                        {
                            s_strawMaterial = r.sharedMaterial;
                            return s_strawMaterial;
                        }
                    }

                    Transform straw = roof.transform.Find("New/high/understraw");
                    if (straw != null)
                    {
                        Renderer r = straw.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null)
                        {
                            s_strawMaterial = r.sharedMaterial;
                            return s_strawMaterial;
                        }
                    }
                }

                // 2. Fallback: wood_roof
                GameObject basicRoof = ZNetScene.instance.GetPrefab("wood_roof");
                if (basicRoof != null)
                {
                    Transform lod = basicRoof.transform.Find("New/large_lod");
                    if (lod != null)
                    {
                        Renderer r = lod.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null)
                        {
                            s_strawMaterial = r.sharedMaterial;
                            return s_strawMaterial;
                        }
                    }
                }
            }

            return fallbackMat ?? new Material(Shader.Find("Standard"));
        }

        private static Material? GetStrawAlphaMaterial()
        {
            if (s_strawAlphaMaterial != null) return s_strawAlphaMaterial;

            if (ZNetScene.instance != null)
            {
                // 1. Primary: wood_roof_45 New/high/straw (uses straw_roof_alpha)
                GameObject roof = ZNetScene.instance.GetPrefab("wood_roof_45");
                if (roof != null)
                {
                    Transform straw = roof.transform.Find("New/high/straw");
                    if (straw != null)
                    {
                        Renderer r = straw.GetComponent<Renderer>();
                        if (r != null && r.sharedMaterial != null)
                        {
                            s_strawAlphaMaterial = r.sharedMaterial;
                            return s_strawAlphaMaterial;
                        }
                    }
                }

                // 2. Fallback: goblin_strawpile (uses straw_roof_worn_alpha)
                GameObject strawpile = ZNetScene.instance.GetPrefab("goblin_strawpile");
                if (strawpile != null)
                {
                    Renderer r = strawpile.GetComponentInChildren<Renderer>();
                    if (r != null && r.sharedMaterial != null)
                    {
                        s_strawAlphaMaterial = r.sharedMaterial;
                        return s_strawAlphaMaterial;
                    }
                }
            }

            return null;
        }

        private void OnInventoryChanged()
        {
            if (_isTroughVisualActive)
            {
                UpdateFeedFill();
            }
        }

        public void UpdateFeedFill()
        {
            if (_feedMoundObj == null || _container == null) return;

            int foodCount = TroughRegistry.CountAcceptableFood(_container, null);
            if (foodCount <= 0)
            {
                _feedMoundObj.SetActive(false);
            }
            else
            {
                _feedMoundObj.SetActive(true);
                float ratio = Mathf.Clamp01(foodCount / 30f);
                float yPos = Mathf.Lerp(0.20f, 0.44f, ratio);
                _feedMoundObj.transform.localPosition = new Vector3(0f, yPos, 0f);
            }
        }
    }
}
