using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Presentation
{
    [Serializable]
    public struct PlayerLayeredSpriteFrame
    {
        [SerializeField] private Sprite bodySprite;
        [SerializeField] private Sprite armsSprite;

        public Sprite BodySprite => bodySprite;
        public Sprite ArmsSprite => armsSprite;
    }

    /// <summary>
    /// Mirrors the Animator-driven body frame onto the matching unarmed arms overlay.
    /// A future equipped-weapon presenter can temporarily take ownership of the overlay renderer.
    /// </summary>
    public sealed class PlayerUnarmedArmsPresenter2D : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private PlayerLayeredSpriteFrame[] frameMappings = Array.Empty<PlayerLayeredSpriteFrame>();
        [SerializeField] private bool ownsRenderer = true;

        private Dictionary<Sprite, Sprite> _armsByBodySprite;
        private Sprite _lastBodySprite;

        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public SpriteRenderer ArmsWeaponSpriteRenderer => armsWeaponSpriteRenderer;
        public int MappingCount => frameMappings?.Length ?? 0;
        public bool OwnsRenderer => ownsRenderer;

        private void Awake()
        {
            BuildLookup();
        }

        private void OnEnable()
        {
            _lastBodySprite = null;
            SynchronizeIfChanged();
        }

        private void LateUpdate()
        {
            SynchronizeIfChanged();
        }

        public void SetRendererOwnership(bool shouldOwnRenderer)
        {
            if (ownsRenderer == shouldOwnRenderer)
            {
                return;
            }

            ownsRenderer = shouldOwnRenderer;
            if (ownsRenderer)
            {
                _lastBodySprite = null;
                SynchronizeIfChanged();
            }
        }

        public bool TryGetArmsSprite(Sprite bodySprite, out Sprite armsSprite)
        {
            EnsureLookup();
            armsSprite = null;
            return bodySprite != null && _armsByBodySprite.TryGetValue(bodySprite, out armsSprite);
        }

        public void SynchronizeIfChanged()
        {
            if (!ownsRenderer || bodySpriteRenderer == null || armsWeaponSpriteRenderer == null)
            {
                return;
            }

            Sprite bodySprite = bodySpriteRenderer.sprite;
            if (bodySprite == _lastBodySprite)
            {
                return;
            }

            _lastBodySprite = bodySprite;
            if (TryGetArmsSprite(bodySprite, out Sprite armsSprite))
            {
                armsWeaponSpriteRenderer.sprite = armsSprite;
            }
        }

        private void EnsureLookup()
        {
            if (_armsByBodySprite == null)
            {
                BuildLookup();
            }
        }

        private void BuildLookup()
        {
            int capacity = frameMappings?.Length ?? 0;
            _armsByBodySprite = new Dictionary<Sprite, Sprite>(capacity);
            if (frameMappings == null)
            {
                return;
            }

            for (int index = 0; index < frameMappings.Length; index++)
            {
                PlayerLayeredSpriteFrame mapping = frameMappings[index];
                if (mapping.BodySprite == null || mapping.ArmsSprite == null)
                {
                    continue;
                }

                if (_armsByBodySprite.ContainsKey(mapping.BodySprite))
                {
                    Debug.LogError("Duplicate body sprite in the unarmed arms mapping: " + mapping.BodySprite.name, this);
                    continue;
                }

                _armsByBodySprite.Add(mapping.BodySprite, mapping.ArmsSprite);
            }
        }
    }
}
