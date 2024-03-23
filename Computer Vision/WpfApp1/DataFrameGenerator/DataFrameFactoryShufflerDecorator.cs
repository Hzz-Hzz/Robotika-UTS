using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using DatasetEditor.model;
using Microsoft.Data.Analysis;
using WpfApp1;

namespace DataFrameGenerator;

public class DataFrameFactoryShufflerDecorator : IDataFrameFromContourListFactory
{
    private IDataFrameFromContourListFactory factory;
    private Random rng;
    public DataFrameFactoryShufflerDecorator(IDataFrameFromContourListFactory factory, Random? rng=null) {
        this.factory = factory;
        this.rng = rng ?? new Random();
    }

    public DataFrame getDataFrame(ContourList[] contourLists, DatasetImageLabel[]? labels = null) {
        var shuffler = new ArrayShuffler();

        shuffler.createRandomShuffle(rng, contourLists.Length);
        var shuffledContourLists = shuffler.applyShuffle(contourLists);
        labels = shuffler.applyShuffle(labels);

        Debug.Assert(shuffledContourLists != null);
        return factory.getDataFrame(shuffledContourLists, labels);
    }
}



class ArrayShuffler
{
    private int[]? shuffleResult;

    public void createRandomShuffle(Random random, int size) {
        shuffleResult = Enumerable.Range(0, size).ToArray();
        Shuffle(random, shuffleResult);
    }

    public T[]? applyShuffle<T>(T[]? array) {
        if (array == null)
            return null;
        if (shuffleResult == null)
            throw new ValidationException("shuffleResult is null. Probably You forgot to call createRandomShuffle");
        if (shuffleResult.Length != array.Length)
            throw new ValidationException("Given array has different length than the one initialized when calling createRandomShuffle");


        var ret = new T[array.Length];
        for (int i = 0; i < array.Length; i++) {
            var index = shuffleResult[i];
            ret[i] = array[index];
        }
        return ret;
    }



    // credits: https://stackoverflow.com/a/110570/7069108
    private void Shuffle<T> (Random rng, params T[] array) {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }
}