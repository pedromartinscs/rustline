using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Controlled downward motion for the existing kinematic, trigger-hitbox actors.</summary>
    internal sealed class EnemyGroundSupport2D
    {
        private const float Skin = .02f;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private readonly ContactFilter2D _ground = new ContactFilter2D
            { useLayerMask = true, layerMask = 1 << 6, useTriggers = false };
        private readonly Vector2 _offset, _size;
        private float _fallSpeed;

        internal EnemyGroundSupport2D(Rigidbody2D body, Collider2D footprint)
        {
            // Keep the physical footprint after death disables the weapon trigger.
            _offset = (Vector2)footprint.bounds.center - body.position;
            _size = footprint.bounds.size;
        }

        internal void Reset() => _fallSpeed = 0f;

        internal bool BlocksHorizontal(Rigidbody2D body, int direction)
        {
            // Preserve the old .8-unit forward limit, including the footprint half-width.
            // Unlike the old mid-height ray this also sees a knee-high step after landing.
            int count = Physics2D.BoxCast(body.position + _offset, _size, 0f, Vector2.right * direction,
                _ground, _hits, Mathf.Max(0f, .8f - _size.x * .5f));
            for (int i = 0; i < count; i++)
                if (_hits[i].normal.x * direction < -.5f) return true;
            return false;
        }

        // Returns true while descending. Horizontal AI must not overwrite this MovePosition.
        internal bool Step(Rigidbody2D body, float dt)
        {
            _fallSpeed = Mathf.Min(_fallSpeed + Mathf.Abs(Physics2D.gravity.y) * dt, 24f);
            float distance = _fallSpeed * dt;
            int count = Physics2D.BoxCast(body.position + _offset, _size, 0f, Vector2.down,
                _ground, _hits, distance + Skin + .001f);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
                if (_hits[i].normal.y > .5f) nearest = Mathf.Min(nearest, _hits[i].distance);
            if (nearest <= Skin + .001f)
            {
                _fallSpeed = 0f;
                // Kinematic MovePosition leaves a velocity after the simulation step.
                // Idle, hit-paused and dead actors must not coast through the landing.
                body.linearVelocity = Vector2.zero;
                return false;
            }
            body.MovePosition(body.position + Vector2.down * Mathf.Min(distance, Mathf.Max(0f, nearest - Skin)));
            if (nearest <= distance + Skin) _fallSpeed = 0f;
            return true;
        }
    }
}
