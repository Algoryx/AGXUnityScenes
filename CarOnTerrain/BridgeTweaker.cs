using UnityEngine;
using AGXUnity;

namespace Assets.AGXUnityScenes.CarOnTerrain
{
  public class BridgeTweaker : MonoBehaviour
  {
    public float Compliance               = 1.0E-6f;
    public float Damping                  = 0.05f;
    public float Mass                     = 250.0f;
    public Vector3 LinearVelocityDamping  = Vector3.zero;
    public Vector3 AngularVelocityDamping = Vector3.zero;

    private void Start()
    {
      RigidBody[] bodies = GetComponentsInChildren<RigidBody>();
      foreach ( var rb in bodies ) {
        rb.MassProperties.Mass.UserValue  = Mass;
        rb.MassProperties.Mass.UseDefault = false;

        rb.LinearVelocityDamping  = LinearVelocityDamping;
        rb.AngularVelocityDamping = AngularVelocityDamping;
      }

      Constraint[] hinges = GetComponentsInChildren<Constraint>();
      foreach ( var hinge in hinges ) {
        if ( hinge.Type != ConstraintType.Hinge )
          continue;

        hinge.GetController<LockController>().Enable = true;
        hinge.GetController<LockController>().RowData[ 0 ].Compliance = Compliance;
        hinge.GetController<LockController>().RowData[ 0 ].Damping = Damping;
      }
    }
  }
}
