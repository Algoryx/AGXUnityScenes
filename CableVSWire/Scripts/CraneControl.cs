using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;

public class CraneControl : MonoBehaviour {


  public float ArmCompliance = 1E-07f;
  public float WinchSpeed = 0.0f;

  private List<AGXUnity.Constraint> m_lockJoints;
  private AGXUnity.WireWinch m_winch;

  // Use this for initialization
  void Start () {
    AGXUnity.Constraint[] constraints = GetComponentsInChildren<AGXUnity.Constraint>();

    m_winch = GetComponentInChildren<WireWinch>();

    m_lockJoints = new List<Constraint>();
    foreach(var c in constraints)
    {
      if (c.Type == ConstraintType.LockJoint)
        m_lockJoints.Add(c);
    }    
  }
	

  private void updateCompliance()
  {
    foreach (var c in m_lockJoints)
      c.Native.setCompliance(ArmCompliance);
  }

	// Update is called once per frame
	void Update () {
    updateCompliance();

    if (m_winch != null)
      m_winch.Native.setSpeed(WinchSpeed);
  }


}
