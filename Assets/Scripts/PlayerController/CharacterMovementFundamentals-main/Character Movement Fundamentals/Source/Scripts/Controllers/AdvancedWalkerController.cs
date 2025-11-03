using System.Collections;
using UnityEngine;
using Proselyte.Sigils;
//using Proselyte.Persistence;

namespace CMF
{
	//Advanced walker controller script;
	//This controller is used as a basis for other controller types ('SidescrollerController');
	//Custom movement input can be implemented by creating a new script that inherits 'AdvancedWalkerController' and overriding the 'CalculateMovementDirection' function;
	public class AdvancedWalkerController : Controller 
	{
		//public PersistentId persistentId;

		//References to attached components;
		protected Transform tr;
		protected Mover mover;
		protected CeilingDetector ceilingDetector;
		protected SmoothPosition smoothPosition;

		[Header("Incoming References")]
		[SerializeField] Vector2Reference moveInput;
		[SerializeField] BoolReference crouchInput;
		[SerializeField] BoolReference sprintInput;
		[SerializeField] BoolReference jumpInput;

		[SerializeField] GameEvent OnSettingsToggleCrouch;

		[SerializeField] GameEvent OnPlayerPopulateDataRequest;
		[SerializeField] GameEvent OnPlayerApplyDataRequest;
		[SerializeField] PlayerSaveDataSO playerSaveDataSO;
		[SerializeField] UserSettingsDataSO userSettingsDataSO;
		
        //Jump key variables;
        bool jumpInputIsLocked = false;
        bool jumpKeyWasPressed = false;
		bool jumpKeyWasLetGo = false;
		bool jumpKeyIsPressed = false;

		//Crouch key fields
		private bool crouchInputIsLocked = false;
		private bool crouchKeyWasPressed = false;
		private bool crouchKeyIsPressed = false;
		private bool crouchToggleInit = false;

		//Movement speed;
		public float walkSpeed = 3f;
		public float sprintSpeed = 4.3f;

		// Movement fields
		public bool canMove = true;

		//How fast the controller can change direction while in the air;
		//Higher values result in more air control;
		public float airControlRate = 2f;

		//Jump speed;
		public float jumpSpeed = 10f;

		//Jump duration variables;
		public float jumpDuration = 0.2f;
		float currentJumpStartTime = 0f;

		//Crouch speed;
		[SerializeField] float crouchedSpeed = 1.5f;

        //Crouch variables;
        private bool crouchIsToggle = false;
		[SerializeField] float crouchedHeight = 1f;
		[SerializeField] float crouchTransTime = 0.5f;
		[SerializeField] float mantleTransTime = 0.5f;
        private bool wishStand = true;
        //private bool isMantling;
		private bool isCrouchMantle;
        private bool isCrouching;
		public bool IsCrouching => isCrouching;
		private float standingHeight;
		public float StandingHeight => standingHeight;
		public float CrouchedHeight => crouchedHeight;
		private float standingCamHeight;
		private float crouchedCamHeight;
		private Coroutine transitionCoroutine;
		[SerializeField] AudioSource bodyAudioSource;
		private float bodyAudioPitchInit;
		private float bodyAudioVolumeInit;
        private int prevBodyTransClipIndex = 0;
		[SerializeField] FootstepPack bodyTransitionSFX;

		// Mantle variables
		private Vector3 mantleStartCamPosWorld;
		[SerializeField] Transform playerCameraRootTransform; // used to get the main camera position
		[SerializeField] CameraController playerCameraControls; // used to get the main camera controls rotation
		[SerializeField] SmoothPosition playerCameraSmoothPositionComponent;

        //Ladder Climbing variables
        [SerializeField] float climbForce = 45f; // Force when climbing;
        [SerializeField] float sprintClimbForce = 60f; // Force when climbing but faster;
		[SerializeField] float clingForce = 10f; // Subtle force to keep player on ladder;
		public float ClimbForce => climbForce;
		public float SprintClimbForce => sprintClimbForce;
		public float ClingForce => clingForce;

		//'AirFriction' determines how fast the controller loses its momentum while in the air;
		//'GroundFriction' is used instead, if the controller is grounded;
		public float airFriction = 0.5f;
		public float groundFriction = 100f;

		//Current momentum;
		protected Vector3 momentum = Vector3.zero;

		//Saved velocity from last frame;
		Vector3 savedVelocity = Vector3.zero;

		//Saved horizontal movement velocity from last frame;
		Vector3 savedMovementVelocity = Vector3.zero;

