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

    public DataFrame getDataFrame(params ContourList[] contourLists) {
        var contourListsArr = contourLists.Select(e => e).ToArray();
        Shuffle(contourListsArr);
        return factory.getDataFrame(contourListsArr);
    }

    // credits: https://stackoverflow.com/a/110570/7069108
    private void Shuffle<T> (T[] array) {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}