using System.Collections;
using System.Linq;

using UnityEngine;
namespace AnythingWorld.Behaviour
{ 
    /// <summary>
    /// MonoBehaviour handling all the keyboard-based input controllers.
    /// </summary>
    public class PlayerCoreMovement : MonoBehaviour
    {
        #region Public Variables
        public enum PlayerMovementType
        {
            FirstPerson,
            ThirdPerson,
            TopDown,
            SideScroller
        }

        public PlayerMovementType MovementType = PlayerMovementType.FirstPerson;
        [Header("Look Variables")]
        public string MouseXLookAxisName = "Mouse X";
        public string MouseYLookAxisName = "Mouse Y";
        public float RotationSensitivity = 0.02f;
        public float RotationLimitY = 85f;
        public bool InvertX = false;
        public bool InvertY = true;
        [Header("Move Variables")]
        public string HorizontalMovementAxisName = "Horizontal";
        public string VerticalMovementAxisName = "Vertical";
        public float MovementSpeed = 5f;
        public float MovementAcceleration = 3f;
        [Header("Expanded Movement Variables")]
        public bool EnableRunning = true;
        public string RunButtonName = "Fire3";
        public float MovementRunSpeed = 8f;
        public float MovementRunTransitionSpeed = 4f;
        [Space]
        public bool EnableCrouching = true;
        public string CrouchButtonName = "Fire1";
        public float MovementCrouchSpeed = 3f;
        public float CharacterCrouchTransitionSpeed = 4f;
        public float CharacterCrouchHeight = 1.5f;
        [Space]
        public bool EnableSliding = false;
        public float MovementSlideLength = 1f;
        public float MovementSlideSpeed = 8f;
        public float CharacterSlideTransitionSpeed = 6f;
        public float CharacterSlideHeight = 1f;
        [Space]
        public bool EnableJumping = true;
        public string JumpButtonName = "Jump";
        public float JumpForce = 10f;
        public float FallMultiplier = 2.5f;
        public float LowJumpMultiplier = 2f;
        public float AerialMovementAcceleration = .75f;
        public float GroundCheckRadius = 0.5f;
        public LayerMask GroundLayerMask;
        #endregion

        #region Private Variables
        internal bool IsGrounded = true;
        internal bool IsRunning;
        internal bool IsCrouching;
        internal bool IsSliding;

        internal bool BlockMovement;
        internal bool BlockInput;
        internal bool ClearHeadSpace;

        private CharacterController _characterController;
        private Camera _characterCamera;
        private AnythingAnimationProcessor _animationProcessor;

        internal float CharacterRotationX;
        internal float CameraRotationY;

        private float _movementEaseTime;
        private float _movementRunTransitionTime;
        private float _characterCrouchTransitionTime;
        private float _characterSlideTransitionTime;

        internal float GroundVelocity;
        internal float VerticalVelocity;

        private float _characterHeight;
        private float _characterRadius;
        private float _cameraRelativeHeight;

        private IEnumerator _runningCoroutine;
        private IEnumerator _crouchingCoroutine;
        private IEnumerator _slidingCoroutine;

        internal Vector3 MovementVector = Vector3.zero;
        internal Vector3 SlidingVector = Vector3.forward;
        internal Vector3 GravityEffect = Vector3.zero;

        private Vector2 _inputMoveVector;
        private bool _crouchButtonFlag;
        private bool _runButtonFlag;
        private bool _lowJumpFlag;
        private bool _falling;
        #endregion

        #region Unity Functions
        /// <summary>
        /// Sets up the player controller's different variables.
        /// </summary>
        private void Awake()
        {
            Cursor.visible = false;

            if (_characterController == null && !GetComponent<CharacterController>())
            {
                Debug.LogError($"{name} does not have a Character Controller component attached to it!");
                Debug.Break();
            }

            if (_characterCamera == null && !GetComponentInChildren<Camera>())
            {
                Debug.LogError($"No Camera is attached to {name}!");
                Debug.Break();
            }

            if (GetComponent<AnythingAnimationProcessor>()) _animationProcessor = GetComponent<AnythingAnimationProcessor>();
            _characterController = GetComponent<CharacterController>();
            _characterCamera = GetComponentInChildren<Camera>();

            _characterHeight = _characterController.height;
            _characterRadius = _characterController.radius;
            _cameraRelativeHeight = _characterCamera.transform.localPosition.y / _characterHeight;
        }

