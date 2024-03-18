using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.Util;
using WpfApp1.ContourLinkScoreStrategy;

namespace WpfApp1;

public class ContourList
{
    private int _sourceImageWidth;
    private int _sourceImageHeight;
    public int sourceImageWidth => _sourceImageWidth;
    public int sourceImageHeight => _sourceImageHeight;

    private VectorOfVectorOfPoint rawContours;
    private List<ContourPoint> _contours;
    public List<ContourPoint> contours => _contours;
    public ILinkScoreCalculation linkScoreCalculation;
    public double heightPerWidthAspectRatio => _sourceImageHeight / _sourceImageWidth;

    private int halfWidth => _sourceImageWidth / 2;

    public ContourList(VectorOfVectorOfPoint rawContours, int sourceImageWidth, int sourceImageHeight,
        ILinkScoreCalculation? linkScoreCalculation=null) {
        this.linkScoreCalculation = linkScoreCalculation ?? new ContourLinkBasedOnAreaAndDistance();
        this.rawContours = rawContours;
        this._sourceImageWidth = sourceImageWidth;
        this._sourceImageHeight = sourceImageHeight;
        initializeContourList();
        initializeContourLinks();
    }

    private void initializeContourList() {
        _contours = new List<ContourPoint>(rawContours.Size);
        for (int i = 0; i < this.rawContours.Size; i++) {
            var contour = ContourPoint.fromVectorOfPoint(rawContours[i]);
            if (contour == null)
                continue;
            _contours.Add(contour);
        }
        _contours.Sort((a, b) =>  linkScoreCalculation.nodePrioritySorter(this, a, b));
    }

    private void initializeContourLinks() {
        // This is just for a set. The value is unused
        var visitedLinks = new ConditionalWeakTable<ContourPoint, ContourPoint>();
        var urutan = 0;
        foreach (var contour in _contours) {
            ContourPoint? targetLink = contour;

            var vectorSet = new SetOfNormalizedVector2(0.2);

            do {
                if (visitedLinks.TryGetValue(targetLink, out _))
                    break;
                if (!validateNotTurningToOpositeDirection(targetLink, vectorSet))
                    break;
                visitedLinks.Add(targetLink, targetLink);
                targetLink.order = urutan;

                targetLink = updateLink(targetLink);
                urutan++;
            } while (targetLink != null);
        }
    }

    private bool validateNotTurningToOpositeDirection(ContourPoint targetLink, SetOfNormalizedVector2 vectorSet) {
        Vector2? nextDirection = (targetLink.backwardLink != null)?
            targetLink.vector2 - targetLink.backwardLink.vector2 : null;
        if (nextDirection != null && vectorSet.checkIfConflicting(nextDirection.Value, 1.9))
            return false;
        if (nextDirection != null)
            vectorSet.Add(nextDirection.Value);
        return true;
    }


    private ContourPoint? updateLink(ContourPoint toBeUpdated) {
        var closest = new Tuple<double, ContourPoint?>(Double.PositiveInfinity, null);
        foreach (var nextLinkCandidate in _contours) {
            if (nextLinkCandidate == toBeUpdated)
                continue;
            if (nextLinkCandidate.link == toBeUpdated)
                continue;
            if (nextLinkCandidate.order != null)  // to make sure the graph will be acyclic graph
                continue;
            if (!linkScoreCalculation.isHardConstraintSatisfied(this, toBeUpdated, nextLinkCandidate))
                continue;

            var score = linkScoreCalculation.getScore(toBeUpdated, nextLinkCandidate);
            if (score < closest.Item1) {
                closest = new Tuple<double, ContourPoint?>(score, nextLinkCandidate);
            }
        }
        if (closest.Item2 == null)
            return null;
        var targetLink = closest.Item2;
        toBeUpdated.link = targetLink;
        closest.Item2.backwardLink = toBeUpdated;
        return targetLink;
    }

    public void removeOutliers(double maximumRadianThreshold = Math.PI*2/9) {
        _contours.Sort((a, b) => {
            if (a.order == null || b.order == null)
                return 0;
            return (a.order??0) - (b.order ?? 0);
        });

        List<ContourPoint> toBeDeleted = new List<ContourPoint>();
        do {
            foreach (var contour in _contours) {
                if (contour.isOutlier(maximumRadianThreshold)) {
                    toBeDeleted.Add(contour);
                }
            }
            toBeDeleted.ForEach((x) => x.deleteThis());
            toBeDeleted.Clear();
        } while (toBeDeleted.Count > 0);
    }

}

