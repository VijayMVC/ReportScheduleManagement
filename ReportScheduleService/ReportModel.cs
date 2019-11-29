using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace ReportScheduleService
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

        [XmlAttribute("Type")]
        public string ColumnType { get; set; }
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

    [DataContract]
    public class ParameterType
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Alias { get; set; }

        [DataMember]
        public string Value { get; set; }

        [DataMember]
        public string Type { get; set; }
    }

    [DataContract]
    public class ColumnType
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Alias { get; set; }
    }
}
