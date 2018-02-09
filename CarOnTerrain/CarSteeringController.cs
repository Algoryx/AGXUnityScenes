using UnityEngine;
using AGXUnity;

public class CarSteeringController : MonoBehaviour
{
  public Constraint RightConstraint = null;
  public Constraint LeftConstraint = null;

  public RigidBody SpeedReference = null;

  public KeyCode KeyRight = KeyCode.RightArrow;
  public KeyCode KeyLeft  = KeyCode.LeftArrow;

  public float MaxAngle           = 30.0f;
  public float SpeedScaleKeyDown  = 1.0f;
  public float SpeedScaleRealease = 1.0f;

  private LockController m_rightController = null;
  private LockController m_leftController  = null;

  private void Start()
  {
    if ( RightConstraint == null || LeftConstraint == null )
      Debug.LogWarning( "Car steering controller inactive since one or both constraints aren't assigned." );

    m_rightController = RightConstraint.GetController<LockController>( Constraint.ControllerType.Rotational );
    m_leftController  = LeftConstraint.GetController<LockController>( Constraint.ControllerType.Rotational );
    if ( m_rightController == null || m_leftController == null ) {
      Debug.LogWarning( "Car steering controller inactive since one or both constraints doesn't have rotation controllers." );
      if ( m_rightController == null )
        Debug.LogWarning( "  RightConstraint.Type: " + RightConstraint.Type, RightConstraint );
      if ( m_leftController == null )
        Debug.LogWarning( "  LeftConstraint.Type: " + LeftConstraint.Type, LeftConstraint );
    }
  }

  private void FixedUpdate()
  {
    if ( m_rightController == null || m_leftController == null )
      return;

    bool steerRightActive = Input.GetKey( KeyRight );
    bool steerLeftActive  = Input.GetKey( KeyLeft );
    bool bothActive       = steerRightActive && steerLeftActive;
    float maxAngleRad     = Mathf.Deg2Rad * MaxAngle;
    float targetAngle     = bothActive ?
                              0.0f :
                            steerRightActive ?
                              maxAngleRad :
                            steerLeftActive ?
                             -maxAngleRad :
                              0.0f;

    var speedScale = 1.0f;
    if ( SpeedReference != null ) {
      var speed = SpeedReference.LinearVelocity.magnitude;
      speedScale = Mathf.Sqrt( speed );
    }
    var lerpSpeed  = targetAngle == 0.0f ?
                       30.0f * SpeedScaleRealease / speedScale :
                       15.0f * SpeedScaleKeyDown / speedScale;

    var rightCurrentAngle = RightConstraint.GetCurrentAngle( Constraint.ControllerType.Rotational );
    var leftCurrentAngle  = LeftConstraint.GetCurrentAngle( Constraint.ControllerType.Rotational );
    m_rightController.Position = Mathf.Lerp( rightCurrentAngle, targetAngle, lerpSpeed * Time.deltaTime );
    m_leftController.Position  = Mathf.Lerp( leftCurrentAngle, targetAngle, lerpSpeed * Time.deltaTime );
  }
}
