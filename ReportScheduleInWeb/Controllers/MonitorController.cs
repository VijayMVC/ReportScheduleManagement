using OfficeOpenXml;
using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace ReportScheduleInWeb.Controllers
{
    public class MonitorController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Monitor
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim(), User_id = x.user_id }).ToList();

            var statuses = new[] { new SelectListItem { Value = "new", Text = "новое" },
                                   new SelectListItem { Value = "work", Text = "в работе" },
                                   new SelectListItem { Value = "wait", Text = "ожидание" },
                                   new SelectListItem { Value = "fail", Text = "завершено с ошибкой" },
                                   new SelectListItem { Value = "done", Text = "завершено" }
            };

            ViewBag.Authors = new SelectList(users, "User_id", "Fullname");
            ViewBag.Statuses = new SelectList(statuses, "Value", "Text");
            ViewBag.Wish_users = new SelectList(users, "User_id", "Fullname");

            return View();
        }

        [HttpPost]
        public JsonResult SearchWishes()
        {
            try
            {
                List<int> userRoles = (List<int>)Session["userRoles"];

                int current_user_id = Convert.ToInt32(Session["userID"]);
                int user_id = (Request.Form["search_user_id"] == "") ? 0 : Convert.ToInt32(Request.Form["search_user_id"]);
                int report_type_id = (Request.Form["search_report_type_id"] == "") ? 0 : Convert.ToInt32(Request.Form["search_report_type_id"]);
                string report_type_name = (report_type_id != 0) ? db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_name : String.Empty;
                string status = Request.Form["search_status"].Trim();
                DateTime? startdate = (Request.Form["search_startdate"] != "") ? DateTime.ParseExact(Request.Form["search_startdate"] + " 00:00:00", "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture) : (DateTime?)null;
                DateTime? enddate = (Request.Form["search_enddate"] != "") ? (DateTime.ParseExact(Request.Form["search_enddate"] + " 00:00:00", "yyyy-MM-dd hh:mm:ss", CultureInfo.InvariantCulture)).AddDays(1) : (DateTime?)null;
                List<int> places = new List<int>(Request.Form["search_places"].Split(',').Select(Int32.Parse).ToList());

                if ((user_id != 0) || (report_type_id != 0) || (!String.IsNullOrEmpty(status)) || (startdate != null) || (enddate != null) || (places[0] != 0))
                {
                    List<int> wish_places = new List<int>();
                    if (places[0] != 0)
                    {
                        wish_places = db.Tasks.Where(x => places.Contains(x.task_place_id)).Select(x => x.task_wish_id).ToList();
                    }

                    List<WishViewModel> WishList = db.Wishes.OrderByDescending(x => x.wish_createdate)
                                                    .Where(x => (user_id != 0) ? (x.wish_user_id == user_id) : (x.wish_user_id == x.wish_user_id))
                                                    .Where(x => (report_type_id != 0) ? (x.wish_report_type_name == report_type_name) : x.wish_report_type_name == x.wish_report_type_name)
                                                    .Where(x => (!String.IsNullOrEmpty(status)) ? (x.wish_status == status) : x.wish_status == x.wish_status)
                                                    .Where(x => (startdate != null) ? (x.wish_createdate >= startdate) : x.wish_createdate == x.wish_createdate)
                                                    .Where(x => (enddate != null) ? (x.wish_createdate <= enddate) : x.wish_createdate == x.wish_createdate)
                                                    .Where(x => (wish_places.Count != 0) ? (wish_places.Contains(x.wish_id)) : x.wish_id == x.wish_id)
                                                    .Select(x => new WishViewModel
                                                    {
                                                        Wish_id = x.wish_id,
                                                        Wish_createdate = x.wish_createdate,
                                                        Wish_report_type_name = x.wish_report_type_name,
                                                        User_id = x.wish_user_id,
                                                        Wish_status = x.wish_status
                                                    }).ToList();

                    List<WishViewModel> ResultList = new List<WishViewModel>();

                    //Проверяем доступы
                    foreach (var w in WishList)
                    {
                        //Суперадмин или админ
                        if (userRoles.Contains(1) || userRoles.Contains(2))
                        {
                            ResultList.Add(w);
                        }
                        else
                        {

                            Wish_report_relation wrr = db.Wish_report_relation.Where(x => x.wrr_wish_id == w.Wish_id).SingleOrDefault();

                            if (wrr != null)
                            {
                                switch (wrr.wrr_access_type)
                                {
                                    //Задание видно для всех
                                    case 0:
                                        ResultList.Add(w);
                                        break;
                                    //Для пользователей отчета
                                    case 1:
                                        if (db.Report_user_relation.Where(x => x.rur_report_type_id == wrr.wrr_report_type_id && x.rur_user_id == current_user_id).Count() != 0)
                                            ResultList.Add(w);
                                        break;
                                    //Для конкретных пользователей
                                    case 2:
                                        if (db.Wish_user_relation.Where(x => x.wur_wish_id == w.Wish_id && x.wur_user_id == current_user_id).Count() != 0)
                                            ResultList.Add(w);
                                        break;
                                }
                            }
                            else
                            {
                                ResultList.Add(w);
                            }
                        }
                    }

                    foreach (var w in ResultList)
                    {
                        w.User_name = db.Users.Where(x => x.user_id == w.User_id).Select(x => ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim()).SingleOrDefault();
                        switch (w.Wish_status)
                        {
                            case "new":
                                w.Wish_status = "новое";
                                break;
                            case "work":
                                w.Wish_status = "в работе";
                                break;
                            case "wait":
                                w.Wish_status = "ожидание";
                                break;
                            case "fail":
                                w.Wish_status = "завершено с ошибкой";
                                break;
                            case "done":
                                w.Wish_status = "завершено";
                                break;
                            default:
                                w.Wish_status = "неизвестно";
                                break;
                        }
                    }

                    return Json(ResultList, JsonRequestBehavior.AllowGet);
                }

                return Json(new List<WishViewModel>(), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public JsonResult GetParameters(int WishId)
        {
            List<ParameterType> ParameterList = new List<ParameterType>();

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
            return Json(ParameterList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAccess(int WishId)
        {
            int result = 0;
            try
            {
                short? wrr = db.Wish_report_relation.Where(x => x.wrr_wish_id == WishId).SingleOrDefault().wrr_access_type;

                result = wrr ?? -1;
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetAccessUsers(int WishId)
        {
            List<int> UserList = db.Wish_user_relation.Where(x => x.wur_wish_id == WishId).Select(x => x.wur_user_id).ToList();
            return Json(UserList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetColumns(int WishId)
        {
            List<ColumnType> ColumnList = new List<ColumnType>();

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
            return Json(ColumnList, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult GetTasks(int WishId)
        {
            List<TaskViewModel> TaskList = db.Tasks.Where(x => x.task_wish_id == WishId).Select(x => new TaskViewModel
            {
                Task_id = x.task_id,
                Task_wish_id = x.task_wish_id,
                Task_place_id = x.task_place_id,
                Task_startdate = x.task_startdate,
                Task_number_attempts = x.task_number_attempts,
                Task_last_error_text_short = x.task_last_error_text == null ? "-" : x.task_last_error_text.Length > 40 ? x.task_last_error_text.Substring(0, 35) + "..." : x.task_last_error_text,
                Task_last_error_text = x.task_last_error_text,
                Task_status = x.task_status
            }).ToList();

            foreach (var t in TaskList)
            {
                t.Task_place_name = db.Places.Where(x => x.place_id == t.Task_place_id).Select(x => x.place_name).SingleOrDefault();
                switch (t.Task_status)
                {
                    case "new":
                        t.Task_status = "новое";
                        break;
                    case "work":
                        t.Task_status = "в работе";
                        break;
                    case "wait":
                        t.Task_status = "ожидание";
                        break;
                    case "fail":
                        t.Task_status = "завершено с ошибкой";
                        break;
                    case "done":
                        t.Task_status = "завершено";
                        break;
                    default:
                        t.Task_status = "неизвестно";
                        break;
                }
            }

            return Json(TaskList.OrderBy(x => x.Task_status).ThenBy(x => x.Task_place_name), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SetAccess()
        {
            int result = 0;

            try
            {
                //Доступы к просмотру
                int[] Access_users = Array.ConvertAll(Request.Form.GetValues("access_user"), s => int.Parse(s));
                int wish_id = Convert.ToInt32(Request.Form["wish_id"]);
                int access_type = Convert.ToInt32(Request.Form["access_type"]);

                Wish_report_relation wrr = db.Wish_report_relation.Where(x => x.wrr_wish_id == wish_id).SingleOrDefault();

                if (wrr == null)
                {
                    result = 1;
                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                wrr.wrr_access_type = (short)access_type;

                //Выбранные пользователи
                if (access_type == 2)
                {
                    List<Wish_user_relation> Wur = new List<Wish_user_relation>();

                    foreach (Wish_user_relation wur in db.Wish_user_relation.Where(x => x.wur_wish_id == wish_id))
                    {
                        Wur.Add(wur);
                    }

                    foreach (Users user in db.Users)
                    {
                        if (Access_users.Contains(user.user_id))
                        {
                            if (Wur.Find(x => x.wur_user_id == user.user_id) == null)
                            {
                                db.Wish_user_relation.Add(new Wish_user_relation() { wur_user_id = user.user_id, wur_wish_id = wish_id });
                            }
                        }
                        else
                        {
                            if (Wur.Find(x => x.wur_user_id == user.user_id) != null)
                            {
                                db.Wish_user_relation.Remove(Wur.Find(x => x.wur_user_id == user.user_id));
                            }
                        }
                    }
                }
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult CreateExcel()
        {
            Guid result = Guid.NewGuid();
            try
            {
                int wish_id = (Request.Form["wish_id"] == "") ? 0 : Convert.ToInt32(Request.Form["wish_id"]);
                int task_id = (Request.Form["task_id"] == "") ? 0 : Convert.ToInt32(Request.Form["task_id"]);

                var fileName = result.ToString() + ".xlsx";
                var outputDir = Server.MapPath("~/Temp/Shared_reports/");

                DirectoryInfo dirInfo = new DirectoryInfo(outputDir);

                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                };

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

                    string json = new JavaScriptSerializer().Serialize(GetParameters(wish_id).Data);
                    string json2 = new JavaScriptSerializer().Serialize(GetColumns(wish_id).Data);

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
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public FileResult GetFile(string path)
        {
            // Путь к файлу
            string file_path = Server.MapPath("~/Temp/Shared_reports/" + path + ".xlsx");
            // Тип файла - content-type
            string file_type = "application/vnd.ms-excel";
            // Имя файла - необязательно
            string file_name = "report.xlsx";
            return File(file_path, file_type, file_name);
        }

        [HttpPost]
        public JsonResult ForceTask()
        {
            int result = -1;

            int task_id = (Request.Form["task_id"] == "") ? 0 : Convert.ToInt32(Request.Form["task_id"]);
            int wish_id = (Request.Form["wish_id"] == "") ? 0 : Convert.ToInt32(Request.Form["wish_id"]);

            if (wish_id == 0)
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            result = 0;

            string wish_status = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault().wish_status;

            if ((wish_status != "new") && (wish_status != "work") && (wish_status != "wait"))
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            Tasks task = db.Tasks.Where(x => x.task_id == task_id).SingleOrDefault();

            if ((task.task_status == "fail") || (task.task_status == "wait"))
            {
                task.task_number_attempts = 0;
                task.task_startdate = DateTime.Now;
                db.SaveChanges();
            }

            return Json(task_id, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult EndWish()
        {
            int result = 0;

            int wish_id = (Request.Form["wish_id"] == "") ? 0 : Convert.ToInt32(Request.Form["wish_id"]);

            if (wish_id == 0)
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            Wishes wish = db.Wishes.Where(x => x.wish_id == wish_id).SingleOrDefault();

            if ((wish.wish_status == "fail") || (wish.wish_status == "done"))
            {
                return Json(result, JsonRequestBehavior.AllowGet);
            }

            //Всем задачам в ожидании ставим дату и время запуска
            foreach (var t in db.Tasks.Where(x => x.task_wish_id == wish_id && x.task_status != "done" && x.task_status != "fail"))
            {
                t.task_startdate = DateTime.Now;
            }

            wish.wish_deadline = DateTime.Now;
            db.SaveChanges();

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}