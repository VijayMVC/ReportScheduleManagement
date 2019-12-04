using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Web;
using System.Web.Http;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ReportScheduleService
{
    [ServiceContract(Namespace = "")]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class ReportService
    {
        [WebGet(UriTemplate = "/CreateFileExcel/{WishId}")]
        public string GetPathExcelByWishID(string WishId)
        {
            int wid;
            if (!int.TryParse(WishId, out wid))
            {
                throw new HttpResponseException(HttpStatusCode.BadRequest);
            }

            return CreateExcel(wid);
        }

        public string GetParameters(int WishId)
        {
            List<ParameterType> ParameterList = new List<ParameterType>();

            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                string wish_report_xml = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_report_type_xml ?? "";
                DateTime wish_deadline = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_deadline;
                int? wish_attempts = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_total_attempts;

                if (!String.IsNullOrEmpty(wish_report_xml))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(wish_report_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        foreach (var p in report.Parameters)
                        {
                            ParameterList.Add(new ParameterType
                            {
                                Name = p.ParameterName,
                                Alias = p.ParameterAlias,
                                Type = p.ParameterDataType,
                                Value = (p.ParameterDataType == "startdate") || (p.ParameterDataType == "enddate") || (p.ParameterDataType == "date") ? DateTime.ParseExact(p.ParameterValue, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture).ToString() : p.ParameterValue
                            });
                        }

                        ParameterList.Add(new ParameterType
                        {
                            Name = "deadline",
                            Alias = "deadline",
                            Type = "deadline",
                            Value = wish_deadline.ToString()
                        });

                        ParameterList.Add(new ParameterType
                        {
                            Name = "attempts",
                            Alias = "attempts",
                            Type = "attempts",
                            Value = wish_attempts == null ? "-" : wish_attempts.ToString()
                        });
                    }
                }
            }
            return new JavaScriptSerializer().Serialize(ParameterList);
        }

        public string GetColumns(int WishId)
        {
            List<ColumnType> ColumnList = new List<ColumnType>();

            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                string wish_report_xml = db.Wishes.Where(x => x.wish_id == WishId).SingleOrDefault().wish_report_type_xml ?? "";

                if (!String.IsNullOrEmpty(wish_report_xml))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(wish_report_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        foreach (var c in report.Columns)
                        {
                            ColumnList.Add(new ColumnType
                            {
                                Name = c.ColumnName,
                                Type = c.ColumnType,
                                Alias = c.ColumnAlias
                            });
                        }
                    }
                }
            }
            return new JavaScriptSerializer().Serialize(ColumnList);
        }

        public string CreateExcel(int wish_id)
        {
            string url = ConfigurationManager.AppSettings.Get("url");

            Guid result = Guid.NewGuid();
            var fileName = result.ToString() + ".xlsx";
            var outputDir = HttpContext.Current.Server.MapPath("~/Temp/Shared_reports/");
            var host = HttpContext.Current.Request.Url.Scheme + "://" + HttpContext.Current.Request.Url.Authority + "/" + url + "/Temp/Shared_reports/";

            DirectoryInfo dirInfo = new DirectoryInfo(outputDir);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            };

            try
            {
                using (ReportScheduleEntities db = new ReportScheduleEntities())
                {
                    string status = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault().wish_status;

                    if (String.IsNullOrEmpty(status) || ((status != "done") && (status != "fail")))
                    {
                        return "error";
                    }

                    int task_id;

                    int count_tasks = db.Tasks.Where(x => x.task_wish_id == wish_id).Count();

                    if (count_tasks == 1)
                    {
                        task_id = db.Tasks.Where(x => x.task_wish_id == wish_id).SingleOrDefault().task_id;
                    }
                    else
                    {
                        task_id = 0;
                    }

                    var file = new FileInfo(outputDir + fileName);

                    using (var package = new ExcelPackage(file))
                    {
                        ExcelWorksheet worksheet = package.Workbook.Worksheets.Add("Отчет");

                        List<string> Place_done = new List<string>();

                        int row = 1;
                        int col;

                        //Название отчета
                        string report_type_name = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault().wish_report_type_name;

                        //Общий отчет по заданию
                        if (task_id != 0)
                        {
                            var place_id = db.Tasks.Where(x => x.task_id == task_id).SingleOrDefault().task_place_id;
                            report_type_name += "(" + db.Places.Where(x => x.place_id == place_id).SingleOrDefault().place_name_in_report + ")";
                        }

                        worksheet.Cells[row, 1].Value = report_type_name;

                        //string json = new JavaScriptSerializer().Serialize(GetParameters(wish_id).Data);
                        //string json2 = new JavaScriptSerializer().Serialize(GetColumns(wish_id).Data);

                        string json = GetParameters(wish_id);
                        string json2 = GetColumns(wish_id);

                        MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
                        MemoryStream ms2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json2));
                        ms.Position = 0;
                        ms2.Position = 0;

                        DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(List<ParameterType>));
                        List<ParameterType> ParameterList = (List<ParameterType>)ser.ReadObject(ms);

                        DataContractJsonSerializer ser2 = new DataContractJsonSerializer(typeof(List<ColumnType>));
                        List<ColumnType> ColumnList = (List<ColumnType>)ser2.ReadObject(ms2);

                        row++;

                        //Параметры отчета
                        foreach (var param in ParameterList)
                        {
                            if ((param.Name != "attempts") && (param.Name != "deadline"))
                            {
                                worksheet.Cells[row, 1].Value += param.Alias + " = " + param.Value + "; ";
                            }
                        }

                        row++;

                        col = 1;

                        //Общий отчет по заданию
                        if (task_id == 0)
                        {
                            worksheet.Cells[row, 1].Value = "Место";
                            col++;
                        }

                        foreach (var item in ColumnList)
                        {
                            worksheet.Cells[row, col].Value = item.Alias;
                            col++;
                        }

                        row++;

                        foreach (var t in db.Tasks.Where(x => x.task_wish_id == wish_id && x.task_status == "done").Where(x => (task_id != 0) ? (x.task_id == task_id) : (x.task_id == x.task_id)))
                        {
                            string report_data_xml = db.Report_data.Where(x => x.report_data_task_id == t.task_id).SingleOrDefault().report_data_xml;
                            string place_name = db.Places.Where(x => x.place_id == t.task_place_id).SingleOrDefault().place_name_in_report;
                            Place_done.Add(place_name);

                            XmlDocument xDoc = new XmlDocument();
                            xDoc.LoadXml(report_data_xml);

                            XmlElement xRoot = xDoc.DocumentElement;

                            foreach (XmlNode xnode in xRoot)
                            {
                                //Общий отчет по заданию
                                if (task_id == 0)
                                {
                                    worksheet.Cells[row, 1].Value = place_name;
                                }
                                foreach (XmlNode childnode in xnode.ChildNodes)
                                {
                                    //Есть в списке столбцов
                                    if (ColumnList.Where(x => x.Alias == childnode.Name).Count() != 0)
                                    {
                                        //Общий отчет по заданию
                                        if (task_id == 0)
                                        {
                                            col = ColumnList.FindIndex(x => x.Alias == childnode.Name) + 2;
                                        }
                                        else
                                        {
                                            col = ColumnList.FindIndex(x => x.Alias == childnode.Name) + 1;
                                        }
                                        switch (ColumnList.Where(x => x.Alias == childnode.Name).SingleOrDefault().Type)
                                        {
                                            case "integer":
                                                //worksheet.Cells[row, col].Style.Numberformat.Format = "0";
                                                int number;
                                                if (Int32.TryParse(childnode.InnerText, out number))
                                                {
                                                    worksheet.Cells[row, col].Value = number;
                                                }
                                                else
                                                {
                                                    worksheet.Cells[row, col].Value = String.Empty;
                                                }
                                                break;
                                            default:
                                                worksheet.Cells[row, col].Value = childnode.InnerText;
                                                break;
                                        }
                                    }
                                }
                                row++;

                            }
                        }


                        var TitleCell = worksheet.Cells[1, 1, 1, worksheet.Dimension.End.Column];
                        var TitleCellFont = TitleCell.Style.Font;
                        var ParamCell = worksheet.Cells[2, 1, 2, worksheet.Dimension.End.Column];
                        var ParamCellFont = ParamCell.Style.Font;
                        var HeaderCell = worksheet.Cells[3, 1, 3, worksheet.Dimension.End.Column];
                        var HeaderCellFont = HeaderCell.Style.Font;

                        var AllCell = worksheet.Cells[3, 1, worksheet.Dimension.End.Row, worksheet.Dimension.End.Column];

                        AllCell.AutoFitColumns();
                        AllCell.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        AllCell.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        AllCell.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                        AllCell.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                        TitleCellFont.Size = 16;
                        TitleCellFont.Bold = true;
                        TitleCellFont.Italic = true;

                        ParamCellFont.Size = 14;
                        ParamCellFont.Italic = true;

                        HeaderCellFont.Size = 12;
                        HeaderCellFont.Bold = true;
                        HeaderCell.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;

                        //Общий отчет по заданию
                        if (task_id == 0)
                        {
                            ExcelWorksheet worksheet2 = package.Workbook.Worksheets.Add("Места");
                            worksheet2.Cells[1, 1].Value = "Выгружены";
                            worksheet2.Cells[1, 2].Value = "Нет данных";

                            List<string> Place_error = new List<string>();

                            foreach (var t in db.Tasks.Where(x => x.task_wish_id == wish_id))
                            {
                                var place_check = db.Places.Where(x => x.place_id == t.task_place_id).SingleOrDefault().place_name_in_report;

                                if (!Place_done.Contains(place_check))
                                {
                                    Place_error.Add(place_check);
                                }
                            }

                            row = 2;
                            foreach (string s in Place_done.OrderBy(x => x))
                            {
                                worksheet2.Cells[row, 1].Value = s;
                                row++;
                            }

                            row = 2;
                            foreach (string s in Place_error.OrderBy(x => x))
                            {
                                worksheet2.Cells[row, 2].Value = s;
                                row++;
                            }

                            var HeaderPlace = worksheet2.Cells[1, 1, 1, worksheet2.Dimension.End.Column];
                            var HeaderPlaceFont = HeaderPlace.Style.Font;

                            var AllPlace = worksheet2.Cells[1, 1, worksheet2.Dimension.End.Row, worksheet2.Dimension.End.Column];

                            AllPlace.AutoFitColumns();
                            AllPlace.Style.Border.Top.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            AllPlace.Style.Border.Right.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            AllPlace.Style.Border.Bottom.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;
                            AllPlace.Style.Border.Left.Style = OfficeOpenXml.Style.ExcelBorderStyle.Thin;

                            HeaderPlaceFont.Size = 12;
                            HeaderPlaceFont.Bold = true;
                            HeaderPlace.Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Center;
                        }

                        package.Save();
                    }
                }
                return host + fileName;
            }
            catch
            {
                return "error";
            };
        }

    }
}
