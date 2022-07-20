using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Grasshopper.Kernel.Types.Transforms;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_7a0de : GH_ScriptInstance
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
  private void RunScript(Point3d centerPt, double length, double radiusOfPipe, double angle, int count, ref object outFrames)
  {

    angle = RhinoMath.ToRadians(angle);

    Plane basePlane = new Plane(centerPt, Vector3d.ZAxis);
    Interval intervalLength = new Interval(-length / 2, length / 2);
    Box originBox = new Box(basePlane, intervalLength, intervalLength, intervalLength);
    Curve[] wireframes=originBox.ToBrep().GetWireframe(-1);

    if(radiusOfPipe<=0)
    {
      Print("Invalid radius value");
      return;
    }

    List<Brep> pipeList = new List<Brep>();
    for (int i = 0; i < wireframes.Length; i++)
    {
      Brep[] pipe =Brep.CreatePipe(wireframes[i], radiusOfPipe, true, PipeCapMode.Round, true, 0.1, 0.1);
      pipeList.AddRange(pipe);
    }
    GH_Brep pipeFrame = new GH_Brep(Brep.CreateBooleanUnion(pipeList, 0.1)[0]);

    ITransform transform1 = new Rotation(centerPt, Vector3d.XAxis, angle);
    ITransform transform2 = new Rotation(centerPt, -1*Vector3d.YAxis, angle);
    ITransform transform3 = new Rotation(centerPt, Vector3d.ZAxis, angle);
    List<GH_Transform> resourceForms = new List<GH_Transform>();
    resourceForms.Add(new GH_Transform(transform1));
    resourceForms.Add(new GH_Transform(transform2));
    resourceForms.Add(new GH_Transform(transform3));

    GH_Transform compoundTransform = new GH_Transform();
    foreach (GH_Transform resourceForm in resourceForms)
    {
      foreach (ITransform transform in resourceForm.CompoundTransforms)
      {
        compoundTransform.CompoundTransforms.Add(transform.Duplicate());
      }
    }
    compoundTransform.ClearCaches();

    GH_Brep gh_Brep = new GH_Brep(originBox.ToBrep());
    gh_Brep.Transform(compoundTransform.Value);
    Transform xformForBoundingBox = Transform.ChangeBasis(Plane.WorldXY, Plane.WorldXY);
    GH_Box innerGhBox = new GH_Box(originBox);
    GH_Box outterGhBox = new GH_Box(GH_Brep.BrepTightBoundingBox(gh_Brep.Value));
    double scaleFactor = Math.Abs((innerGhBox.Value.X.Max-innerGhBox.Value.X.Min) / (outterGhBox.Value.X.Max-outterGhBox.Value.X.Min));

    ITransform transform4 = new Scale(centerPt, scaleFactor);
    compoundTransform.CompoundTransforms.Add(transform4.Duplicate());
    compoundTransform.ClearCaches();

    List<GH_Transform> list = new List<GH_Transform>();
    list.Add(compoundTransform);
    List<int> repeatCounts = new List<int>();
    for (int i = 0; i < count; i++)
    {
      repeatCounts.Add(i+1);
    }
    DataTree<GH_Transform> compoundTransformTree = new DataTree<GH_Transform>();
    int num2 = -1;
    for (int i = 0; i < repeatCounts.Count; i++)
    {
      for (int j = 0; j < repeatCounts[i]; j++)
      {
        num2++;
        if (num2 == list.Count)
        {
          num2 = 0;
        }
        compoundTransformTree.Add(list[num2],new GH_Path(i));
      }
    }

    List<GH_Transform> resultCompoundTransformList = new List<GH_Transform>();
    for (int i = 0; i < compoundTransformTree.BranchCount; i++)
    {
      GH_Transform resultCompoundTransform = new GH_Transform();
      foreach(GH_Transform gh_trans in compoundTransformTree.Branch(i))
      {
        foreach(ITransform itrans in gh_trans.CompoundTransforms)
        {
          resultCompoundTransform.CompoundTransforms.Add(itrans);
        }
      }
      resultCompoundTransform.ClearCaches();
      resultCompoundTransformList.Add(resultCompoundTransform);
    }

    List<GH_Brep> resultBrepFrames = new List<GH_Brep>();
    resultBrepFrames.Add(pipeFrame);
    for (int i = 0; i < resultCompoundTransformList.Count; i++)
    {
      GH_Brep resultBrepFrame = pipeFrame.DuplicateBrep();
      resultBrepFrame.Transform(resultCompoundTransformList[i].Value);
      resultBrepFrames.Add(resultBrepFrame);
    }


    outFrames = resultBrepFrames;

 
  }
  #endregion
  #region Additional

  #endregion
}