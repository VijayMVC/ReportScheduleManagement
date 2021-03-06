﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace ReportScheduleInWeb.Models
{
    [DataContract]
    public class ColumnType
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Type { get; set; }

        [DataMember]
        public string Alias { get; set; }
    }

    [DataContract]
    public class ParameterType
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        [DisplayName("Параметр")]
        public string Alias { get; set; }

        [DataMember]
        [DisplayName("Значение")]
        public string Value { get; set; }

        [DataMember]
        [DisplayName("Тип")]
        public string Type { get; set; }
    }

    public class Place
    {
        public string Place_id { get; set; }

        [DisplayName("Место")]
        public string Place_name { get; set; }
    }

    public class ReportType
    {
        public string report_type_id { get; set; }

        public string report_type_name { get; set; }

        public string report_group_name { get; set; }
    }

    public class WishViewModel
    {
        public WishViewModel()
        {
            Columns = new List<ColumnType>();
            Parameters = new List<ParameterType>();
            Places = new List<Place>();
        }

        public int Wish_id { get; set; }

        [DisplayName("Дата создания")]
        public System.DateTime Wish_createdate { get; set; }

        [DisplayName("Дедлайн")]
        public System.DateTime Wish_deadline { get; set; }

        [DisplayName("Попытки")]
        public Nullable<int> Wish_total_attempts { get; set; }

        [DisplayName("Тип отчета")]
        public string Wish_report_type_name { get; set; }

        [DisplayName("Имя отчета")]
        public string Report_type_id { get; set; }

        [DisplayName("Скрипт")]
        public string Select_command { get; set; }

        [DisplayName("Поля")]
        public List<ColumnType> Columns { get; set; }

        [DisplayName("Параметры")]
        public List<ParameterType> Parameters { get; set; }

        [DisplayName("Статус")]
        public string Wish_status { get; set; }

        [DisplayName("Места")]
        public List<Place> Places { get; set; }

        [DisplayName("Заказчик")]
        public int User_id { get; set; }

        public string User_name { get; set; }
    }
}