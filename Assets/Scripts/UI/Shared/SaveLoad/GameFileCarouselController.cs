using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Shared.SaveLoad{
    /// Controls a train of save bullets sliding along a curved spline rail out of a mask.
    [ExecuteAlways]
    public class GameFileCarouselController : MonoBehaviour{
        [Header("Save Prefab")] [SerializeField]
        private GameObject saveBulletPrefab;

        [Header("Static Elements")] [Tooltip("If true, the first item in items list is preserved across BuildCarousel/DestroyCarousel calls.")] [SerializeField]
        private bool preserveFirstItem;

        [Header("Scroll Input")] [Range(0f, 1f)] [SerializeField]
        private float scrollProgress;

        [Header("Carousel Elements")] [SerializeField]
        private List<RectTransform> items = new();

        [Header("Track Endpoints")] [SerializeField]
        private Vector2 startPoint = new(-200f, 0f);

        [SerializeField] private Vector2 endPoint = new(400f, -300f);

        [Header("Arch Profile")] [SerializeField]
        private float archHeight = 60f;

        [SerializeField] private AnimationCurve archCurve = CreateSquareArchCurve();

        [Header("Train Spacing & Padding")] [SerializeField]
        private float itemSpacing = 140f;

        [SerializeField] private float startCardPadding = 1f;
        [SerializeField] private float endCardPadding   = 1f;
        [SerializeField] private float startPixelOffset;
        [SerializeField] private float endPixelOffset;

        [Header("Editor Testing")] [SerializeField]
        private int testSelectIndex = -1;

        private readonly List<GameFileBullet> _registeredBullets = new();
        private          Coroutine            _removalCoroutine;

        public bool PreserveFirstItem{
            get => preserveFirstItem;
            set => preserveFirstItem = value;
        }

        public GameFileBullet               SelectedBullet{ get; private set; }
        public event Action<GameFileBullet> OnSelectionChanged;

        /// Initializes layout and binds bullet listeners.
        private void Awake(){
            RefreshItemsList();
            UpdateLayout();
            BindBulletHandlers();
        }

        /// Binds bullet listeners on start.
        private void Start() => BindBulletHandlers();

        /// Binds bullet listeners when component activates.
        private void OnEnable() => BindBulletHandlers();

        /// Unbinds bullet listeners when component deactivates.
        private void OnDisable() => UnbindBulletHandlers();

        /// Refreshes layout and test selection in the editor.
        private void OnValidate(){
            RefreshItemsList();
            UpdateLayout();
            if (testSelectIndex >= 0 && testSelectIndex < items.Count && items[testSelectIndex].TryGetComponent<GameFileBullet>(out var bullet))
                SelectBullet(bullet);
        }

        /// Clears bullets from carousel except optionally the first template item.
        [ContextMenu("Destroy Carousel")]
        public void DestroyCarousel(){
            ClearSelection();
            UnbindBulletHandlers();
            int keepCount = preserveFirstItem && items.Count > 0 ? 1 : 0;
            while (items.Count > keepCount){
                RectTransform item = items[^1];
                items.RemoveAt(items.Count - 1);
                if (Application.isPlaying) Destroy(item.gameObject);
                else DestroyImmediate(item.gameObject);
            }
            UpdateLayout();
        }

        /// Destroys carousel bullets.
        public void DestroyLoadList() => DestroyCarousel();

        /// Instantiates and positions save bullets along the track.
        public void BuildCarousel(List<GameFileBulletData> bulletDataList){
            DestroyCarousel();
            foreach (GameFileBulletData data in bulletDataList){
                GameObject bulletObj = Instantiate(saveBulletPrefab, transform);
                bulletObj.name = $"SaveBullet_{data.slotName}";
                items.Add(bulletObj.GetComponent<RectTransform>());
                bulletObj.GetComponent<GameFileBullet>().Initialize(string.IsNullOrEmpty(data.characterName) ? data.slotName : data.characterName, data.location, data.timestamp, data.snapshot, data.slotName);
            }
            BindBulletHandlers();
            UpdateLayout();
            ClearSelection();
        }

        /// Sets train position along the rail using normalized progress.
        public void SetScrollProgress(float progress){
            scrollProgress = Mathf.Clamp01(progress);
            UpdateLayout();
        }

        /// Selects the specified bullet and updates highlight on all bullets.
        public void SelectBullet(GameFileBullet bulletToSelect){
            SelectedBullet = bulletToSelect;
            foreach (GameFileBullet bullet in _registeredBullets)
                if (bullet != null)
                    bullet.SetSelected(bullet == bulletToSelect);
            OnSelectionChanged?.Invoke(SelectedBullet);
        }

        /// Clears active bullet selection.
        public void ClearSelection(){
            SelectedBullet = null;
            foreach (GameFileBullet bullet in _registeredBullets)
                if (bullet != null)
                    bullet.SetSelected(false);
            OnSelectionChanged?.Invoke(null);
        }

        /// Deletes a bullet and smoothly animates remaining items along the track.
        public void DeleteBullet(GameFileBullet bullet){
            if (bullet == null) return;
            RectTransform rt = bullet.GetComponent<RectTransform>();
            int index = items.IndexOf(rt);
            if (index < 0) return;

            if (SelectedBullet == bullet)
                ClearSelection();

            bullet.OnClicked -= HandleBulletClicked;
            _registeredBullets.Remove(bullet);
            items.RemoveAt(index);

            if (_removalCoroutine != null)
                StopCoroutine(_removalCoroutine);

            if (Application.isPlaying)
                _removalCoroutine = StartCoroutine(AnimateBulletRemoval(rt));
            else{
                DestroyImmediate(rt.gameObject);
                UpdateLayout();
            }
        }

        /// Smoothly interpolates remaining bullets to their new layout positions and destroys removed bullet.
        private IEnumerator AnimateBulletRemoval(RectTransform removedRt){
            float elapsed = 0f;
            const float duration = 0.25f;

            Vector2[] startPositions = new Vector2[items.Count];
            Vector2[] targetPositions = new Vector2[items.Count];
            for (int i = 0; i < items.Count; i++){
                startPositions[i] = items[i].anchoredPosition;
                targetPositions[i] = CalculateItemTrackPosition(i, items.Count);
            }

            Vector3 initialScale = removedRt.localScale;

            while (elapsed < duration){
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < items.Count; i++)
                    items[i].anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], smoothT);

                if (removedRt != null)
                    removedRt.localScale = Vector3.Lerp(initialScale, Vector3.zero, smoothT);

                yield return null;
            }

            UpdateLayout();
            if (removedRt != null)
                Destroy(removedRt.gameObject);
            _removalCoroutine = null;
        }

        /// Positions all items along the spline rail based on progress and spacing.
        public void UpdateLayout(){
            if (items.Count == 0) return;
            int index = 0;
            foreach (RectTransform item in items)
                item.anchoredPosition = CalculateItemTrackPosition(index++, items.Count);
        }

        /// Calculates anchored position on track for an item at a given index.
        private Vector2 CalculateItemTrackPosition(int index, int totalCount){
            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f) return startPoint;
            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);
            float startLeadDistance = (startCardPadding * itemSpacing) + startPixelOffset;
            float totalTrainSpan = Mathf.Max(0, totalCount - 1 - endCardPadding) * itemSpacing;
            float endLeadDistance = Mathf.Max(startLeadDistance, trackLength + totalTrainSpan + endPixelOffset);
            float leadTravelDistance = Mathf.Lerp(startLeadDistance, endLeadDistance, scrollProgress);
            float itemDist = leadTravelDistance - (index * itemSpacing);
            return EvaluateTrackPosition(itemDist, trackLength, trackDir, trackNormal);
        }

        /// Calculates anchored coordinate on spline track at a given distance.
        private Vector2 EvaluateTrackPosition(float distance, float trackLength, Vector2 trackDir, Vector2 trackNormal){
            float normalizedT = distance / trackLength;
            if (normalizedT <= 0f) return startPoint + (trackDir * distance);
            if (normalizedT >= 1f) return endPoint + (trackDir * (distance - trackLength));
            Vector2 basePos = Vector2.Lerp(startPoint, endPoint, normalizedT);
            return basePos + (trackNormal * (archCurve.Evaluate(normalizedT) * archHeight));
        }

        /// Collects existing child transforms into item list if empty.
        private void RefreshItemsList(){
            if (items.Count > 0) return;
            foreach (Transform child in transform)
                if (child.TryGetComponent<RectTransform>(out var rectTransform))
                    items.Add(rectTransform);
        }

        /// Binds click events on all bullets.
        public void BindBulletHandlers(){
            UnbindBulletHandlers();
            foreach (RectTransform item in items){
                if (!item.TryGetComponent<GameFileBullet>(out var bullet)) continue;
                bullet.OnClicked += HandleBulletClicked;
                _registeredBullets.Add(bullet);
                if (bullet.IsSelected) SelectedBullet = bullet;
            }
        }

        /// Unbinds click events from bullets.
        private void UnbindBulletHandlers(){
            foreach (GameFileBullet bullet in _registeredBullets)
                bullet.OnClicked -= HandleBulletClicked;
            _registeredBullets.Clear();
        }

        /// Commits selection when bullet is clicked.
        private void HandleBulletClicked(GameFileBullet bullet) => SelectBullet(bullet);

        /// Creates default arch animation curve for spline track.
        private static AnimationCurve CreateSquareArchCurve() => new(
            new Keyframe(0.0f, 0.0f, 0f, 10f),
            new Keyframe(0.1f, 1.0f, 0f, 0f),
            new Keyframe(0.9f, 1.0f, 0f, 0f),
            new Keyframe(1.0f, 0.0f, -10f, 0f)
        );

        /// Draws editor track gizmo spline.
        private void OnDrawGizmosSelected(){
            Vector2 baseline = endPoint - startPoint;
            float trackLength = baseline.magnitude;
            if (trackLength < 0.001f) return;
            Vector2 trackDir = baseline / trackLength;
            Vector2 trackNormal = new(-trackDir.y, trackDir.x);
            Gizmos.color = Color.cyan;
            const int segments = 40;
            Vector3 prevWorldPos = transform.TransformPoint(EvaluateTrackPosition(0f, trackLength, trackDir, trackNormal));
            for (int s = 1; s <= segments; s++){
                Vector3 currentWorldPos = transform.TransformPoint(EvaluateTrackPosition((float)s / segments * trackLength, trackLength, trackDir, trackNormal));
                Gizmos.DrawLine(prevWorldPos, currentWorldPos);
                prevWorldPos = currentWorldPos;
            }
        }
    }
}
