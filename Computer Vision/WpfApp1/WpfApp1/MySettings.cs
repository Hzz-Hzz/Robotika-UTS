using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace WpfApp1;

public class MySettings
{
    public double Top { get; set; } = 0;
    public double Left { get; set; } = 0;
    public double Width { get; set; } = 500;
    public double Height { get; set; } = 500;
    public bool Maximized { get; set; } = true;

    public void Save(string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            XmlSerializer xmls = new XmlSerializer(typeof(MySettings));
            xmls.Serialize(sw, this);
        }
    }
    public static MySettings Read(string filename)
    {
        using (StreamReader sw = new StreamReader(filename))
        {
            XmlSerializer xmls = new XmlSerializer(typeof(MySettings));
            return xmls.Deserialize(sw) as MySettings;
        }
    }

}