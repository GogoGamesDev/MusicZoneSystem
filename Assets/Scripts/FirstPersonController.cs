using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    public bool CanMove;
    public bool IsSprinting = false; //> canSprint && Input.GetAxis("sprintKey");
    
    [Header("Function Options")]
    [SerializeField] private bool canSprint = true;

        [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;


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

    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100;
    [SerializeField] private float staminaUseMultiplier = 5;
    [SerializeField] private float timeBeforeStaminaRegenStarts = 5;
    [SerializeField] private float staminaValueIncrement = 2;
    [SerializeField] private float staminaTimeIncrement = 0.1f;
    private float currentStamina;
    private Coroutine regeneratingStamina;
    public static Action<float> OnStaminaChange;


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

    private bool isWalking = false;

    private Camera playerCamera;
    private CharacterController characterController;
    private Vector3 moveDirection;
    private Vector2 currentInput;
    private float rotationX = 0;
    public static FirstPersonController instance;


    void Awake()
    {
        instance = this;
        //Get camera/controller components
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        currentStamina = maxStamina;
    }

    void Update()
    {
        if(CanMove)
        {
            //Cursor Lock
            Cursor.visible = false; 
            Cursor.lockState = CursorLockMode.Locked;

            HandleMovementInput();
            HandleMouseLook();
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
        
        currentInput = new Vector2((IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        float moveDirectionY = moveDirection.y;
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
        moveDirection.y = moveDirectionY;
         
    }

    private void HandleMouseLook()
    {
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
        rotationX  = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
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
    
    private void ApplyFinalMovements()
    {
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        characterController.Move(moveDirection * Time.deltaTime);
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
