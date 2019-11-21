using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ReportScheduleInWeb.Models
{
    public class LoginViewModel
    {
        public int user_id { get; set; }

        public string user_login { get; set; }
        public string user_new_login { get; set; }

        [DataType(DataType.Password)]
        public string user_password { get; set; }

        [DataType(DataType.Password)]
        public string user_new_password { get; set; }

        [DataType(DataType.Password)]
        public string user_confirm_password { get; set; }

        [DataType(DataType.EmailAddress)]
        public string user_email { get; set; }

        public string user_surname { get; set; }

        public string user_name { get; set; }

        public string user_patronymic { get; set; }

        public string LoginErrorMessage { get; set; }

        public bool user_isdeleted { get; set; }

        public Nullable<int> role_id { get; set; }
    }
}