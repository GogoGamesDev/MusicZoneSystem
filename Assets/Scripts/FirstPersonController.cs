using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FirstPersonController : MonoBehaviour
{
    public bool CanMove;
    public bool IsSprinting = false; //> canSprint && Input.GetAxis("sprintKey");
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;
    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;
    public bool isFPS = true;
    public PauseManager pause;


    
    [Header("Function Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadbob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;
    [SerializeField] public bool useFootsteps = true;
    [SerializeField] private bool useStamina = true;
    [SerializeField] private bool useFlashlight = true;
    [SerializeField] private bool useHP = true;

    [Header("Controls")]
    /*[SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode zoomKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode interactKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode flashlightKey = KeyCode.F;*/
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Movement Parameters")]
    [SerializeField] public float walkSpeed = 6.0f;
    [SerializeField] public float sprintSpeed = 9.0f;
    [SerializeField] private float crouchSpeed = 3.0f;
    [SerializeField] private float slopeSpeed = 8f;
       
    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Health Parameters")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float timeBeforeRegenStarts = 3;
    [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    public float currentHealth;
    private Coroutine regeneratingHealth;
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100;
    [SerializeField] private float staminaUseMultiplier = 5;
    [SerializeField] private float timeBeforeStaminaRegenStarts = 5;
    [SerializeField] private float staminaValueIncrement = 2;
    [SerializeField] private float staminaTimeIncrement = 0.1f;
    private float currentStamina;
    private Coroutine regeneratingStamina;
    public static Action<float> OnStaminaChange;


    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouching Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0,0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0,0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;
    
    [Header("Headbob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.11f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;

    [Header("Zoom Parameters")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;

    [Header("Footsteps Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultiplier = 1.5f;
    [SerializeField] private float sprintStepMultiplier = 0.6f;
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] woodClips = default; 
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    [SerializeField] private AudioClip[] carpetClips = default;
    [SerializeField] private AudioClip[] waterClips = default;
    [SerializeField] private AudioClip[] woodClipsVarian = default;
    [SerializeField] private AudioClip[] glassClips = default;
    [SerializeField] private AudioClip[] longWaterClips = default;



    private float footstepTimer = 0;
    [SerializeField] private float footsteepVolume = 1f;
    private float GetCurrentOfset => isCrouching ? baseStepSpeed * crouchStepMultiplier : IsSprinting ? baseStepSpeed * sprintStepMultiplier : baseStepSpeed;

    [Header("Flashlights Parameters")]
    [SerializeField] private AudioSource flashlightAudioSource = default;
    [SerializeField] private AudioClip[] flashlightClips = default;
    [SerializeField] private Light flashlightObj;

    //Sliding Parameters
    private Vector3 hitPointNormal;
    private bool IsSliding
    {
        get
        {
            if(characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                hitPointNormal = slopeHit.normal;
                return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
            }
            else
            {
                return false;
            }
        }
    }
 
    [Header("Interaction")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayer;
    private Interactable currentInteractable;

    [Header("Animation")]
    public Animator anim;
    private bool isWalking = false;

    private Camera playerCamera;
    private CharacterController characterController;
    private Vector3 moveDirection;
    private Vector2 currentInput;
    private float rotationX = 0;
    public static FirstPersonController instance;

    private void OnEnable()
    {
        OnTakeDamage += ApplyDamage;
    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        instance = this;
        //Get camera/controller components
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        defaultFOV = playerCamera.fieldOfView;
    }

    void Update()
    {
        if(CanMove == true && pause.isPaused == false)
        {
            //Cursor Lock
            Cursor.visible = false; 
            Cursor.lockState = CursorLockMode.Locked;

            HandleMovementInput();
            HandleMouseLook();
            
            if(canJump)
            {
               HandleJump();
            }

            if(canCrouch)
            {
                HandleCrouch();
            }

            if(canUseHeadbob)
            {
                handleHeadbob();
            }

            if(canZoom)
            {
                HandleZoom();
            }
            
            if(useFootsteps)
            {
                Handle_Footsteps();
            }

            if(canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }
            
            if(useStamina)
            {
                handleStamina();
            }

            if(useFlashlight)
            {
                handleFlashlight();
            }
            ApplyFinalMovements();
        }
    }

    private void HandleMovementInput()
    {
        if(canSprint && Input.GetAxis("sprintKey") > 0)
        {
            IsSprinting = true;
        }
        else
        {
            IsSprinting = false;
        }
        if(isFPS == true)
        {
            currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

            float moveDirectionY = moveDirection.y;
            moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
            moveDirection.y = moveDirectionY;
        }
        else
        {
            isWalking = !isWalking;

            currentInput = new Vector2((IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

            float moveDirectionY = moveDirection.y;
            moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
            moveDirection.y = moveDirectionY;

            // Verifica si el jugador está caminando
            isWalking = Mathf.Abs(currentInput.x) > 0.1f || Mathf.Abs(currentInput.y) > 0.1f;

            // Actualiza el parámetro "IsWalking" en el Animator
            anim.SetBool("isWalking", isWalking);
        }
        
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX  = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void HandleJump()
    {
        if(ShouldJump)
        {
            moveDirection.y = jumpForce;
        }
    }

    private void HandleCrouch()
    {
        if(ShouldCrouch)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private void handleHeadbob()
    {   
        if(Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x, 
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void handleStamina()
    {
        if(IsSprinting && currentInput != Vector2.zero)
        {
            if(regeneratingStamina != null)
            {
                StopCoroutine(regeneratingStamina);
                regeneratingStamina = null;
            }

            currentStamina -= staminaUseMultiplier * Time.deltaTime;
            if(currentStamina < 0)
            {
                currentStamina = 0;
            }

            OnStaminaChange?.Invoke(currentStamina);

            if(currentStamina <= 0)
            {
                canSprint = false;
            }
        }
        if(!IsSprinting && currentStamina < maxStamina && regeneratingStamina == null)
        {
            regeneratingStamina = StartCoroutine(RegenerateStamina());
        }
    }

    private void HandleZoom()
    {
        if(canZoom && Input.GetButtonDown("zoomKey"))
        {
            if(zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToogleZoom(true));
        }

        if (Input.GetButtonUp("zoomKey"))
        {
            if(zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }

            zoomRoutine = StartCoroutine(ToogleZoom(false));
        }
    }

    private void handleFlashlight()
    {
        if(Input.GetButtonDown("flashlightKey"))
        {
            if (flashlightObj.enabled)
            {
                flashlightOff();
            }
            else
            {
                flashlightOn();
            }
        }
    }

    public void flashlightOff()
    {
        flashlightObj.enabled = false;
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.PlayOneShot(flashlightClips[UnityEngine.Random.Range(0, flashlightClips.Length -1)]);
        }
    }

    public void flashlightOn()
    {
        flashlightObj.enabled = true;
        if (flashlightAudioSource != null)
        {
            flashlightAudioSource.PlayOneShot(flashlightClips[UnityEngine.Random.Range(0, flashlightClips.Length -1)]);
        }
    }

    private void HandleInteractionCheck()
    {
        if(Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            if(hit.collider.gameObject.layer == 6 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractable);

                if(currentInteractable)
                {
                    currentInteractable.OnFocus();
                }
            }
        }
        else if(currentInteractable)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    private void HandleInteractionInput()
    {
        if(Input.GetButtonDown("interactKey") && currentInteractable != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayer))
        {
            currentInteractable.OnInteract();
        }
    }
    private void Handle_Footsteps()
    {
        if(!characterController.isGrounded) return;
        if(currentInput == Vector2.zero) return;

        
        footstepTimer -= Time.deltaTime;

        if(footstepTimer <= 0)
        {
            footstepAudioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            if(Physics.Raycast(characterController.transform.position, Vector3.down, out RaycastHit hit, 3))
            {
                footstepAudioSource.volume = footsteepVolume;
                switch(hit.collider.tag)
                {
                    case "Foootsteps/WOOD":
                        footstepAudioSource.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length -1)]);
                        break;
                    case "Footsteps/METAL":
                        footstepAudioSource.PlayOneShot(metalClips[UnityEngine.Random.Range(0, metalClips.Length -1)]);
                        break;
                    case "Footsteps/GRASS":
                        footstepAudioSource.PlayOneShot(grassClips[UnityEngine.Random.Range(0, grassClips.Length -1)]);
                        break;
                    case "Footsteps/CARPET":
                        footstepAudioSource.PlayOneShot(carpetClips[UnityEngine.Random.Range(0, carpetClips.Length -1)]);
                        break;
                    case "Footsteps/WATER":
                        footstepAudioSource.PlayOneShot(waterClips[UnityEngine.Random.Range(0, waterClips.Length -1)]);
                        break;
                    case "Footsteps/WOODV":
                        footstepAudioSource.PlayOneShot(woodClipsVarian[UnityEngine.Random.Range(0, woodClipsVarian.Length -1)]);
                        break;
                    case "Footsteps/GLASS":
                        footstepAudioSource.PlayOneShot(glassClips[UnityEngine.Random.Range(0, glassClips.Length -1)]);
                        break;
                    case "Footsteps/LONGWATER":
                        footstepAudioSource.PlayOneShot(longWaterClips[UnityEngine.Random.Range(0, longWaterClips.Length -1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(woodClips[UnityEngine.Random.Range(0, woodClips.Length -1)]);
                        break;
                }
            }

            footstepTimer = GetCurrentOfset;
        }
        
    }

    private void ApplyDamage(float dmg)
    {
        if(useHP == true)
        {
            currentHealth -= dmg;
            OnDamage?.Invoke(currentHealth);

            if(currentHealth <= 0)
            {
              KillPlayer();
            }
            else if(regeneratingHealth != null)
            {
              StopCoroutine(regeneratingHealth);
            }

            regeneratingHealth = StartCoroutine(RegenerateHealth());
        }
        

    }
    
    private void KillPlayer()
    {
        currentHealth = 0;
        if(regeneratingHealth != null)
        {
            StopCoroutine(regeneratingHealth);
        }
        print("DEAD");
    }

    private void ApplyFinalMovements()
    {
        if(!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        if(willSlideOnSlopes && IsSliding)
        {
            moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSpeed;
        }
        characterController.Move(moveDirection * Time.deltaTime);
    }

    private IEnumerator CrouchStand()
    {
        if(isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
        {
            yield break;
        }
        duringCrouchAnimation = true;

        float timeElapsed = 0;
        float targetHeight  = isCrouching ? standingHeight : crouchHeight;
        float currentHeight = characterController.height;
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        while(timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed/timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }

    private IEnumerator ToogleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = playerCamera.fieldOfView;
        float timeElapsed = 0;

        while(timeElapsed < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElapsed / timeToZoom);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }

    private IEnumerator RegenerateHealth()
    {
        yield return new WaitForSeconds(timeBeforeRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(healthTimeIncrement);

        while(currentHealth <= maxHealth)
        {
            currentHealth += healthValueIncrement;

            if(currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }

            OnHeal?.Invoke(currentHealth);
            yield return timeToWait;
        }
        
        regeneratingHealth = null;

    } 

    private IEnumerator RegenerateStamina()
    {
        yield return new WaitForSeconds(timeBeforeStaminaRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(staminaTimeIncrement);

        while(currentStamina < maxStamina)
        {
            if(currentStamina > 15)
            {
                canSprint = true;
            }            

            currentStamina += staminaValueIncrement;

            if(currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }

            OnStaminaChange?.Invoke(currentStamina);
            yield return timeToWait;
        } 

        regeneratingStamina = null;
    }
}
