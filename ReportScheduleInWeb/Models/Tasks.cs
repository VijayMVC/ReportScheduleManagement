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
    
    public partial class Tasks
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Tasks()
        {
            this.Report_data = new HashSet<Report_data>();
        }
    
        public int task_id { get; set; }
        public System.DateTime task_startdate { get; set; }
        public int task_wish_id { get; set; }
        public int task_place_id { get; set; }
        public Nullable<int> task_number_attempts { get; set; }
        public string task_status { get; set; }
        public string task_last_error_text { get; set; }
    
        public virtual Places Places { get; set; }
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Report_data> Report_data { get; set; }
        public virtual Wishes Wishes { get; set; }
    }
}
