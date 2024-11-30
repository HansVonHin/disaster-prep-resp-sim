// SJM Tech
// www.sjmtech3d.com
// Updated by Bloo_Void
//
// Unofficial First Person Camera AddOn for Invector Basic/Melee/Shooter Template.
//
// rev. 2.6.2.c
//           
// use:
// 1 - assign this script to the Invector Controller. 
// 2 - assign the camera gameobject child of vThirdPersonCamera into the Main Camera of this script in the editor (If there is no camera selected, the MainCamera of the scene will be used.)
// 3 - Have fun! Adjust to your liking. 
//

using Invector;
using Invector.vCamera;
using Invector.vCharacterController;
using Invector.vEventSystems;
using UnityEngine;
using UnityEngine.Events;

[vClassHeader(" First Person Camera ", "Assign the child camera gameobject in vThirdPersonCamera into mainCamera.", iconName = "FPCameraSwapIcon")]
public class vFirstPersonCamera : vMonoBehaviour
{
    #region Camera Settings
    [vEditorToolbar("Camera Settings")]
    [vSeparator("Camera Postion Settings")]
    [Space(5)]
    [Tooltip("Set the camera inside vThirdPersonCamera into here. If empty, the MainCamera will be used")]
    public Camera mainCamera;
    [Tooltip("Set the Camera Near Plane")]
    public float cameraNearClip = 0.01f;
    [Tooltip("Set the Camera Y offset from the head bone")]
    public float cameraYOffset = 0.1f;
    [Tooltip("Set the Camera Z offset from the head bone")]
    public float cameraZOffset = 0.02f;
    [Space(5)]
    //
    [vSeparator("Head Collider Settings")]
    [Tooltip("Enable head collision to prevent the camera from clipping into objects")]
    public bool enableHeadCollider = true;
    [vHideInInspector("enableHeadCollider")]
    [Tooltip("Show head collision Gizmos")]
    public bool showGizmos = true;
    [vHideInInspector("enableHeadCollider")]
    [Tooltip("Head collision radius")]
    public float colliderRadius = 0.12f;
    [vHideInInspector("enableHeadCollider")]
    [Tooltip("Head collision center")]
    public Vector3 colliderCenter = new Vector3(0, 0.1f, 0.04f);
    [Space(20)]
    //
    [vSeparator("Action Angle Limit")]
    [Space(5)]
    [Tooltip("The 'Horizontal' clamp angle for head look during actions")]
    [Range(0f, 90f)]
    public float actionHAngleLimit = 90f;
    //
    [vSeparator("Body Rotation Settings")]
    [Space(5)]
    [Tooltip("Set the default Animator Update Mode")]
    public AnimatorUpdateMode animatorUpdateMode = AnimatorUpdateMode.Fixed;
    [Space(5)]
    [Tooltip("Set the strafe body IK reactivity respect head rotation")]
    [Range(0f, 2f)]
    public float strafeBodyIKWeight = 1.25f;
    [Space(5)]
    [Tooltip("Set the free body IK reactivity respect head rotation")]
    [Range(0f, 2f)]
    public float freeBodyIKWeight = 1.25f;
    [Space(5)]
    [Tooltip("The threshold angle between player head and player body; beyond which the rotation begins")]
    [Range(0f, 70f)]
    public float RotationThld = 55f;
    [Space(5)]
    //
    [vSeparator("Controller Settings")]
    [Space(5)]
    [Tooltip("use cinematic camera during DEFAULT actions")]
    public bool cinematicOnActions = false;
    [Tooltip("use cinematic camera by external calls")]
    public bool cinematicOnRequest = true;
    [Tooltip("add Crosshair UI prefab at start")]
    public bool addCrosshair = true;            // Crosshair UI spawning
    #endregion Camera Settings

    #region Camera Mode
    // swap requisite
    [vEditorToolbar("Camera Mode")]
    [vSeparator("Camera Mode Settings")]
    [Space(5)]
    [Tooltip("Assign keyboard button for camera mode")]
    public KeyCode cameraModeKey = KeyCode.Backspace;
    [Space(5)]
    [Tooltip("Set Third Person as default mode on start")]
    public bool isThirdPerson;
    [Space(5)]
    [Header("Third Person Settings")]
    [Tooltip("Set default loomotion type")]
    public vThirdPersonMotor.LocomotionType defaultThirdLocomotion;
    [Space(5)]
    [Tooltip("Force strafe mode in FreeWithStrafe")]
    public bool thrdCameraDefaultStrafe = false;
    [Space(5)]
    [vSeparator("Events")]
    [Space(5)]
    public UnityEvent FirstPersonMode;
    [Space(5)]
    public UnityEvent ThirdPersonMode;
    #endregion

