using System;
using Code.Scripts.Utils;
using UnityEngine;

namespace Code.Scripts.Checkers
{
    public class CheckersPiece : MonoBehaviour, Actuator.IActuatorMovable
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        public GridGameboard.PlayerColor color;
        public GridGameboard.Position InitialPosition;
        public GridGameboard.Position CurrentPosition;
        public bool promoted;
        public bool Shown { get; private set; } = true;

        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        private static readonly int Dissolve = Shader.PropertyToID("_Dissolve");
        private GameObject _crown;
        private MeshRenderer _meshRenderer;
        private Coroutine _flipCoroutine;

        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////
        
        private void Awake()
        {
            _crown = Instantiate(Checkers.Instance.crownTemplate, transform, false);
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
            _meshRenderer.material.SetFloat(Dissolve, 1f);
        }

        private void Start()
        {
            transform.position = GetPositionForTile(InitialPosition);
        }

        //////////////////////
        //////// Misc ////////
        //////////////////////
        
        public bool IsDirectionAllowed(GridGameboard.Position toPosition)
        {
            if (promoted) return true;
            
            var rowIncreasing = toPosition.Row > CurrentPosition.Row;
            return color == GridGameboard.PlayerColor.White ? rowIncreasing : !rowIncreasing;
        }
        
        public void MaybePromote()
        {
            if (promoted) return;

            switch (color)
            {
                case GridGameboard.PlayerColor.Black when (CurrentPosition.Row == Checkers.BLACK_PROMOTION_ROW):
                case GridGameboard.PlayerColor.White when (CurrentPosition.Row == Checkers.WHITE_PROMOTION_ROW):
                    promoted = true;
                    Promote(true);
                    break;
                
                case GridGameboard.PlayerColor.None:
                default:
                    // No action
                    break;
            }
        }

        
        //////////////////////////////////
        //////// IActuatorMovable ////////
        //////////////////////////////////
        
        public float GetSquaredDistanceFromActuator(Vector3 actuatorPosition)
        {
            if (!Shown) return float.MaxValue;
            
            var position = transform.position;
            return (new Vector2(position.x, position.z) - 
                    new Vector2(actuatorPosition.x, actuatorPosition.z)).sqrMagnitude;
        } 
        
        public Vector3 GetPositionForActuator() => gameObject.transform.position;
        
        public void SetActuatorSelected(bool selected, Actuator actuator) {}

        public void BeginActuatorMove(Actuator actuator)
            => Checkers.Instance.BeginMove(this);

        public void ActuatorMove(Actuator actuator, Vector3 position, int movingObjectsCount)
            => Checkers.Instance.Move(this, position);
        
        public void EndActuatorMove(Actuator actuator, Vector3 position, int movingObjectsCount) 
            => Checkers.Instance.EndMove(this, position);

        //////////////////////////////////////
        //////// ANIMATION COROUTINES ////////
        //////////////////////////////////////

