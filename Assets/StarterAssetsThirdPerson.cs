using UnityEngine;
using UnityEngine.AI;
using Pathfinding;

namespace Pathfinding.Examples
{
    /// <summary>
    /// Animation helper specifically adapted for Unity's Starter Assets Third Person Controller.
    /// This script should be attached to an NPC GameObject with an AIPath component for movement
    /// and a NavMeshAgent for AI navigation.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(IAstarAI))]
    public class ThirdPersonNPCAnimation : VersionedMonoBehaviour
    {
        public Animator anim;
        public GameObject endOfPathEffect;

        // Animation IDs for the Starter Assets animations
        private static readonly int MotionSpeedHash = Animator.StringToHash("MotionSpeed");
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        bool isAtDestination;
        IAstarAI ai;
        Transform tr;
        protected Vector3 lastTarget;

        protected override void Awake()
        {
            base.Awake();
            ai = GetComponent<IAstarAI>();
            tr = GetComponent<Transform>();
        }

        void OnTargetReached()
        {
            if (endOfPathEffect != null && Vector3.Distance(tr.position, lastTarget) > 1)
            {
                Instantiate(endOfPathEffect, tr.position, tr.rotation);
                lastTarget = tr.position;
            }
        }

        protected void Update()
        {
            if (ai.reachedEndOfPath)
            {
                if (!isAtDestination) OnTargetReached();
                isAtDestination = true;
            }
            else
            {
                isAtDestination = false;
            }

            Vector3 relVelocity = tr.InverseTransformDirection(ai.velocity);
            relVelocity.y = 0;

            float speed = relVelocity.magnitude / anim.transform.lossyScale.x;
            float distanceToTarget = Vector3.Distance(tr.position, ai.destination);

            // Adjust animation speed based on distance
            if (distanceToTarget > 5f) // Threshold for running
            {
                anim.SetFloat(SpeedHash, speed * 1.5f); // Increase for running speed
                anim.SetFloat(MotionSpeedHash, 1.5f); // Example speed multiplier
            }
            else
            {
                anim.SetFloat(SpeedHash, speed * 0.5f); // Decrease for walking speed
                anim.SetFloat(MotionSpeedHash, 0.5f);
            }
        }
    }
}