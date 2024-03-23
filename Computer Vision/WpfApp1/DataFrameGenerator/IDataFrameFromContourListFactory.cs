using DatasetEditor.model;
using Microsoft.Data.Analysis;
using WpfApp1;

namespace DataFrameGenerator;

public interface IDataFrameFromContourListFactory
{
    public DataFrame getDataFrame(ContourList[] contourLists, DatasetImageLabel[]? labels=null);

    public DataFrame getDataFrame(ContourList contourLists, DatasetImageLabel? labels = null) {
        return getDataFrame(new[] { contourLists }, (labels==null)? null : new []{labels});
    }

}