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
    
    public partial class Wish_report_relation
    {
        public int wrr_id { get; set; }
        public int wrr_wish_id { get; set; }
        public int wrr_report_type_id { get; set; }
        public short wrr_access_type { get; set; }
    
        public virtual Report_types Report_types { get; set; }
        public virtual Wishes Wishes { get; set; }
    }
}
