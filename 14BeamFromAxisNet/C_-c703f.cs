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
  private void RunScript(List<Curve> verticalLines, List<Curve> horizontalLines, double extensionLength, double heightOfColumn, double beamHeight, double beamWidth, bool type, ref object A, ref object B, ref object C, ref object D, ref object E, ref object F)
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

    A = verticalLines;
    B = horizontalLines;
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

  }
  #endregion
}