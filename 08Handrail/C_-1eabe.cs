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
public abstract class Script_Instance_1eabe : GH_ScriptInstance
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
  private void RunScript(Curve planarCurve, int countGrade1, int countGrade2, double height, double diameterGrade1, double diameterGrade2, bool isCircle, ref object outHorizontalBreps, ref object outMajorRods, ref object outMinorRods)
  {
    
    if (!planarCurve.IsPlanar())
    {
      Print("Not a planar curve");
      return;
    }
    else
      Print("A planar curve");

    if (!planarCurve.IsClosed)
    {
      double t0Trimed;
      planarCurve.LengthParameter(diameterGrade1 / 2, out t0Trimed);
      planarCurve = planarCurve.Trim(new Interval(t0Trimed, planarCurve.Domain.Max));

      planarCurve.LengthParameter(planarCurve.GetLength() - diameterGrade1 / 2, out t0Trimed);
      planarCurve = planarCurve.Trim(new Interval(planarCurve.Domain.Min, t0Trimed));
      Print("Not a closed curve");
    }
    else
      Print("A closed curve");

    diameterGrade2 = Math.Min(diameterGrade1, diameterGrade2);

    //Create major pts
    Point3d[] majorPts;
    double[] tsOfMajorPts;

    tsOfMajorPts = planarCurve.DivideByCount(countGrade1, true, out majorPts);
    List<Point3d> majorRodPtsList = new List<Point3d>(majorPts);

    //Create minor pts
    Curve[] grade1Curves;
    DataTree<Point3d> minorPtsTree = new DataTree<Point3d>();

    grade1Curves = planarCurve.Split(tsOfMajorPts);
    for (int i = 0; i < grade1Curves.Length; i++)
    {
      Point3d[] minorPts;
      grade1Curves[i].DivideByCount(countGrade2, false, out minorPts);
      minorPtsTree.AddRange(minorPts, new GH_Path(i));
    }

    //Create horizontal breps
    double heightOfBrep = 30;
    double intervalOfTop = heightOfBrep * 2 + 50;
    double intervalOfBottom = 50;
    
    Brep bottomBrep = CreateHorizontalBrep(planarCurve, diameterGrade1, diameterGrade2,heightOfBrep);
    Brep midBrep = bottomBrep.DuplicateBrep();
    Brep topBrep = bottomBrep.DuplicateBrep();
    bottomBrep.Transform(Transform.Translation(0, 0, intervalOfBottom));
    midBrep.Transform(Transform.Translation(0, 0, height - intervalOfTop));
    topBrep.Transform(Transform.Translation(0, 0, height - heightOfBrep));

    List<Brep> horizontalBreps = new List<Brep>();
    horizontalBreps.Add(bottomBrep);
    horizontalBreps.Add(midBrep);
    horizontalBreps.Add(topBrep);

    //Create major rods
    List<Brep> majorRods = CreateMajorRods(planarCurve, majorRodPtsList, height, diameterGrade1, isCircle,heightOfBrep,intervalOfTop);
    //Create minor rods
    DataTree<Brep> minorRods = CreateMinorRods(planarCurve, minorPtsTree, height, diameterGrade2, isCircle,heightOfBrep,intervalOfTop,intervalOfBottom);

    outHorizontalBreps = horizontalBreps;
    outMajorRods = majorRods;
    outMinorRods = minorRods;
  }
  #endregion
  #region Additional
  public Brep CreateHorizontalBrep(Curve planarCurve, double diameterGrade1, double diameterGrade2, double heightOfBrep)
  {
    Brep horizontalBrep = new Brep();
    double widthOfBrep = diameterGrade2 + (diameterGrade1 - diameterGrade2) / 2;
    //double heightOfBrep = 20;

    Plane offsetPlane;
    planarCurve.TryGetPlane(out offsetPlane);
    Curve offsetCrv1 = planarCurve.Offset(offsetPlane, widthOfBrep / 2, 0.1, CurveOffsetCornerStyle.Smooth)[0];
    Curve offsetCrv2 = planarCurve.Offset(offsetPlane, -1 * widthOfBrep / 2, 0.1, CurveOffsetCornerStyle.Smooth)[0];

    
    if (!planarCurve.IsClosed)
    {
      List<Curve> crvsToJoin = new List<Curve>();
      crvsToJoin.Add(offsetCrv1);
      Curve line1 = new Line(offsetCrv1.PointAtStart, offsetCrv2.PointAtStart).ToNurbsCurve();
      crvsToJoin.Add(line1);
      crvsToJoin.Add(offsetCrv2);
      Curve line2 = new Line(offsetCrv1.PointAtEnd, offsetCrv2.PointAtEnd).ToNurbsCurve();
      crvsToJoin.Add(line2);

      Curve outline = Curve.JoinCurves(crvsToJoin, 0.1)[0];
      horizontalBrep = Surface.CreateExtrusion(outline, Vector3d.ZAxis * heightOfBrep).ToBrep().CapPlanarHoles(0.1);
    }
    else
    {
      List<Curve> crvsToLoft = new List<Curve>();
      crvsToLoft.Add(offsetCrv1);
      crvsToLoft.Add(offsetCrv2);

      List<Brep> brepsToJoin = new List<Brep>();
      Brep bottomFace = Brep.CreateFromLoft(crvsToLoft, Point3d.Unset, Point3d.Unset, LoftType.Normal, false)[0];
      Brep topFace = bottomFace.DuplicateBrep();
      topFace.Transform(Transform.Translation(0, 0, heightOfBrep));
      Brep outterFace = Surface.CreateExtrusion(offsetCrv1, Vector3d.ZAxis * heightOfBrep).ToBrep();
      Brep innterFace = Surface.CreateExtrusion(offsetCrv2, Vector3d.ZAxis * heightOfBrep).ToBrep();

      brepsToJoin.Add(bottomFace);
      brepsToJoin.Add(outterFace);
      brepsToJoin.Add(innterFace);
      brepsToJoin.Add(topFace);
      horizontalBrep = Brep.JoinBreps(brepsToJoin, 0.1)[0];
    }


    return horizontalBrep;
  }

  public List<Brep> CreateMajorRods(Curve planarCurve, List<Point3d> majorRodPtsList, double height, double diameterGrade1, bool isCircle, double heightOfBrep, double intervalOfTop)
  {
    List<Brep> majorRods = new List<Brep>();
    for (int i = 0; i < majorRodPtsList.Count; i++)
    {
      if (isCircle == true)
      {
        double heightOfRod = height - intervalOfTop;

        Curve baseCurve = new Circle(majorRodPtsList[i], diameterGrade1 / 2).ToNurbsCurve();
        Brep majorRodPart1 = Surface.CreateExtrusion(baseCurve, Vector3d.ZAxis * heightOfRod).ToBrep().CapPlanarHoles(0.1);

        Point3d pt2 = new Point3d(majorRodPtsList[i]);
        pt2.Transform(Transform.Translation(0, 0, heightOfRod));
        Curve baseCurve2 = new Circle(pt2, diameterGrade1 / 4).ToNurbsCurve();
        Brep majorRodPart2 = Surface.CreateExtrusion(baseCurve2, Vector3d.ZAxis * (intervalOfTop-heightOfBrep)).ToBrep().CapPlanarHoles(0.1);

        List<Brep> brepsToMerge = new List<Brep>();
        brepsToMerge.Add(majorRodPart1);
        brepsToMerge.Add(majorRodPart2);
        Brep majorRod = Brep.MergeBreps(brepsToMerge, 0.01);

        majorRods.Add(majorRod);
      }
      else
      {
        double heightOfRod = height - intervalOfTop;
        double t;

        planarCurve.ClosestPoint(majorRodPtsList[i], out t);
        Vector3d xVec = planarCurve.TangentAt(t);
        Vector3d yVec = Vector3d.CrossProduct(xVec, Vector3d.ZAxis);
        Plane curvePlane = new Plane(majorRodPtsList[i], xVec, yVec);

        Curve baseCurve = new Rectangle3d(curvePlane, new Interval(-diameterGrade1 / 2, diameterGrade1 / 2), new Interval(-diameterGrade1 / 2, diameterGrade1 / 2)).ToNurbsCurve();
        Brep majorRodPart1 = Surface.CreateExtrusion(baseCurve, Vector3d.ZAxis * heightOfRod).ToBrep().CapPlanarHoles(0.1);

        Plane curvePlane2 = new Plane(curvePlane);
        curvePlane2.Transform(Transform.Translation(0, 0, heightOfRod));
        Curve baseCurve2= new Rectangle3d(curvePlane2, new Interval(-diameterGrade1 / 4, diameterGrade1 / 4), new Interval(-diameterGrade1 / 4, diameterGrade1 / 4)).ToNurbsCurve();
        Brep majorRodPart2 = Surface.CreateExtrusion(baseCurve2, Vector3d.ZAxis * (intervalOfTop - heightOfBrep)).ToBrep().CapPlanarHoles(0.1);

        List<Brep> brepsToMerge = new List<Brep>();
        brepsToMerge.Add(majorRodPart1);
        brepsToMerge.Add(majorRodPart2);
        Brep majorRod = Brep.MergeBreps(brepsToMerge, 0.01);

        majorRods.Add(majorRod);
      }
    }
    return majorRods;
  }

  public DataTree<Brep> CreateMinorRods(Curve planarCurve, DataTree<Point3d> minorPtsTree, double height, double diameterGrade2, bool isCircle,double heightOfBrep,double intervalOfTop,double intervalOfBottom)
  {
    DataTree<Brep> minorRods = new DataTree<Brep>();
    for (int i = 0; i < minorPtsTree.BranchCount; i++)
    {
      for (int j = 0; j < minorPtsTree.Branch(i).Count; j++)
      {
        if (isCircle == true)
        {
          double heightOfRod = height - intervalOfTop -(heightOfBrep+intervalOfBottom);
          Point3d pt = new Point3d(minorPtsTree.Branches[i][j]);
          pt.Transform(Transform.Translation(0, 0, (heightOfBrep + intervalOfBottom)));
          Curve baseCurve = new Circle(pt, diameterGrade2 / 2).ToNurbsCurve();
          Brep minorRod = Surface.CreateExtrusion(baseCurve, Vector3d.ZAxis * heightOfRod).ToBrep().CapPlanarHoles(0.1);
          minorRods.Add(minorRod, new GH_Path(i));
        }
        else
        {
          double heightOfRod = height - intervalOfTop -(heightOfBrep + intervalOfBottom);
          double t;
          planarCurve.ClosestPoint(minorPtsTree.Branches[i][j], out t);
          Vector3d xVec = planarCurve.TangentAt(t);
          Vector3d yVec = Vector3d.CrossProduct(xVec, Vector3d.ZAxis);
          Point3d pt = new Point3d(minorPtsTree.Branches[i][j]);
          pt.Transform(Transform.Translation(0, 0, (heightOfBrep + intervalOfBottom)));
          Plane curvePlane = new Plane(pt, xVec, yVec);
          Curve baseCurve = new Rectangle3d(curvePlane, new Interval(-diameterGrade2 / 2, diameterGrade2 / 2), new Interval(-diameterGrade2 / 2, diameterGrade2 / 2)).ToNurbsCurve();
          Brep minorRod = Surface.CreateExtrusion(baseCurve, Vector3d.ZAxis * heightOfRod).ToBrep().CapPlanarHoles(0.1);
          minorRods.Add(minorRod, new GH_Path(i));
        }
      }
    }
    return minorRods;
  }
  #endregion
}