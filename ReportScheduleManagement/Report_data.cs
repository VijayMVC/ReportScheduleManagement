//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReportScheduleManagement
{
    using System;
    using System.Collections.Generic;
    
    public partial class Report_data
    {
        public int report_data_id { get; set; }
        public int report_data_task_id { get; set; }
        public System.DateTime report_data_createdate { get; set; }
        public string report_data_xml { get; set; }
    
        public virtual Tasks Tasks { get; set; }
    }
}