		//Amount of downward gravity;
		public float gravity = 30f;
		[Tooltip("How fast the character will slide down steep slopes.")]
		public float slideGravity = 5f;
		
		//Acceptable slope angle limit;
		public float slopeLimit = 80f;

		[Tooltip("Whether to calculate and apply momentum relative to the controller's transform.")]
		public bool useLocalMomentum = false;

		//Enum describing basic controller states; 
		public enum ControllerState
		{
			Grounded,
			Sliding,
			Falling,
			Rising,
			Jumping
		}
		
		ControllerState currentControllerState = ControllerState.Falling;

		[Tooltip("Optional camera transform used for calculating movement direction. If assigned, character movement will take camera view into account.")]
		public Transform cameraTransform;

		//Inspection Variables
		private bool isInspecting;
		private bool isSitting;
		public bool IsInspecting => isInspecting;
		public bool IsSitting => isSitting;

        //Get references to all necessary components;
        void Awake()
        {
            mover = GetComponent<Mover>();
            tr = transform;
            ceilingDetector = GetComponent<CeilingDetector>();
            //mantleDetector = GetComponent<MantleDetector>();
            smoothPosition = GetComponentInChildren<SmoothPosition>();
            standingHeight = mover.ColliderHeight;
            standingCamHeight = smoothPosition.GetLocalHeight();
            crouchedCamHeight = (standingCamHeight / standingHeight) * crouchedHeight;

            // Init pitch and volume variables for crouching;
            bodyAudioPitchInit = bodyAudioSource.pitch;
            bodyAudioVolumeInit = bodyAudioSource.volume;
        }

        private void OnEnable()
        {
			OnPlayerPopulateDataRequest.RegisterListener(PlayerPopulateData);
			OnPlayerApplyDataRequest.RegisterListener(PlayerApplyPackedData);
			OnSettingsToggleCrouch.RegisterListener(SetCrouchIsToggle);
        }

        private void OnDisable()
        {
            OnPlayerPopulateDataRequest.UnregisterListener(PlayerPopulateData);
			OnPlayerApplyDataRequest.UnregisterListener(PlayerApplyPackedData);
            OnSettingsToggleCrouch.UnregisterListener(SetCrouchIsToggle);
        }

		public void SetCrouchIsToggle()
		{
            crouchIsToggle = userSettingsDataSO.crouchIsToggle;
        }
        private void Start()
        {
			SetCrouchIsToggle();
        }

        void Update()
		{
			HandleJumpKeyInput();
			HandleCrouchKeyInput();
		}

		public void PlayerPopulateData()
		{
			//playerSaveDataSO.persistentId = this.persistentId;
            playerSaveDataSO.position = transform.position;
			playerSaveDataSO.cameraPosition = playerCameraRootTransform.position;
			playerSaveDataSO.cameraRotation = playerCameraControls.transform.rotation;
			playerSaveDataSO.isCrouched = isCrouching;
			playerSaveDataSO.wishStand = wishStand;
            //playerSaveDataSO.isMantling = isMantling;
			//playerSaveDataSO.mantleTargetPos = mantleDetector.mantleSurfaceHitPos;
			//playerSaveDataSO.mantleWasCrouched = isCrouchMantle;
        }

		public void PlayerApplyPackedData()
		{
			crouchKeyWasPressed = false;

            mover.rig.isKinematic = true;
			mover.transform.position = playerSaveDataSO.position;
			mover.rig.isKinematic = false;
			//playerCameraRootTransform.position = playerSaveDataSO.cameraPosition;
			//playerCameraSmoothPositionComponent.ResetCurrentPosition();
            playerCameraControls.SetRotationAngles(playerSaveDataSO.cameraRotation.eulerAngles.x, playerSaveDataSO.cameraRotation.eulerAngles.y);

			wishStand = playerSaveDataSO.wishStand;

    //        // Reconstruct mantling / crouched state
    //        if(playerSaveDataSO.isMantling)
    //        {
    //            ApplyMantleStateInstant(playerSaveDataSO.mantleTargetPos, playerSaveDataSO.mantleWasCrouched);
    //        }
    //        else
    //        {
				//// Apply crouch state first
				//if(playerSaveDataSO.isCrouched)
    //                ApplyCrouchStateInstant(crouchedHeight, crouchedCamHeight);
    //            else
    //                ApplyCrouchStateInstant(standingHeight, standingCamHeight);

				//// Then apply camera position
    //            playerCameraRootTransform.position = playerSaveDataSO.cameraPosition;
    //            playerCameraSmoothPositionComponent.ResetCurrentPosition();

    //            // Initialize toggle state if player is crouched
    //            if(crouchIsToggle && playerSaveDataSO.isCrouched)
    //            {
    //                crouchToggleInit = true;
    //            }
    //        }

        }

