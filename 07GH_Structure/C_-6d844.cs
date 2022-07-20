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
public abstract class Script_Instance_6d844 : GH_ScriptInstance
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
  private void RunScript(Point3d datumPt, double width, double height, double depth, double widthOfFrame, double depthOfGlass, int countOfShutter, int angleOfShutter, ref object outWindowFrame, ref object outGlass, ref object outShutters)
  {
    Brep windowFrame = CreateWindowFrame(datumPt, width, height, depth, widthOfFrame);

    Brep glass = CreateGlass(datumPt, width, height, depth, widthOfFrame, depthOfGlass);

    List<Brep> shutters = CreateShutters(datumPt, width, height, depth, widthOfFrame, depthOfGlass, countOfShutter, angleOfShutter);

    outWindowFrame = windowFrame;
    outGlass = glass;
    outShutters = shutters;
  }
  #endregion
  #region Additional
  public Brep CreateWindowFrame(Point3d datumPt, double width, double height, double depth, double widthOfFrame)
  {
    Brep windowFrame = new Brep();
    Curve baseCurve = new Rectangle3d(new Plane(datumPt, Vector3d.XAxis, Vector3d.ZAxis), width, height).ToNurbsCurve();
    Brep outterBrep = Surface.CreateExtrusion(baseCurve, Vector3d.YAxis * depth).ToBrep().CapPlanarHoles(0.01);

    Plane offsetPlane;
    baseCurve.TryGetPlane(out offsetPlane);
    Curve innerCurve = baseCurve.Offset(offsetPlane, -1*widthOfFrame, 0.01, CurveOffsetCornerStyle.Sharp)[0];
    Brep innerBrep = Surface.CreateExtrusion(innerCurve, Vector3d.YAxis * depth).ToBrep().CapPlanarHoles(0.01);

    windowFrame = Brep.CreateBooleanDifference(outterBrep, innerBrep, 0.01, false)[0];
    return windowFrame;
  }

  public Brep CreateGlass(Point3d datumPt, double width, double height, double depth, double widthOfFrame, double depthOfGlass)
  {
    Brep glass = new Brep();
    depthOfGlass = Math.Min(depth, depthOfGlass);

    Curve baseCurve = new Rectangle3d(new Plane(datumPt, Vector3d.XAxis, Vector3d.ZAxis), width, height).ToNurbsCurve();
    Brep outterBrep = Surface.CreateExtrusion(baseCurve, Vector3d.YAxis * depth).ToBrep().CapPlanarHoles(0.01);

    Plane offsetPlane;
    baseCurve.TryGetPlane(out offsetPlane);
    Curve innerCurve = baseCurve.Offset(offsetPlane, -1 * widthOfFrame, 0.01, CurveOffsetCornerStyle.Sharp)[0];

    innerCurve.Transform(Transform.Translation(0, (depth - depthOfGlass) / 2, 0));
    glass = Surface.CreateExtrusion(innerCurve, Vector3d.YAxis * depthOfGlass).ToBrep().CapPlanarHoles(0.01);
    return glass;
  }

  public List<Brep> CreateShutters(Point3d datumPt, double width, double height, double depth, double widthOfFrame, double depthOfGlass, int countOfShutter, int angleOfShutter)
  {
    List<Brep> shutters = new List<Brep>();
    double lengthOfShutter = width - 2 * widthOfFrame;
    double heightOfShutter = (height - 2 * widthOfFrame) / countOfShutter;
    depthOfGlass = Math.Min(depth, depthOfGlass);
    double depthOfShutter = ((depth - depthOfGlass) / 2) / 4;
    double rotateAngle = Rhino.RhinoMath.ToRadians(angleOfShutter);

    Point3d startPoint = new Point3d(datumPt);
    startPoint.Transform(Transform.Translation(widthOfFrame, 0, widthOfFrame));
    
    for (int i = 0; i < countOfShutter; i++)
    {
      Point3d pt = new Point3d(startPoint);
      pt.Transform(Transform.Translation(0, 0, i * heightOfShutter));
      Plane shutterPlane = new Plane(pt, Vector3d.XAxis, Vector3d.ZAxis);
      Curve baseCurve = new Rectangle3d(shutterPlane, lengthOfShutter, heightOfShutter).ToNurbsCurve();
      Brep shutter = Surface.CreateExtrusion(baseCurve, Vector3d.YAxis * depthOfShutter).ToBrep().CapPlanarHoles(0.01);
      shutter.Transform(Transform.Translation(0, depthOfShutter, 0));
      Point3d centerPt = shutter.GetBoundingBox(true).Center;
      shutter.Transform(Transform.Rotation(rotateAngle, Vector3d.XAxis, centerPt));
      shutters.Add(shutter);
    }

    return shutters;
  }
  #endregion
}