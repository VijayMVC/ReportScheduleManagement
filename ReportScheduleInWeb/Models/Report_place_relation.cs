//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReportScheduleInWeb.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Report_place_relation
    {
        public int rpr_id { get; set; }
        public int rpr_report_type_id { get; set; }
        public int rpr_place_id { get; set; }
    
        public virtual Places Places { get; set; }
        public virtual Report_types Report_types { get; set; }
    }
}