    #region Private Variables
    //
    private bool isAction = false;              //Force action status (Integrations)
    private bool isCinematic;                   //disable mouse user input (Cinematic)
    private bool isUpdateModeNormal = false;
    private bool stateDone = false;

    // inVector references
    private vThirdPersonInput vInput;
    private vHeadTrack vHeadT;

    // Animator and Bones
    private Animator animator;
    private Transform headBone;
    private GameObject headBoneRef;
    private GameObject headBoneRotCorrection;

    // Animator StateInfo
    public vAnimatorStateInfos animatorStateInfos;
    private bool isCustomAction = false;
    //private bool isAiming = false;

    //
    private bool lateUpdateSync;
    private GameObject headCollider;
    private bool headColliderStatus = true;
    private vThirdPersonCamera tpCamera;
    private bool cameraModeLast;
    private bool startJumpandRotate;
    #endregion

    void Start()
    {
        tpCamera = FindObjectOfType<vThirdPersonCamera>();

        // if there is no custom camera ... use the main camera.
        if (mainCamera == null)
        {
            mainCamera = Camera.main.gameObject.GetComponent<Camera>();
        }

        // set the optimal near clip plane and depth
        mainCamera.GetComponent<Camera>().nearClipPlane = cameraNearClip;

        // set vTPC reference
        vInput = GetComponent<vThirdPersonInput>();
        startJumpandRotate = vInput.cc.jumpAndRotate;
        if (!isThirdPerson)//FP Mode
        {
            vInput.cc.locomotionType = vThirdPersonMotor.LocomotionType.OnlyStrafe;
            vInput.cc.sprintOnlyFree = false;
            vInput.cc.strafeSpeed.rotateWithCamera = false;
        }

        vHeadT = GetComponent<vHeadTrack>();
        vHeadT.strafeBodyWeight = strafeBodyIKWeight;
        vHeadT.freeBodyWeight = freeBodyIKWeight;

        // find the head bone
        animator = GetComponent<Animator>();
        headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        // add animation state listener
        animatorStateInfos = new vAnimatorStateInfos(animator);
        animatorStateInfos.RegisterListener();

        animator.updateMode = animatorUpdateMode;

        if (animator.updateMode != AnimatorUpdateMode.Fixed)
        {
            isUpdateModeNormal = true;
        }
        else
        {
            isUpdateModeNormal = false;
        }

        // create head collision object
        headColliderStatus = enableHeadCollider;

        if (enableHeadCollider)
        {
            headCollider = new GameObject("HeadCollision");
            headCollider.AddComponent<vFPCameraHeadCollider>();
            headCollider.layer = 15;
            headCollider.tag = "Player";
            headCollider.AddComponent<SphereCollider>();
            headCollider.GetComponent<SphereCollider>().radius = colliderRadius;
            headCollider.GetComponent<SphereCollider>().center = colliderCenter;
            headCollider.transform.parent = this.transform;
            headCollider.transform.localRotation = Quaternion.identity;
        }

        // create bones reference
        headBoneRef = new GameObject("HeadRef");
        headBoneRotCorrection = new GameObject("FPCameraRoot");

        // position bones reference
        var camOffset = (transform.root.forward * cameraZOffset) + (transform.root.up * cameraYOffset);
        headBoneRef.transform.position = headBone.transform.position + camOffset;
        headBoneRotCorrection.transform.position = headBone.transform.position;
        headBoneRotCorrection.transform.rotation = headBone.transform.root.rotation;
        headBoneRef.transform.rotation = headBone.transform.rotation;
        headBoneRef.transform.parent = headBoneRotCorrection.transform;

        // find and stop "UnderBody" animator layer to reduce lags during camera free look
        for (int i = 0; i < animator.layerCount; i++)
        {
            if (animator.GetLayerName(i) == "UnderBody")
            {
                animator.SetLayerWeight(i, 0);
            }
        }

    }

    void FixedUpdate()
    {
        lateUpdateSync = true;

        if (!vInput.cc.ragdolled && Time.timeScale != 0)
        {
            if (!isCinematic)
            {
                if (!isAction)
                {
                    CameraLook();
                }
            }
        }
    }

    void LateUpdate()
    {
        if (isUpdateModeNormal)
        {
            lateUpdateSync = true;
        }

        if (lateUpdateSync)
        {
            lateUpdateSync = false;

            if (!vInput.cc.ragdolled && Time.timeScale != 0)
            {
                if (cinematicOnActions)
                {
                    if (vInput.cc.customAction)
                    {
                        isCinematic = true;
                        stateDone = false;
                    }
                    else if (!vInput.cc.customAction && !stateDone)
                    {
                        isCinematic = false;
                        stateDone = true;
                    }
                }

                FaceToCamera();
                CameraHeadBonePosition();

                if (isCinematic)
                {
                    CinematicCam(); // no user input during cinematic
                }
                else
                {
                    CharacterRotation();

                    if (isAction)
                    {
                        CameraLook();
                    }

                    CameraHeadBoneRotation();
                }
            }
            else
            {
                CinematicCam(); // no user input during ragdoll
            }
        }
    }

