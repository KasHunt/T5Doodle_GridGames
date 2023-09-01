using System;
using System.Collections;
using System.Collections.Generic;
using Code.Scripts.Utils;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Scripts.Chess
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class ChessPiece : MonoBehaviour, Actuator.IActuatorMovable
    {
        ///////////////////////////////////
        //////// Public Properties ////////
        ///////////////////////////////////
        
        public Material material;
        public Material selectedMaterial;
        public Material fragmentMaterial;
        public GridGameboard.Position InitialPosition;

        public PieceType pieceType;
        public GridGameboard.PlayerColor playerColor;
        
        public PieceType Type => _promotedType ?? pieceType;

        ////////////////////////////////////
        //////// Private Properties ////////
        ////////////////////////////////////
        
        [CanBeNull] private GameObject _geometry;
        
        private readonly List<Rigidbody> _fragments = new();
        private readonly List<Renderer> _fragmentRenderers = new();
        private Vector3 _initialPosition;

        private bool _selected;
        private Material _fragmentMaterial;
        private Coroutine _animateCoroutine;
        
        private PieceType? _promotedType;

        private ChessPieceSpecialization _specializationInstance;
        private CapsuleCollider _collider;

        /////////////////////////////////
        //////// Unity Lifecycle ////////
        /////////////////////////////////

        private void Awake()
        {
            // Configure the collider
            _collider = GetComponent<CapsuleCollider>();
            _collider.height = 1.5f;
            _collider.radius = 0.3f;
            _collider.center = new Vector3(0, 0.6f, 0);
        }

        private void Start()
        {
            _initialPosition = transform.position;

            _specializationInstance = Chess.SpecializationForType(pieceType);
            SetGeometryTemplate(Chess.Instance.GeometryForType(pieceType),
                Chess.RotationForType(pieceType, playerColor));

            Appear();
        }

        ///////////////////////
        //////// Setup ////////
        ///////////////////////
        
        private void SetGeometryTemplate(GameObject template, Quaternion rotation)
        {
            // Destroy any existing geometry
            ClearFragments();
            if (_geometry) Destroy(_geometry);

            // Instantiate the template
            var newGeometry = Instantiate(template, transform, false);
            newGeometry.transform.rotation = rotation;
            newGeometry.SetActive(true);
            _geometry = newGeometry;

            // Prepare the fragments
            PrepareFragments();
        }

        private void ClearFragments()
        {
            _fragments.Clear();
            _fragmentRenderers.Clear();
        }

        private void PrepareFragments()
        {
            // Clone the fragment material so we can fade it
            _fragmentMaterial = Instantiate(fragmentMaterial);

            for (var i = 0; i < _geometry!.transform.childCount; i++)
            {
                var child = _geometry.transform.GetChild(i).gameObject;

                // Add the rigidbody
                var rigidBody = child.AddComponent<Rigidbody>();
                rigidBody.useGravity = false;
                rigidBody.isKinematic = true;

                // Set the fragment material
                child.AddComponent<ChessPieceFragment>();
                child.SetActive(false);

                _fragments.Add(rigidBody);
                _fragmentRenderers.Add(child.GetComponent<Renderer>());
                _fragmentRenderers[i].material = _fragmentMaterial;
            }

            _fragments[0].gameObject.SetActive(true);
            _fragmentRenderers[0].material = material;
        }

        public IEnumerable<GridGameboard.Position> GetValidMoves(GridGameboard.Position position, GameState gameState)
            => _specializationInstance.GetValidMoves(playerColor, position, gameState);

        public void PromoteTo(PieceType toType)
        {
            _promotedType = toType;
            SetGeometryTemplate(Chess.Instance.GeometryForType(toType), Chess.RotationForType(toType, playerColor));
            _specializationInstance = Chess.SpecializationForType(toType);
        }

        //////////////////////////////////
        //////// IActuatorMovable ////////
        //////////////////////////////////

        public float GetSquaredDistanceFromActuator(Vector3 actuatorPosition) =>
            (actuatorPosition - _collider.ClosestPoint(actuatorPosition)).sqrMagnitude;

        public Vector3 GetPositionForActuator() => gameObject.transform.position;

        public void SetActuatorSelected(bool selected, Actuator _)
        {
            if (selected == _selected) return;
            _selected = selected;
            _fragmentRenderers[0].material = selected ? selectedMaterial : material;
        }

        public void BeginActuatorMove(Actuator _)
        {
            Chess.Instance.SetValidMovesBoardTiles(this);
        }

        public void ActuatorMove(Actuator actuator, Vector3 position, int movingObjectsCount)
        {
            transform.position = position;
            Chess.Instance.SetCurrentMoveBoardTile(Chess.Instance.GetClosesGridPosition(position), true);
        }

        public void EndActuatorMove(Actuator actuator, Vector3 position, int movingObjectsCount)
        {
            // Clear the 'valid moves' indicator from the board
            Chess.Instance.SetValidMovesBoardTiles(null);
            var gridPosition = Chess.Instance.GetClosesGridPosition(position);
            Chess.Instance.SetCurrentMoveBoardTile(gridPosition, false);

            // Try to make the move
            var resultingPosition =
                Chess.Instance.MakeMove(this, Chess.Instance.GetClosesGridPosition(position));

            // And set our position to whatever we're told to move to
            var duration = Equals(resultingPosition, gridPosition)
                ? Chess.Instance.snapAnimationDuration
                : Chess.Instance.moveAnimationDuration;

            var height = Equals(resultingPosition, gridPosition)
                ? Chess.Instance.gridOffsetHeight
                : Chess.Instance.moveAnimationArcHeight;

            AnimatePositionTo(resultingPosition, duration, height);
        }

        //////////////////////////////////////
        //////// ANIMATION COROUTINES ////////
        //////////////////////////////////////

        private void Appear()
        {
            var chess = Chess.Instance;

            var startPosition = new Vector3(_initialPosition.x, chess.appearHeight, _initialPosition.z);
            var endPosition = _initialPosition;

            var xDelay = (Mathf.Abs(InitialPosition.Row - 4) / 3.5f) * 10;
            var appearDelay = (InitialPosition.Column + xDelay) * chess.appearStagger;

            transform.position = startPosition;
            StartCoroutine(CoroutineUtils.TimedCoroutine(chess.appearDuration, appearDelay,
                null,
                delegate(float t) { transform.position = Vector3.Lerp(startPosition, endPosition, t); },
                delegate
                {
                    transform.position = endPosition;
                    SoundManager.Instance.PlaySound(Chess.Instance.thudSound, 1);
                }
            ));
        }

        public void AnimatePositionTo(GridGameboard.Position position, float duration, float arcHeight)
        {
            var fromPosition = transform.position;
            var toPosition = Chess.Instance.GetGridPosition(position);

            if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);

            _animateCoroutine = StartCoroutine(CoroutineUtils.TimedCoroutine(duration, 0,
                null,
                delegate(float t)
                {
                    transform.position = VectorUtils.LerpBezierArc(fromPosition, toPosition, arcHeight, t);
                },
                delegate
                {
                    transform.position = toPosition;
                    SoundManager.Instance.PlaySound(Chess.Instance.thudSound, 1);
                    _animateCoroutine = null;
                }
            ));
        }

        ///////////////////////////////////
        //////// Capture Animation ////////
        ///////////////////////////////////
        
        private void CreateExplosionParticleSystem(Color particleColor)
        {
            var chess = Chess.Instance;

            // Create, configure and play the explosion particle system
            var explosionParticleSystem = Instantiate(Chess.Instance.explosionParticleSystem);

            // Configure lifetime
            var psLifetime = explosionParticleSystem.main.startLifetime;
            psLifetime.constant = chess.fadeDelay + chess.fadeDuration;
            var fadePoint = 1 - (chess.fadeDuration / (chess.fadeDelay + chess.fadeDuration));

            // Configure color
            var psColor = explosionParticleSystem.colorOverLifetime;
            var psColorGradient = new Gradient();
            psColorGradient.SetKeys(new[]
            {
                new GradientColorKey(particleColor, 0.0f),
                new GradientColorKey(particleColor, 1.0f)
            }, new[]
            {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(1.0f, fadePoint),
                new GradientAlphaKey(0.0f, 1.0f)
            });
            psColor.color = psColorGradient;

            // Configure position
            explosionParticleSystem.transform.position = _fragments[0].transform.position;

            // Start the explosion
            explosionParticleSystem.Play();
        }

        private void ExplodeFragments()
        {
            var chess = Chess.Instance;

            // Enable and configure the fragments
            for (var i = 1; i < _fragments.Count; i++)
            {
                var rb = _fragments[i];
                rb.gameObject.SetActive(true);
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            // Explode the fragments
            for (var i = 1; i < _fragments.Count; i++)
            {
                var rb = _fragments[i];
                var explodePosition =
                    rb.position + new Vector3(
                        Random.Range(-chess.explosionPositionRange, chess.explosionPositionRange),
                        Random.Range(-chess.explosionPositionRange, chess.explosionPositionRange),
                        Random.Range(-chess.explosionPositionRange, chess.explosionPositionRange)
                    );
                rb.AddExplosionForce(chess.explosionPower, explodePosition, chess.explosionRadius, 3.0f);
            }
        }

        private void RemoveFragmentShadows()
        {
            foreach (var fragmentRenderer in _fragmentRenderers)
            {
                fragmentRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        public void Explode(bool taken, int takenPieceIndex)
        {
            var chess = Chess.Instance;
            if (chess.captureSound) SoundManager.Instance.PlaySound(chess.captureSound, 0.2f);

            // Create the particle system with the appropriate color 
            var particleColor = playerColor == GridGameboard.PlayerColor.Black
                ? chess.blackFragmentColor
                : chess.whiteFragmentColor;
            CreateExplosionParticleSystem(particleColor);

            // Disable the 'whole' fragment
            _fragments[0].gameObject.SetActive(false);

            ExplodeFragments();

            StartCoroutine(FadeOutRoutine(taken, playerColor, takenPieceIndex));
        }

        private IEnumerator FadeOutRoutine(bool taken, GridGameboard.PlayerColor takenPlayerColor, int takenPieceIndex)
        {
            var chess = Chess.Instance;

            // Wait before starting to fade
            yield return new WaitForSeconds(chess.fadeDelay + 0.1f);

            RemoveFragmentShadows();
            var startColor = _fragmentMaterial.color;
            var endColor = _fragmentMaterial.color.ColorWithAlpha(0);

            var fadeDuration = chess.fadeDuration;
            var elapsedTime = 0.0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / fadeDuration;
                _fragmentMaterial.color = Color.Lerp(startColor, endColor, t);
                yield return null;
            }

            // Deactivate the objects
            for (var i = 1; i < _fragments.Count; i++)
            {
                _fragments[i].gameObject.SetActive(false);
            }

            // Reposition the piece into the taken pieces area and reactivate the 'whole' piece
            if (taken)
            {
                var rank = (takenPieceIndex / 2f) - 0.25f;
                rank = (takenPlayerColor == GridGameboard.PlayerColor.Black) ? rank : 7f - rank;
                var file = takenPlayerColor == GridGameboard.PlayerColor.Black ? -1.5f : 8.5f;
                var position = Chess.Instance.GetGridPosition(rank, file);
                position.y = 0f; // We're off the board (on the table)
                transform.position = position;

                _fragmentRenderers[0].material = _fragmentMaterial;
                _fragments[0].gameObject.SetActive(true);
            }

            // Fade up the alpha
            startColor = startColor.ColorWithAlpha(0.8f);
            elapsedTime = 0.0f;
            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                var t = elapsedTime / fadeDuration;
                _fragmentMaterial.color = Color.Lerp(endColor, startColor, t);
                yield return null;
            }
        }
        
        //////////////////////
        //////// Misc ////////
        //////////////////////

        public static string NameForPiece(PieceType type, GridGameboard.PlayerColor color,
            GridGameboard.Position startPosition)
        {
            var side = (startPosition.Column <= ChessPositions.QUEEN_FILE) ? "Queenside" : "Kingside";
            return (type) switch
            {
                PieceType.King => $"{color} {type}",
                PieceType.Queen => $"{color} {type}",
                PieceType.Bishop => $"{color} {side} {type}",
                PieceType.Knight => $"{color} {side} {type}",
                PieceType.Rook => $"{color} {side} {type}",
                PieceType.Pawn => $"{color} {type} (File {startPosition.Column + 1})",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public override string ToString() => NameForPiece(Type, playerColor, InitialPosition) +
                                             ((_promotedType != null) ? " (Promoted to " + _promotedType + ")" : "");
    }
    
    public class ChessPieceFragment : MonoBehaviour
    {
        private void OnCollisionEnter(Collision collision)
        {
            var impactSounds = Chess.Instance.destructionSounds;
            if (impactSounds.Count == 0) return;

            var impactVelocity = collision.relativeVelocity.magnitude;
            var volume = Mathf.Clamp(impactVelocity / Chess.Instance.maxAudioVelocity, 0f, 1f);
            var sound = impactSounds[Random.Range(0, impactSounds.Count)];
            SoundManager.Instance.PlaySound(sound, volume);
        }
    }
}