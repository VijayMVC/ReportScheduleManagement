using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace ReportScheduleInWeb.Models
{
    public class UserViewModel
    {
        public int user_id { get; set; }
        [DisplayName("Логин*")]
        public string user_login { get; set; }
        [DisplayName("Пароль*")]
        [DataType(DataType.Password)]
        public string user_password { get; set; }
        [DisplayName("Удален")]
        public bool user_isdeleted { get; set; }
        [DisplayName("Фамилия*")]
        public string user_surname { get; set; }
        [DisplayName("Имя*")]
        public string user_name { get; set; }
        [DisplayName("Отчество")]
        public string user_patronymic { get; set; }
        [DisplayName("Email")]
        public string user_email { get; set; }

        [DisplayName("Повтор пароля*")]
        [DataType(DataType.Password)]
        public string user_password_confirm { get; set; }

        [DisplayName("Старый пароль*")]
        [DataType(DataType.Password)]
        public string user_password_old { get; set; }

        [DisplayName("Роли")]
        public List<int> roles { get; set; }

        public string user_roles { get; set; }

        [DisplayName("Отчеты")]
        public List<int> report_types { get; set; }

        [DisplayName("ФИО")]
        public string user_FIO { get; set; }
        
        public string UserErrorMessage { get; set; }

        public int changeable { get; set; }

        public UserViewModel()
        {
            roles = new List<int>();
            report_types = new List<int>();
            changeable = 1;
        }
    }
}