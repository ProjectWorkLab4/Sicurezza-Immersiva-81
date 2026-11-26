using SI81.Rig;
using UnityEngine;

namespace SI81.MachineLock
{
    /// <summary>
    /// Collision-safety helper for grabbable Scenario 08 props (cones, absorbent mats, absorbent socks).
    /// Calls <see cref="Physics.IgnoreCollision(Collider, Collider, bool)"/> between every prop collider and
    /// the shared Player Rig's <see cref="CharacterController"/> while keeping the prop colliders non-trigger
    /// so <c>RayInteractor</c> raycasts always hit them for grabbing.
    /// </summary>
    public static class MachineLockCollisionSafety
    {
        /// <summary>
        /// Resolves the active player <see cref="CharacterController"/> from <see cref="RigRegistration.Instance"/>
        /// or a scene fallback lookup, and ignores physical collisions between it and every collider on
        /// <paramref name="propRoot"/>.
        /// </summary>
        /// <param name="propRoot">Root GameObject of the grabbable prop.</param>
        /// <returns>true when a CharacterController was found and collision ignore was applied.</returns>
        public static bool IgnorePlayerCollisions(GameObject propRoot)
        {
            if (propRoot == null) return false;
            CharacterController controller = ResolvePlayerController();
            if (controller == null) return false;
            return IgnoreControllerCollisions(propRoot, controller);
        }

        /// <summary>
        /// Ignores physical collisions between every collider under <paramref name="propRoot"/> and the
        /// specified <paramref name="controller"/> without converting colliders to triggers.
        /// </summary>
        /// <param name="propRoot">Root GameObject of the grabbable prop.</param>
        /// <param name="controller">Player CharacterController to ignore.</param>
        /// <returns>true when at least one collider pair was updated.</returns>
        public static bool IgnoreControllerCollisions(GameObject propRoot, CharacterController controller)
        {
            if (propRoot == null || controller == null) return false;
            Collider[] colliders = propRoot.GetComponentsInChildren<Collider>(true);
            bool applied = false;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider propCollider = colliders[i];
                if (propCollider == null || propCollider == controller) continue;
                Physics.IgnoreCollision(propCollider, controller, true);
                applied = true;
            }
            return applied;
        }

        /// <summary>
        /// Resolves the player <see cref="CharacterController"/> via <see cref="RigRegistration.Instance"/>
        /// first, falling back to any active <see cref="CharacterController"/> in the loaded scenes.
        /// </summary>
        /// <returns>The player's CharacterController, or null if none is present.</returns>
        public static CharacterController ResolvePlayerController()
        {
            RigRegistration rig = RigRegistration.Instance;
            if (rig != null)
            {
                CharacterController rigController = rig.GetComponent<CharacterController>();
                if (rigController == null) rigController = rig.GetComponentInParent<CharacterController>();
                if (rigController == null) rigController = rig.GetComponentInChildren<CharacterController>(true);
                if (rigController != null) return rigController;
            }

            return Object.FindFirstObjectByType<CharacterController>();
        }
    }
}
