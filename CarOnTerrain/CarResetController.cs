using System;
using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;

namespace Assets.AGXUnityScenes.CarOnTerrain
{
  public class CarResetController : MonoBehaviour
  {
    public GameObject Chassis              = null;
    public Vector3 LocalAttachmentPosition = Vector3.up;
    public KeyCode KeyReset                = KeyCode.Space;
    public float LinearVelocityDamping     = 10.0f;
    public float AngularVelocityDamping    = 10.0f;
    public float ForceMagnitudeScale       = 50.0f;

    private bool m_active              = false;
    private RigidBody m_rb             = null;
    private Vector3 m_orgLinVelDamping = Vector3.zero;
    private Vector3 m_orgAngVelDamping = Vector3.zero;

    private void Start()
    {
      if ( Chassis == null )
        Debug.LogWarning( "Car reset controller inactive since chassis hasn't been given." );

      m_rb = Chassis.GetComponentInChildren<RigidBody>();
      if ( m_rb == null )
        Debug.LogWarning( "Car reset controller inactive since chassis has no RigidBody visible component." );
    }

    private void FixedUpdate()
    {
      if ( m_rb == null )
        return;

      if ( Input.GetKey( KeyReset ) )
        OnResetActive();
      else
        OnResetInactive();
    }

    private void OnResetActive()
    {
      var chassisUp = Chassis.transform.up;
      var tiltAngle = Vector3.Angle( chassisUp, Vector3.up );
      if ( tiltAngle > 20.0f ) {
        if ( !m_active ) {
          m_orgLinVelDamping = m_rb.LinearVelocityDamping;
          m_orgAngVelDamping = m_rb.AngularVelocityDamping;
          m_rb.LinearVelocityDamping  = LinearVelocityDamping * Vector3.one;
          m_rb.AngularVelocityDamping = AngularVelocityDamping * Vector3.one;
          m_active = true;
        }

        m_rb.Native.addForceAtPosition( ( ForceMagnitudeScale * m_rb.MassProperties.Mass.Value * Vector3.up ).ToHandedVec3(),
                                        Chassis.transform.TransformPoint( LocalAttachmentPosition ).ToHandedVec3() );
      }
      else
        OnResetInactive();
    }

    private void OnResetInactive()
    {
      if ( !m_active )
        return;

      m_rb.LinearVelocityDamping = m_orgLinVelDamping;
      m_rb.AngularVelocityDamping = m_orgAngVelDamping;
      m_active = false;
    }
  }
}
