using UnityEngine;
using AGXUnity;
using System.Collections.Generic;

public class SocketController : MonoBehaviour
{
    public Transform Plug = null;
    public List<AGXUnity.Collide.Shape> SocketObstacles = new List<AGXUnity.Collide.Shape>();
    public Transform CorrectOrientation = null;
    public float Tolerance = 2f;
    public float PluggedInDistance = 0.05f;

    private float test;

    private void Update()
    {
        if (Plug == null || SocketObstacles.Count < 1 || CorrectOrientation == null)
            return;
        
        bool correctOrientation = Tolerance > Vector3.Angle(Plug.forward, CorrectOrientation.forward);
        bool pluggedIn = PluggedInDistance > Vector3.Magnitude(Plug.position - CorrectOrientation.position);
        test = Vector3.Magnitude(Plug.position - CorrectOrientation.position);
        foreach (var obstacle in SocketObstacles)
        {
            if (obstacle.enabled && correctOrientation)
                obstacle.enabled = false;
            else if(!obstacle.enabled && !pluggedIn && !correctOrientation)
                obstacle.enabled = true;
        }
    }
}
