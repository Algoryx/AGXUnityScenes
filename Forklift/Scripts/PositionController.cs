using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

public class PositionController : ScriptComponent
{
  public List<Constraint> MotorConstraints = new List<Constraint>();

  public KeyCode PositiveKey = KeyCode.W;
  public KeyCode NegativeKey = KeyCode.S;

  public float MaxSpeed = 2f;
  public float Acceleration = 1f;

  public bool IsMoving
  {
    get
    {
      bool moving = false;
      foreach (var speed in m_speed)
        moving |= Mathf.Abs(speed) > 0.0001;
      return moving;
    }
  }

  List<LockController> m_locks = new List<LockController>();
  List<RangeReal> m_range = new List<RangeReal>();
  List<float> m_speed = new List<float>();

  // Start is called before the first frame update
  protected override bool Initialize()
  {

    foreach (var motorConstraint in MotorConstraints)
    {
      if (motorConstraint == null || motorConstraint.GetInitialized<Constraint>() == null)
        return false;

      var constraint = motorConstraint.GetInitialized<Constraint>();
      m_locks.Add(constraint.GetController<LockController>());
      m_range.Add(constraint.GetController<RangeController>().Range);
      m_speed.Add(0);
    }
     
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
    for (int i = 0; i < m_locks.Count; i++)
    {
      float targetPosition = Mathf.Lerp(m_range[i].Min, m_range[i].Max, (input + 1) / 2);
      float neededSpeed = Mathf.Min(MaxSpeed, Mathf.Abs((targetPosition - m_locks[i].Position) / Time.fixedDeltaTime));
      float targetSpeed = (targetPosition > m_locks[i].Position) ? neededSpeed : -neededSpeed;

      m_speed[i] = Mathf.MoveTowards(m_speed[i], targetSpeed, Time.fixedDeltaTime * Acceleration);
      m_locks[i].Position = Mathf.Clamp((m_locks[i].Position + Time.fixedDeltaTime * m_speed[i]), m_range[i].Min, m_range[i].Max);
    }
  }
}
