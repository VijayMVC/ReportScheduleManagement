using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace ReportScheduleInWeb.Models
{
    public class ColumnType
    {
        public string Name { get; set; }

        public string Alias { get; set; }
    }

    public class ParameterType
    {
        public string Name { get; set; }

        public string Value { get; set; }

        public string Type { get; set; }
    }

    public class WishViewModel
    {
        public WishViewModel()
        {
            columns = new List<ColumnType>();
            parameters = new List<ParameterType>();
        }

        public int wish_id { get; set; }

        [DisplayName("Дата создания")]
        public System.DateTime wish_createdate { get; set; }

        [DisplayName("Дедлайн")]
        public System.DateTime wish_deadline { get; set; }

        [DisplayName("Попытки")]
        public Nullable<int> wish_total_attempts { get; set; }

        [DisplayName("Отчет")]
        public string wish_report_type_name { get; set; }

        public string report_type_id { get; set; }

        [DisplayName("Скрипт")]
        public string select_command { get; set; }

        [DisplayName("Поля")]
        public List<ColumnType> columns { get; set; }

        [DisplayName("Параметры")]
        public List<ParameterType> parameters { get; set; }

        [DisplayName("Статус")]
        public string wish_status { get; set; }
    }
}