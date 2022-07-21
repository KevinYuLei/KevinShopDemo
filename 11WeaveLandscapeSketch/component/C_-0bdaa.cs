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
using Rhino.Geometry.Intersect;


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
  private void RunScript(Point3d datumPt, double radius, double height, double slackness, int fittingDegree, int countOfUnit, double radiusOfPipe, bool isCircle, ref object outPipes)
  {
    if (countOfUnit < 4)
      countOfUnit = 4;

    double extensionLength = 100;
    double shortSideLength = (radius + extensionLength) * 2;
    double longSideLength = shortSideLength * Math.Tan(Math.PI / 6) * 2;
    double radiusOfHexagon = (longSideLength / countOfUnit) / 2;

    //create bottom rectangle
    Rectangle3d bottomRectangle = new Rectangle3d(new Plane(datumPt, Vector3d.XAxis, Vector3d.YAxis), new Interval(-longSideLength / 2, longSideLength / 2), new Interval(-shortSideLength / 2, shortSideLength / 2));
    //create top rectangle
    Rectangle3d topRectangle = bottomRectangle;
    topRectangle.Transform(Transform.Translation(0, 0, height));

    //create hexagon in the center of bottom rectangle
    Curve hexagon = CreatePolygon(datumPt,radiusOfHexagon, 6);

    //create the loft brep
    Brep loftBrep = CreateLoftBrep(datumPt, radius, height, slackness, fittingDegree, radiusOfHexagon, topRectangle);

    //create boundary brep of bottom rectangle
    Brep contourBrep = Brep.CreatePlanarBreps(bottomRectangle.ToNurbsCurve(), 0.1)[0];

    //explode the hexagon
    List<Curve> segmentsOfHexagon = new List<Curve>();
    segmentsOfHexagon.AddRange(((PolyCurve)hexagon).Explode());

    //initialize parameters for contour
    double contourDistance = radiusOfHexagon * Math.Sin(Math.PI / 3) * 2;
    double obliqueLength = longSideLength * Math.Cos(Math.PI / 6) + shortSideLength * Math.Cos(Math.PI / 3);
    double countOfObliqueContours = obliqueLength / contourDistance;
    double countOfHorizontalContours = countOfUnit;

    //correct the countOfObliqueContour
    if (countOfUnit%4==1.0)
    {
      countOfObliqueContours += 0.5;
    }
    else if(countOfUnit % 4==2.0)
    {
      countOfObliqueContours -= 1;
    }
    else if(countOfUnit % 4==3.0)
    {
      countOfObliqueContours -= 0.5;
    }
    //correct the countOfHorizontalContour
    if (countOfHorizontalContours%2==1.0)
    {
      countOfHorizontalContours -= 1;
    }

    //create contours in 3 directions
    List<Curve> obliqueContours1 = CreateContours(segmentsOfHexagon[0], segmentsOfHexagon[3], contourBrep, contourDistance, countOfObliqueContours);
    List<Curve> horizontalContours = CreateContours(segmentsOfHexagon[1], segmentsOfHexagon[4], contourBrep, contourDistance, countOfHorizontalContours);
    List<Curve> obliqueContours2 = CreateContours(segmentsOfHexagon[2], segmentsOfHexagon[5], contourBrep, contourDistance, countOfObliqueContours);

    //correct contours
    //create bigger hexagon to trim contours
    Curve correctionHexagon = CreatePolygon(datumPt, radiusOfHexagon * 3, 6);
    //create correction points 
    Curve correctionCircle = new Circle(datumPt, radiusOfHexagon * Math.Sqrt(6) / 2).ToNurbsCurve();

    List<Curve> correctionObliqueContours1 = CorrectContours(datumPt, correctionHexagon, correctionCircle, obliqueContours1, countOfObliqueContours);
    List<Curve> correctionHorizontalContours = CorrectContours(datumPt, correctionHexagon, correctionCircle, horizontalContours, countOfHorizontalContours);
    List<Curve> correctionObliqueContours2 = CorrectContours(datumPt, correctionHexagon, correctionCircle, obliqueContours2, countOfObliqueContours);

    List<Curve> projectionCrvsFromOC1 = CreateProjectionCurves(correctionObliqueContours1, loftBrep);
    List<Curve> projectionCrvsFromHC = CreateProjectionCurves(correctionHorizontalContours, loftBrep);
    List<Curve> projectionCrvsFromOC2 = CreateProjectionCurves(correctionObliqueContours2, loftBrep);

    List<Curve> allProjectionCrvs = new List<Curve>();
    allProjectionCrvs.AddRange(projectionCrvsFromOC1);
    allProjectionCrvs.AddRange(projectionCrvsFromHC);
    allProjectionCrvs.AddRange(projectionCrvsFromOC2);

    //limit the max radius of pipe according to the bottom circle(corretion circle)
    double fixFactorForMaxPipeRadius = 1.2;
    double maxPipeRadius = (correctionCircle.GetLength()/24)/fixFactorForMaxPipeRadius;
    radiusOfPipe = Math.Min(radiusOfPipe, maxPipeRadius);
    //create pipes
    List<Brep> pipes = CreatePipes(allProjectionCrvs, radiusOfPipe, isCircle);

    outPipes = pipes;
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

  public Brep CreateLoftBrep(Point3d datumPt, double radius, double height, double slackness, int fittingDegree, double radiusOfHexagon, Rectangle3d topRectangle)
  {
    //create a seires of circles to loft
    List<Curve> curvesToLoft = new List<Curve>();
    for (int i = 0; i <= fittingDegree; i++)
    {
      double scaleFactor = ((radiusOfHexagon * Math.Sqrt(6) / 2) / radius) * (1 + i * slackness);
      double moveHeight = height / fittingDegree;
      Curve curveToLoft = new Circle(datumPt, radius * scaleFactor).ToNurbsCurve();
      curveToLoft.Transform(Transform.Translation(0, 0, i * moveHeight));
      curvesToLoft.Add(curveToLoft);
    }
    //add the last circle in the top rectangle without scaling
    Curve lastCurve = new Circle(datumPt, radius).ToNurbsCurve();
    lastCurve.Transform(Transform.Translation(0, 0, height));
    curvesToLoft.Add(lastCurve);

    //create loft part 1
    Brep loftBrepPart1 = Brep.CreateFromLoft(curvesToLoft, Point3d.Unset, Point3d.Unset, LoftType.Loose, false)[0];

    //create loft part 2 with boundary surface/brep
    CurveList curveList = new CurveList();
    curveList.Add(topRectangle.ToNurbsCurve());
    curveList.Add(lastCurve);
    Brep loftBrepPart2 = Brep.CreatePlanarBreps(curveList, 0.1)[0];

    Brep loftBrep = Brep.JoinBreps(new List<Brep> { loftBrepPart1, loftBrepPart2 }, 0.1)[0];

    return loftBrep;
  }

  public List<Curve> CreateContours(Curve segmentOfHexagon,Curve oppositeSegment,Brep contourBrep,double contourDistance,double countOfContours)
  {
    List<Curve> contours = new List<Curve>();

    Point3d midPtOfSegment1 = segmentOfHexagon.PointAtLength(segmentOfHexagon.GetLength() / 2);
    double tOfMidPt1;
    segmentOfHexagon.ClosestPoint(midPtOfSegment1, out tOfMidPt1);
    Vector3d tangentAtMidPt1 = segmentOfHexagon.TangentAt(tOfMidPt1);
    Plane originPlane = new Plane(midPtOfSegment1, tangentAtMidPt1, Vector3d.ZAxis);

    Point3d midPtOfSegment2 = oppositeSegment.PointAt(oppositeSegment.GetLength() / 2);
    double tOfMidPt2;
    oppositeSegment.ClosestPoint(midPtOfSegment2, out tOfMidPt2);
    Vector3d tangentAtMidPt2 = oppositeSegment.TangentAt(tOfMidPt2);
    Plane oppositePlane = new Plane(midPtOfSegment2, tangentAtMidPt2, Vector3d.ZAxis);
    
    for (int i = 0; i < countOfContours/2; i++)
    {
      Plane interPlane = originPlane;
      Vector3d move = originPlane.ZAxis * contourDistance * i;
      interPlane.Transform(Transform.Translation(move));
      Curve[] interCrvs;
      Point3d[] interPts;
      Intersection.BrepPlane(contourBrep, interPlane, 0.1, out interCrvs, out interPts);
      if (interCrvs == null)
        continue;
      contours.AddRange(interCrvs);
    }

    for (int i = 0; i < countOfContours/2; i++)
    {
      Plane interPlane = oppositePlane;
      Vector3d move = oppositePlane.ZAxis * contourDistance * i;
      interPlane.Transform(Transform.Translation(move));
      Curve[] interCrvs;
      Point3d[] interPts;
      Intersection.BrepPlane(contourBrep, interPlane, 0.1, out interCrvs, out interPts);
      if (interCrvs == null)
        continue;
      contours.AddRange(interCrvs);
    }
    return contours;
  }

  public List<Curve> CorrectContours(Point3d datumPt,Curve correctionHexagon, Curve correctionCircle , List<Curve> contours,double countOfContours)
  {
    List<Curve> correctionContours = new List<Curve>();

    //select the exact curve
    Curve contour1 = contours[0];
    Curve contour2 = contours[(int)countOfContours / 2];

    //compute the intersection points of contour and hexagon
    CurveIntersections intersection1 = Intersection.CurveCurve(contour1, correctionHexagon, 0.1, 0.1);
    CurveIntersections intersection2 = Intersection.CurveCurve(contour2, correctionHexagon, 0.1, 0.1);

    //extract the outter parts of the contour
    Curve part1OfContour1 = contour1.Trim(contour1.Domain.Min, intersection1[0].ParameterA);
    Curve part2OfContour1 = contour1.Trim(intersection1[1].ParameterA, contour1.Domain.Max);

    Curve part1OfContour2 = contour2.Trim(contour2.Domain.Min, intersection2[0].ParameterA);
    Curve part2OfContour2 = contour2.Trim(intersection2[1].ParameterA, contour2.Domain.Max);

    List<Curve> crvs = new List<Curve>();
    crvs.Add(part1OfContour1);
    crvs.Add(part2OfContour1);
    crvs.Add(part1OfContour2);
    crvs.Add(part2OfContour2);

    correctionContours.AddRange(contours);
    correctionContours.RemoveAt(0);
    correctionContours.RemoveAt((int)countOfContours / 2 - 1);
    for (int i = 0; i < crvs.Count; i++)
    {
      //initialize the end point closest to the datumPt
      Point3d pt1;
      if (i%2==0)
      {
        pt1 = crvs[i].PointAtEnd;
      }
      else
      {
        crvs[i].Reverse();
        pt1 = crvs[i].PointAtEnd;
      }

      //find the point on circle closest to the pt1 
      Point3d pt2 = new Point3d();
      Curve contourX;
      if (i < 2)
        contourX = contour1.DuplicateCurve();
      else
        contourX = contour2.DuplicateCurve();

      CurveIntersections circleIntersectionPts=Intersection.CurveCurve(contourX, correctionCircle, 0.1, 0.1);
      if (i % 2 == 0)
      {
        pt2 = circleIntersectionPts[0].PointA;
        pt2.Transform(Transform.Rotation(Math.PI / 6, datumPt));
      }
        
      else
      {
        pt2 = circleIntersectionPts[1].PointA;
        pt2.Transform(Transform.Rotation(-Math.PI / 6, datumPt));
      }

      Curve blendCurve=Curve.CreateBlendCurve(crvs[i], new Line(pt2, datumPt).ToNurbsCurve(), BlendContinuity.Curvature, 0, 1);

      Curve correctionContour = Curve.JoinCurves(new List<Curve> { crvs[i], blendCurve }, 0.1)[0];
      correctionContours.Add(correctionContour);
    }
    return correctionContours;
  }

  public List<Curve> CreateProjectionCurves(List<Curve> correctionCurves,Brep loftBrep)
  {
    List<Curve> projectionCurves = new List<Curve>();
    for (int i = 0; i < correctionCurves.Count; i++)
    {
      Curve[] projectionCurve = Curve.ProjectToBrep(correctionCurves[i], loftBrep, Vector3d.ZAxis, 0.1);
      projectionCurves.AddRange(projectionCurve);
    }
    return projectionCurves;
  }

  public List<Brep> CreatePipes(List<Curve> allProjectionCrvs,double radiusOfPipe, bool isCircle)
  {
    List<Brep> pipes = new List<Brep>();
    if (isCircle == true)
    {
      for (int i = 0; i < allProjectionCrvs.Count; i++)
      {
        Brep[] pipe = Brep.CreatePipe(allProjectionCrvs[i], radiusOfPipe, true, PipeCapMode.Flat, true, 0.1, 0.1);
        pipes.AddRange(pipe);
      }
    }
    else
    {
      for (int i = 0; i < allProjectionCrvs.Count; i++)
      {
        Plane basePlane = new Plane(allProjectionCrvs[i].PointAtEnd, allProjectionCrvs[i].TangentAtEnd);
        Curve baseRectangle = new Rectangle3d(basePlane, new Interval(-radiusOfPipe, radiusOfPipe), new Interval(-radiusOfPipe, radiusOfPipe)).ToNurbsCurve();
        //initialize sweepOneRail object
        SweepOneRail sweepOneRail = new SweepOneRail();
        sweepOneRail.ClosedSweep = allProjectionCrvs[i].IsClosable(0.1);
        sweepOneRail.SweepTolerance = 0.1;
        sweepOneRail.MiterType = 0;
        //performSweep
        Brep[] pipe = sweepOneRail.PerformSweep(allProjectionCrvs[i], baseRectangle);
        //cap planar holes
        for (int j = 0; j < pipe.Length; j++)
        {
          pipe[j] = pipe[j].CapPlanarHoles(0.1);
        }
        pipes.AddRange(pipe);
      }
    }
    return pipes;
  }
  #endregion
}