using System;
using UnityEngine;
using static CloneManager;

public class CloneController : MonoBehaviour
{
    public CloneData cloneData;

    // State data
    private Rigidbody rb;
    private int frame;
    private Vector3 nextPos;
    public bool throwInput { get; private set; }
    private int nextThrowInputChange;

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        frame = 0;
        throwInput = false;
        nextThrowInputChange = 0;
    }

    void FixedUpdate()
    {
        if (frame/(cloneData.PositionSkipFrames + 1) < cloneData.Positions.Length)
        {
            if (frame % cloneData.PositionSkipFrames == 0)
            {
                int nextIndex = frame / (cloneData.PositionSkipFrames + 1);
                nextPos = cloneData.Positions[nextIndex];
            }

            if (cloneData.ThrowInputs.Length < nextThrowInputChange)
            {
                if (frame == cloneData.ThrowInputs[nextThrowInputChange])
                {
                    throwInput = !throwInput;
                }
                nextThrowInputChange++;
            }

            // move whatever fraction of the way to the target is necessary
            Vector3 partialMove = transform.position + (nextPos - transform.position)/(cloneData.PositionSkipFrames + 1);
            //Debug.Log(partialMove);
            rb.MovePosition(partialMove);
            frame++;
        }
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

    public void Kill()
    {
        Destroy(gameObject);
    }
}
