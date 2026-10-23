using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Owned child of an inert detonated bomb. No damage, collision or projectile logic.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BombardierExplosionPresenter2D : MonoBehaviour
    {
        public const int FrameCount = 6;
        public const float FrameDuration = 0.06f;
        public const float Duration = FrameCount * FrameDuration;
        private SpriteRenderer _renderer;
        private IReadOnlyList<Sprite> _frames;
        private float _elapsed;
        private int _startedFrame;
        public int FrameIndex { get; private set; } = -1;
        public bool IsComplete { get; private set; }
        public SpriteRenderer BodyRenderer => _renderer;

        public static int SelectFrame(float elapsed) => elapsed >= Duration ? -1 :
            Mathf.Clamp((int)(elapsed / FrameDuration), 0, FrameCount - 1);

        public void Play(IReadOnlyList<Sprite> frames, Material material, Vector2 impact)
        {
            _frames = frames;
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sharedMaterial = material;
            _renderer.sortingOrder = 32;
            _renderer.color = Color.white;
            transform.position = new Vector3(Mathf.Round(impact.x * 16f) / 16f, Mathf.Round(impact.y * 16f) / 16f);
            _elapsed = 0f;
            _startedFrame = Time.frameCount;
            IsComplete = false;
            ApplyFrame(0);
        }

        private void LateUpdate()
        {
            if (IsComplete || _frames == null || Time.frameCount == _startedFrame) return;
            _elapsed += Time.deltaTime;
            int frame = SelectFrame(_elapsed);
            if (frame < 0) { Cancel(); return; }
            ApplyFrame(frame);
        }

        private void OnDisable()
        {
            // A disabled visual must not keep its spent owner alive indefinitely.
            if (!IsComplete) Cancel();
        }

        public void Cancel()
        {
            IsComplete = true;
            FrameIndex = -1;
            if (_renderer != null) _renderer.enabled = false;
            enabled = false;
        }

        private void ApplyFrame(int frame)
        {
            if (FrameIndex == frame) return;
            FrameIndex = frame;
            _renderer.sprite = _frames[frame];
        }
    }
}

