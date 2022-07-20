using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;



/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_ea76d : GH_ScriptInstance
{
  #region Utility functions
  /// <summary>Print a String to the [Out] Parameter of the Script component.</summary>
  /// <param name="text">String to print.</param>
  private void Print(string text) { /* Implementation hidden. */ }
  /// <summary>Print a formatted String to the [Out] Parameter of the Script component.</summary>
  /// <param name="format">String format.</param>
  /// <param name="args">Formatting parameters.</param>
  private void Print(string format, params object[] args) { /* Implementation hidden. */ }
  /// <summary>Print useful information about an object instance to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj) { /* Implementation hidden. */ }
  /// <summary>Print the signatures of all the overloads of a specific method to the [Out] Parameter of the Script component. </summary>
  /// <param name="obj">Object instance to parse.</param>
  private void Reflect(object obj, string method_name) { /* Implementation hidden. */ }
  #endregion

  #region Members
  /// <summary>Gets the current Rhino document.</summary>
  private readonly RhinoDoc RhinoDocument;
  /// <summary>Gets the Grasshopper document that owns this script.</summary>
  private readonly GH_Document GrasshopperDocument;
  /// <summary>Gets the Grasshopper script component that owns this script.</summary>
  private readonly IGH_Component Component;
  /// <summary>
  /// Gets the current iteration count. The first call to RunScript() is associated with Iteration==0.
  /// Any subsequent call within the same solution will increment the Iteration count.
  /// </summary>
  private readonly int Iteration;
  #endregion
  /// <summary>
  /// This procedure contains the user code. Input parameters are provided as regular arguments,
  /// Output parameters as ref arguments. You don't have to assign output parameters,
  /// they will have a default value.
  /// </summary>
  #region Runscript
  private void RunScript(Surface srf, List<Polyline> inPolyline, int degree, ref object OutPolylines, ref object OutMesh, ref object A)
  {
    //instantiate the collection of all panels
    List<Polyline> outPanels = new List<Polyline>();

    //limit to 6 subdivisions
    if (degree > 6)
      degree = 6;
    for (int i = 0; i < degree; i++)
    {
      //outer polylines
      List<Polyline> plines = new List<Polyline>();
      //mid polylines
      List<Polyline> midPlines = new List<Polyline>();
      //generate subdivided panels
      bool result = SubPanelOnSurface(srf, inPolyline.ToArray(), ref plines, ref midPlines);
      if (result == false)
        break;

      //add outer panels
      outPanels.AddRange(plines);
      //add mid panels only in the last iteration
      if (i == degree - 1)
        outPanels.AddRange(midPlines);
      else //subdivede mid panels only
        inPolyline = midPlines;

      A = midPlines;
    }

    //create a mesh from all polylines
    Mesh joinedMesh = new Mesh();
    for (int i = 0; i < outPanels.Count; i++)
    {
      Mesh mesh = Mesh.CreateFromClosedPolyline(outPanels[i]);
      joinedMesh.Append(mesh);
    }

    //make sure all mesh faces normals are  in the same general direction
    joinedMesh.UnifyNormals();

    //assign output
    OutPolylines = outPanels;
    OutMesh = joinedMesh;
  }
  #endregion
  #region Additional
  public bool SubPanelOnSurface(Surface srf,Polyline[] inputPanels, ref List<Polyline> outPanels,ref List<Polyline> midPanels)
  {
    //check for a valid input
    if (inputPanels.Length == 0 || null == srf)
      return false;

    for (int i = 0; i < inputPanels.Length; i++)
    {
      Polyline ipline = inputPanels[i];
      if (!ipline.IsValid || !ipline.IsClosed)
        continue;

      //stack of points
      List<Point3d> stack = new List<Point3d>();
      Polyline newPline = new Polyline();

      for (int j = 1; j < ipline.Count; j++)
      {
        Line line = new Line(ipline[j - 1], ipline[j]);
        if (line.IsValid)
        {
          Point3d mid = line.PointAt(0.5);
          double s, t;
          srf.ClosestPoint(mid,out s, out t);
          mid = srf.PointAt(s, t);
          newPline.Add(mid);
          stack.Add(ipline[j - 1]);
          stack.Add(mid);
        }
      }

      //add the first 2 point to close last triangle
      stack.Add(stack[0]);
      stack.Add(stack[1]);

      //close
      newPline.Add(newPline[0]);
      midPanels.Add(newPline);

      for (int j = 2; j < stack.Count; j++)
      {
        Polyline pl = new Polyline(new List<Point3d> { stack[j - 2], stack[j - 1], stack[j], stack[j - 2] });
        outPanels.Add(pl);
      }
    }
    return true;
  }
  #endregion
}