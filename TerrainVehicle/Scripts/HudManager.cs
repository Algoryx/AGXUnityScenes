using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using AGXUnity;
using UnityEngine.Assertions;


public class HudManager : MonoBehaviour
{
  
  public Text SpeedText;
  public Text EngineRPMText;
  public Text FrictionText;
  public Slider FrictionSlider;
  public ContactMaterial GroundTireMaterial;

  public static double Speed = 0.0;
  public static double EngineRPM = 0.0;

  private FPSCamera FPSCameraScript;
  private CameraFollow CameraFollowScript;

  protected void Start()
  {
    if (SpeedText == null)
    {
      Debug.LogWarning("SpeedText object is null");
      return;
    }
    if (EngineRPMText == null)
    {
      Debug.LogWarning("EngineRPMText object is null");
      return;
    }


    var camera = GameObject.Find("Main Camera");
    Assert.IsNotNull(camera);

    SpeedText.text = string.Format("Speed {0:0.00} Km/h", Speed * 3.6);
    EngineRPMText.text = string.Format("Engine: {0:0000} rpm", EngineRPM);

    FPSCameraScript = camera.GetComponent<FPSCamera>();
    Assert.IsNotNull(FPSCameraScript, "Missing FPS Camera Object");

    CameraFollowScript = camera.GetComponent<CameraFollow>();
    Assert.IsNotNull(CameraFollowScript, "Missing Camera Follow Object");

    Assert.IsNotNull(GroundTireMaterial, "ContactMaterial Tire Ground is missing");

    FrictionSlider.value = GroundTireMaterial.FrictionCoefficients[0];
  }

  // Update is called once per frame
  void Update()
  {
    if (SpeedText == null)
      return;  

    FrictionText.text = string.Format("Friction: {0:0.0}", GroundTireMaterial.FrictionCoefficients[0]);
    EngineRPMText.text = string.Format("Engine: {0:0000} rpm", EngineRPM);
    SpeedText.text = string.Format("Speed {0:0.00} Km/h", Speed * 3.6);
  }

  public void ToggleFollowVehicleChanged(bool val)
  {
    FPSCameraScript.enabled = !val;
    CameraFollowScript.enabled = val;
  }

  public void FrictionSliderChanged(float val)
  {
    GroundTireMaterial.FrictionCoefficients = new Vector2(val,val);
  }

}
