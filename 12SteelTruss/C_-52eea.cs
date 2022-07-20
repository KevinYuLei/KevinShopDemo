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
public abstract class Script_Instance_52eea : GH_ScriptInstance
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
  private void RunScript(Surface srf, int UCount, int VCount, double offsetDistance, double uvRodRadiusOfTop, double uvRodRadiusOfBottom, double obliqueRodRadius, double verticleRodRadius, bool isGrid, bool reverse, ref object outURodsTop, ref object outVRodsTop, ref object outObliqueRods, ref object outURodsBottom, ref object outVRodsBottom, ref object outVerticleRods)
  {
    //规范输入曲面
    GH_Surface gh_Surface = new GH_Surface(srf);
    //曲面总区间
    UVInterval uvInterval = new UVInterval(srf.Domain(0), srf.Domain(1));


    //分割使用的二维区间
    DataTree<UVInterval> isoUVTree = new DataTree<UVInterval>();
    Divide2DInterval(UCount, VCount, uvInterval, ref isoUVTree);
    //分割后的曲面
    DataTree<Surface> isoSurfacesTree = new DataTree<Surface>();
    CreateIsoSurfaces(gh_Surface, UCount, VCount, isoUVTree, ref isoSurfacesTree);
    //曲面U方向线
    DataTree<Curve> uCurvesTree = new DataTree<Curve>();
    DataTree<Brep> uRodsTree = new DataTree<Brep>();
    GetUCurves(isoSurfacesTree, UCount, VCount, isGrid, ref uCurvesTree, uvRodRadiusOfTop, ref uRodsTree);
    //曲面V方向线
    DataTree<Curve> vCurvesTree = new DataTree<Curve>();
    DataTree<Brep> vRodsTree = new DataTree<Brep>();
    GetVCurves(isoSurfacesTree, UCount, VCount, isGrid, ref vCurvesTree, uvRodRadiusOfTop, ref vRodsTree);
    //曲面斜杆线
    DataTree<Curve> obliqueCurvesTree = new DataTree<Curve>();
    DataTree<Brep> obliqueRodsTree = new DataTree<Brep>();
    CreateObliqueCurves(isoSurfacesTree, UCount, VCount, ref obliqueCurvesTree, obliqueRodRadius, ref obliqueRodsTree);


    //偏移后的曲面（底面）
    Surface offsetSrf = gh_Surface.Face.DuplicateSurface();
    int flag = 1;
    if (reverse)
      flag = -1;
    offsetSrf = offsetSrf.Offset(flag * offsetDistance, RhinoDoc.ActiveDoc.ModelAbsoluteTolerance);
    //规范偏移后的曲面（底面）
    GH_Surface gh_OffsetSrf = new GH_Surface(offsetSrf);
    //偏移后曲面（底面）的总区间
    UVInterval uvIntervalOfOffsetSrf = new UVInterval(offsetSrf.Domain(0), offsetSrf.Domain(1));
    //分割使用的二维区间
    DataTree<UVInterval> isoUVTreeOfOffsetSrf = new DataTree<UVInterval>();
    Divide2DInterval(UCount, VCount, uvIntervalOfOffsetSrf, ref isoUVTreeOfOffsetSrf);
    //底面分割后的曲面
    DataTree<Surface> isoSurfacesTreeOfOffsetSrf = new DataTree<Surface>();
    CreateIsoSurfaces(gh_OffsetSrf, UCount, VCount, isoUVTreeOfOffsetSrf, ref isoSurfacesTreeOfOffsetSrf);
    //底面斜杆线
    DataTree<Curve> obliqueCurvesTreeOfOffsetSrf = new DataTree<Curve>();
    CreateObliqueCurves(isoSurfacesTreeOfOffsetSrf, UCount, VCount, ref obliqueCurvesTreeOfOffsetSrf);
    //底面斜杆线交点(理论上空间四边形的对角线并非一定有交点，改用求面中心点）
    DataTree<Point3d> intersectionPtsOfOffsetSrf = new DataTree<Point3d>();
    GetIntersectionPoints(isoSurfacesTreeOfOffsetSrf, UCount, VCount, ref intersectionPtsOfOffsetSrf);
    //底面经过斜杆交点的U方向线
    DataTree<Curve> uCurvesTreeThroughIntersectionPts = new DataTree<Curve>();
    DataTree<Brep> uRodsTreeThroughIntersectionPts = new DataTree<Brep>();
    CreateUCurvesThroughIntersectionPts(intersectionPtsOfOffsetSrf, UCount, VCount, ref uCurvesTreeThroughIntersectionPts, uvRodRadiusOfBottom, uRodsTreeThroughIntersectionPts);
    //底面经过斜杆交点的V方向线
    DataTree<Curve> vCurvesTreeThroughIntersectionPts = new DataTree<Curve>();
    DataTree<Brep> vRodsTreeThroughIntersectionPts = new DataTree<Brep>();
    CreateVCurvesThroughIntersectionPts(intersectionPtsOfOffsetSrf, UCount, VCount, ref vCurvesTreeThroughIntersectionPts, uvRodRadiusOfBottom, vRodsTreeThroughIntersectionPts);


    //垂直连接线
    DataTree<Curve> verticleLines = new DataTree<Curve>();
    DataTree<Brep> verticleRodsTree = new DataTree<Brep>();
    CreateVerticleLines(isoSurfacesTree, intersectionPtsOfOffsetSrf, UCount, VCount, ref verticleLines, verticleRodRadius, ref verticleRodsTree);

    outURodsTop = uRodsTree;
    outVRodsTop = vRodsTree;
    outObliqueRods = obliqueRodsTree;
    outURodsBottom = uRodsTreeThroughIntersectionPts;
    outVRodsBottom = vRodsTreeThroughIntersectionPts;
    outVerticleRods = verticleRodsTree;

  }
  #endregion
  #region Additional
  public void Limit(ref double v, double min, double max)
  {
    v = Math.Min(v, max);
    v = Math.Max(v, min);
  }
  
  public void Divide2DInterval(int UCount, int VCount, UVInterval uvInterval,ref DataTree<UVInterval> isoUVTree)
  {
    for (int i = 1; i <= UCount; i++)
    {
      double UInvervalPmin = (double)(i - 1) / (double)UCount;
      double UIntervalPmax = (double)i / (double)UCount;
      double UIntervalMin = uvInterval.U.ParameterAt(UInvervalPmin);
      double UIntervalMax = uvInterval.U.ParameterAt(UIntervalPmax);
      for (int j = 1; j <= VCount; j++)
      {
        double VIntervalPmin = (double)(j - 1) / (double)VCount;
        double VIntervalPmax = (double)j / (double)VCount;
        double VIntervalMin = uvInterval.V.ParameterAt(VIntervalPmin);
        double VIntervalMax = uvInterval.V.ParameterAt(VIntervalPmax);
        isoUVTree.Add(new UVInterval(new Interval(UIntervalMin, UIntervalMax), new Interval(VIntervalMin, VIntervalMax)), new GH_Path(i - 1));
      }
    }
  }

  public void CreateIsoSurfaces(GH_Surface gh_Surface, int UCount, int VCount, DataTree<UVInterval> isoUVTree, ref DataTree<Surface> isoSurfacesTree)
  {
    BrepFace face = gh_Surface.Face;
    double min = face.Domain(0).Min;
    double max = face.Domain(0).Max;
    double min2 = face.Domain(1).Min;
    double max2 = face.Domain(1).Max;

    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        double isoUMin = isoUVTree.Branch(i)[j].U.Min;
        double isoUMax = isoUVTree.Branch(i)[j].U.Max;
        double isoVMin = isoUVTree.Branch(i)[j].V.Min;
        double isoVMax = isoUVTree.Branch(i)[j].V.Max;
        Limit(ref isoUMin, min, max);
        Limit(ref isoUMax, min, max);
        Limit(ref isoVMin, min2, max2);
        Limit(ref isoVMax, min2, max2);

        double num = 1E-08;
        if (isoUMax - isoUMin < num)
        {
          Print("Subsurface is invalid because the trimming U-domain is singular.");
          return;
        }
        if (isoVMax - isoVMin < num)
        {
          Print("Subsurface is invalid because the trimming V-domain is singular.");
          return;
        }

        Surface isoSurface = gh_Surface.Face.Trim(new Interval(isoUMin, isoUMax), new Interval(isoVMin, isoVMax));
        isoSurfacesTree.Add(isoSurface, new GH_Path(i));
      }
    }
  }

  public void GetUCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, bool isGrid, ref DataTree<Curve> uCurvesTree)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        if(isGrid)
        {
          Point3d startPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3].StartVertex.Location;
          Point3d endPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3].EndVertex.Location;
          Curve uCurve = new Line(startPt, endPt).ToNurbsCurve();
          uCurvesTree.Add(uCurve, new GH_Path(i));
        }
        else
        {
          Curve uCurve = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3];
          uCurvesTree.Add(uCurve, new GH_Path(i));
        }
      }
    }
    for (int j = 0; j < VCount; j++)
    {
      if(isGrid)
      {
        Point3d startPt=isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1].StartVertex.Location;
        Point3d endPt = isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1].EndVertex.Location;
        Curve lastUCurve = new Line(startPt, endPt).ToNurbsCurve();
        uCurvesTree.Add(lastUCurve, new GH_Path(UCount));
      }
      else
      {
        Curve lastUCurve = isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1];
        uCurvesTree.Add(lastUCurve, new GH_Path(UCount));
      }
    }
  }

  public void GetUCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, bool isGrid, ref DataTree<Curve> uCurvesTree,double uvRodRadius,ref DataTree<Brep> uRodsTree)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        if (isGrid)
        {
          Point3d startPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3].StartVertex.Location;
          Point3d endPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3].EndVertex.Location;
          Curve uCurve = new Line(startPt, endPt).ToNurbsCurve();
          Brep[] uRod = Brep.CreatePipe(uCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
          uCurvesTree.Add(uCurve, new GH_Path(i));
          uRodsTree.AddRange(uRod, new GH_Path(i));
        }
        else
        {
          Curve uCurve = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[3];
          Brep[] uRod = Brep.CreatePipe(uCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
          uCurvesTree.Add(uCurve, new GH_Path(i));
          uRodsTree.AddRange(uRod, new GH_Path(i));
        }
      }
    }
    for (int j = 0; j < VCount; j++)
    {
      if (isGrid)
      {
        Point3d startPt = isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1].StartVertex.Location;
        Point3d endPt = isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1].EndVertex.Location;
        Curve lastUCurve = new Line(startPt, endPt).ToNurbsCurve();
        Brep[] lastURod = Brep.CreatePipe(lastUCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        uCurvesTree.Add(lastUCurve, new GH_Path(UCount));
        uRodsTree.AddRange(lastURod, new GH_Path(UCount));
      }
      else
      {
        Curve lastUCurve = isoSurfacesTree.Branch(UCount - 1)[j].ToBrep().Edges[1];
        Brep[] lastURod = Brep.CreatePipe(lastUCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        uCurvesTree.Add(lastUCurve, new GH_Path(UCount));
        uRodsTree.AddRange(lastURod, new GH_Path(UCount));
      }

    }
  }

  public void GetVCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, bool isGrid, ref DataTree<Curve> vCurvesTree)
  {
    for (int j = 0; j < VCount; j++)
    {
      for (int i = 0; i < UCount; i++)
      {
        if(isGrid)
        {
          Point3d startPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0].StartVertex.Location;
          Point3d endPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0].EndVertex.Location;
          Curve vCurve = new Line(startPt, endPt).ToNurbsCurve();
          vCurvesTree.Add(vCurve, new GH_Path(j));
        }
        else
        {
          Curve vCurve = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0];
          vCurvesTree.Add(vCurve, new GH_Path(j));
        }
      }
    }
    for (int i = 0; i < UCount; i++)
    {
      if(isGrid)
      {
        Point3d startPt = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2].StartVertex.Location;
        Point3d endPt = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2].EndVertex.Location;
        Curve lastVCurve = new Line(startPt, endPt).ToNurbsCurve();
        vCurvesTree.Add(lastVCurve, new GH_Path(VCount));
      }
      else
      {
        Curve lastVCurve = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2];
        vCurvesTree.Add(lastVCurve, new GH_Path(VCount));
      }

    }
  }

  public void GetVCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, bool isGrid, ref DataTree<Curve> vCurvesTree, double uvRodRadius,ref DataTree<Brep> vRodsTree)
  {
    for (int j = 0; j < VCount; j++)
    {
      for (int i = 0; i < UCount; i++)
      {
        if(isGrid)
        {
          Point3d startPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0].StartVertex.Location;
          Point3d endPt = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0].EndVertex.Location;
          Curve vCurve = new Line(startPt, endPt).ToNurbsCurve();
          Brep[] vRod = Brep.CreatePipe(vCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
          vCurvesTree.Add(vCurve, new GH_Path(j));
          vRodsTree.AddRange(vRod, new GH_Path(j));
        }
        else
        {
          Curve vCurve = isoSurfacesTree.Branch(i)[j].ToBrep().Edges[0];
          Brep[] vRod = Brep.CreatePipe(vCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
          vCurvesTree.Add(vCurve, new GH_Path(j));
          vRodsTree.AddRange(vRod, new GH_Path(j));
        }

      }
    }
    for (int i = 0; i < UCount; i++)
    {
      if(isGrid)
      {
        Point3d startPt = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2].StartVertex.Location;
        Point3d endPt = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2].EndVertex.Location;
        Curve lastVCurve = new Line(startPt, endPt).ToNurbsCurve();
        Brep[] lastVRod = Brep.CreatePipe(lastVCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        vCurvesTree.Add(lastVCurve, new GH_Path(VCount));
        vRodsTree.AddRange(lastVRod, new GH_Path(VCount));
      }
      else
      {
        Curve lastVCurve = isoSurfacesTree.Branch(i)[VCount - 1].ToBrep().Edges[2];
        Brep[] lastVRod = Brep.CreatePipe(lastVCurve, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        vCurvesTree.Add(lastVCurve, new GH_Path(VCount));
        vRodsTree.AddRange(lastVRod, new GH_Path(VCount));
      }

    }
  }

  public void CreateObliqueCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, ref DataTree<Curve> obliqueCurvesTree)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        Point3d p0 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[0].Location;
        Point3d p1 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[1].Location;
        Point3d p2 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[2].Location;
        Point3d p3 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[3].Location;
        Curve line1 = new Line(p0, p2).ToNurbsCurve();
        Curve line2 = new Line(p1, p3).ToNurbsCurve();
        obliqueCurvesTree.Add(line1, new GH_Path(i, j));
        obliqueCurvesTree.Add(line2, new GH_Path(i, j));
      }
    }
  }

  public void CreateObliqueCurves(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, ref DataTree<Curve> obliqueCurvesTree, double obliqueRodRadius,ref DataTree<Brep> obliqueRodsTree)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        Point3d p0 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[0].Location;
        Point3d p1 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[1].Location;
        Point3d p2 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[2].Location;
        Point3d p3 = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[3].Location;
        Curve line1 = new Line(p0, p2).ToNurbsCurve();
        Curve line2 = new Line(p1, p3).ToNurbsCurve();
        obliqueCurvesTree.Add(line1, new GH_Path(i, j));
        obliqueCurvesTree.Add(line2, new GH_Path(i, j));

        Brep[] obliqueRod1 = Brep.CreatePipe(line1, obliqueRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        Brep[] obliqueRod2 = Brep.CreatePipe(line2, obliqueRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        obliqueRodsTree.AddRange(obliqueRod1, new GH_Path(i, j));
        obliqueRodsTree.AddRange(obliqueRod2, new GH_Path(i, j));
      }
    }
  }

  public void GetIntersectionPoints(DataTree<Surface> isoSurfacesTree, int UCount, int VCount, ref DataTree<Point3d> intersectionPts)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        double u = isoSurfacesTree.Branch(i)[j].Domain(0).Mid;
        double v = isoSurfacesTree.Branch(i)[j].Domain(1).Mid;
        Point3d intersectionPt = isoSurfacesTree.Branch(i)[j].PointAt(u, v);
        intersectionPts.Add(intersectionPt, new GH_Path(i));
      }
    }
  }
  
  public void CreateVerticleLines(DataTree<Surface> isoSurfacesTree, DataTree<Point3d> intersectionPts, int UCount, int VCount, ref DataTree<Curve> verticleLines, double verticleRodRadius, ref DataTree<Brep> verticleRodsTree)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount; j++)
      {
        for (int k = 0; k < isoSurfacesTree.Branch(i)[j].ToBrep().Vertices.Count; k++)
        {
          Point3d startPt = isoSurfacesTree.Branch(i)[j].ToBrep().Vertices[k].Location;
          Point3d endPt = intersectionPts.Branch(i)[j];
          Curve verticleLine = new Line(startPt, endPt).ToNurbsCurve();
          Brep[] verticleRod = Brep.CreatePipe(verticleLine, verticleRodRadius, false, PipeCapMode.Round, false, 0.1, 0.1);
          verticleLines.Add(verticleLine, new GH_Path(i, j));
          verticleRodsTree.AddRange(verticleRod, new GH_Path(i, j));
        }
      }
    }
  }

  public void CreateUCurvesThroughIntersectionPts(DataTree<Point3d> intersectionPts, int UCount, int VCount,ref DataTree<Curve> uCurvesTreeThroughIntersectionPts, double uvRodRadius, DataTree<Brep> uRodsTreeThroughIntersectionPts)
  {
    for (int i = 0; i < UCount; i++)
    {
      for (int j = 0; j < VCount - 1; j++)
      {
        Point3d pt1 = intersectionPts.Branch(i)[j];
        Point3d pt2 = intersectionPts.Branch(i)[j + 1];
        Curve uCurveThroughIntersectionPts = new Line(pt1, pt2).ToNurbsCurve();
        uCurvesTreeThroughIntersectionPts.Add(uCurveThroughIntersectionPts, new GH_Path(i));
        Brep[] uRodThroughIntersectionPts = Brep.CreatePipe(uCurveThroughIntersectionPts, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        uRodsTreeThroughIntersectionPts.AddRange(uRodThroughIntersectionPts, new GH_Path(i));
      }
    }
  }

  public void CreateVCurvesThroughIntersectionPts(DataTree<Point3d> intersectionPts, int UCount, int VCount, ref DataTree<Curve> vCurvesTreeThroughIntersectionPts, double uvRodRadius, DataTree<Brep> vRodsTreeThroughIntersectionPts)
  {
    for (int j = 0; j < VCount; j++)
    {
      for (int i = 0; i < UCount-1; i++)
      {
        Point3d pt1 = intersectionPts.Branch(i)[j];
        Point3d pt2 = intersectionPts.Branch(i + 1)[j];
        Curve vCurveThroughIntersectionPts = new Line(pt1, pt2).ToNurbsCurve();
        vCurvesTreeThroughIntersectionPts.Add(vCurveThroughIntersectionPts, new GH_Path(j));
        Brep[] vRodThroughIntersectionPts = Brep.CreatePipe(vCurveThroughIntersectionPts, uvRodRadius, false, PipeCapMode.Round, true, 0.1, 0.1);
        vRodsTreeThroughIntersectionPts.AddRange(vRodThroughIntersectionPts, new GH_Path(j));
      }
    }
  }
  #endregion
}