        /// <summary>
        /// Polls for the different input styles.
        /// </summary>
        private void Update()
        {
            switch (MovementType)
            {
                case PlayerMovementType.FirstPerson:
                    TwoDegreeMovement();
                    TwoDegreeViewing();
                    if (EnableJumping) Jump();
                    if (EnableRunning) Run();
                    if (EnableCrouching) Crouch();
                    break;
                case PlayerMovementType.ThirdPerson:
                    TwoDegreeMovement(true);
                    TwoDegreeViewing();
                    if (EnableJumping) Jump();
                    if (EnableRunning) Run();
                    if (EnableCrouching) Crouch();
                    break;
                case PlayerMovementType.TopDown:
                    TwoDegreeMovement(true);
                    if (EnableRunning) Run();
                    if (EnableCrouching) Crouch();
                    break;
                case PlayerMovementType.SideScroller:
                    OneDegreeMovement(true);
                    if (EnableJumping) Jump();
                    if (EnableRunning) Run();
                    if (EnableCrouching) Crouch();
                    break;
                default:
                    break;
            }

            if (_animationProcessor?.GetAnimator()) _animationProcessor.SetSpeed(((MovementVector.normalized.magnitude / 2f) * Mathf.Lerp(1f, 2f, _movementRunTransitionTime)) * _movementEaseTime);
        }

        /// <summary>
        /// Applies the physics calculations for the controller to move.
        /// </summary>
        void FixedUpdate()
        {
            if (!_characterController.enabled)
            {
                return;
            }

            IsGrounded = Physics.CheckSphere(transform.position, GroundCheckRadius, GroundLayerMask);
            ClearHeadSpace = !Physics.CheckSphere(transform.position + new Vector3(0f, CharacterCrouchHeight, 0f), GroundCheckRadius, GroundLayerMask);

            if (!IsGrounded)
            {
                if (_animationProcessor?.GetAnimator()) _animationProcessor.Fall();
                if (VerticalVelocity < 0f)
                {
                    VerticalVelocity += Physics.gravity.y * FallMultiplier * Time.fixedDeltaTime;
                }
                else if (VerticalVelocity >= 0f && _lowJumpFlag)
                {
                    VerticalVelocity += Physics.gravity.y * LowJumpMultiplier * Time.fixedDeltaTime;
                }
                else
                {
                    VerticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;
                }
            }
            else
            {
                if (_animationProcessor?.GetAnimator()) _animationProcessor.Land();
            }
            GravityEffect = new Vector3(0, VerticalVelocity, 0);

            if (IsSliding)
            {
                GroundVelocity = MovementSlideSpeed;
            }
            else if (IsCrouching)
            {
                GroundVelocity = MovementCrouchSpeed;
            }
            else
            {
                GroundVelocity = Mathf.Lerp(MovementSpeed, MovementRunSpeed, _movementRunTransitionTime);
            }

            if (_movementEaseTime > 0f)
            {
                if (_inputMoveVector.magnitude > 0)
                {
                    _movementEaseTime = Mathf.Lerp(0f, 1f, _movementEaseTime + (Time.fixedDeltaTime * MovementAcceleration));
                }
                else if (IsGrounded)
                {
                    _movementEaseTime = Mathf.Lerp(0f, 1f, _movementEaseTime - (Time.fixedDeltaTime * MovementAcceleration));
                    if (_movementEaseTime <= 0f) MovementVector = new Vector3(_inputMoveVector.x, 0, _inputMoveVector.y);
                }
            }
            _characterController.Move((BlockMovement ? Vector3.zero : _characterController.transform.TransformDirection(MovementVector.normalized) * GroundVelocity * Time.fixedDeltaTime * _movementEaseTime) + GravityEffect * Time.fixedDeltaTime);
        }
        #endregion Unity Functions

