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
public abstract class Script_Instance_72c8d : GH_ScriptInstance
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
  private void RunScript(DataTree<Curve> crvsA, DataTree<Curve> crvsB, ref object shortCrvs, ref object longCrvs)
  {
    DataTree<double> maxLengthOfCrvsA = calculateMaxLenth(crvsA);
    DataTree<double> maxLengthOfCrvsB = calculateMaxLenth(crvsB);

    DataTree<Curve> longCrvsTree = new DataTree<Curve>();
    DataTree<Curve> shortCrvsTree = new DataTree<Curve>();

    for (int i = 0; i < maxLengthOfCrvsA.BranchCount; i++)
    {
      if (maxLengthOfCrvsA.Branch(i)[0] > maxLengthOfCrvsB.Branch(i)[0])
      {
        longCrvsTree.AddRange(crvsA.Branch(i), new GH_Path(i));
        shortCrvsTree.AddRange(crvsB.Branch(i), new GH_Path(i));
      }
      else
      {
        longCrvsTree.AddRange(crvsB.Branch(i), new GH_Path(i));
        shortCrvsTree.AddRange(crvsA.Branch(i), new GH_Path(i));
      }
    }

    shortCrvs = shortCrvsTree;
    longCrvs = longCrvsTree;

  }
  #endregion
  #region Additional

  public DataTree<double> calculateMaxLenth(DataTree<Curve> crvs)
  {
    DataTree<double> maxLengthOfCrvs = new DataTree<double>();
    for (int i = 0; i < crvs.BranchCount; i++)
    {
      double maxLength = 0;
      for (int j = 0; j < crvs.Branch(i).Count; j++)
      {
        double currentLength = crvs.Branch(i)[j].GetLength();
        if (currentLength > maxLength)
        {
          maxLength = currentLength;
        }
      }
      maxLengthOfCrvs.Add(maxLength, new GH_Path(i));
    }
    return maxLengthOfCrvs;
  }
  #endregion
}