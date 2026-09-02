using System;
using UnityEngine;

namespace Rustline.Presentation
{
    public sealed class PlayerJumpDustFx2D : MonoBehaviour
    {
        public const float FrameDuration = 0.08f;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();

        private float _frameElapsed;
        private int _currentFrameIndex;
        private bool _playing;

        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public int FrameCount => frames?.Length ?? 0;
        public int CurrentFrameIndex => _currentFrameIndex;

        public Sprite GetFrame(int index)
        {
            return frames != null && index >= 0 && index < frames.Length ? frames[index] : null;
        }

        public void Initialize(bool flipX)
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            _frameElapsed = 0f;
            _currentFrameIndex = 0;
            _playing = true;
            spriteRenderer.flipX = flipX;
            spriteRenderer.sprite = frames[0];
        }

        private void Update()
        {
            if (!_playing)
            {
                return;
            }

            _frameElapsed += Time.deltaTime;
            if (_frameElapsed < FrameDuration)
            {
                return;
            }

            if (_currentFrameIndex >= frames.Length - 1)
            {
                _playing = false;
                Destroy(gameObject);
                return;
            }

            _frameElapsed = 0f;
            _currentFrameIndex++;
            spriteRenderer.sprite = frames[_currentFrameIndex];
        }
    }
}