        #region Controls
        /// <summary>
        /// The update loop that tracks two-dimensional movement.
        /// </summary>
        /// <param name="faceForward">If the model of the controller should face the forward direction of movement</param>
        void TwoDegreeMovement(bool faceForward = false)
        {
            if (!BlockInput) _inputMoveVector = new Vector2(Input.GetAxisRaw(HorizontalMovementAxisName), Input.GetAxisRaw(VerticalMovementAxisName));
            if (_inputMoveVector.magnitude > 0)
            {
                if (MovementVector.magnitude <= 0) _movementEaseTime = Mathf.Lerp(0f, 1f, Time.deltaTime * MovementAcceleration);
                MovementVector = new Vector3(_inputMoveVector.x, 0, _inputMoveVector.y);
                if (faceForward && _animationProcessor.GetMesh() && Mathf.Abs(MovementVector.magnitude) > 0)
                    _animationProcessor.GetMesh().transform.localRotation = Quaternion.Lerp(_animationProcessor.GetMesh().transform.localRotation, Quaternion.LookRotation(MovementVector, Vector3.up), Time.deltaTime * GroundVelocity);
            }

            if (!IsGrounded && !_falling)
            {
                StartCoroutine(WaitForGround());
            }
        }

        /// <summary>
        /// The update loop that tracks one-dimensional movement.
        /// </summary>
        /// <param name="faceForward">If the model of the controller should face the forward direction of movement</param>
        void OneDegreeMovement(bool faceForward = false)
        {
            _inputMoveVector = new Vector2(Input.GetAxisRaw(HorizontalMovementAxisName), 0f);
            if (_inputMoveVector.magnitude > 0)
            {
                if (MovementVector.magnitude <= 0) _movementEaseTime = Mathf.Lerp(0f, 1f, Time.deltaTime * MovementAcceleration);
                MovementVector = new Vector3(0f, 0f, _inputMoveVector.x);
                if (faceForward && _animationProcessor.GetMesh() && Mathf.Abs(MovementVector.magnitude) > 0)
                    _animationProcessor.GetMesh().transform.localRotation = Quaternion.LookRotation(MovementVector, Vector3.up);
            }

            if (!IsGrounded && !_falling)
            {
                StartCoroutine(WaitForGround());
            }
        }

        /// <summary>
        /// The update loop that tracks the camera controls.
        /// </summary>
        void TwoDegreeViewing()
        {
            Vector2 _tempInputViewVector = new Vector2(Input.GetAxisRaw(MouseXLookAxisName), Input.GetAxisRaw(MouseYLookAxisName));

            CharacterRotationX += _tempInputViewVector.x * RotationSensitivity * (InvertX ? -1 : 1);
            _characterController.transform.localRotation = Quaternion.AngleAxis(CharacterRotationX, Vector3.up);

            CameraRotationY += _tempInputViewVector.y * RotationSensitivity * (InvertY ? -1 : 1);
            if (Mathf.Abs(CameraRotationY) > RotationLimitY) CameraRotationY = Mathf.Sign(CameraRotationY) * RotationLimitY;
            _characterCamera.transform.localRotation = Quaternion.AngleAxis(CameraRotationY, Vector3.right);
        }

        /// <summary>
        /// The update loop tracking if the player has jumped or not.
        /// </summary>
        void Jump()
        {
            if (Input.GetButtonDown(JumpButtonName) && IsGrounded && ClearHeadSpace)
            {
                if (_animationProcessor?.GetAnimator()) _animationProcessor.Jump();
                VerticalVelocity = JumpForce;
            }

            if (Input.GetButtonUp(JumpButtonName) && VerticalVelocity >= 0)
            {
                _lowJumpFlag = true;
            }
        }

        /// <summary>
        /// The update loop tracking if the player is running or not.
        /// </summary>
        void Run()
        {
            if (Input.GetButtonDown(RunButtonName))
            {
                _runButtonFlag = true;
                if (_runningCoroutine != null) StopCoroutine(_runningCoroutine);
                _runningCoroutine = ToggleRun(true);
                StartCoroutine(_runningCoroutine);
            }
            if (Input.GetButtonUp(RunButtonName))
            {
                _runButtonFlag = false;
                if (EnableSliding && IsSliding)
                {
                    if (_slidingCoroutine != null) StopCoroutine(_slidingCoroutine);
                    _slidingCoroutine = CancelSlide();
                    StartCoroutine(_slidingCoroutine);
                }
                else
                {
                    if (_runningCoroutine != null) StopCoroutine(_runningCoroutine);
                    _runningCoroutine = ToggleRun(false);
                    StartCoroutine(_runningCoroutine);
                }
            }
        }

