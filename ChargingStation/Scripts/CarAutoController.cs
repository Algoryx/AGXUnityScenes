using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;
using System.Collections;
using UnityEditor;
using System;

public class CarAutoController : MonoBehaviour
{
    public RigidBody Chassis = null;
    public Constraint RearRightHinge = null;
    public Constraint RearLeftHinge = null;
    public Constraint FrontRightHinge = null;
    public Constraint FrontLeftHinge = null;

    public AnimationCurve TorqueCurve = AnimationCurve.EaseInOut(0.0f, 1.0E4f, 50.0f, 0.0f);

    public bool Invert = true;
    public bool ShowSpeedInfo = true;

    public float BrakingDistance = 5f;

    public Transform TargetPosition = null;
    private bool moving = false;

    private float accelerator = 0f;

    private Constraint[] hinges = null;
    private TargetSpeedController[] motors = null;
    private LockController[] parkingBrakes = null;

    public Action<bool> AtTargetEvent = delegate { };

    public void Goto()
    {
        if (TargetPosition == null)
            return;

        if (!moving)
            StartCoroutine(MoveTowardsTarget());
    }

    private IEnumerator MoveTowardsTarget()
    {
        moving = true;

        foreach (var brake in parkingBrakes)
            brake.Enable = false;

        motors[2].Enable = true;
        motors[3].Enable = true;

        float distanceAlongForward = Vector3.Dot(TargetPosition.position - Chassis.transform.position, Chassis.transform.forward);
        while (distanceAlongForward > 0)
        {
            accelerator = Mathf.Min(1f, Mathf.Abs(distanceAlongForward) / BrakingDistance) * Mathf.Sign(distanceAlongForward);
            yield return null;

            distanceAlongForward = Vector3.Dot(TargetPosition.position - Chassis.transform.position, Chassis.transform.forward);
        }

        Debug.Log("At target " + TargetPosition.name);
        AtTargetEvent(true);
        accelerator = 0;

        motors[2].Enable = false;
        motors[3].Enable = false;

        for (int i = 0; i < 4; i++)
        {
            parkingBrakes[i].Position = hinges[i].GetCurrentAngle();
            parkingBrakes[i].Enable = true;
        }
        moving = false;
    }

    public bool IsValid
    {
        get
        {
            return RearRightHinge != null &&
                   RearLeftHinge != null &&
                   FrontRightHinge != null &&
                   FrontLeftHinge != null &&
                   Chassis != null;
        }
    }

    private float CalculateSpeedForward()
    {
        return Vector3.Dot(Chassis.transform.forward, Chassis.Native.getVelocity().ToHandedVector3());
    }

    private void Start()
    {
        if (!IsValid)
            Debug.LogWarning("Car driving controller inactive since not all reference objects has been assigned.");

        hinges = new Constraint[4];
        motors = new TargetSpeedController[4];
        parkingBrakes = new LockController[4];

        hinges[0] = RearRightHinge;
        hinges[1] = RearLeftHinge;
        hinges[2] = FrontRightHinge;
        hinges[3] = FrontLeftHinge;

        for (int i = 0; i < 4; i++)
        {
            motors[i] = hinges[i].GetController<TargetSpeedController>();
            parkingBrakes[i] = hinges[i].GetController<LockController>();
        }
        motors[2].Enable = true;
        motors[3].Enable = true;

        Goto();
    }

    private void Update()
    {
        if (!IsValid)
            return;

        if (!moving)
        {
            if (Mathf.Abs(CalculateSpeedForward()) > 0.01f)
                for (int i = 0; i < 4; i++)
                    parkingBrakes[i].Position = hinges[i].GetCurrentAngle();
            return;
        }

        bool forwardActive = accelerator > 0;
        bool reverseActive = accelerator < 0;
        bool bothActive = forwardActive && reverseActive;

        float torqueRight = accelerator * TorqueCurve.Evaluate(Mathf.Abs(RearRightHinge.GetCurrentSpeed()));
        float torqueLeft = accelerator * TorqueCurve.Evaluate(Mathf.Abs(RearLeftHinge.GetCurrentSpeed()));

        float forceRangeRight = 0.0f;
        float forceRangeLeft = 0.0f;
        float scale = Invert ? -1.0f : 1.0f;


        if (!bothActive && forwardActive)
        {
            forceRangeRight = scale * torqueRight;
            forceRangeLeft = scale * torqueLeft;
        }
        else if (!bothActive && reverseActive)
        {
            forceRangeRight = -scale * torqueRight;
            forceRangeLeft = -scale * torqueLeft;
        }

        //motors[0].RowData[0].ForceRange = new RangeReal() { Min = forceRangeRight, Max = forceRangeRight };
        //motors[1].RowData[0].ForceRange = new RangeReal() { Min = forceRangeLeft, Max = forceRangeLeft };
        motors[2].RowData[0].ForceRange = new RangeReal() { Min = 0.1f * forceRangeRight, Max = 0.1f * forceRangeRight };
        motors[3].RowData[0].ForceRange = new RangeReal() { Min = 0.1f * forceRangeLeft, Max = 0.1f * forceRangeLeft };
    }

    private void OnGUI()
    {
        if (ShowSpeedInfo)
          GUILayout.Label(AGXUnity.Utils.GUI.MakeLabel(string.Format("Speed: {0:0} km/h", 3.6f * CalculateSpeedForward()), 20));
    }
}

[CustomEditor(typeof(CarAutoController))]
public class CarAutoControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();

        CarAutoController carAutoController = (CarAutoController)target;

        if (GUILayout.Button("Go to target"))
        {
            carAutoController.Goto();
        }
    }
}