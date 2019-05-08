using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AGXUnity;
using AGXUnity.Utils;
using AGXUnity.Collide;

public class Pulley : ScriptComponent
{
  // Start is called before the first frame update
  protected override bool Initialize()
  {
    Shape[] shapes = GetComponentsInChildren<Shape>();

    foreach (Shape shape in shapes)
    {
      var geom = shape.GetInitialized<Shape>().NativeGeometry;
      geom.getPropertyContainer().addPropertyBool( "Pulley", true );

    }
    return true;
  }

  // Update is called once per frame
  void Update()
  {

  }
}