        /// <summary>
        /// The update loop tracking if the player is crouching or not.
        /// </summary>
        void Crouch()
        {
            if (IsGrounded && !IsCrouching)
            {
                if (Input.GetButtonDown(CrouchButtonName))
                {
                    _crouchButtonFlag = true;
                    if (EnableSliding && IsRunning && _characterController.transform.InverseTransformDirection(_characterController.velocity).z >= MovementSpeed && MovementVector.z > 0 && _movementRunTransitionTime >= 1)
                    {
                        if (_slidingCoroutine != null) StopCoroutine(_slidingCoroutine);
                        _slidingCoroutine = Slide();
                        StartCoroutine(_slidingCoroutine);
                    }
                    else
                    {
                        if (_crouchingCoroutine != null) StopCoroutine(_crouchingCoroutine);
                        _crouchingCoroutine = ToggleCrouch(true);
                        StartCoroutine(_crouchingCoroutine);
                    }
                }
            }

            if (Input.GetButtonUp(CrouchButtonName))
            {
                if (ClearHeadSpace)
                {
                    _crouchButtonFlag = false;
                    if (EnableSliding && IsSliding)
                    {
                        if (_slidingCoroutine != null) StopCoroutine(_slidingCoroutine);
                        _slidingCoroutine = CancelSlide();
                        StartCoroutine(_slidingCoroutine);
                    }
                    else
                    {
                        if (_crouchingCoroutine != null) StopCoroutine(_crouchingCoroutine);
                        _crouchingCoroutine = ToggleCrouch(false);
                        StartCoroutine(_crouchingCoroutine);
                    }
                }
                else
                {
                    StartCoroutine(WaitForHeadspace());
                }
            }
        }
        #endregion Controls

