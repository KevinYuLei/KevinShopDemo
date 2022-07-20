using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Collections;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_0bdaa : GH_ScriptInstance
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
  private void RunScript(Point3d datumPt, double radius, double height, double slackness, int fittingDegree, int countOfUnit, double radiusOfPipe, bool isCircle, ref object A, ref object B, ref object C, ref object D, ref object E, ref object F, ref object AA, ref object AB, ref object AC, ref object AD, ref object AE, ref object AF)
  {
    double extensionLength = 100;
    double shortSideLength = (radius + extensionLength) * 2;
    double longSideLength = shortSideLength * Math.Tan(Math.PI / 6) * 2;
    double radiusOfHexagon = (longSideLength / countOfUnit) / 2;

    //�����ײ���׼����
    Rectangle3d bottomRectangle = new Rectangle3d(new Plane(datumPt, Vector3d.XAxis, Vector3d.YAxis), new Interval(-longSideLength / 2, longSideLength / 2), new Interval(-shortSideLength / 2, shortSideLength / 2));
    //������������
    Rectangle3d topRectangle = bottomRectangle;
    topRectangle.Transform(Transform.Translation(0, 0, height));

    //��������������������
    Curve hexagon = CreatePolygon(datumPt,radiusOfHexagon, 6);

    //�������������
    List<Curve> curvesToLoft = new List<Curve>();
    for (int i = 0; i <=fittingDegree; i++)
    {
      double scaleFactor = ((radiusOfHexagon * Math.Sqrt(6) / 2) / radius) * (1+i*slackness);
      double moveHeight = height / fittingDegree;
      Curve curveToLoft = new Circle(datumPt, radius*scaleFactor).ToNurbsCurve();
      curveToLoft.Transform(Transform.Translation(0, 0, i * moveHeight));
      curvesToLoft.Add(curveToLoft);
    }
    //���������������һ��δ���ŵķ���Բ��
    Curve lastCurve = new Circle(datumPt, radius).ToNurbsCurve();
    lastCurve.Transform(Transform.Translation(0, 0, height));
    curvesToLoft.Add(lastCurve);

    //�����������岿��1
    Brep loftBrepPart1 = Brep.CreateFromLoft(curvesToLoft, Point3d.Unset, Point3d.Unset, LoftType.Loose, false)[0];

    //�����������岿��2
    CurveList curveList = new CurveList();
    curveList.Add(topRectangle.ToNurbsCurve());
    curveList.Add(lastCurve);
    Brep loftBrepPart2 = Brep.CreatePlanarBreps(curveList, 0.1)[0];

    Brep loftBrep = Brep.JoinBreps(new List<Brep> { loftBrepPart1, loftBrepPart2 }, 0.1)[0];

    List<Curve> segmentsOfHexagon = new List<Curve>();
    segmentsOfHexagon.AddRange(((PolyCurve)hexagon).Explode());
    double ContourDistance = radiusOfHexagon * Math.Sin(Math.PI / 3) * 2;
    


    A = bottomRectangle;
    B = hexagon;
    C = loftBrep;
    D = segmentsOfHexagon;
  }
  #endregion
  #region Additional
  public Curve CreatePolygon(Point3d datumPt,double span,int sides)
  {
    PolyCurve polygon = new PolyCurve();
    double anglePerSide = Math.PI * 2 / sides;
    for (int i = 0; i < sides; i++)
    {
      Point3d startPt = new Point3d(span * Math.Cos(anglePerSide * i), span * Math.Sin(anglePerSide * i), 0);
      startPt += datumPt;
      Point3d endPt = new Point3d(span * Math.Cos(anglePerSide * (i + 1)), span * Math.Sin(anglePerSide * (i + 1)), 0);
      endPt += datumPt;
      polygon.Append(new Line(startPt, endPt));
    }
    return polygon;
  }
  #endregion
}