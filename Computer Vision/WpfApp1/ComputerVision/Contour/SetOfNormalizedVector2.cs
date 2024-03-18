using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Emgu.CV;

namespace WpfApp1;

public class SetOfNormalizedVector2
{
    private double _minimumDissimilarity;
    private List<Vector2> vectors;


    public SetOfNormalizedVector2(double minimumDissimilarity) {
        if (minimumDissimilarity < 0 || minimumDissimilarity > 2) {
            throw new ValidationException("maximumSimilarity should only be a number between 0 and 2");
        }

        vectors = new();
        this._minimumDissimilarity = minimumDissimilarity;
    }

    public void Add(Vector2 vector2) {
        vector2 = Vector2.Normalize(vector2);
        var mostSimilar = GetMostSimilar(vector2);
        if (mostSimilar == null) {
            vectors.Add(vector2);
            return;
        }

        var dissimilarity = 1 - Vector2.Dot(mostSimilar!.Value, vector2);  // 0 = exactly the same, 2 = very dissimilar
        if (dissimilarity < this._minimumDissimilarity)
            return;
        vectors.Add(vector2);
    }


    public Vector2? GetMostSimilar(Vector2 find) {
        find = Vector2.Normalize(find);
        Vector2? ret = null;
        double maximumSimilarity = Double.NegativeInfinity;

        foreach (var vector in vectors) {
            var similarity = Vector2.Dot(vector, find);
            if (similarity > maximumSimilarity) {
                maximumSimilarity = similarity;
                ret = vector;
            }
        }
        return ret;
    }

    public Vector2? GetMostDissimilar(Vector2 find) {
        find = Vector2.Normalize(find);
        Vector2? ret = null;
        double minimumSimilarity = Double.PositiveInfinity;

        foreach (var vector in vectors) {
            var similarity = Vector2.Dot(vector, find);
            if (similarity < minimumSimilarity) {
                minimumSimilarity = similarity;
                ret = vector;
            }
        }
        return ret;
    }

    public bool checkIfConflicting(Vector2 find, double maximumDissimilarity) {
        if (maximumDissimilarity < 0 || maximumDissimilarity > 2) {
            throw new ValidationException("maximumDissimilarity should be between 0 and 2");
        }

        find = Vector2.Normalize(find);
        var mostDissimilar = GetMostDissimilar(find);
        if (mostDissimilar == null)
            return false;
        var dissimilarity = 1 - Vector2.Dot(mostDissimilar.Value, find);
        return dissimilarity > maximumDissimilarity;
    }


}