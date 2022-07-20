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
public abstract class Script_Instance_45a84 : GH_ScriptInstance
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
  private void RunScript(int num, ref object OutCurves, ref object OutSurface, ref object OutLoft, ref object OutPoints)
  {
    //list of all points
    List<Point3d> allPoints = new List<Point3d>();
    //list of curves
    List<Curve> curves = new List<Curve>();

    for (int y = 0; y < num; y++)
    {
      //curve points
      List<Point3d> crvPoints = new List<Point3d>();
      for (int x = 0; x < num; x++)
      {
        double z = Math.Sin(Math.PI + (x + y));
        Point3d pt = new Point3d(x, y, z);
        crvPoints.Add(pt);
        allPoints.Add(pt);
      }
      //create a degree 3 nurbs curve from control points
      NurbsCurve crv = (NurbsCurve)Curve.CreateControlPointCurve(crvPoints, 3);
      curves.Add(crv);
    }
    //create a nurbs surface from control points
    NurbsSurface srf = NurbsSurface.CreateFromPoints(allPoints, num, num, 3, 3);

    //create a loft brep from curves
    Brep[] breps = Brep.CreateFromLoft(curves, Point3d.Unset, Point3d.Unset, LoftType.Tight, false);

    //Assign output
    OutCurves = curves;
    OutSurface = srf;
    OutLoft = breps;
    OutPoints = allPoints;
  }
  #endregion
  #region Additional

  #endregion
}