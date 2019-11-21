using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace ReportScheduleInWeb.Models
{
    public class RoleViewModel
    {
        public int role_id { get; set; }

        [DisplayName("Роль*")]
        public string role_name { get; set; }
    }
}