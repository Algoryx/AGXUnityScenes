using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

/*
 * Drives 1D constraints (prismatic, hinges) by applying force to TargetSpeedController.
 */
public class ForceController : ScriptComponent
{
  public List<Constraint> MotorConstraints = new List<Constraint>();
  public PositionController PreventBrakingController = null;

  public KeyCode PositiveKey = KeyCode.W;
  public KeyCode NegativeKey = KeyCode.S;

  public AnimationCurve TorqueCurve = AnimationCurve.EaseInOut(0.0f, 1.0E4f, 10.0f, 0.0f);

  public bool UseBrake = true;

  public bool Debug = true;

  private float m_brakingForce;

  List<TargetSpeedController> m_motors = new List<TargetSpeedController>();

  protected override bool Initialize()
  {
    foreach (var motorConstraint in MotorConstraints)
    {
      if (motorConstraint == null || motorConstraint.GetInitialized<Constraint>() == null)
        return false;

      m_motors.Add(motorConstraint.GetInitialized<Constraint>().GetController<TargetSpeedController>());

      if (Debug)
      {
        UnityEngine.Debug.Log("Motor: " + name + ", constraint: " + motorConstraint.name);
      }
    }

    m_brakingForce = TorqueCurve.Evaluate(0);

    return true;
  }
  
  void FixedUpdate()
  {
    // Keyboard input - could be moved to standalone input component
    float input = 0;
    if (Input.GetKey(PositiveKey))
      input = 1;
    else if (Input.GetKey(NegativeKey))
      input = -1;

    // Update
    for (int i = 0; i < m_motors.Count; i++)
    {
      if (UseBrake && input == 0 && (PreventBrakingController == null || !PreventBrakingController.IsMoving))
      {
        if (Debug)
          m_brakingForce = TorqueCurve.Evaluate(0);
        m_motors[i].RowData[0].ForceRange = new RangeReal() { Min = -m_brakingForce, Max = m_brakingForce };
      }
      else
      {
        float drivingForce = input * TorqueCurve.Evaluate(Mathf.Abs(MotorConstraints[i].GetCurrentSpeed()));
        m_motors[i].RowData[0].ForceRange = new RangeReal() { Min = drivingForce, Max = drivingForce };
      }
    }
  }
}
