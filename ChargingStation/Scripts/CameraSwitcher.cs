using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    public CarAutoController carAutoController = null;
    public RobotEndController robotController = null;
    public CinemachineVirtualCamera carCamera = null;
    public CinemachineVirtualCamera robotCamera = null;
    public CinemachineVirtualCamera robotCloseUpCamera = null;


    void Start()
    {
        carCamera.enabled = true;
        robotCamera.enabled = false;
        robotCloseUpCamera.enabled = false;
        carAutoController.AtTargetEvent += (value) => { robotCamera.enabled = value; Debug.Log("AtTargetEvent: " + value);  };
        robotController.VeryCloseEvent += (value) => { robotCloseUpCamera.enabled = value; Debug.Log("VeryCloseEvent: " + value); };
    }
}
