using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

public class SpeedController : ScriptComponent
{
  public List<Constraint> MotorConstraints = new List<Constraint>();

  public KeyCode PositiveKey = KeyCode.W;
  public KeyCode NegativeKey = KeyCode.S;
  public string JoystickAxisName = "Axis";

  public float MaxSpeed = 2f;
  public float Acceleration = 1f;

  List<TargetSpeedController> m_motors = new List<TargetSpeedController>();

  private bool m_hasJoysticks = false;

  // Start is called before the first frame update
  protected override bool Initialize()
  {

    foreach (var motorConstraint in MotorConstraints)
    {
      if (motorConstraint == null || motorConstraint.GetInitialized<Constraint>() == null)
        return false;

      var motor = motorConstraint.GetInitialized<Constraint>().GetController<TargetSpeedController>();
      m_motors.Add(motor);
    }

    if (Input.GetJoystickNames().Length > 0)
      m_hasJoysticks = true;

    return true;
  }
  
  void FixedUpdate()
  {
    // Input - could be moved to standalone input component
    float input = m_hasJoysticks ? Input.GetAxis(JoystickAxisName) : 0;
    if (Input.GetKey(PositiveKey))
      input = 1;
    else if (Input.GetKey(NegativeKey))
      input = -1;

    float target = input * MaxSpeed;

    // Update
    for (int i = 0; i < m_motors.Count; i++)
    {
      m_motors[i].Speed = Mathf.MoveTowards(m_motors[i].Speed, target, Time.fixedDeltaTime * Acceleration);
    }
  }
}
