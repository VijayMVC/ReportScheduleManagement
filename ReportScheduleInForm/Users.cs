//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReportScheduleInForm
{
    using System;
    using System.Collections.Generic;
    
    public partial class Users
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public Users()
        {
            this.Wishes = new HashSet<Wishes>();
        }
    
        public int user_id { get; set; }
        public string user_login { get; set; }
        public string user_password { get; set; }
        public bool user_isdeleted { get; set; }
        public string user_surname { get; set; }
        public string user_name { get; set; }
        public string user_patronymic { get; set; }
        public string user_email { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<Wishes> Wishes { get; set; }
    }
}