        #region Help Functions
        /// <summary>
        /// Fits the character controller's collider to the mesh.
        /// </summary>
        /// <param name="meshObject">The model to fit the collider around</param>
        public void FitCharacterControllerToMesh(GameObject meshObject)
        {
            if (_characterController != null)
            {
                if (meshObject.GetComponentInChildren<SkinnedMeshRenderer>())
                {
                    SkinnedMeshRenderer skinnedRenderer = meshObject.GetComponentInChildren<SkinnedMeshRenderer>();
                    meshObject.transform.localPosition = new Vector3(0f, skinnedRenderer.sharedMesh.bounds.size.y * meshObject.transform.localScale.y / 2f, 0f);

                    skinnedRenderer.sharedMesh.RecalculateBounds();

                    _characterController.height = (skinnedRenderer.sharedMesh.bounds.size.y * meshObject.transform.localScale.y) - _characterController.skinWidth;
                    _characterController.radius = Mathf.Min(_characterController.height / 2, Mathf.Max(skinnedRenderer.sharedMesh.bounds.extents.x * meshObject.transform.localScale.x, skinnedRenderer.sharedMesh.bounds.extents.z * meshObject.transform.localScale.z));
                    _characterController.center = skinnedRenderer.sharedMesh.bounds.center + meshObject.transform.localPosition + (_characterController.skinWidth / 2 * Vector3.up);

                    _characterHeight = _characterController.height;
                    _characterRadius = _characterController.radius;
                    _cameraRelativeHeight = _characterCamera.transform.localPosition.y / _characterHeight;

                    _characterController.enabled = true;
                    return;
                }

                if (meshObject.GetComponentsInChildren<MeshFilter>().Any())
                {
                    MeshFilter[] meshFilters = meshObject.GetComponentsInChildren<MeshFilter>();

                    var totalBounds = new Bounds(Vector3.zero, Vector3.zero);
                    var meshCenter = Vector3.zero;

                    foreach (var mFilter in meshFilters)
                    {
                        var mMesh = mFilter.sharedMesh;
                        meshCenter += mMesh.bounds.center;
                    }
                    meshCenter /= meshFilters.Length;
                    totalBounds.center = meshCenter;

                    foreach (var mFilter in meshFilters)
                    {
                        var mMesh = mFilter.sharedMesh;
                        if (totalBounds.size == Vector3.zero)
                            totalBounds = mMesh.bounds;
                        else
                            totalBounds.Encapsulate(mMesh.bounds);
                    }

                    _characterController.height = (totalBounds.size.y * meshObject.transform.localScale.y) - _characterController.skinWidth;
                    _characterController.radius = Mathf.Min(_characterController.height / 2, Mathf.Max(totalBounds.extents.x * meshObject.transform.localScale.x, totalBounds.extents.z * meshObject.transform.localScale.z));
                    _characterController.center = totalBounds.center + meshObject.transform.localPosition + (_characterController.skinWidth / 2 * Vector3.up);

                    _characterHeight = _characterController.height;
                    _characterRadius = _characterController.radius;
                    _cameraRelativeHeight = _characterCamera.transform.localPosition.y / _characterHeight;

                    _characterController.enabled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Gradually changes the character's height over a set time.
        /// </summary>
        /// <param name="fromHeight">The starting height of the character</param>
        /// <param name="toHeight">The end height of the character</param>
        /// <param name="timer">The current state of the height lerping (0-1)</param>
        void PlayerHeightChange(float fromHeight, float toHeight, float timer)
        {
            toHeight -= _characterController.skinWidth;

            _characterController.height = Mathf.Lerp(fromHeight,
                                                     toHeight,
                                                     timer);

            _characterController.center = Vector3.Lerp(new Vector3(_characterController.center.x, fromHeight / 2, _characterController.center.z),
                                                       new Vector3(_characterController.center.x, toHeight / 2, _characterController.center.z),
                                                       timer);

            if (_characterController.radius * 2 >= _characterController.height)
            {
                _characterController.radius = _characterController.height / 2;
            }
            else
            {
                _characterController.radius = _characterRadius;
            }

            _characterCamera.transform.localPosition = Vector3.Lerp(new Vector3(_characterCamera.transform.localPosition.x, fromHeight * _cameraRelativeHeight, _characterCamera.transform.localPosition.z),
                                                                    new Vector3(_characterCamera.transform.localPosition.x, toHeight * _cameraRelativeHeight, _characterCamera.transform.localPosition.z),
                                                                    timer);
        }
        #endregion Help Functions

        #region IEnumerators
        /// <summary>
        /// Coroutine that affects the player's movement speed whilst airbound.
        /// </summary>
        IEnumerator WaitForGround()
        {
            _falling = true;
            while (_movementEaseTime > 0 && !IsGrounded)
            {
                _movementEaseTime -= Time.deltaTime * AerialMovementAcceleration;
                yield return new WaitForEndOfFrame();
            }
            yield return new WaitUntil(() => IsGrounded);

            _falling = false;
            _lowJumpFlag = false;
            MovementVector = new Vector3(_inputMoveVector.x, 0, _inputMoveVector.y);
        }

        /// <summary>
        /// Coroutine that waits for the player to have headspace to uncrouch.
        /// </summary>
        IEnumerator WaitForHeadspace()
        {
            yield return new WaitUntil(() => ClearHeadSpace);

            if (_crouchingCoroutine != null) StopCoroutine(_crouchingCoroutine);
            _crouchingCoroutine = ToggleCrouch(false);
            StartCoroutine(_crouchingCoroutine);
        }

        /// <summary>
        /// Coroutine that gradually changes between the player's walk speed and run speed.
        /// </summary>
        /// <param name="activateFlag">Should the player be running?</param>
        IEnumerator ToggleRun(bool activateFlag)
        {
            IsRunning = activateFlag;
            while (!IsGrounded)
            {
                yield return null;
            }

            if (activateFlag)
            {
                while (_movementRunTransitionTime < 1f)
                {
                    _movementRunTransitionTime += Time.fixedDeltaTime * MovementRunTransitionSpeed;
                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                while (_movementRunTransitionTime > 0f)
                {
                    _movementRunTransitionTime -= Time.fixedDeltaTime * MovementRunTransitionSpeed;
                    yield return new WaitForFixedUpdate();
                }
            }
        }

        /// <summary>
        /// Coroutine that toggles between the player's standard height and crouching height.
        /// </summary>
        /// <param name="activateFlag">Should the player be crouching?</param>
        IEnumerator ToggleCrouch(bool activateFlag)
        {
            _characterCrouchTransitionTime = 0;
            _movementRunTransitionTime = 0f;

            float startHeight = _characterController.height;
            if (activateFlag)
            {
                IsCrouching = true;

                while (_characterCrouchTransitionTime < 1f)
                {
                    _characterCrouchTransitionTime += Time.fixedDeltaTime * CharacterCrouchTransitionSpeed;
                    PlayerHeightChange(startHeight, CharacterCrouchHeight, _characterCrouchTransitionTime);
                    yield return new WaitForFixedUpdate();
                }
            }
            else
            {
                yield return new WaitUntil(() => !Physics.CheckSphere(transform.position + new Vector3(0f, CharacterCrouchHeight, 0f), GroundCheckRadius, GroundLayerMask));

                IsCrouching = false;

                while (_characterCrouchTransitionTime < 1f)
                {
                    _characterCrouchTransitionTime += Time.fixedDeltaTime * CharacterCrouchTransitionSpeed;
                    PlayerHeightChange(startHeight, _characterHeight, _characterCrouchTransitionTime);
                    yield return new WaitForFixedUpdate();
                }

                if (_runningCoroutine != null) StopCoroutine(_runningCoroutine);
                _runningCoroutine = ToggleRun(IsRunning);
                StartCoroutine(_runningCoroutine);
            }
        }

        /// <summary>
        /// Coroutine making the player slide in their movement direction.
        /// </summary>
        IEnumerator Slide()
        {
            MovementVector = SlidingVector;

            BlockInput = IsSliding = true;
            IsRunning = false;

            while (_characterSlideTransitionTime < 1f)
            {
                _characterSlideTransitionTime += Time.fixedDeltaTime * CharacterSlideTransitionSpeed;
                PlayerHeightChange(_characterHeight, CharacterSlideHeight, _characterSlideTransitionTime);
                yield return new WaitForFixedUpdate();
            }

            yield return new WaitForSeconds(MovementSlideLength);
            yield return new WaitUntil(() => IsGrounded);

            BlockInput = IsSliding = false;
            IsCrouching = true;
            IsRunning = true;

            yield return new WaitUntil(() => !Physics.CheckSphere(transform.position + new Vector3(0f, CharacterCrouchHeight, 0f), GroundCheckRadius, GroundLayerMask));

            while (_characterSlideTransitionTime > 0f)
            {
                _characterSlideTransitionTime -= Time.fixedDeltaTime * CharacterSlideTransitionSpeed;
                PlayerHeightChange(CharacterCrouchHeight, CharacterSlideHeight, _characterSlideTransitionTime);
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Coroutine cancelling the player's slide preemptively.
        /// </summary>
        IEnumerator CancelSlide()
        {
            yield return new WaitUntil(() => IsGrounded);

            BlockInput = IsSliding = false;
            IsCrouching = _crouchButtonFlag;
            IsRunning = ClearHeadSpace;

            _movementRunTransitionTime = 0f;

            if (_runningCoroutine != null) StopCoroutine(_runningCoroutine);
            _runningCoroutine = ToggleRun(_runButtonFlag);
            StartCoroutine(_runningCoroutine);

            yield return new WaitUntil(() => !Physics.CheckSphere(transform.position + new Vector3(0f, CharacterCrouchHeight, 0f), GroundCheckRadius, GroundLayerMask));

            while (_characterSlideTransitionTime > 0f)
            {
                _characterSlideTransitionTime -= Time.fixedDeltaTime * CharacterSlideTransitionSpeed;
                PlayerHeightChange(IsCrouching ? CharacterCrouchHeight : _characterHeight, CharacterSlideHeight, _characterSlideTransitionTime);
                yield return new WaitForFixedUpdate();
            }
        }
        #endregion IEnumerators
    }
}