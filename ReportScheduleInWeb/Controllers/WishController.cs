using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;
using EncryptStringSample;
using Newtonsoft.Json;
using ReportScheduleInWeb.Models;

namespace ReportScheduleInWeb.Controllers
{
    public class WishController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Wish
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetParameters(int ReportTypeId)
        {
            List<ParameterType> ParameterList = new List<ParameterType>();

            string report_xml = db.Report_types.Where(x => x.report_type_id == ReportTypeId).SingleOrDefault().report_type_xml ?? "";

            if (!String.IsNullOrEmpty(report_xml))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                using (TextReader reader = new StringReader(report_xml))
                {
                    ReportModel report = (ReportModel)serializer.Deserialize(reader);

                    foreach (var p in report.Parameters)
                    {
                        ParameterList.Add(new ParameterType
                        {
                            Name = p.ParameterName,
                            Alias = p.ParameterAlias,
                            Type = p.ParameterDataType
                        });
                    }
                }
            }
            return Json(ParameterList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetPlacesByPlaceTypeId(int PlaceTypeId)
        {
            List<int> PlaceList = db.Place_type_relation.Where(x => x.ptr_place_type_id == PlaceTypeId).Select(x => x.ptr_place_id).ToList();

            if (PlaceTypeId == 0)
            {
                List<int> EmptyList = new List<int>();
                return Json(EmptyList, JsonRequestBehavior.AllowGet);
            }
            return Json(PlaceList, JsonRequestBehavior.AllowGet);
        }

        public class parameters
        {
            public string name { get; set; }
            public string value { get; set; }
        }

        public class places
        {
            public int id { get; set; }
            public string startdate { get; set; }
        }

        [HttpPost]
        public JsonResult AddWish()
        {
            var result = 0;
            try
            {
                //Вытаскиваем данные из запроса
                int report_type_id = Convert.ToInt32(Request.Form["report_type_id"]);
                int user_id = Convert.ToInt32(Request.Form["user_id"]);
                string report_type_name = db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_name;
                DateTime deadlineValue = DateTime.ParseExact(Request.Form["deadlineValue"].ToString(), "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                int? attemptsCount = Request.Form["attemptsCount"].ToString() != "null" ? Convert.ToInt32(Request.Form["attemptsCount"].ToString()) : (int?)null;
                List<parameters> ParamList = JsonConvert.DeserializeObject<List<parameters>>(Request.Form["parameters"].ToString());
                List<places> PlaceList = JsonConvert.DeserializeObject<List<places>>(Request.Form["places"].ToString());

                string wish_report_type_xml = String.Empty;

                string report_type_xml = db.Report_types.Where(x => x.report_type_id == report_type_id).SingleOrDefault().report_type_xml ?? "";

                List<ParameterType> ParameterList = new List<ParameterType>();

                if (!String.IsNullOrEmpty(report_type_xml))
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(ReportModel));

                    using (TextReader reader = new StringReader(report_type_xml))
                    {
                        ReportModel report = (ReportModel)serializer.Deserialize(reader);

                        foreach (var p in report.Parameters)
                        {
                            ParameterList.Add(new ParameterType
                            {
                                Name = p.ParameterName,
                                Alias = p.ParameterAlias,
                                Value = 
                                    (p.ParameterDataType == "startdate") || (p.ParameterDataType == "enddate")
                                    ? DateTime.ParseExact(ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss") 
                                    : ParamList.Where(x => x.name == p.ParameterName).SingleOrDefault().value
                            });
                        }

                        wish_report_type_xml = "<?xml version=\"1.0\" encoding=\"utf-16\"?>";
                        wish_report_type_xml += "<TableDataSource SelectCommand=\"" + SecurityElement.Escape(report.SelectCommand) + "\">";
                        foreach (var c in report.Columns)
                        {
                            wish_report_type_xml += "<Column Name=\"" + c.ColumnName + "\" Alias=\"" + c.ColumnAlias + "\" />";
                        }
                        if (ParamList.Count != 0)
                        {
                            foreach (var p in ParameterList)
                            {
                                wish_report_type_xml += "<CommandParameter Name=\"" + p.Name + "\" Alias=\"" + p.Alias + "\" Value=\"" + p.Value + "\" />";
                            }
                        }
                        wish_report_type_xml += "</TableDataSource>";
                    }
                }
                else
                {
                    result = 1;
                }



                Wishes Wish = new Wishes();
                Wish.wish_createdate = Convert.ToDateTime(DateTime.Now);
                Wish.wish_deadline = deadlineValue;
                Wish.wish_total_attempts = attemptsCount;
                Wish.wish_report_type_name = report_type_name;
                Wish.wish_status = "not_ready";
                Wish.wish_report_type_xml = wish_report_type_xml;
                Wish.wish_user_id = user_id;
                db.Wishes.Add(Wish);
                db.SaveChanges();

                int wish_id = Wish.wish_id;

                foreach (var p in PlaceList)
                {
                    Tasks Task = new Tasks();
                    Task.task_startdate = DateTime.ParseExact(p.startdate, "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture); ;
                    Task.task_wish_id = wish_id;
                    Task.task_place_id = p.id;
                    Task.task_number_attempts = null;
                    Task.task_status = "new";
                    Task.task_last_error_text = null;
                    db.Tasks.Add(Task);
                }
                Wish.wish_status = "new";
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}