using System.Collections;
using StarterAssets;
using UnityEngine;

namespace Frenzy_Folks.Scripts.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class FrenzyThirdPersonController : ThirdPersonController
    {
        private const float MaxExternalSpeed = 12f;
        private const float ExternalDeceleration = 8f;

        private Vector3 _externalVelocity;
        private bool _isApplyingExternalVelocity;
        
        public void ApplyExternalImpulse(Vector3 impulse)
        {
            impulse.y = 0f;
            _externalVelocity = Vector3.ClampMagnitude(
                _externalVelocity + impulse,
                MaxExternalSpeed);

            if (!_isApplyingExternalVelocity)
            {
                StartCoroutine(ApplyExternalVelocity());
            }
        }

        private IEnumerator ApplyExternalVelocity()
        {
            _isApplyingExternalVelocity = true;

            while (_externalVelocity.sqrMagnitude > 0.001f)
            {
                _controller.Move(_externalVelocity * Time.deltaTime);
                _externalVelocity = Vector3.MoveTowards(
                    _externalVelocity,
                    Vector3.zero,
                    ExternalDeceleration * Time.deltaTime);
                yield return null;
            }

            _externalVelocity = Vector3.zero;
            _isApplyingExternalVelocity = false;
        }
    }
}
