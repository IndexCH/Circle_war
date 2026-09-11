using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CircleWar
{
    [DisallowMultipleComponent]
    public sealed class PlayerJump : MonoBehaviour
    {
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField, Min(0.1f)] private float jumpHeight = 0.9f;
        [SerializeField, Min(0.1f)] private float jumpDuration = 0.65f;
        private Vector3 groundPosition;
        private Vector3 jumpDirection = Vector3.up;
        private float jumpAge;
        private GameHud hud;
        private GameRuntimeData observedRuntime;
        private int runRevision;
        public bool IsAirborne { get; private set; }
        public float Height { get; private set; }
        public Vector3 GroundPosition => IsAirborne ? groundPosition : transform.position;

        private void Awake() { groundPosition = transform.position; }

        private void Update()
        {
            if (hud == null) hud = FindAnyObjectByType<GameHud>();
            if (hud != null && (observedRuntime != hud.RuntimeData || runRevision != hud.RuntimeData.RunRevision))
            {
                ResetJump();
                observedRuntime = hud.RuntimeData;
                runRevision = observedRuntime.RunRevision;
            }
            if (hud != null && hud.HudData.PlayerStats.Hp.Value <= 0) { ResetJump(); return; }
            if (Input.GetKeyDown(jumpKey)) TryJump();
            if (!IsAirborne) return;
            jumpAge += Time.deltaTime;
            float progress = Mathf.Clamp01(jumpAge / jumpDuration);
            Height = 4f * jumpHeight * progress * (1f - progress);
            transform.position = groundPosition + jumpDirection * Height;
            if (progress >= 1f) ResetJump();
        }

        public bool TryJump()
        {
            if (!isActiveAndEnabled || IsAirborne || Time.timeScale <= 0f) return false;
            if (hud == null) hud = FindAnyObjectByType<GameHud>();
            if (hud != null && (hud.HudData.Dialogue.IsVisible.Value || hud.HudData.PlayerStats.Hp.Value <= 0)) return false;
            GameObject selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (selected != null && selected.GetComponent<InputField>() != null) return false;
            observedRuntime = hud != null ? hud.RuntimeData : null;
            runRevision = observedRuntime != null ? observedRuntime.RunRevision : 0;
            groundPosition = transform.position;
            CircleMapView map = CircleMapView.Active;
            jumpDirection = map != null ? ((Vector3)map.DiskCenter - groundPosition).normalized : Vector3.up;
            jumpDirection.z = 0f;
            if (jumpDirection.sqrMagnitude < .01f) jumpDirection = Vector3.up;
            jumpAge = 0f;
            IsAirborne = true;
            return true;
        }

        public void ResetJump()
        {
            if (IsAirborne) transform.position = groundPosition;
            IsAirborne = false;
            Height = 0f;
            jumpAge = 0f;
        }

        private void OnDisable() { ResetJump(); }
    }
}
