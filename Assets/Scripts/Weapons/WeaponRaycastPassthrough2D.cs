using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>
    /// Marks a physical collider that should still block gameplay movement while being transparent
    /// to weapon hitscan selection. This is intentionally collider-local so skipping it never skips
    /// another collider on the same layer or farther along the ray.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponRaycastPassthrough2D : MonoBehaviour
    {
    }
}
