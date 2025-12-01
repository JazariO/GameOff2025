using System;
using UnityEngine;

public class BirdBrain : MonoBehaviour
{
    [SerializeField] Rigidbody body;
    [SerializeField] Collider meshCollider;
    [SerializeField] Animator animator;

    [SerializeField] BirdSO settings;

    public enum State
    {
        Idle,
        Fly
    }
    public State curState;

    [Serializable] public struct StatData
    {
        internal Vector3 targetPosition;
        internal Vector3 targetVelocity;
        internal Vector3 curVelocity;
    }
    public StatData statData;
    [Serializable] public struct InputData
    {
        internal Vector3 move;
        internal Vector3 direction;
    }
    [SerializeField] InputData inputData;

    [Serializable] public struct AnimData
    {
        internal int flyingHash;
        internal int idleHash;
    }
    [SerializeField] AnimData animData;

    private void Awake()
    {
        animData.idleHash = Animator.StringToHash("Idle");
        animData.flyingHash = Animator.StringToHash("Fly");
    }

    private void Start()
    {
        body.useGravity = false;
    }
    private void Update()
    {
        SelectState();
        UpdateState();
    }

    private void FixedUpdate()
    {
        FixedUpdateState();

        statData.targetVelocity = settings.moveSpeed * inputData.move;
        body.linearVelocity = Vector3.Lerp(body.linearVelocity, statData.targetVelocity, settings.accelation * Time.fixedDeltaTime);
    }

    private void SelectState()
    {
        if (Vector3.SqrMagnitude(inputData.move) > 0)
        {
            SetState(State.Fly);
        }
        else
        {
            SetState(State.Idle);
        }
    }
    private void SetState(State newState)
    {
        if (curState == newState) return;
        ExitState();
        curState = newState;
        EnterState();
    }

    private void EnterState()
    {
        switch (curState)
        {
            case State.Idle:
            {
                animator.Play(animData.idleHash);
            }
            break;

            case State.Fly:
            {
                animator.Play(animData.flyingHash);
            }
            break;
        }
    }
    private void UpdateState()
    {
        switch (curState)
        {
            case State.Idle:
            {

            }
            break;

            case State.Fly:
            {
                inputData.direction = (statData.targetPosition - transform.position).normalized;

                inputData.move = Vector3.Distance(transform.position, statData.targetPosition) > 0.1f ? inputData.direction : Vector3.zero;
            }
            break;
        }
    }

    private void FixedUpdateState()
    {
        switch (curState)
        {
            case State.Idle:
            {

            }
            break;

            case State.Fly:
            {

            }
            break;
        }
    }
    private void ExitState()
    {
        switch(curState)
        { 
            case State.Idle:
            {

            }
            break;

            case State.Fly:
            {

            }
            break;
        }
    }
}