    void Update()
    {
        if (animatorStateInfos.HasTag("CustomAction") || animatorStateInfos.HasTag("LockMovement"))
        {
            isCustomAction = true;
        }
        else
        {
            isCustomAction = false;
        }

        /*if (animatorStateInfos.HasTag("Headtrack"))
        {
            isAiming = true;
        }
        else
        {
            isAiming = false;
        }*/

        if (animator.updateMode != AnimatorUpdateMode.Fixed)
        {
            isUpdateModeNormal = true;
        }
        else
        {
            isUpdateModeNormal = false;
        }



        // swap by key
        if (Input.GetKeyDown(cameraModeKey))
        {
            FpcSwap();
        }

        if (cameraModeLast != isThirdPerson)
        {
            if (isThirdPerson) // TP Mode
            {
                ThirdPersonEvent();

                if (headColliderStatus)
                {
                    enableHeadCollider = false;
                    headCollider.SetActive(false);
                }

                vInput.cc.locomotionType = defaultThirdLocomotion;

                if (thrdCameraDefaultStrafe)
                {
                    vInput.cc.isStrafing = true;
                }
                else
                {
                    vInput.cc.isStrafing = false;
                }

                cameraModeLast = isThirdPerson;

            }
            else if (!isThirdPerson)// FP Mode
            {
                FirstPersonEvent();

                headCollider.SetActive(headColliderStatus);
                enableHeadCollider = true;


                vInput.cc.locomotionType = vThirdPersonMotor.LocomotionType.OnlyStrafe;
                vInput.cc.sprintOnlyFree = false;
                vInput.cc.strafeSpeed.rotateWithCamera = false;

                cameraModeLast = isThirdPerson;
            }
        }

        if (!isThirdPerson)
        {
            // Limit mouseX during actions
            float minX = transform.eulerAngles.y + (-actionHAngleLimit);
            float maxX = transform.eulerAngles.y + actionHAngleLimit;

            float tempx = Mathf.Clamp(tpCamera.mouseX, minX, maxX);

            if (tpCamera.mouseX > tempx + 100)
            {
                tpCamera.mouseX -= 360;
            }
            else if (tpCamera.mouseX < tempx - 100)
            {
                tpCamera.mouseX += 360;
            }

            if (vInput.cc.customAction || isAction || isCustomAction)
            {
                tpCamera.mouseX = Mathf.Clamp(tpCamera.mouseX, minX, maxX);
                tpCamera.mouseY = Mathf.Clamp(tpCamera.mouseY, tpCamera.lerpState.yMinLimit, tpCamera.lerpState.yMaxLimit);
            }
        }
    }

    void CameraHeadBonePosition()
    {
        if (!isThirdPerson)
        {
            headBoneRotCorrection.transform.position = headBone.transform.position;
            mainCamera.transform.position = headBoneRef.transform.position;

            if (enableHeadCollider)
                headCollider.transform.position = headBone.transform.position;
        }
        else
        {
            mainCamera.transform.position = tpCamera.transform.position;
        }
    }

    void CameraHeadBoneRotation()
    {
        if (!isThirdPerson)
        {
            headBone.rotation = headBoneRef.transform.rotation;
            mainCamera.transform.rotation = tpCamera.transform.rotation;
            if (enableHeadCollider)
                headCollider.transform.rotation = headBone.transform.rotation;
        }
    }

    void CinematicCam()
    {
        if (!isThirdPerson && Time.timeScale != 0)
        {
            tpCamera.mouseY = transform.eulerAngles.NormalizeAngle().x;
            tpCamera.mouseX = transform.eulerAngles.NormalizeAngle().y;
            headBoneRotCorrection.transform.position = headBone.transform.position;
            headBoneRotCorrection.transform.rotation = headBone.transform.rotation;
            mainCamera.transform.position = headBoneRef.transform.position;
            mainCamera.transform.rotation = headBone.transform.rotation;
        }
    }

    void CameraLook()
    {
        headBoneRotCorrection.transform.rotation = tpCamera.transform.rotation;
    }

