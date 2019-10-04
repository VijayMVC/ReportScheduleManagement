using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace ReportScheduleInWeb.Models
{
    public class PermissionViewModel
    {
        public int permission_id { get; set; }

        [DisplayName("Разрешение*")]
        public string permission_name { get; set; }

        [DisplayName("Роли")]
        public List<Roles> roles { get; set; }

        public PermissionViewModel()
        {
            roles = new List<Roles>();
        }
    }
}
