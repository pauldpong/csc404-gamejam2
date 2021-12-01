using System;
using UnityEngine;
using static CloneManager;

public class CloneController : MonoBehaviour
{
    public CloneData cloneData;

    [SerializeField]
    private GameObject linePrefab;
    [SerializeField]
    private GameObject trailPrefab;

    // State data
    private Rigidbody rb;
    private int frame;
    private Vector3 nextPos;
    private int paused;

    private CloneHitByBall ballScript;
    public bool throwInput { get; private set; }
    private int nextThrowInputChangeIndex;

    private CloneGuard guardScript;
    public bool guardInput { get; private set; }
    private int nextGuardInputChangeIndex;

    private GameObject lr;
    private GameObject tr;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        guardScript = GetComponentInChildren<CloneGuard>();
        ballScript = GetComponent<CloneHitByBall>();
        frame = 0;
        throwInput = false;
        guardInput = false;
        nextThrowInputChangeIndex = 0;
    }

    void FixedUpdate()
    {
        if (frame / (cloneData.PositionSkipFrames + 1) < cloneData.Positions.Length)
        {
            if (frame % cloneData.PositionSkipFrames == 0)
            {
                int nextIndex = frame / (cloneData.PositionSkipFrames + 1);
                SetupLines(nextIndex);
                nextPos = cloneData.Positions[nextIndex];
            }

            if (cloneData.ThrowInputs.Length > nextThrowInputChangeIndex)
            {
                if (frame == cloneData.ThrowInputs[nextThrowInputChangeIndex])
                {
                    throwInput = !throwInput;
                    nextThrowInputChangeIndex++;
                }
            }

            if (cloneData.GuardInputs.Length > nextGuardInputChangeIndex)
            {
                if (frame == cloneData.GuardInputs[nextGuardInputChangeIndex])
                {
                    guardInput = !guardInput;
                    nextGuardInputChangeIndex++;
                }
            }
            guardScript.UpdateGuard(guardInput && !ballScript.HasBall());

            // move whatever fraction of the way to the target is necessary
            Vector3 partialMove = transform.position + (nextPos - transform.position) / (cloneData.PositionSkipFrames + 1);
            //Debug.Log(partialMove);
            rb.MovePosition(partialMove);
            if (paused <= 0)
            {
                frame++;
            }
            else
            {
                paused -= 1;
            }
        }
    }

    public void SetupLines(int index)
    {

        Vector3[] positions = new Vector3[3];
        if (index + 2 < cloneData.Positions.Length)
        {
            positions[0] = cloneData.Positions[index];
            positions[1] = cloneData.Positions[index + 1];
            positions[2] = cloneData.Positions[index + 2];
        }
        else if (index + 1 < cloneData.Positions.Length)
        {
            positions[0] = cloneData.Positions[index];
            positions[1] = cloneData.Positions[index + 1];
        }
        else
        {
            positions[0] = cloneData.Positions[index];
        }

        //if (tr != null)
        //{
        //    Destroy(tr);
        //}
        //tr = Instantiate(trailPrefab, positions[0], Quaternion.identity);
        //tr.transform.parent = null;
        //if (cloneData.PlayerNumber == PlayerData.PlayerNumber.PlayerOne)
        //{
        //    tr.GetComponent<TrailRenderer>().startColor = Color.blue;
        //    tr.GetComponent<TrailRenderer>().endColor = Color.blue;
        //}
        //else
        //{
        //    tr.GetComponent<TrailRenderer>().startColor = Color.red;
        //    tr.GetComponent<TrailRenderer>().endColor = Color.red;
        //}
        //tr.GetComponent<TrailRenderer>().time = 2;
        //tr.GetComponent<TrailRenderer>().AddPositions(positions);
        if (lr != null)
        {
            Destroy(lr);
        }
        lr = Instantiate(linePrefab, positions[0], Quaternion.identity);
        lr.transform.parent = null;
        if (cloneData.PlayerNumber == PlayerData.PlayerNumber.PlayerOne)
        {
            lr.GetComponent<LineRenderer>().material.color = Color.blue;
        }
        else
        {
            lr.GetComponent<LineRenderer>().material.color = Color.red;
        }
        lr.GetComponent<LineRenderer>().useWorldSpace = true;
        lr.GetComponent<LineRenderer>().positionCount = positions.Length;
        lr.GetComponent<LineRenderer>().startWidth = 1.0f;
        lr.GetComponent<LineRenderer>().endWidth = 1.0f;
        lr.GetComponent<LineRenderer>().SetPositions(positions);

    }

    public Quaternion GetNextRotation(float timeSinceLastUpdate)
    {
        float howFarToSlerp = (timeSinceLastUpdate / Time.fixedDeltaTime) + (frame % (cloneData.RotationSkipFrames + 1)) / (cloneData.RotationSkipFrames + 1);
        int currentRot = Math.Min(frame / (cloneData.RotationSkipFrames + 1), cloneData.Rotations.Length - 1);
        int nextRot = Math.Min(currentRot + 1, cloneData.Rotations.Length - 1);
        return Quaternion.Slerp(cloneData.Rotations[currentRot], cloneData.Rotations[nextRot], howFarToSlerp);
    }

    public void SetData(CloneData cloneData)
    {
        this.cloneData = cloneData;
    }

    public void Pause()
    {
        paused = GameConfigurations.cloneMaxPauseFrames;
    }

    public void Unpause()
    {
        paused = 0;
    }

    public void Kill()
    {
        if (lr != null)
            Destroy(lr.gameObject);
        if (tr != null)
            Destroy(tr.gameObject);
        Destroy(gameObject);
    }
}

