using Frenzy_Folks.Scripts.Core.FSM;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Traps.Turret
{
    public class TurretIdleState : TurretState
    {
        private float _initialAngle;
        private Quaternion _returnStartRotation;
        private float _returnElapsed;
        private float _sweepStartTime;
        private bool _isReturning;
        
        public override void Init(FSM fsm)
        {
            base.Init(fsm);
            _initialAngle = Fsm.turretHead.transform.localRotation.eulerAngles.y;
        }

        public override void Enter()
        {
            base.Enter();
            _returnStartRotation = Fsm.turretHead.localRotation;
            _returnElapsed = 0f;
            _isReturning = true;
        }

        public override void Update()
        {
            base.Update();

            if (_isReturning)
            {
                _returnElapsed += Time.deltaTime;
                float duration = Fsm.ReturnToIdleDuration;
                float progress = duration <= 0f ? 1f : Mathf.Clamp01(_returnElapsed / duration);
                float easedProgress = Mathf.SmoothStep(0f, 1f, progress);
                Fsm.turretHead.localRotation = Quaternion.Slerp(
                    _returnStartRotation,
                    Quaternion.Euler(0f, _initialAngle, 0f),
                    easedProgress);

                if (progress < 1f) return;

                _isReturning = false;
                _sweepStartTime = Time.time;
                return;
            }

            float sweepAngle = Fsm.SweepAngle / 2f;
            float sweepSpeed = Fsm.SweepSpeed;
            float angle = Mathf.Sin((Time.time - _sweepStartTime) * sweepSpeed) * sweepAngle;
            Fsm.turretHead.transform.localRotation = Quaternion.Euler(0f, angle + _initialAngle, 0f);
        }

        public override void OnTargetStay(Collider other)
        {
            base.OnTargetStay(other);
            Debug.Log("Attacking player!");
            if (!other.CompareTag("Player")) return;
            Fsm.targetTransform = other.transform;
            Debug.Log(other.transform);
            Fsm.TransitionTo("Targeting");
        }
    }
}
