﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class ReportScheduleEntities : DbContext
    {
        public ReportScheduleEntities()
            : base("name=ReportScheduleEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<Places> Places { get; set; }
        public virtual DbSet<Report_data> Report_data { get; set; }
        public virtual DbSet<Report_types> Report_types { get; set; }
        public virtual DbSet<Tasks> Tasks { get; set; }
        public virtual DbSet<Wishes> Wishes { get; set; }
        public virtual DbSet<Users> Users { get; set; }
    }
}
