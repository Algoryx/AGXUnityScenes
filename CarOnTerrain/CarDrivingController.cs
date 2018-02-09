using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;

public class CarDrivingController : MonoBehaviour
{
  public GameObject Chassis         = null;
  public Vector3 ChassisForward     = Vector3.left;
  public Constraint RearRightHinge  = null;
  public Constraint RearLeftHinge   = null;
  public Constraint FrontRightHinge = null;
  public Constraint FrontLeftHinge  = null;

  public KeyCode KeyForward = KeyCode.UpArrow;
  public KeyCode KeyReverse = KeyCode.DownArrow;

  public AnimationCurve TorqueCurve = AnimationCurve.EaseInOut( 0.0f, 1.0E4f, 50.0f, 0.0f );

  public bool Invert = true;

  private RigidBody m_chassisBody;

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
    return Vector3.Dot( Chassis.transform.TransformDirection( ChassisForward ), m_chassisBody.Native.getVelocity().ToHandedVector3() );
  }

  private void Start()
  {
    if ( !IsValid )
      Debug.LogWarning( "Car driving controller inactive since not all reference objects has been assigned." );

    m_chassisBody = Chassis.GetComponentInChildren<RigidBody>();
    if ( m_chassisBody == null )
      Debug.LogWarning( "Car driving controller inactive since chassis has no RigidBody.", Chassis );

    FrontRightHinge.GetController<TargetSpeedController>().Enable = true;
    FrontLeftHinge.GetController<TargetSpeedController>().Enable = true;
  }

  private void Update()
  {
    if ( !IsValid || m_chassisBody == null )
      return;

    bool forwardActive = Input.GetKey( KeyForward );
    bool reverseActive = Input.GetKey( KeyReverse );
    bool bothActive    = forwardActive && reverseActive;

    float torqueRight = TorqueCurve.Evaluate( Mathf.Abs( RearRightHinge.GetCurrentSpeed() ) );
    float torqueLeft  = TorqueCurve.Evaluate( Mathf.Abs( RearLeftHinge.GetCurrentSpeed() ) );

    float forceRangeRight = 0.0f;
    float forceRangeLeft  = 0.0f;
    float scale = Invert ? -1.0f : 1.0f;
    if ( !bothActive && forwardActive ) {
      forceRangeRight = scale * torqueRight;
      forceRangeLeft  = scale * torqueLeft;
    }
    else if ( !bothActive && reverseActive ) {
      forceRangeRight = -scale * torqueRight;
      forceRangeLeft  = -scale * torqueLeft;
    }

    RearRightHinge.GetController<TargetSpeedController>().RowData[ 0 ].ForceRange = new RangeReal() { Min = forceRangeRight, Max = forceRangeRight };
    FrontRightHinge.GetController<TargetSpeedController>().RowData[ 0 ].ForceRange = new RangeReal() { Min = 0.1f * forceRangeRight, Max = 0.1f * forceRangeRight };
    RearLeftHinge.GetController<TargetSpeedController>().RowData[ 0 ].ForceRange = new RangeReal() { Min = forceRangeLeft, Max = forceRangeLeft };
    FrontLeftHinge.GetController<TargetSpeedController>().RowData[ 0 ].ForceRange = new RangeReal() { Min = 0.1f * forceRangeLeft, Max = 0.1f * forceRangeLeft };
  }

  private void OnGUI()
  {
    GUILayout.Label( AGXUnity.Utils.GUI.MakeLabel( string.Format( "Speed: {0:0} km/h", 3.6f * CalculateSpeedForward() ), 20 ) );
  }
}