    void CharacterRotation()
    {
        if (!isThirdPerson)
        {
            Quaternion newRotation = Quaternion.identity;
            // rotate the body only when there is no movement
            if (Input.GetAxis("Horizontal") == 0 && Input.GetAxis("Vertical") == 0)
            {
                // Get camera forward in the character's rotation space
                Vector3 camRelative = transform.InverseTransformDirection(mainCamera.transform.forward);

                // Get the angle of the camera forward relative to the character forward
                float angle = Mathf.Atan2(camRelative.x, camRelative.z) * Mathf.Rad2Deg;
                float a = 0;

                // check the angle threshold
                if (Mathf.Abs(angle) > Mathf.Abs(RotationThld))
                {
                    a = angle - RotationThld;
                    if (angle < 0)
                        a = angle + RotationThld;

                    // Body Rotation
                    if (isAction || isCustomAction || vInput.cc.customAction)
                    {
                        return;
                    }
                    else
                    {
                        newRotation = Quaternion.AngleAxis(a, transform.up) * transform.rotation;

                        if (!isUpdateModeNormal)
                        {
                            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.fixedDeltaTime * vInput.cc.strafeSpeed.rotationSpeed);
                        }
                        else
                        {
                            transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * vInput.cc.strafeSpeed.rotationSpeed);
                        }
                    }
                }
                else
                {
                    newRotation = transform.rotation;
                }
            }
        }
    }

    void FaceToCamera()
    {
        if (!isThirdPerson)
        {
            // set transform rotation to mouseX when holding the negative vertical input (S by default)
            // and is not grounded or airborne. If this is not done, in FP Mode, the character body will turn
            // the opposite way when falling or jumping while S is pressed.
            if (vInput.cc.input.z < 0 && (!vInput.cc.isGrounded || vInput.cc.isInAirborne))
            {
                vInput.cc.jumpAndRotate = false;

                //transform.rotation = Quaternion.Euler(0, tpCamera.mouseX, 0); // no smoothing
                Quaternion rotatePlayerY = Quaternion.Euler(0, tpCamera.mouseX, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotatePlayerY, Time.fixedDeltaTime * vInput.cc.strafeSpeed.rotationSpeed); // smooth by fixed time
            }
            else
            {
                vInput.cc.jumpAndRotate = startJumpandRotate;
            }

            // face the direction of the camera when attacking/blocking/swimming
            // this can be bypassed if you remove the animator tag "LockRotation" from attack anims, but I didnt want to do that. 
            if (animatorStateInfos.HasTag("Attack") || animatorStateInfos.HasTag("isBlocking"))
            {
                Quaternion rotatePlayerY = Quaternion.Euler(0, tpCamera.mouseX, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotatePlayerY, Time.fixedDeltaTime * vInput.cc.strafeSpeed.rotationSpeed); // smooth by fixed time
            }

            //when swimming, disable headcollider and set character rotation to mouseX so the character cant turn the opposite way. 
            //the best way would be to use a backwards swimming anim with this, but the addon does not have one.
            if (vInput.cc.IsAnimatorTag("isSwimming"))
            {
                headCollider.SetActive(false);
                Quaternion rotatePlayerY = Quaternion.Euler(0, tpCamera.mouseX, 0);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotatePlayerY, Time.fixedDeltaTime * vInput.cc.strafeSpeed.rotationSpeed); // smooth by fixed time
            }
            else
            {
                headCollider.SetActive(headColliderStatus);
            }
        }
    }

    // call this to stop rotating body when threshold is reached.
    //  and when you need the camera rotate with body when there is no mouse inputs. (riding, driving...)
    public void IsAction(bool status)
    {
        isAction = status;
        if (isAction)
        {
            headBoneRotCorrection.transform.parent = this.transform.root;
        }
        else
        {
            headBoneRotCorrection.transform.parent = null;
        }
    }

    // call this to use cinematic camera movement.
    public void IsCinematic(bool state)
    {
        if (cinematicOnRequest) isCinematic = state;
    }

    public void OnDrawGizmosSelected()
    {
        if (mainCamera != null && showGizmos && enableHeadCollider)
        {
            animator = GetComponent<Animator>();
            headBone = animator.GetBoneTransform(HumanBodyBones.Head);
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawSphere(headBone.transform.position + (headBone.transform.forward * colliderCenter.z) + headBone.transform.up * colliderCenter.y + headBone.transform.right * colliderCenter.x, colliderRadius);
        }
    }

    // FP Mode Event
    void FirstPersonEvent()
    {
        FirstPersonMode.Invoke();
    }

    // TP Mode Event
    void ThirdPersonEvent()
    {
        ThirdPersonMode.Invoke();
    }

    void FpcSwap()
    {
        isThirdPerson = !isThirdPerson;
    }

    public void FpcSetThirdMode(bool value)
    {
        isThirdPerson = value;
    }
}
