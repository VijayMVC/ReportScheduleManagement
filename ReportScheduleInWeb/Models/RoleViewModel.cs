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

        [DisplayName("Разрешения")]
        public List<Permissions> permissions { get; set; }

        public RoleViewModel()
        {
            permissions = new List<Permissions>();
        }
    }
}