using UnityEngine;

namespace Rustline.Gameplay.Player
{
    public sealed class PlayerJumpGrace
    {
        public float CoyoteRemaining { get; private set; }
        public float BufferRemaining { get; private set; }

        public void Buffer(float duration)
        {
            BufferRemaining = Mathf.Max(BufferRemaining, duration);
        }

        public void Tick(bool grounded, float deltaTime, float coyoteDuration)
        {
            CoyoteRemaining = grounded
                ? coyoteDuration
                : Mathf.Max(0f, CoyoteRemaining - deltaTime);
            BufferRemaining = Mathf.Max(0f, BufferRemaining - deltaTime);
        }

        public bool TryConsume()
        {
            if (BufferRemaining <= 0f || CoyoteRemaining <= 0f)
            {
                return false;
            }

            BufferRemaining = 0f;
            CoyoteRemaining = 0f;
            return true;
        }

        public void Reset()
        {
            CoyoteRemaining = 0f;
            BufferRemaining = 0f;
        }
    }
}
