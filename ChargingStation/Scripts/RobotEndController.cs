using UnityEngine;
using AGXUnity;
using System.Collections.Generic;
using System.Collections;
using System;

public class RobotEndController : ScriptComponent
{
    public RigidBody FollowBody = null;
    public RigidBody GrippedBody = null;
    public RigidBody GrippingBody = null;

    public float ManualMovementSpeed = 1;
    public float ManualMovementSpeedSpherical = 10;
    public float ManualRotationSpeed = 10;
    public float AutoMovementSpeed = 1;
    public float ZoomInDistance = 0.5f;


    [Tooltip("From Rest Point")]
    public float MaxReach = 2f;
    public float MaxGripDistance = 0.2f;

    public Transform RobotOrigin = null;
    public Transform RestPosition = null;
    public Transform ChargingPlug = null;
    public Transform CarSocket = null;

    public Vector3 grippingPositionOffset = new Vector3();
    public Vector3 grippingRotationOffset = new Vector3();

    public Action<bool> VeryCloseEvent = delegate { };

    private bool isAutomated = false;
    private bool gripped = false;
    private agx.Constraint lockJoint = null;
    private bool waitForKeyUp = false;

    private bool previouslyClose = false;


    void FixedUpdate()
    {
        ControlMovement();

        CheckDistance();
    }

    private new void Start()
    {
        base.Start();

        if (RestPosition != null)
            StartCoroutine(AnimateTowards(RestPosition));
    }

    private void CheckDistance()
    {
        if (!gripped || CarSocket == null || ChargingPlug == null)
            return;

        bool close = (CarSocket.position - ChargingPlug.position).magnitude < ZoomInDistance;
        if (close != previouslyClose)
        {
            VeryCloseEvent(close);
            previouslyClose = close;
        }
    }

    private void ControlMovement()
    {
        if (Input.GetKey(KeyCode.X))
            Gripper(!gripped);

        waitForKeyUp = Input.anyKey;

        if (!Input.anyKey || isAutomated || FollowBody == null || RobotOrigin == null)
            return;

        // WASD + QE as inputs, mapped like camera control in Unity
        // RFV+TGB for rotation
        // X for pickup

        Vector3 movement = new Vector3();
        Vector3 rotation = new Vector3();

        if (Input.GetKey(KeyCode.W))
            movement.x = ManualMovementSpeed;
        else if (Input.GetKey(KeyCode.S))
            movement.x = -ManualMovementSpeed;
        if (Input.GetKey(KeyCode.A))
            movement.z = -ManualMovementSpeedSpherical;
        if (Input.GetKey(KeyCode.D))
            movement.z = ManualMovementSpeedSpherical;
        if (Input.GetKey(KeyCode.Q))
            movement.y = ManualMovementSpeedSpherical;
        if (Input.GetKey(KeyCode.E))
            movement.y = -ManualMovementSpeedSpherical;
        if (Input.GetKey(KeyCode.T))
            rotation.x = ManualRotationSpeed;
        if (Input.GetKey(KeyCode.G))
            rotation.y = ManualRotationSpeed;
        if (Input.GetKey(KeyCode.B))
            rotation.z = ManualRotationSpeed;
        if (Input.GetKey(KeyCode.Y))
            rotation.x = -ManualRotationSpeed;
        if (Input.GetKey(KeyCode.H))
            rotation.y = -ManualRotationSpeed;
        if (Input.GetKey(KeyCode.N))
            rotation.z = -ManualRotationSpeed;

        if (Input.GetKey(KeyCode.Alpha1) && RestPosition != null)
            StartCoroutine(AnimateTowards(RestPosition));
        if (Input.GetKey(KeyCode.Alpha2) && ChargingPlug != null)
            StartCoroutine(AnimateTowards(ChargingPlug));
        if (Input.GetKey(KeyCode.Alpha3) && CarSocket != null)
            StartCoroutine(AnimateTowards(CarSocket));

        Vector3 direction = FollowBody.transform.position - RobotOrigin.position;

        movement *= Time.fixedDeltaTime;
        direction = Quaternion.Euler(0, movement.z, movement.y) * direction;
        direction = (direction.magnitude + movement.x) * direction.normalized;

        FollowBody.transform.rotation *= Quaternion.Euler(rotation * Time.fixedDeltaTime);
        FollowBody.transform.position = direction + RobotOrigin.position;
        FollowBody.SyncNativeTransform();
    }

    private void Gripper(bool grip)
    {
        if (GrippingBody == null || FollowBody == null || waitForKeyUp)
            return;

        if (grip)
        {
            GrippedBody.SyncNativeTransform();
            GrippingBody.SyncNativeTransform(); // Is this needed...?
            if (Vector3.Distance(GrippedBody.transform.position, GrippingBody.transform.position) > MaxGripDistance)
            {
                Debug.Log("Couldn't reach to grip: " + (GrippedBody.transform.position - GrippingBody.transform.position).magnitude + ", pos1: " + GrippedBody.transform.position + ", pos2: " + GrippingBody.transform.position);
                return;
            }

            if (lockJoint == null)
            {
                previouslyClose = false;
                
                agx.Frame grippedFrame = new agx.Frame();
                agx.Frame grippingFrame = new agx.Frame();

                Vector3 offset = GrippingBody.transform.rotation * grippingPositionOffset;
                grippedFrame.setLocalTranslate(GrippedBody.Native.getCmLocalTranslate());
                grippingFrame.setLocalTranslate(new agx.Vec3(offset.x, offset.y, offset.z));
                grippedFrame.setLocalRotate(new agx.EulerAngles(grippingRotationOffset.x * Mathf.Deg2Rad, grippingRotationOffset.y * Mathf.Deg2Rad, grippingRotationOffset.z * Mathf.Deg2Rad));

                lockJoint = new agx.LockJoint(GrippedBody.Native, grippedFrame, GrippingBody.Native, grippingFrame);
                lockJoint.setCompliance(0.00001f);
                lockJoint.setForceRange(new agx.RangeReal(-200, 200));
                lockJoint.setDamping(1);
                GetSimulation().add(lockJoint);
            }
        }
        else
        {
            if (lockJoint != null)
            {
                lockJoint.setEnable(false);
                lockJoint = null;
                VeryCloseEvent(false);
            }
        }

        gripped = !gripped;
    }

    private IEnumerator AnimateTowards(Transform target)
    {
        float maxDistance = (RestPosition.position - target.position).magnitude;

        if (maxDistance > MaxReach)
        {
            Debug.Log("Too far away");
            yield break;
        }

        float distance = (target.position - FollowBody.transform.position).magnitude;

        isAutomated = true;

        Vector3 startPos = FollowBody.transform.position;
        Quaternion startRot = FollowBody.transform.rotation;
        Vector3 endPos = target.position;
        Quaternion endRot = target.rotation;
        float t = 0;

        while (distance > 0.01f && t < 1)
        {
            t += AutoMovementSpeed * Time.fixedDeltaTime;

            FollowBody.transform.position = Vector3.Slerp(startPos, endPos, t);
            FollowBody.transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            FollowBody.SyncNativeTransform();

            distance = (target.position - FollowBody.transform.position).magnitude + Quaternion.Angle(target.rotation, FollowBody.transform.rotation);

            yield return null;
        }

        isAutomated = false;
    }

    private void OnDrawGizmos()
    {
        if (RestPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(RestPosition.position, 0.05f);
        }
    }
}
