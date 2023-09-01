using Code.Scripts.Utils;
using UnityEngine;

namespace Code.Scripts.Reversi
{
    public class ReversiPiece : MonoBehaviour, Actuator.IActuatorMovable
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        public bool IsKinematic { set => _rigidbody.isKinematic = value; }
        
        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        private Rigidbody _rigidbody;

        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////
        
        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        //////////////////////////////////
        //////// IActuatorMovable ////////
        //////////////////////////////////
        
        public float GetSquaredDistanceFromActuator(Vector3 actuatorPosition)
        {
            var position = transform.position;
            return (new Vector2(position.x, position.z) - 
                    new Vector2(actuatorPosition.x, actuatorPosition.z)).sqrMagnitude;
        }
        
        public Vector3 GetPositionForActuator() => gameObject.transform.position;
        public void SetActuatorSelected(bool selected, Actuator actuator) {}
        public void BeginActuatorMove(Actuator _) => Reversi.Instance.BeginMove(this, gameObject.transform.position);
        public void ActuatorMove(Actuator _, Vector3 position, int __) => Reversi.Instance.Move(this, position);
        public void EndActuatorMove(Actuator _, Vector3 position, int __) => Reversi.Instance.EndMove(this, position);

        //////////////////////////////////////
        //////// ANIMATION COROUTINES ////////
        //////////////////////////////////////
        
        public void AnimatePositionTo(Vector3 toPosition, float apexHeight, float duration, bool kinematic)
        {
            var fromPosition = transform.position;
            _rigidbody.isKinematic = kinematic;
            
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, 0,
                null,
                delegate(float t)
                {
                    transform.position = VectorUtils.LerpBezierArc(fromPosition, toPosition, apexHeight, t);
                },          
                delegate
                {
                    transform.position = toPosition;
                    _rigidbody.isKinematic = false;
            
                    SoundManager.Instance.PlaySound(Reversi.Instance.thudSound, 1);
                }
            ));
        }
        
        public void AnimateRotationTo(Quaternion endRotation, float duration)
        {
            var startRotation = transform.rotation;
            
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, 0,
                null,
                delegate(float t)
                {
                    transform.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                },          
                delegate
                {
                    transform.rotation = endRotation;
                }
            ));
        }
        
        public void AnimateFlip(Quaternion endRotation, float apexHeight, float duration, float delay)
        {
            var transform1 = transform;
            var startPosition = transform1.position;
            var startRotation = transform1.rotation;
            
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, delay, 
                null,
                delegate(float t)
                {
                    transform1.position = VectorUtils.LerpBezierArc(startPosition, startPosition, apexHeight, t);
                    transform1.rotation = Quaternion.Slerp(startRotation, endRotation, t);
                },
                delegate
                {
                    transform1.position = startPosition;
                    transform1.rotation = endRotation;
                    SoundManager.Instance.PlaySound(Reversi.Instance.thudSound, 1);
                }
            ));
        }
    }
}