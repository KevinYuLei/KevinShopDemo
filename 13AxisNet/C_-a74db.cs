using System;
using System.Collections;
using System.Collections.Generic;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

using Rhino.Geometry.Intersect;


/// <summary>
/// This class will be instantiated on demand by the Script component.
/// </summary>
public abstract class Script_Instance_a74db : GH_ScriptInstance
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
  private void RunScript(Point3d datumPt, List<double> sizeX, List<double> sizeY, double extensionLength, bool createColumns, double heightOfColumn, double radiusOfColumn, bool isCircle, ref object outVerticalLines, ref object outHorizontalLines, ref object outPts, ref object outColumns)
  {
    List<Curve> verticalLines = new List<Curve>();
    List<Curve> horizontalLines = new List<Curve>();
    DataTree<Point3d> centerPts = new DataTree<Point3d>();
    DataTree<Brep> columns = new DataTree<Brep>();

    double lengthOfVLine = extensionLength * 2;
    double lengthOfHLine = extensionLength * 2;
    for (int i = 0; i < sizeX.Count; i++)
    {
      lengthOfHLine += sizeX[i];
    }
    for (int i = 0; i < sizeY.Count; i++)
    {
      lengthOfVLine += sizeY[i];
    }

    CreateLines(datumPt, sizeX, 0, extensionLength, lengthOfVLine, ref verticalLines);
    CreateLines(datumPt, sizeY, 1, extensionLength, lengthOfHLine, ref horizontalLines);

    if(createColumns==true)
    {
      CreateColumns(verticalLines, horizontalLines, heightOfColumn, radiusOfColumn, isCircle, ref centerPts, ref columns);
    }

    outVerticalLines = verticalLines;
    outHorizontalLines = horizontalLines;
    outPts = centerPts;
    outColumns = columns;
  }
  #endregion
  #region Additional

  //direction==0,create verticle lines along X axis
  //direction==1,create horizontal lines along Y axis
  public void CreateLines(Point3d datumPt,List<double> sizeList,int direction,double extensionLength,double lengthOfLine,ref List<Curve> resultLines)
  {
    for (int i = 0; i <= sizeList.Count; i++)
    {
      Point3d startPt = new Point3d(datumPt);
      double delta = extensionLength;
      int flagX = 1;
      int flagY = 0;
      for (int j = 0; j < i; j++)
      {
        delta += sizeList[j];
      }
      if(direction==1)
      {
        flagX = 0;
        flagY = 1;
      }
      startPt.Transform(Transform.Translation(flagX * delta, flagY * delta, 0));
      Point3d endPt = new Point3d(startPt);
      endPt.Transform(Transform.Translation(flagY * lengthOfLine, flagX * lengthOfLine, 0));
      Curve resultLine = new Line(startPt, endPt).ToNurbsCurve();
      resultLines.Add(resultLine);
    }
  }

  public void CreateColumns(List<Curve> verticalLines,List<Curve> horizontalLines,double heightOfColumn,double radiusOfColumn,bool isCircle,ref DataTree<Point3d> centerPts,ref DataTree<Brep> columns)
  {
    for (int i = 0; i < verticalLines.Count; i++)
    {
      for (int j = 0; j < horizontalLines.Count; j++)
      {
        CurveIntersections intersections = Intersection.CurveCurve(verticalLines[i], horizontalLines[j], 0.1, 0.1);
        for (int k = 0; k < intersections.Count; k++)
        {
          Point3d centerPt = intersections[k].PointA;
          centerPts.Add(centerPt, new GH_Path(i));

          Plane plane = new Plane(centerPt, Vector3d.XAxis, Vector3d.YAxis);
          Curve baseCurve;
          if (isCircle)
          {
            baseCurve = new Circle(centerPt, radiusOfColumn).ToNurbsCurve();
          }
          else
          {
            Interval edge = new Interval(-radiusOfColumn, radiusOfColumn);
            baseCurve = new Rectangle3d(plane, edge, edge).ToNurbsCurve();
          }
          Brep column = Surface.CreateExtrusion(baseCurve, new Vector3d(0, 0, heightOfColumn)).ToBrep().CapPlanarHoles(0.1);
          columns.Add(column, new GH_Path(i));
        }
      }
    }
  }
  #endregion
}