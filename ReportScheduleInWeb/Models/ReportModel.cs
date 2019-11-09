using System.Collections.Generic;
using System.Xml.Serialization;

namespace ReportScheduleInWeb
{
    [XmlRoot("TableDataSource")]
    public class ReportModel
    {
        public ReportModel()
        {
            Columns = new List<Column>();
            Parameters = new List<Parameter>();
        }

        [XmlAttribute("SelectCommand")]
        public string SelectCommand { get; set; }

        [XmlElement("Column")]
        public List<Column> Columns;

        [XmlElement("CommandParameter")]
        public List<Parameter> Parameters;
    }

    public class Column
    {
        [XmlAttribute("Name")]
        public string ColumnName { get; set; }

        [XmlAttribute("Alias")]
        public string ColumnAlias { get; set; }
    }

    public class Parameter
    {
        [XmlAttribute("Name")]
        public string ParameterName { get; set; }

        [XmlAttribute("Alias")]
        public string ParameterAlias { get; set; }

        [XmlAttribute("Value")]
        public string ParameterValue { get; set; }

        [XmlAttribute("DataType")]
        public string ParameterDataType { get; set; }
    }
}
