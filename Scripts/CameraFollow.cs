using UnityEngine;

public class CameraFollow : MonoBehaviour
{
  public Transform Target           = null;
  public Vector3 Forward            = Vector3.forward;
  public float Distance             = 4.0f;
  public float Height               = 2.0f;
  public float RotationLerpScale    = 2.0f;
  public float TranslationLerpScale = 2.0f;

  void Start()
  {
    if ( Target == null )
      Debug.LogWarning( "Camera follow inactive since target object isn't given." );
  }

  void FixedUpdate()
  {
    if ( Target == null )
      return;

    var targetRotation = Quaternion.LookRotation( Target.position - transform.position, Vector3.up );
    transform.rotation = Quaternion.Slerp( transform.rotation, targetRotation, RotationLerpScale * Time.deltaTime );

    var targetPosition = Target.position - Distance * Target.TransformDirection( Forward ) + Height * Vector3.up;
    transform.position = Vector3.Slerp( transform.position, targetPosition, TranslationLerpScale * Time.deltaTime );
  }
}