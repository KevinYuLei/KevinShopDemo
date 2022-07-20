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
public abstract class Script_Instance_c703f : GH_ScriptInstance
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
  private void RunScript(List<Curve> verticalLines, List<Curve> horizontalLines, double extensionLength, double heightOfColumn, double beamHeight, double beamWidth, bool type, ref object outVerticalBeams, ref object outHorizontalBeams)
  {
    //trim the both curve end to get the beam baseCurve in the real length
    TrimLines(extensionLength, ref verticalLines);
    TrimLines(extensionLength, ref horizontalLines);

    //move the curve to the top of the column
    for (int i = 0; i < verticalLines.Count; i++)
    {
      verticalLines[i].Transform(Transform.Translation(0, 0, heightOfColumn));
    }
    for (int i = 0; i < horizontalLines.Count; i++)
    {
      horizontalLines[i].Transform(Transform.Translation(0, 0, heightOfColumn));
    }

    List<Brep> verticalBeams = new List<Brep>();
    List<Brep> horizontalBeams = new List<Brep>();
    CreateBeams(verticalLines, beamHeight, beamWidth, type, ref verticalBeams);
    CreateBeams(horizontalLines, beamHeight, beamWidth, type, ref horizontalBeams);

    outVerticalBeams = verticalBeams;
    outHorizontalBeams = horizontalBeams;
  }
  #endregion
  #region Additional
  public void TrimLines(double extensionLength,ref List<Curve> lines)
  {
    for (int i = 0; i < lines.Count; i++)
    {
      lines[i] = lines[i].Trim(CurveEnd.Both, extensionLength);
    }
  }

  public void CreateBeams(List<Curve> beamLines, double beamHeight,double beamWidth,bool type,ref List<Brep> beams)
  {
    double flag = 1;
    if (type == false)
      flag = -1;

    
    for (int i = 0; i < beamLines.Count; i++)
    {
      Curve curve1 = beamLines[i].Offset(Plane.WorldXY, beamWidth / 2, 0.1, CurveOffsetCornerStyle.Sharp)[0];
      Curve curve2 = beamLines[i].Offset(Plane.WorldXY, -beamWidth / 2, 0.1, CurveOffsetCornerStyle.Sharp)[0];
      Curve curve3 = new Line(curve1.PointAtStart, curve2.PointAtStart).ToNurbsCurve();
      Curve curve4 = new Line(curve1.PointAtEnd, curve2.PointAtEnd).ToNurbsCurve();

      Curve baseCurve = Curve.JoinCurves(new List<Curve> { curve1, curve2, curve3, curve4 }, 0.1)[0];
      Brep beam = Surface.CreateExtrusion(baseCurve, new Vector3d(0, 0, beamHeight * flag)).ToBrep().CapPlanarHoles(0.1);

      beams.Add(beam);
    }
  }
  #endregion
}