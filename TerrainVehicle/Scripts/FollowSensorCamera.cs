using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowSensorCamera : MonoBehaviour {

  public Transform Target;
  public Vector3 Forward= new Vector3(0,0,1);
  public float Distance=0.5f;
  public RenderTexture TargetTexture;


  Vector3 relativePosition;

	// Use this for initialization
	void Start () {
    if (Target == null) {
      Debug.LogWarning("Camera follow inactive since target object isn't given.");
      return;
    }

    relativePosition = transform.position - Target.position;
    relativePosition = Target.InverseTransformVector(relativePosition);
  }
	
	// Update is called once per frame
  void Update()
  {
    if (Target == null)
      return;

    var forward = Target.TransformVector(Forward);
    forward.Normalize();

    var dir = Target.TransformVector(relativePosition);

    transform.position = Target.position + dir;

    var lookAt = transform.position + forward * 1.0f;
    transform.LookAt(lookAt);


    // assumes you have your RenderTexture renderTexture
    //Texture2D tex2d = new Texture2D(TargetTexture.width, TargetTexture.height, TextureFormat.RGB24, false);

    //RenderTexture.active = TargetTexture;
    //tex2d.ReadPixels(new Rect(0, 0, TargetTexture.width, TargetTexture.height), 0, 0);
    //tex2d.Apply();

    //var pixels = tex2d.GetPixels32();
  }
}
