using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using agx;

/// <summary>
/// Class to control the winch construction from the editor
/// </summary>
public class WinchControl : ScriptComponent
{
  // Controlled by the user to set the speed of the winch
  public float winchSpeed = 0.0f;

  // Controlled by the user to set the speed of the prismatic lift
  public float liftPrismaticSpeed = 0.0f;

  // Controlled by the user to set the speed of rotating the base hinge
  public float baseHingeSpeed = 0.0f;


  // To be set by the user in the editor
  public AGXUnity.Constraint baseHinge;
  public AGXUnity.Constraint liftPrismatic;
  public AGXUnity.Wire mainWire;

  // Will display the number of meter inside the winch
  public float spooledIn=0;

  // Will display the tension (N) applied by the winch to the wire
  public float winchTension = 0;


  // Internal variables
  private agx.TargetSpeedController m_baseHingeMotor;
  private agx.TargetSpeedController m_liftPrismaticMotor;
  private agxWire.WireWinchController m_winch;


  // Start is called before the first frame update
  protected override bool Initialize()
  {

    // Get the winch from the wire (set in the editor)
    m_winch = mainWire.GetInitialized<Wire>().EndWinch.Native;

    if (m_winch == null)
    {
      Debug.LogWarning("No EndWinch", this);
      return false;
    }

    // Get the native (AGX Dynamics Hinge) from the editor
    var hinge = baseHinge.GetInitialized<AGXUnity.Constraint>().Native.asHinge();
    if (hinge == null)
    {
      Debug.LogWarning("No hinge available", this);
      return false;
    }
    m_baseHingeMotor = hinge.getMotor1D();

    // Get the native Prismatic constraint set in the editor
    var prismatic = liftPrismatic.GetInitialized<AGXUnity.Constraint>().Native.asPrismatic();
    if (prismatic == null)
    {
      Debug.LogWarning("No prismatic available", this);
      return false;
    }
    m_liftPrismaticMotor = prismatic.getMotor1D();


    return true;
  }

  // Update is called once per frame
  void Update()
  {

    // Clamp the speed of the prismatic motor
    const float prismaticMaxSpeed = 0.1f;
    liftPrismaticSpeed = Mathf.Clamp(liftPrismaticSpeed, -prismaticMaxSpeed, prismaticMaxSpeed);
    if (m_liftPrismaticMotor != null)
    {
      m_liftPrismaticMotor.setSpeed(liftPrismaticSpeed);
    }

    // Clamp the speed of the hinge motor
    const float baseHingeMaxSpeed = 0.1f;
    baseHingeSpeed = Mathf.Clamp(baseHingeSpeed, -baseHingeMaxSpeed, baseHingeMaxSpeed);
    if (m_baseHingeMotor != null)
    {
      m_baseHingeMotor.setSpeed(baseHingeSpeed);

    }

    // Clamp the speed of the winch
    const float winchMaxSpeed = 0.20f;
    winchSpeed = Mathf.Clamp(winchSpeed, -winchMaxSpeed, winchMaxSpeed);
    if (m_winch != null)
    {

      // Get the force applied by the winch to the wire
      winchTension = (float)m_winch.getCurrentForce();

      // Get the number of meter of wire inside the winch
      spooledIn = (float)m_winch.getPulledInWireLength();

      // Stop spooling in if we have a certain amount of wire inside the winch
      if (spooledIn > 16.3 && winchSpeed < 0)
        winchSpeed = 0;
      m_winch.setSpeed(winchSpeed);
    }
  }

}
