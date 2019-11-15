using System;
using System.ComponentModel;

namespace ReportScheduleInWeb.Models
{
    public class TaskViewModel
    {
        public int Task_id { get; set; }

        public int Task_wish_id { get; set; }

        [DisplayName("Место")]
        public string Task_place_name { get; set; }
        public int Task_place_id { get; set; }

        [DisplayName("Дата и время запуска")]
        public System.DateTime Task_startdate { get; set; }

        [DisplayName("Попытки")]
        public Nullable<int> Task_number_attempts { get; set; }

        [DisplayName("Статус")]
        public string Task_status { get; set; }

        [DisplayName("Последняя ошибка")]
        public string Task_last_error_text { get; set; }
    }
}