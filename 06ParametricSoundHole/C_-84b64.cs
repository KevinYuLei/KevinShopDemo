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
public abstract class Script_Instance_84b64 : GH_ScriptInstance
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
  private void RunScript(Point3d pt, double innerRadius, double distance, int unitCount, int polarArraryCount, double maxRotateAngle, ref object result)
  {
    //创建系列点的X坐标差值
    List<double> deltaXs = new List<double>();
    for (int i = 0; i < unitCount; i++)
    {
      double deltaX = innerRadius + i * distance;
      deltaXs.Add(deltaX);
    }

    //创建单元体直线系列点
    List<Point3d> unitPoints = new List<Point3d>();
    for (int i = 0; i < unitCount; i++)
    {
      Point3d linearPoint = new Point3d(pt);
      linearPoint.X = linearPoint.X + deltaXs[i];
      unitPoints.Add(linearPoint);
    }

    //创建系列点旋转角度
    List<double> rotateAngles = new List<double>();
    //转换为弧度制，并求出各个点之间旋转角度的公差
    double stepAngle = (maxRotateAngle / 180) * Math.PI / (unitCount - 1);
    for (int i = 0; i < unitCount; i++)
    {
      double rotateAngle = i * stepAngle;
      rotateAngles.Add(rotateAngle);
    }

    //旋转单元体直线系列点
    for (int i = 0; i < unitCount; i++)
    {
      Point3d rotatePt = unitPoints[i];
      rotatePt.Transform(Transform.Rotation(rotateAngles[i],pt));
      unitPoints[i] = rotatePt;
    }

    //创建系列点半径
    List<double> radii = new List<double>();
    for (int i = 0; i < unitCount; i++)
    {
      double maxRadius = distance / 1.5;
      double minRadius = maxRadius / 4;
      double radius = ((maxRadius - minRadius) / (deltaXs[deltaXs.Count - 1] - deltaXs[0])) * (deltaXs[i]-deltaXs[0]) + minRadius;
      radii.Add(radius);
    }

    //创建系列圆
    List<Circle> unitCircles = new List<Circle>();
    for (int i = 0; i < unitCount; i++)
    {
      Circle circle = new Circle(unitPoints[i], radii[i]);
      unitCircles.Add(circle);
    }

    DataTree<Circle> resultCircles = PolarArray(unitCircles, pt, polarArraryCount, 2 * Math.PI);

    result = resultCircles;
  }
  #endregion
  #region Additional
    public DataTree<Circle> PolarArray(List<Circle> circles, Point3d centerPt,int number, double sweepAngle )
  {
    DataTree<Circle> arrayedGeometry = new DataTree<Circle>();
    double rotateAngle = sweepAngle / (double)number;
    for (int i = 0; i < number; i++)
    {
      for (int j = 0; j < circles.Count; j++)
      {
        Circle circleCopy = circles[j];
        circleCopy.Transform(Transform.Rotation(rotateAngle * i, centerPt));
        arrayedGeometry.Add(circleCopy, new GH_Path(i));
    }
    }

    return arrayedGeometry;
  }
  #endregion
}