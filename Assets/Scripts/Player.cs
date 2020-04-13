﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Player : MonoBehaviour
{
    private LevelManager levelManager;

    private Rigidbody                                       rig;
    private int                                             jump = 0;
    private bool                                            isJumping = false;
    [SerializeField] private Material                       materialPlayer;
    private List<Vector3>                                   shadowPos = new List<Vector3>();
    private LaunchLevel managerLevel;

    [Header("Movements")]
    [SerializeField] [Range(0f, 1000f)] private float        speed = 0f;
    [SerializeField] [Range(100f, 1000f)] private float     jumpForce = 0f;
    [SerializeField] [Range(0, 10)] private int             numberOfJump = 0;
    [SerializeField] [Range(0f, 300f)] private float         gravityModifier = 100f;
    [SerializeField] private bool                           jumpWithSquat = false;
    //[SerializeField] [Range(0f, 50f)] private float         velocityYmin = 5f;

    [Header("Ground Checker")]
    [SerializeField] private List<Transform>                groundChecker;

    [Header("Features")]
    [SerializeField] private Transform                      directionalLight = null;

    [Header("Shadow and Light")]
    [Tooltip("Time in sec to be completely Black")]
    [SerializeField] private float                          timerInShadow = 2f;
    [Tooltip("Time in sec to be completely White")]
    [SerializeField] private float                          timerInLight = 6f;


    private bool umbrella = false;
    [Space]
    [Header("Umbrella")]
    [SerializeField] private GameObject umbrel = null;
    [SerializeField] private float timerUmbrella = 1f;
    [SerializeField] private PlayerColorBar barPlayer;
    [SerializeField] private UmbrellaColorBar barUmbrella;
    [SerializeField] private UmbrellaBar umbrellaBar;
    [SerializeField] [Range(1f,2f)] private float fallOfPlaner = 1.2f;
    [SerializeField] [Range(1f, 7f)] private float speedOfPlaner = 2f;
    [SerializeField] private float divSpeedPlayer = 1f;
    [SerializeField] private bool UmbrellaOnIfJump = false;
    [SerializeField] private float umbrellaTimeOpen = 5f;
    [SerializeField] private float reloadUmbrellaTime = 2f;
    private bool umbrellaForcON = false;
    private float initialTimerUmbrella = 0f;
    private bool planer = false;
    private bool umbrellaJump = false;
    [HideInInspector] public float valueBarUmbrella = 0f;
    [HideInInspector] public bool inShadow;
    [HideInInspector] public float colorPlayer;

    [Space]
    [Header("Courage")]
    public float maxCourage = 0f;
    [SerializeField] private float secToAddInLight = 1f;
    private float courage = 0f;
    public float Courage { 
        get { return courage; } 
        set { if (value > maxCourage) courage = maxCourage; else courage = value; barUmbrella.RefreshBar(); } 
    }


    [Header("Camera Zoom")]
    [SerializeField] private float distanceToRoof = 2f;
    private bool roofAbovePlayer = false;
    public bool RoofOnPlayer { get { return roofAbovePlayer; } }
    private bool squat = false;
    private float sizeSquat = 0.5f;

    [Space]
    [SerializeField] private bool debug = false;
    [SerializeField] private bool checkPoint = false;

    private float Horizontal = 0f;
    private float Vertical = 0f;

    private void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();
    }

    private void Start()
    {
        managerLevel = FindObjectOfType<LaunchLevel>();
        rig = GetComponent<Rigidbody>();
        SetBasicShadowPos();
        initialTimerUmbrella = timerUmbrella;
        Courage = 0;

        if (checkPoint)
        {
            if (managerLevel.checkpoint != Vector3.zero)
                gameObject.transform.position = managerLevel.checkpoint;
        }
    }

    private void FixedUpdate()
    {
        Horizontal = Input.GetAxis("Horizontal") * speed * Time.fixedDeltaTime; // Used to move player
        Vertical = Input.GetAxis("Vertical") * speed * Time.fixedDeltaTime;     
     
    }

    void Update()
    {
        if (!levelManager.dead && !levelManager.pause && !levelManager.win) // If player is alive
        {
            if (!squat && !roofAbovePlayer)
            {
                UmbrellaActiveOrNot();
            }

            PlayerMovement();
            RoofAbovePLayer();

            if (!roofAbovePlayer)
            {
                PlayerSquat();
            }

            inShadow = CheckShadow();
            ColorOfPlayer();
            ManageCourage();
        }
    }

    private void PlayerMovement()
    {
         
        rig.velocity = new Vector3(Horizontal , rig.velocity.y , Vertical);

        
        Vector3 targetDirection = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (targetDirection.magnitude != 0)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            gameObject.transform.parent = null;
            gameObject.transform.rotation = targetRotation;
        }

        if (rig.velocity.y < 0f && !umbrella)
        {
            rig.AddForce(Vector3.down * gravityModifier);
        }

        if (Input.GetButtonDown("Jump") && (jump < numberOfJump - 1 || GroundCheck() && numberOfJump > 0) && !isJumping)
        {
            

            if (!UmbrellaOnIfJump)
            {
                if (jump == 0) // umbrella off if you jump
                {
                    umbrellaJump = true;
                }
            }
            rig.velocity = new Vector3(rig.velocity.x, 0f, rig.velocity.z); // TODO: maybe if velocity y > 0 keep actual + new else if velocity < 0 reset to 0
            rig.AddForce(Vector3.up * jumpForce);
            jump++;

            gameObject.transform.parent = null;

            if (gameObject.transform.localScale.y < 1 && !jumpWithSquat)
            {
                gameObject.transform.localScale = new Vector3(transform.localScale.x, 1f, transform.localScale.z);
                squat = false;
                SetBasicShadowPos();
                speed = speed * 2;
            }
        }

        if (GroundCheck())
        {
            jump = 0;
        }
    }

    private bool CheckShadow()
    {
        RaycastHit[] hits;

        bool check = true;

        for (int i = 0; i < shadowPos.Count; i++)
        {
            hits = Physics.RaycastAll(transform.position - shadowPos[i], -directionalLight.forward);
            if (hits.Length > 0)
            {

                for (int j = 0; j < hits.Length; j++)
                {
                    if (!hits[j].collider.gameObject.CompareTag("Wall"))
                    {
                        Debug.DrawRay(transform.position - shadowPos[i], hits[j].point - (transform.position - shadowPos[i]), Color.green);
                    }
                    else
                    {
                        if (hits.Length < 2)
                        {
                            check = false;
                            break;
                        }
                    }
                }
            }
            else
            {
                check = false;
            }
               
        }

        return check;
    }

    public void SetBasicShadowPos()
    {
        if (squat == false)
        {
            if (shadowPos.Count != 0)
            {
                shadowPos.Clear();
            }
            shadowPos.Add(new Vector3(-0.365f, -0.44f, -0.39f));
            shadowPos.Add(new Vector3(0.365f, -0.44f, -0.39f));
            shadowPos.Add(new Vector3(0.365f, -0.44f, 0.39f));
            shadowPos.Add(new Vector3(-0.365f, -0.44f, 0.39f));

            shadowPos.Add(new Vector3(-0.365f, 0.44f, -0.39f));
            shadowPos.Add(new Vector3(0.365f, 0.44f, -0.39f));
            shadowPos.Add(new Vector3(0.365f, 0.44f, 0.39f));
            shadowPos.Add(new Vector3(-0.365f, 0.44f, 0.39f));
        }
        else
        {
            if (shadowPos.Count != 0)
            {
                shadowPos.Clear();
            }
            shadowPos.Add(new Vector3(-0.365f, -0.44f + sizeSquat/2, -0.39f));
            shadowPos.Add(new Vector3(0.365f, -0.44f + sizeSquat/2, -0.39f));
            shadowPos.Add(new Vector3(0.365f, -0.44f + sizeSquat/2, 0.39f));
            shadowPos.Add(new Vector3(-0.365f, -0.44f + sizeSquat/2, 0.39f));

            shadowPos.Add(new Vector3(-0.365f, 0.44f - sizeSquat/2, -0.39f));
            shadowPos.Add(new Vector3(0.365f, 0.44f - sizeSquat/2, -0.39f));
            shadowPos.Add(new Vector3(0.365f, 0.44f - sizeSquat/2, 0.39f));
            shadowPos.Add(new Vector3(-0.365f, 0.44f - sizeSquat/2, 0.39f));
        }
    }

    private bool GroundCheck()
    {
        bool isGrounded = false;

        for (int i = 0; i < groundChecker.Count; i++)
        {
            RaycastHit hit;

            if (Physics.Raycast(groundChecker[i].position, transform.TransformDirection(-Vector3.up), out hit, 0.60f))
                isGrounded = true;
        }

        return isGrounded;
    }

    private void ColorOfPlayer()
    {
        if (materialPlayer.color.r <= 1)
        {
            if (inShadow == false)
            {
                colorPlayer += 1 / timerInLight * Time.fixedDeltaTime;
                materialPlayer.color = new Color(colorPlayer, colorPlayer, colorPlayer, 255);
                barPlayer.RefreshBar();
            }
        }
        if (materialPlayer.color.r >= 0)
        {
            if (inShadow == true)
            { 
                colorPlayer -= 1 / timerInShadow * Time.fixedDeltaTime;
                materialPlayer.color = new Color(colorPlayer, colorPlayer, colorPlayer, 255);
                barPlayer.RefreshBar();
            }
        }

        if(colorPlayer > 1) // if he get this value he die 
        {
            colorPlayer = 1;
            materialPlayer.color = new Color(1, 1, 1, 255);
            barPlayer.RefreshBar();
            Lose();
        }
        if (colorPlayer < 0f)
        {
            colorPlayer = 0;
            materialPlayer.color = new Color(0, 0, 0, 255);
            barPlayer.RefreshBar();
        }

    }

    public void Lose()
    {
        if (!debug)
        {
            levelManager.dead = true;
        }
    }

    public void UmbrellaActiveOrNot()
    {
        ManageUmbrellaBar();
        // Umbrella input and condition 
        UmbrellaInput();
        // Planer
        PlanerCalcul();
    }

    private void RoofAbovePLayer()
    {
        RaycastHit hit;

        if(Physics.Raycast(transform.position,Vector3.up, out hit, distanceToRoof))
        {
            if (!hit.collider.gameObject.CompareTag("Umbrella"))
            {
                roofAbovePlayer = true;
                Debug.DrawRay(transform.position, hit.point - transform.position, Color.red);
            }
            else
            {
                roofAbovePlayer = false;
            }
        }
        else
        {
            roofAbovePlayer = false;
        }

    }

    private void PlayerSquat()
    {
        if(Input.GetButtonDown("CircleButton") || Input.GetKeyDown(KeyCode.LeftShift))
        {
            Transform saveParent = gameObject.transform.parent;
            gameObject.transform.parent = null;
            //manage umbrella with squat
            if (umbrella == true)
            {
                timerUmbrella = initialTimerUmbrella;
                umbrella = !umbrella;
                umbrel.SetActive(umbrella);
            
                if (planer == true)
                {
                    planer = false;
                    speed = speed / speedOfPlaner;
                }
            
                umbrel.transform.rotation = Quaternion.Euler(0, 0, 0);
                umbrel.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + 1, gameObject.transform.position.z);
                speed = speed * divSpeedPlayer;
            }

            // squat
            if (gameObject.transform.localScale.y > sizeSquat + 0.1f)
            {
               
                gameObject.transform.localScale = new Vector3(transform.localScale.x , sizeSquat, transform.localScale.z);
                gameObject.transform.Translate(0, - sizeSquat / 2, 0);
                squat = true;
                SetBasicShadowPos();
                speed = speed / 2;
            }
            else
            {
                gameObject.transform.localScale = new Vector3(transform.localScale.x, 1f, transform.localScale.z);
                squat = false;
                SetBasicShadowPos();
                speed = speed * 2;
            }

            gameObject.transform.parent = saveParent;
        }
    }

    private void ManageCourage()
    {
        if ( courage == maxCourage)
        {
            Courage = 0;
            timerInLight += secToAddInLight;
        }
    }

    private void ManageUmbrellaBar()
    {
        // manage bar of umbrella 
        if (umbrella && valueBarUmbrella < 1)
        {
            valueBarUmbrella += 1 / umbrellaTimeOpen * Time.deltaTime;
        }
        if ((!umbrella && inShadow) && valueBarUmbrella > 0)
        {
            valueBarUmbrella -= 1 / reloadUmbrellaTime * Time.deltaTime;
        }
        umbrellaBar.RefreshBar();

        if (valueBarUmbrella > 1) // turn off umbrella
        {
            valueBarUmbrella = 1;
            umbrellaForcON = true;
        }
        if (valueBarUmbrella < 0)
            valueBarUmbrella = 0;
    }

    private void PlanerCalcul()
    {
        if (umbrella == true && GroundCheck() == false)
        {
            rig.velocity = new Vector3(rig.velocity.x, rig.velocity.y / fallOfPlaner, rig.velocity.z);
            if (planer == false)
            {
                planer = true;
                speed = speed * speedOfPlaner;
            }
        }
        if (planer == true && GroundCheck() == true)
        {
            planer = false;
            speed = speed / speedOfPlaner;
        }
    }

    private void UmbrellaInput()
    {
        if (timerUmbrella > 0)
        {
            timerUmbrella -= Time.fixedDeltaTime;
        }
        if ((((Input.GetAxis("RightTrigger") > 0 || Input.GetKeyDown(KeyCode.Return)) && timerUmbrella < 0) && valueBarUmbrella < 1)
            || (umbrellaJump == true && umbrella == true) || umbrellaForcON)
        {
            umbrellaForcON = false;
            if (umbrellaJump == true)
            {
                umbrellaJump = false;
            }

            timerUmbrella = initialTimerUmbrella;
            umbrella = !umbrella;
            umbrel.SetActive(umbrella);

            if (umbrella == true)
            {
                speed = speed / divSpeedPlayer;
            }
            else
            {
                // reset plane if you tunr off umbrella in air 
                if (planer == true)
                {
                    planer = false;
                    speed = speed / speedOfPlaner;
                }
                umbrel.transform.rotation = Quaternion.Euler(0, 0, 0);
                umbrel.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + 1, gameObject.transform.position.z);
                speed = speed * divSpeedPlayer;

            }
        }
    }
}