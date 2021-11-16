using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerThrowBall : MonoBehaviour
{
    // Config
    public PlayerData.PlayerNumber playerNumber;
    //private CrosshairScript crosshair;
    public float lockDistance = 10000;

    // Pass config
    public int searchWidth = 100;
    public float searchStep = 0.2f;

    // State info
    private bool throwBall = false;
    private bool passBall = false;
    private float chargeBall;
    private GameObject ball;
    private Rigidbody lockedTarget;
    private CloneHitByBall cloneWithBall;

    private ScoringManager scoringManager;

    PlayerControls controls;
    private bool throwInput = false;
    private bool lockInput = false;

    private CooldownTimer dashCooldown;

    private int frame;

    private PlayerConfig playerConfig;
    private PlayerRecording records;

    private GameObject[] clones;
    private List<GameObject> playerClones = new List<GameObject>();

    // sound effects
    [HideInInspector]
    public AudioManager audioManager;
    private AudioSource runningWithoutBall;
    private AudioSource throwBallSound;
    private AudioSource stunSound;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.Throw.canceled += ctx =>
        {
            throwInput = false;
        };

        controls.Gameplay.Lockon.canceled += ctx =>
        {
            lockInput = false;
            passBall = false;
        };
    }

    public void InitializePlayerConfig(PlayerConfig pc)
    {
        playerConfig = pc;
        playerConfig.Input.onActionTriggered += Input_onActionTriggered;
    }

    private void Input_onActionTriggered(InputAction.CallbackContext obj)
    {
        if (obj.action.name == controls.Gameplay.Throw.name)
        {
            OnThrow(obj);
        }
        else if (obj.action.name == controls.Gameplay.Lockon.name)
        {
            OnLock(obj);
        }
    }

    public void OnThrow(InputAction.CallbackContext context)
    {
        throwInput = context.action.triggered;
    }

    public void OnLock(InputAction.CallbackContext context)
    {
        lockInput = context.action.triggered;
    }

    // Start is called before the first frame update
    void Start()
    {
        records = GetComponent<PlayerRecording>();
        playerNumber = GetComponent<PlayerData>().playerNumber;
        scoringManager = FindObjectOfType<ScoringManager>();

        CooldownTimer[] dashCooldowns = FindObjectsOfType<CooldownTimer>();
        foreach (CooldownTimer dashCooldown in dashCooldowns) {
            if (GetComponent<PlayerData>().playerNumber == PlayerData.PlayerNumber.PlayerOne && dashCooldown.name.StartsWith("P1")) {
                this.dashCooldown = dashCooldown;
            }
            else if (GetComponent<PlayerData>().playerNumber == PlayerData.PlayerNumber.PlayerTwo && dashCooldown.name.StartsWith("P2")) {
                this.dashCooldown = dashCooldown;
            }
        }

        audioManager = FindObjectOfType<AudioManager>();
        runningWithoutBall = audioManager.GetAudio("Running");
        throwBallSound = audioManager.GetAudio("ThrowBall");
        stunSound = audioManager.GetAudio("Stunning");
    }

    private void FixedUpdate()
    {
        if (throwBall || passBall)
        {
            throwBall = false;
            passBall = false;
            ball.transform.parent = null;
            ball.GetComponent<PlayerData>().playerNumber = playerNumber;
            ball.GetComponent<Rigidbody>().isKinematic = false;
            Vector3 throwForce = transform.forward * GameConfigurations.horizontalThrowingForce;
            throwForce *= GameConfigurations.speedBoostFactor * (1 + ball.GetComponent<BallScript>().GetCharge());
            throwForce += Vector3.up * GameConfigurations.verticalThrowingForce;
            ball.GetComponent<Rigidbody>().AddForce(throwForce);
            ball.GetComponent<BallScript>().SetHomingTarget(lockedTarget);
            ball = null;
            chargeBall = 0;
            lockedTarget = null;
            throwBallSound.Play();
        }

        records.RecordInput(throwInput, frame);
        frame++;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if ball gone
        if (ball && ball.transform.parent != transform)
        {
            ball = null;
            chargeBall = 0;
            lockedTarget = null;
            return;
        }

        // Update throw inputs
        if (ball)
        {
            BallScript ballScript = ball.GetComponent<BallScript>();
            if (throwInput)
            {
                chargeBall += Time.deltaTime;
                if (chargeBall > GameConfigurations.ballChargeTime)
                {
                    if (ballScript.GetCharge() < GameConfigurations.maxBallCharge - 1) {
                        ballScript.AddCharge();
                    }
                    chargeBall = 0;
                }
            }
            else if (chargeBall > 0)
            {
                throwBall = true;
                dashCooldown.AbilityEnabled();
            }
        }

        // Send input to clone if necessary
        else if (cloneWithBall && !lockedTarget)
        //else if (cloneWithBall)
        {
            if (lockInput)
            {
                cloneWithBall.SetTarget(GetComponent<Rigidbody>());
                cloneWithBall.Fire();
                lockInput = false;
                passBall = false;
                lockedTarget = null;
            }
            else
            {
                cloneWithBall.SetTarget(null);
            }

            if (throwInput)
            {
                cloneWithBall.Fire();
                throwInput = false;
            }
        }

        // Update lock on targets
        if (!lockedTarget && ball)
        {
            if (lockInput)
            {
                playerClones.Clear();
                clones = GameObject.FindGameObjectsWithTag("Clone");
                foreach (GameObject clone in clones)
                {
                    if (clone.GetComponent<PlayerData>().playerNumber == playerNumber)
                        playerClones.Add(clone);
                }

                lockedTarget = GetClosestClone();
                passBall = true;
            }
        }
        else if (!lockInput || !ball)
        {
            lockedTarget = null;
        }
    }

    private Rigidbody GetClosestClone()
    {
        lockedTarget = null;
        float angle = Mathf.Infinity;

        foreach (GameObject clone in playerClones)
        {
            Vector3 cloneDirection = DirectionTo(clone);

            float cloneAngle = Vector3.Angle(transform.forward, cloneDirection);

            if (cloneAngle < angle)
            {
                lockedTarget = clone.GetComponent<Rigidbody>();
                angle = cloneAngle;
            }

        }

        return lockedTarget;
    }

    private Vector3 DirectionTo(GameObject clone)
    {
        return Vector3.Normalize(clone.transform.position - ball.transform.position);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if not holding ball and object is ball
        if (!ball && collision.transform.tag == "Ball")
        {
            //Debug.Log("balldetected");
            PlayerData ballData = collision.gameObject.GetComponent<PlayerData>();

            // if ball is not of player's color
            if (collision.gameObject.GetComponent<PlayerData>().playerNumber == PlayerData.PlayerNumber.NoPlayer) {
                ///Debug.Log("Claiming un-owned ball");
                ClaimBall(collision);
                ball.GetComponent<BallScript>().ClearCharge();
            }

            // if ball is of opponent's color
            else if (collision.gameObject.GetComponent<PlayerData>().playerNumber != playerNumber)
            {
                

                if (!collision.transform.parent) {
                    ballData.playerNumber = playerNumber;
                    scoringManager.SetCurrentPlayer(playerNumber);
                    ClaimBall(collision);
                    ball.GetComponent<BallScript>().ClearCharge();
                }
                else {
                    // tag ball
                    if (GetComponent<PlayerMovement>().GetDashStatus() == true) {
                        var opponent = collision.transform.parent;
                        opponent.GetComponent<PlayerThrowBall>().dashCooldown.AbilityEnabled();
                        ClaimBall(collision);
                        ball.GetComponent<BallScript>().ClearCharge();
                        if (opponent.GetComponent<PlayerThrowBall>().CheckIfHasBall()) {
                            opponent.GetComponent<PlayerThrowBall>().ReleaseBall();
                        }
                    }
                }
            }

            // if ball is of player's color
            else {
                if (GetComponent<PlayerMovement>().GetStunStatus() == false) {
                    ClaimBall(collision);
                }
            }
        }

        // Stun code
        else if (collision.transform.tag == "Player" && GetComponent<PlayerMovement>().GetDashStatus() == true) {
            var opponentBall = collision.gameObject.GetComponent<PlayerThrowBall>().ball;
            
                Debug.Log("Stunning opponent player.");

            if (opponentBall != null) {
                opponentBall.transform.parent = null;
                opponentBall.GetComponent<Rigidbody>().isKinematic = false;
                opponentBall.GetComponent<Rigidbody>().AddForce(transform.forward * GameConfigurations.horizontalThrowingForce / 50);
                collision.gameObject.GetComponent<PlayerThrowBall>().dashCooldown.AbilityEnabled();
                collision.gameObject.GetComponent<PlayerThrowBall>().ReleaseBall();
            }

            collision.gameObject.GetComponent<PlayerMovement>().SetStunStatus(true);
            stunSound.Play();
            collision.gameObject.GetComponent<PlayerMovement>().StartExplosion(GameConfigurations.stunningSpeed, GameConfigurations.stunningFrame, transform.position);
        }
    }

    public void SetCloneWithBall(CloneHitByBall clone)
    {
        cloneWithBall = clone;
    }

    private void ClaimBall(Collision collision) {
        // Set ball attributes to current player
        ball = collision.gameObject;
        ball.GetComponent<PlayerData>().playerNumber = playerNumber;

        // Pick up ball
        ball.transform.parent = transform;
        ball.transform.localPosition = new Vector3(0, GameConfigurations.ballHeight, GameConfigurations.ballDistance);
        ball.GetComponent<Rigidbody>().isKinematic = true;
        dashCooldown.AbilityDisabled();

        if (runningWithoutBall.isPlaying)
            runningWithoutBall.Stop();
    }

    public void Reset()
    {
        ball = null;
        chargeBall = 0;
        lockedTarget = null;
        frame = 0;
        clones = GameObject.FindGameObjectsWithTag("Clone");
    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }

    public void ReleaseBall() {
        ball = null;
    }

    public bool CheckIfHasBall() {
        return (ball != null);
    }

    public bool CheckIfCharging()
    {
        return (chargeBall > 0 || throwInput);
    }
}