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
public abstract class Script_Instance_8c6cb : GH_ScriptInstance
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
  private void RunScript(DataTree<Curve> crvs, ref object lineCrvs, ref object noLineCrvs)
  {
    DataTree<Curve> lineCrvsTree = new DataTree<Curve>();
    DataTree<Curve> noLineCrvsTree = new DataTree<Curve>();
    for (int i = 0; i < crvs.BranchCount; i++)
    {
      for (int j = 0; j < crvs.Branch(i).Count; j++)
      {
        Curve crv=crvs.Branch(i)[j];
        Vector3d v1 = crv.TangentAtStart;
        Vector3d v2 = crv.TangentAtEnd;
        v1.Unitize();
        v2.Unitize();

        Vector3d v2Reverse = v2;
        v2Reverse.Reverse();
        if (v1==v2 || v1==v2Reverse)
        {
          lineCrvsTree.Add(crvs.Branch(i)[j], new GH_Path(i));
        }
        else
        {
          noLineCrvsTree.Add(crvs.Branch(i)[j], new GH_Path(i));
        }
      }
    }

    lineCrvs = lineCrvsTree;
    noLineCrvs=noLineCrvsTree;
  }
  #endregion
  #region Additional

  #endregion
}