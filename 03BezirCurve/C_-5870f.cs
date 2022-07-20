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
public abstract class Script_Instance_5870f : GH_ScriptInstance
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
  private void RunScript(List<Point3d> bezierPts, int segments, ref object BezierCrv)
  {
    if (segments < 2)
      segments = 2;

    List<Point3d> evalPts = new List<Point3d>();
    double step = 1.0 / (double)segments;

    for (int i = 0; i <= segments; i++)
    {
      double t = i * step;
      Point3d pt = Point3d.Unset;
      EvalPoint(bezierPts, t, ref pt);
      if (pt.IsValid)
        evalPts.Add(pt);
    }

    Polyline pline = new Polyline(evalPts);
    BezierCrv = pline;
  }
  #endregion
  #region Additional
  public void EvalPoint(List<Point3d> points, double t, ref Point3d evalPt)
  {
    //stopping condition - point at parameter t is found
    if (points.Count < 2)
      return;

    List<Point3d> tPoints = new List<Point3d>();

    for (int i = 1; i < points.Count; i++)
    {
      Line line = new Line(points[i - 1], points[i]);
      Point3d pt = line.PointAt(t);
      tPoints.Add(pt);
    }
    if (tPoints.Count == 1)
      evalPt = tPoints[0];
    EvalPoint(tPoints, t, ref evalPt);
  }
  #endregion
}