        private void Promote(bool show)
        {
            // Return if we're already in the target state
            if (show == _crown.activeSelf) return;

            var fromScale = new Vector3(1, show ? 0 : 1, 1);
            var toScale = new Vector3(1, show ? 1 : 0, 1);
            _crown.transform.localScale = fromScale;
            _crown.SetActive(true);
            
            if (show) SoundManager.Instance.PlaySound(Checkers.Instance.promoteSound, 1);

            var duration = Checkers.Instance.promotionDuration * (show ? 1 : 0.3f);
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, 0, 
                null,
                delegate(float tUnscaled)
                {
                    var t = Easing.QuadInOut(tUnscaled);
                
                    _crown.transform.localScale = Vector3.Lerp(fromScale, toScale, t);
                    _crown.transform.rotation = Quaternion.Euler(0, t * 360f, 0);
                },
                delegate
                {
                    _crown.transform.localScale = toScale;
                    _crown.SetActive(show);
                }
            ));
        }
        
        public void ResetPiece(int delayOrder)
        {
            if (_flipCoroutine != null) StopCoroutine(_flipCoroutine);
            CurrentPosition = InitialPosition;
            
            _crown.SetActive(false);

            var delay = delayOrder * Checkers.Instance.appearStagger;
            AnimateDissolve(false, Checkers.Instance.dissolveDuration, delay, true);
        }
        
        public void Capture() => AnimateDissolve(false, Checkers.Instance.dissolveDuration, 0, false);

        private void AnimateDissolve(bool show, float duration, float delay, bool hideForReset)
        {
            // Clear any exising promotion
            Promote(false);
            
            Shown = show;
            var startValue = _meshRenderer.material.GetFloat(Dissolve);
            var endValue = show ? 0 : 1;

            // If the start and the end are the same, or this is a 'snap' dissolve,
            // ensure we're in the correct state and return early
            if (Math.Abs(startValue - endValue) < 0.01f || duration == 0)
            {
                _meshRenderer.material.SetFloat(Dissolve, endValue);

                // Show (after a delay) if we're snapped to a hidden piece, but we're resetting
                if (!hideForReset) return;
                transform.position = GetPositionForTile(InitialPosition);
                transform.rotation = Quaternion.identity;
                AnimateDissolve(true, Checkers.Instance.dissolveDuration, delay, false);
                return;
            }

            // Do the animation
            SoundManager.Instance.PlaySound(Checkers.Instance.dissolveSound, (hideForReset || show) ? 0.05f : 0.5f);
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, delay, 
                null,
                delegate(float t)
                {
                    _meshRenderer.material.SetFloat(Dissolve, Mathf.Lerp(startValue, endValue, t));
                },
                delegate
                {
                    _meshRenderer.material.SetFloat(Dissolve, endValue);

                    // If we're resetting, move to the initial position and fade back up again
                    if (!hideForReset) return;
                    transform.position = GetPositionForTile(InitialPosition);
                    transform.rotation = Quaternion.identity;
                    AnimateDissolve(true, Checkers.Instance.dissolveDuration, 0, false);
                }
            ));
        }
        
        public void AnimateCancelMove() =>
            AnimatePositionTo(CurrentPosition, Checkers.Instance.moveDuration, Checkers.Instance.moveApexHeight, 0);

        public void AnimateApplyMove(GridGameboard.Position position) => 
            AnimatePositionTo(position, Checkers.Instance.snapDuration, 
                Checkers.Instance.moveHeight - Checkers.Instance.boardThickness * 2, 0);

        private static Vector3 GetPositionForTile(GridGameboard.Position position)
        {
            var toPosition = Checkers.Instance.GameBoard.GetPositionForTile(position);
            toPosition.y = Checkers.Instance.boardThickness * 2;
            return toPosition;
        }
        
        private void AnimatePositionTo(GridGameboard.Position position, float duration, float apexHeight, float delay)
        {
            CurrentPosition = position;
            
            var fromPosition = transform.position;
            var toPosition = GetPositionForTile(position);
            
            MaybePromote();
            
            StartCoroutine(CoroutineUtils.TimedCoroutine(duration, delay, 
                null,
                delegate(float t)
                {
                    transform.position = VectorUtils.LerpBezierArc(fromPosition, toPosition, apexHeight, t);
                },
                delegate
                {
                    transform.position = toPosition;
                    if (duration > 0) SoundManager.Instance.PlaySound(Checkers.Instance.thudSound, 1);
                }
            ));
        }
        
        public void AnimateFlip(float apexHeight, float duration, float delay)
        {
            var transform1 = transform;
            var startPosition = transform1.position;
            var endPosition = GetPositionForTile(CurrentPosition);
            const int startRotation = 0;
            const int endRotation = 360;
            
            _flipCoroutine = StartCoroutine(CoroutineUtils.TimedCoroutine(duration, delay, 
                null,
                delegate(float t)
                {
                    transform.position = VectorUtils.LerpBezierArc(startPosition, endPosition, apexHeight, t);
                    transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, endRotation, t));
                },
                delegate
                {
                    transform1.position = endPosition;
                    transform1.rotation = Quaternion.Euler(0, 0, endRotation);
            
                    SoundManager.Instance.PlaySound(Checkers.Instance.thudSound, 0.4f);
                    
                    // Keep the mexican wave going...
                    AnimateFlip(apexHeight, duration, duration * 2);
                }
            ));
        }
    }
}