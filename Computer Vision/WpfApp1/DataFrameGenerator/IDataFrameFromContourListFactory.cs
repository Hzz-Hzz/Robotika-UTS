using Microsoft.Data.Analysis;
using WpfApp1;

namespace DataFrameGenerator;

public interface IDataFrameFromContourListFactory
{
    public DataFrame getDataFrame(params ContourList[] contourLists);
}