        //Handle jump booleans for later use in FixedUpdate;
        void HandleJumpKeyInput()
        {
            bool _newJumpKeyPressedState = jumpInput.Value;

            if (jumpKeyIsPressed == false && _newJumpKeyPressedState == true)
                jumpKeyWasPressed = true;

            if (jumpKeyIsPressed == true && _newJumpKeyPressedState == false)
            {
                jumpKeyWasLetGo = true;
                jumpInputIsLocked = false;
            }

            jumpKeyIsPressed = _newJumpKeyPressedState;
        }

		void HandleCrouchKeyInput()
		{
			// Detect a new press;
			bool _newCrouchKeyPressedState = crouchInput.Value;
			if (crouchKeyIsPressed == false && _newCrouchKeyPressedState)
				crouchKeyWasPressed = true;

			// Detect a release;
			if(crouchKeyIsPressed && _newCrouchKeyPressedState == false)
			{
				crouchInputIsLocked = false;
			}

			crouchKeyIsPressed = _newCrouchKeyPressedState;
		}

        void FixedUpdate()
		{
			if(canMove) ControllerUpdate();
		}

		//Update controller;
		//This function must be called every fixed update, in order for the controller to work correctly;
		void ControllerUpdate()
		{
			//Check if mover is grounded;
			mover.CheckForGround();

			//Determine controller state;
			currentControllerState = DetermineControllerState();

			//Apply friction and gravity to 'momentum';
			HandleMomentum();

			//Check if the player has initiated a jump;
			HandleJumping();

			//Check if the player is attempting a crouch;
			HandleCrouching();

			//Calculate movement velocity;
			Vector3 _velocity = Vector3.zero;
			if(currentControllerState == ControllerState.Grounded)
				_velocity = CalculateMovementVelocity();
			
			//If local momentum is used, transform momentum into world space first;
			Vector3 _worldMomentum = momentum;
			if(useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			//Add current momentum to velocity;
			_velocity += _worldMomentum;
			
			//If player is grounded or sliding on a slope, extend mover's sensor range;
			//This enables the player to walk up/down stairs and slopes without losing ground contact;
			mover.SetExtendSensorRange(IsGrounded());

			//Set mover velocity;		
			mover.SetVelocity(_velocity);

			//Store velocity for next frame;
			savedVelocity = _velocity;
		
			//Save controller movement velocity;
			savedMovementVelocity = CalculateMovementVelocity();

			//Reset jump key booleans;
			jumpKeyWasLetGo = false;
			jumpKeyWasPressed = false;

			//Reset crouch key booleans;
			crouchKeyWasPressed = false;

			//Reset ceiling detector, if one is attached to this gameobject;
			if(ceilingDetector != null)
				ceilingDetector.ResetFlags();
		}

		//Calculate and return movement direction based on player input;
		protected Vector3 CalculateMovementDirection()
		{
			Vector3 _velocity = Vector3.zero;
            _velocity += Vector3.ProjectOnPlane(cameraTransform.right, tr.up).normalized * moveInput.Value.x;
            _velocity += Vector3.ProjectOnPlane(cameraTransform.forward, tr.up).normalized * moveInput.Value.y;

            return _velocity;
		}

		//Calculate and return movement velocity based on player input, controller state, ground normal [...];
		protected Vector3 CalculateMovementVelocity()
		{
			// Early exit if player move input is disabled;
			if(!canMove) return Vector3.zero;
			
			//Calculate (normalized) movement direction;
			Vector3 _velocity = CalculateMovementDirection();

			//Multiply (normalized) velocity with movement speed;
			if(isCrouching)
			{
				_velocity *= crouchedSpeed;
			} else if(sprintInput.Value)
			{
				_velocity *= sprintSpeed;
			} else
			{
				_velocity *= walkSpeed;
			}

			return _velocity;
		}

		//Determine current controller state based on current momentum and whether the controller is grounded (or not);
		//Handle state transitions;
		ControllerState DetermineControllerState()
		{
			//Check if vertical momentum is pointing upwards;
			bool _isRising = IsRisingOrFalling() && (VectorMath.GetDotProduct(GetMomentum(), tr.up) > 0f);
			//Check if controller is sliding;
			bool _isSliding = mover.IsGrounded() && IsGroundTooSteep();
			
			//Grounded;
			if(currentControllerState == ControllerState.Grounded)
			{
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if(_isSliding){
					OnGroundContactLost();
					return ControllerState.Sliding;
				}
				return ControllerState.Grounded;
			}

			//Falling;
			if(currentControllerState == ControllerState.Falling)
			{
				if(_isRising){
					return ControllerState.Rising;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				if(_isSliding){
					return ControllerState.Sliding;
				}
				return ControllerState.Falling;
			}
			
			//Sliding;
			if(currentControllerState == ControllerState.Sliding)
			{	
				if(_isRising){
					OnGroundContactLost();
					return ControllerState.Rising;
				}
				if(!mover.IsGrounded()){
					OnGroundContactLost();
					return ControllerState.Falling;
				}
				if(mover.IsGrounded() && !_isSliding){
					OnGroundContactRegained();
					return ControllerState.Grounded;
				}
				return ControllerState.Sliding;
			}

			//Rising;
			if(currentControllerState == ControllerState.Rising)
			{
				if(!_isRising){
					if(mover.IsGrounded() && !_isSliding){
						OnGroundContactRegained();
						return ControllerState.Grounded;
					}
					if(_isSliding){
						return ControllerState.Sliding;
					}
					if(!mover.IsGrounded()){
						return ControllerState.Falling;
					}
				}

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Rising;
			}

			//Jumping;
			if(currentControllerState == ControllerState.Jumping)
			{
				//Check for jump timeout;
				if((Time.time - currentJumpStartTime) > jumpDuration)
					return ControllerState.Rising;

				//Check if jump key was let go;
				if(jumpKeyWasLetGo)
					return ControllerState.Rising;

				//If a ceiling detector has been attached to this gameobject, check for ceiling hits;
				if(ceilingDetector != null)
				{
					if(ceilingDetector.HitCeiling())
					{
						OnCeilingContact();
						return ControllerState.Falling;
					}
				}
				return ControllerState.Jumping;
			}

			
			return ControllerState.Falling;
		}

        //Check if player has initiated a jump;
        void HandleJumping()
        {
            if (currentControllerState == ControllerState.Grounded)
            {
                if ((jumpKeyWasPressed) && !jumpInputIsLocked)
                {
					// Jump input was pressed, check for surfaces to mantle infront of the player;
					Vector3 mantlePos = Vector3.zero;
     //               if(mantleDetector != null && 
					//	mantleDetector.CanMantle
					//	(
					//		moverGroundPos: mover.GetGroundPoint(), 
					//		camTF: cameraTransform, 
					//		stepHeightRatio: mover.GetStepHeightRatio(), 
					//		capColl: mover.GetCollider(), 
					//		mantlePos: out mantlePos,
					//		crouchMantle: out bool crouchMantle
					//	)
					//)
     //               {
					//	// Mantle is valid, initiate mantle coroutine with specified stand/crouched mantle;
					//	//OnMantleStart(mantlePos, crouchMantle);
     //               } else
					{
						Debug.Log("No mantle target detected, Jumping");
						// else Handle regular jump;
						// Call events;
						OnGroundContactLost();
						OnJumpStart();

						currentControllerState = ControllerState.Jumping;
					}
                }
            }
        }

        // Check if player has changed crouch inputs;
        void HandleCrouching()
        {
            // Ensure the player is grounded before processing crouch/stand logic;
            if(currentControllerState == ControllerState.Grounded && !crouchInputIsLocked)
            {
                // Toggle mode
                if(crouchIsToggle)
                {
                    // If the crouch key was just pressed, toggle the crouch state;
                    if(crouchKeyWasPressed)
                    {
                        if(!crouchToggleInit)
                            crouchToggleInit = true;

                        wishStand = !wishStand;
                        crouchKeyWasPressed = false;
                    }

                    // Handle edge case where character uses toggle mode for crouching but hasn't pressed crouch key yet;
                    if(!crouchToggleInit)
                        return;

                    if(!isCrouching && !wishStand)
                    {
                        OnCrouchStart();
                    }
                    else if(isCrouching && wishStand)
                    {
                        // Attempt to stand up;
                        if(ceilingDetector == null || (!ceilingDetector.HitCeiling() && ceilingDetector.CanStandUp(standingHeight, crouchedHeight, mover.GetCollider())))
                        {
                            OnCrouchEnd();
                        }
                        // Else remain crouching if space is insufficient;
                    }
                }
                // Hold mode;
                else
                {
                    if(crouchKeyIsPressed)
                    {
                        // Begin or continue crouching;
                        if(!isCrouching)
                        {
                            OnCrouchStart();
                        }
                    }
                    else if(isCrouching)
                    {
                        // Attempt to stand up if not holding crouch;
                        if(!ceilingDetector.HitCeiling() && ceilingDetector.CanStandUp(standingHeight, crouchedHeight, mover.GetCollider()))
                        {
                            OnCrouchEnd();
                        }
                        // Else remain crouched due to obstacle;
                    }
                }
            }
        }

        // Apply friction to both vertical and horizontal momentum based on 'friction' and 'gravity';
        // Handle movement in the air;
        // Handle sliding down steep slopes;
        void HandleMomentum()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			Vector3 _verticalMomentum = Vector3.zero;
			Vector3 _horizontalMomentum = Vector3.zero;

			//Split momentum into vertical and horizontal components;
			if(momentum != Vector3.zero)
			{
				_verticalMomentum = VectorMath.ExtractDotVector(momentum, tr.up);
				_horizontalMomentum = momentum - _verticalMomentum;
			}

			//Add gravity to vertical momentum;
			_verticalMomentum -= tr.up * gravity * Time.deltaTime;

			//Remove any downward force if the controller is grounded;
			if(currentControllerState == ControllerState.Grounded && VectorMath.GetDotProduct(_verticalMomentum, tr.up) < 0f)
				_verticalMomentum = Vector3.zero;

			//Manipulate momentum to steer controller in the air (if controller is not grounded or sliding);
			if(!IsGrounded())
			{
				Vector3 _movementVelocity = CalculateMovementVelocity();

				//If controller has received additional momentum from somewhere else;
				if(_horizontalMomentum.magnitude > walkSpeed)
				{
					//Prevent unwanted accumulation of speed in the direction of the current momentum;
					if(VectorMath.GetDotProduct(_movementVelocity, _horizontalMomentum.normalized) > 0f)
						_movementVelocity = VectorMath.RemoveDotVector(_movementVelocity, _horizontalMomentum.normalized);
					
					//Lower air control slightly with a multiplier to add some 'weight' to any momentum applied to the controller;
					float _airControlMultiplier = 0.25f;
					_horizontalMomentum += _movementVelocity * Time.deltaTime * airControlRate * _airControlMultiplier;
				}
				//If controller has not received additional momentum;
				else
				{
					//Clamp _horizontal velocity to prevent accumulation of speed;
					_horizontalMomentum += _movementVelocity * Time.deltaTime * airControlRate;
					_horizontalMomentum = Vector3.ClampMagnitude(_horizontalMomentum, walkSpeed);
				}
			}

			//Steer controller on slopes;
			if(currentControllerState == ControllerState.Sliding)
			{
				//Calculate vector pointing away from slope;
				Vector3 _pointDownVector = Vector3.ProjectOnPlane(mover.GetGroundNormal(), tr.up).normalized;

				//Calculate movement velocity;
				Vector3 _slopeMovementVelocity = CalculateMovementVelocity();
				//Remove all velocity that is pointing up the slope;
				_slopeMovementVelocity = VectorMath.RemoveDotVector(_slopeMovementVelocity, _pointDownVector);

				//Add movement velocity to momentum;
				_horizontalMomentum += _slopeMovementVelocity * Time.fixedDeltaTime;
			}

			//Apply friction to horizontal momentum based on whether the controller is grounded;
			if(currentControllerState == ControllerState.Grounded)
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, groundFriction, Time.deltaTime, Vector3.zero);
			else
				_horizontalMomentum = VectorMath.IncrementVectorTowardTargetVector(_horizontalMomentum, airFriction, Time.deltaTime, Vector3.zero); 

			//Add horizontal and vertical momentum back together;
			momentum = _horizontalMomentum + _verticalMomentum;

			//Additional momentum calculations for sliding;
			if(currentControllerState == ControllerState.Sliding)
			{
				//Project the current momentum onto the current ground normal if the controller is sliding down a slope;
				momentum = Vector3.ProjectOnPlane(momentum, mover.GetGroundNormal());

				//Remove any upwards momentum when sliding;
				if(VectorMath.GetDotProduct(momentum, tr.up) > 0f)
					momentum = VectorMath.RemoveDotVector(momentum, tr.up);

				//Apply additional slide gravity;
				Vector3 _slideDirection = Vector3.ProjectOnPlane(-tr.up, mover.GetGroundNormal()).normalized;
				momentum += _slideDirection * slideGravity * Time.deltaTime;
			}
			
			//If controller is jumping, override vertical velocity with jumpSpeed;
			if(currentControllerState == ControllerState.Jumping)
			{
				momentum = VectorMath.RemoveDotVector(momentum, tr.up);
				momentum += tr.up * jumpSpeed;
			}

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

        // Transitions;

        // This function handles the mantle transition;
        IEnumerator MantleTransition(Vector3 targetMantlePos, bool crouchMantle)
        {
            // Choose an appropriate sound to play for the mantle transition;
            int tries = 0;
            int bodyTransClipIndex;
            do {
                bodyTransClipIndex = Random.Range(0, bodyTransitionSFX.footsteps.Length);
                tries++;
            } while(tries < 4 && prevBodyTransClipIndex == bodyTransClipIndex);
            prevBodyTransClipIndex = bodyTransClipIndex;

            // Assign variations and play mantle transition sound from audio source;
            bodyAudioSource.volume = HelperScript.Deviate(bodyAudioVolumeInit, 0.15f);
            bodyAudioSource.pitch = HelperScript.Deviate(bodyAudioPitchInit, 0.15f);
            bodyAudioSource.clip = bodyTransitionSFX.footsteps[bodyTransClipIndex];
            bodyAudioSource.Play();

			// Record the camera position in local space before setting the collider position
			mantleStartCamPosWorld = playerCameraRootTransform.position;

			// The collider will be instantly placed at the correct position and height;
			if(crouchMantle)
				mover.SetColliderHeight(crouchedHeight);
			else
				mover.SetColliderHeight(standingHeight);
			tr.position = targetMantlePos;

			// We need to use the cached camera's position to smoothly lerp it to the target position;
			// This target is based on whether the player's mantle target is standing or crouched;

			// Pre-calculate standing/crouching/current camera positions;
			Vector3 targetMantleCamPos;
			if (crouchMantle)
                targetMantleCamPos = targetMantlePos + transform.up * crouchedCamHeight;
			else
                targetMantleCamPos = targetMantlePos + transform.up * standingCamHeight;

			// Transform cam points into local space for the lerp;
			Vector3 mantleStartCamPosLocal = tr.InverseTransformPoint(mantleStartCamPosWorld);
			targetMantleCamPos = tr.InverseTransformPoint(targetMantleCamPos);

            // Precalculate mantle loop variables;
            float elapsedTime = 0f;
            while(elapsedTime < mantleTransTime)
            {
                // Calculate interpolation factor;
                float t = elapsedTime / mantleTransTime;
                t = HelperScript.EaseInOut(t);

                // Lerp camera height;
                Vector3 transCamPos = Vector3.Lerp(mantleStartCamPosLocal, targetMantleCamPos, t);
                smoothPosition.SetLocalPosition(transCamPos);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Finalize camera position to target values;
            smoothPosition.SetLocalPosition(targetMantleCamPos);

            transitionCoroutine = null; // Clear the coroutine reference;

			OnMantleEnd();
        }

        //This function handles the crouch transition;
        IEnumerator CrouchTransition
		(
			float startColliderHeight, 
			float targetColliderHeight, 
			float startCamHeight, 
			float targetCamHeight
		)
        {
			// Select a different sound to play each crouch;
            int tries = 0;
            int crouchTransClipIndex = 0;
            do {
                crouchTransClipIndex = Random.Range(0, bodyTransitionSFX.footsteps.Length);
                tries++;
            } while(tries < 4 && prevBodyTransClipIndex == crouchTransClipIndex);
            prevBodyTransClipIndex = crouchTransClipIndex;

			// Assign variations and play crouch transition sound from audio source;
            bodyAudioSource.volume = HelperScript.Deviate(bodyAudioVolumeInit, 0.15f);
            bodyAudioSource.pitch = HelperScript.Deviate(bodyAudioPitchInit, 0.15f);
            bodyAudioSource.clip = bodyTransitionSFX.footsteps[crouchTransClipIndex];
            bodyAudioSource.Play();

			// Precalculate crouch loop variables
            float elapsedTime = 0f;
            float duration = crouchTransTime * Mathf.Abs(startColliderHeight - targetColliderHeight) / (standingHeight - crouchedHeight);
            while(elapsedTime < duration)
            {
                // Calculate interpolation factor;
                float t = elapsedTime / duration;
                t = HelperScript.EaseInOut(t); // Apply easing;

                // Lerp collider height;
                float transHeight = Mathf.Lerp(startColliderHeight, targetColliderHeight, t);
                mover.SetColliderHeight(transHeight);

                // Lerp camera height;
                float transCamHeight = Mathf.Lerp(startCamHeight, targetCamHeight, t);
                smoothPosition.SetLocalHeight(transCamHeight);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Finalize heights to target values;
            mover.SetColliderHeight(targetColliderHeight);
            smoothPosition.SetLocalHeight(targetCamHeight);

            transitionCoroutine = null; // Clear the coroutine reference;
        }

        // Events;

        // This function is called when the player has initiated a jump;
        void OnJumpStart()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Add jump force to momentum;
			momentum += tr.up * jumpSpeed;

			//Set jump start time;
			currentJumpStartTime = Time.time;

            //Lock jump input until jump key is released again;
            jumpInputIsLocked = true;

            //Call event;
            if (OnJump != null)
				OnJump(momentum);

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		// This function is called when the player has intiated a mantle;
		void OnMantleStart(Vector3 targetMantlePos, bool crouchMantle)
		{
            Debug.Log("Mantle Started");
			//isMantling = true;
			isCrouchMantle = crouchMantle;

            // Restrict player controls while mantling;
            canMove = false;
            jumpInputIsLocked = true;

			// Handle crouched state variables;
			if(crouchMantle)
			{
                isCrouching = true;

                // Call event;
                if(OnCrouchDown != null)
                    OnCrouchDown();
            }

            // Stop any ongoing transition coroutine (i.e, crouching);
            if(transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

			// Start the mantle transition;
            StartCoroutine(MantleTransition(targetMantlePos, crouchMantle));
		}

		// This function is called when a mantle is completed;
		void OnMantleEnd()
		{
			// Restore player controls;
			//isMantling = false;
			canMove = true;
		}

        // This function is called when the player has initiated a crouch;
        void OnCrouchStart()
        {
            // Set crouch state;
            isCrouching = true;
            jumpInputIsLocked = true;

            // Stop any ongoing transition coroutine;
            if(transitionCoroutine != null)
            {
                StopCoroutine(transitionCoroutine);
            }

            // Start crouching transition;
            transitionCoroutine = StartCoroutine(CrouchTransition
			(
				mover.ColliderHeight, 
				crouchedHeight, 
				standingCamHeight, 
				crouchedCamHeight
			));

            // Call event;
            if(OnCrouchDown != null)
                OnCrouchDown();
        }

		// This function is called when the player begins standing from a crouch;
        void OnCrouchEnd()
        {
            // Set crouch state;
            isCrouching = false;
            jumpInputIsLocked = false;

            // Stop any ongoing coroutine;
            if(transitionCoroutine != null)
            {
				StopCoroutine(transitionCoroutine);
            }

            // Start standing transition, store it in the exclusive coroutines list;
            transitionCoroutine = StartCoroutine(CrouchTransition
			(
				mover.ColliderHeight, 
				standingHeight, 
				crouchedCamHeight, 
				standingCamHeight
			));

            // Call event;
            if(OnCrouchUp != null)
                OnCrouchUp();
        }

        private void ApplyCrouchStateInstant(float colliderHeight, float camHeight)
        {
            isCrouching = (colliderHeight == crouchedHeight);
            jumpInputIsLocked = isCrouching;

            mover.SetColliderHeight(colliderHeight);
            playerCameraSmoothPositionComponent.SetLocalHeight(camHeight);
        }

        private void ApplyMantleStateInstant(Vector3 targetMantlePos, bool crouchMantle)
        {
            isCrouching = crouchMantle;

            // Set collider height
            mover.SetColliderHeight(crouchMantle ? crouchedHeight : standingHeight);

            // Set player position
            transform.position = targetMantlePos;

			Vector3 cameraOffset = new Vector3(0, (crouchMantle ? crouchedCamHeight : standingCamHeight), 0);

            // Calculate and apply camera world position
            playerCameraRootTransform.position = targetMantlePos + cameraOffset;

			// wishStand should reflect the OPPOSITE of the current crouch state
			if(crouchIsToggle)
			{
				crouchToggleInit = true;
			}

                // Reset smoothing
            playerCameraSmoothPositionComponent.SetLocalPosition(cameraOffset);
            playerCameraSmoothPositionComponent.ResetCurrentPosition();

            OnMantleEnd();
        }



        // This function is called when the controller has lost ground contact;
        // i.e. is either falling or rising, or generally in the air;
        void OnGroundContactLost()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Get current movement velocity;
			Vector3 _velocity = GetMovementVelocity();

			//Check if the controller has both momentum and a current movement velocity;
			if(_velocity.sqrMagnitude >= 0f && momentum.sqrMagnitude > 0f)
			{
				//Project momentum onto movement direction;
				Vector3 _projectedMomentum = Vector3.Project(momentum, _velocity.normalized);
				//Calculate dot product to determine whether momentum and movement are aligned;
				float _dot = VectorMath.GetDotProduct(_projectedMomentum.normalized, _velocity.normalized);

				//If current momentum is already pointing in the same direction as movement velocity,
				//Don't add further momentum (or limit movement velocity) to prevent unwanted speed accumulation;
				if(_projectedMomentum.sqrMagnitude >= _velocity.sqrMagnitude && _dot > 0f)
					_velocity = Vector3.zero;
				else if(_dot > 0f)
					_velocity -= _projectedMomentum;	
			}

			//Add movement velocity to momentum;
			momentum += _velocity;

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		// This function is called when the controller has landed on a surface after being in the air;
		void OnGroundContactRegained()
		{
			//Call 'OnLand' event;
			if(OnLand != null)
			{
				Vector3 _collisionVelocity = momentum;
				//If local momentum is used, transform momentum into world coordinates first;
				if(useLocalMomentum)
					_collisionVelocity = tr.localToWorldMatrix * _collisionVelocity;

				OnLand(_collisionVelocity);
			}
				
		}

		// This function is called when the controller has collided with a ceiling while jumping or moving upwards;
		void OnCeilingContact()
		{
			//If local momentum is used, transform momentum into world coordinates first;
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			//Remove all vertical parts of momentum;
			momentum = VectorMath.RemoveDotVector(momentum, tr.up);

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		//Helper functions;

		//Returns 'true' if vertical momentum is above a small threshold;
		private bool IsRisingOrFalling()
		{
			//Calculate current vertical momentum;
			Vector3 _verticalMomentum = VectorMath.ExtractDotVector(GetMomentum(), tr.up);

			//Setup threshold to check against;
			//For most applications, a value of '0.001f' is recommended;
			float _limit = 0.001f;

			//Return true if vertical momentum is above '_limit';
			return(_verticalMomentum.magnitude > _limit);
		}

		// Returns true if angle between controller and ground normal is too big (> slope limit);
		// i.e. ground is too steep;
		private bool IsGroundTooSteep()
		{
			if(!mover.IsGrounded())
				return true;

			return (Vector3.Angle(mover.GetGroundNormal(), tr.up) > slopeLimit);
		}

		// Getters;

		// Get last frame's velocity;
		public override Vector3 GetVelocity ()
		{
			return savedVelocity;
		}

		// Get last frame's movement velocity (momentum is ignored);
		public override Vector3 GetMovementVelocity()
		{
			return savedMovementVelocity;
		}

		// Get current momentum;
		public Vector3 GetMomentum()
		{
			Vector3 _worldMomentum = momentum;
			if(useLocalMomentum)
				_worldMomentum = tr.localToWorldMatrix * momentum;

			return _worldMomentum;
		}

		// Returns 'true' if controller is grounded (or sliding down a slope);
		public override bool IsGrounded()
		{
			return(currentControllerState == ControllerState.Grounded || currentControllerState == ControllerState.Sliding);
		}

		// Returns 'true' if controller is sliding;
		public bool IsSliding()
		{
			return(currentControllerState == ControllerState.Sliding);
		}

		// Add momentum to controller;
		public void AddMomentum (Vector3 _momentum)
		{
			if(useLocalMomentum)
				momentum = tr.localToWorldMatrix * momentum;

			momentum += _momentum;	

			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * momentum;
		}

		// Set controller momentum directly;
		public void SetMomentum(Vector3 _newMomentum)
		{
			if(useLocalMomentum)
				momentum = tr.worldToLocalMatrix * _newMomentum;
			else
				momentum = _newMomentum;
		}

        public void SetInspectionState(bool value)
        {
            isInspecting = value;
        }

        public void SetSittingState(bool value)
        {
            isSitting = value;
            //standPromptTMP.text = value ? standPrompt : string.Empty;
        }
    }
}
