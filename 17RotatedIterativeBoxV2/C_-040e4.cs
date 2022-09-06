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
public abstract class Script_Instance_040e4 : GH_ScriptInstance
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
  private void RunScript(Point3d centerPt, double length, double radiusOfPipe, double angle, int count, ref object outCrvs, ref object outPipes)
  {
    if (length<=0 || radiusOfPipe <= 0 || count <= 0)
    {
      Print("Invalid radius value");
      return;
    }
    angle = RhinoMath.ToRadians(angle);

    Plane basePlane = new Plane(centerPt, Vector3d.ZAxis);
    Interval intervalLength = new Interval(-length / 2, length / 2);
    Box originBox = new Box(basePlane, intervalLength, intervalLength, intervalLength);

    Curve[] wireframes = originBox.ToBrep().GetWireframe(0);
    List<Curve> wireframeCrvsList = new List<Curve>();
    wireframeCrvsList.AddRange(wireframes);

    //创建旋转的transform
    ITransform transform1 = new Rotation(centerPt, Vector3d.XAxis, angle);
    ITransform transform2 = new Rotation(centerPt, -1 * Vector3d.YAxis, angle);
    ITransform transform3 = new Rotation(centerPt, Vector3d.ZAxis, angle);
    List<GH_Transform> resourceForms = new List<GH_Transform>();
    resourceForms.Add(new GH_Transform(transform1));
    resourceForms.Add(new GH_Transform(transform2));
    resourceForms.Add(new GH_Transform(transform3));
    //合并旋转的transform
    GH_Transform compoundTransform = new GH_Transform();
    foreach (GH_Transform resourceForm in resourceForms)
    {
      foreach (ITransform transform in resourceForm.CompoundTransforms)
      {
        compoundTransform.CompoundTransforms.Add(transform.Duplicate());
      }
    }
    compoundTransform.ClearCaches();

    //通过旋转的transform创建旋转一定角度的盒子(旋转盒)
    GH_Brep rotatedBrep = new GH_Brep(originBox.ToBrep());
    rotatedBrep.Transform(compoundTransform.Value);
    
    GH_Box innerGhBox = new GH_Box(originBox);
    //获得旋转盒的包围盒(以世界坐标为基准)
    GH_Box outterGhBox = new GH_Box(GH_Brep.BrepTightBoundingBox(rotatedBrep.Value));
    //通过内外两个包围盒获得缩放值
    double scaleFactor = Math.Abs((innerGhBox.Value.X.Max - innerGhBox.Value.X.Min) / (outterGhBox.Value.X.Max - outterGhBox.Value.X.Min));

    //创建缩放的transform
    ITransform transform4 = new Scale(centerPt, scaleFactor);
    //合并缩放的transform
    compoundTransform.CompoundTransforms.Add(transform4.Duplicate());
    compoundTransform.ClearCaches();

    //调用递归方法创建基准线
    DataTree<Curve> iterativeCrvs= new DataTree<Curve>();
    CreateIterativeCrvs(iterativeCrvs, wireframeCrvsList, compoundTransform, count);

    DataTree<Brep> iterativePipes = CreateIterativePipes(iterativeCrvs, radiusOfPipe,scaleFactor);

    outCrvs = iterativeCrvs;
    outPipes = iterativePipes;
  }
  #endregion
  #region Additional
  public void CreateIterativeCrvs(DataTree<Curve> iterativeCrvsTree,List<Curve> wireframeCrvsList, GH_Transform compoundTransform, int count)
  {
    //基线条件
    if (count == 1)
    {
      List<Curve> iterativeCrvsList = new List<Curve>();
      for (int i = 0; i < wireframeCrvsList.Count; i++)
      {
        Curve crv = wireframeCrvsList[i].DuplicateCurve();
        iterativeCrvsList.Add(crv);
      }
      iterativeCrvsTree.AddRange(iterativeCrvsList, new GH_Path(count-1));
    }
    else
    {
      List<Curve> iterativeCrvs = new List<Curve>();
      for (int i = 0; i < wireframeCrvsList.Count; i++)
      {
        Curve crv = wireframeCrvsList[i].DuplicateCurve();
        iterativeCrvs.Add(crv);
      }
      for (int i = 0; i < iterativeCrvs.Count; i++)
      {
        Curve crv = iterativeCrvs[i].DuplicateCurve();
        iterativeCrvsTree.Add(crv, new GH_Path(count - 1));
      }

      for (int i = 0; i < iterativeCrvs.Count; i++)
      {
        iterativeCrvs[i].Transform(compoundTransform.Value);
      }
      
      CreateIterativeCrvs(iterativeCrvsTree,iterativeCrvs,compoundTransform,count-1);
    }
  }

  public DataTree<Brep> CreateIterativePipes(DataTree<Curve> iterativeCrvs,double radiusOfPipe,double scaleFactor)
  {
    DataTree<Brep> iterativePipes = new DataTree<Brep>();
    for (int i = 0; i < iterativeCrvs.BranchCount; i++)
    {
      for (int j = 0; j < iterativeCrvs.Branch(i).Count; j++)
      {
        int countOfScale = iterativeCrvs.BranchCount - i - 1;
        double actualScaleFactor = 1.0;
        for (int k = 0; k < countOfScale; k++)
        {
          actualScaleFactor = actualScaleFactor * scaleFactor;
        }
        Brep pipe = Brep.CreatePipe(iterativeCrvs.Branch(i)[j], radiusOfPipe*actualScaleFactor, true, PipeCapMode.Round, true, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance, RhinoDoc.ActiveDoc.ModelAngleToleranceRadians)[0];
        iterativePipes.Add(pipe, new GH_Path(i));
      }
    }
    return iterativePipes;
  }
  #endregion
}