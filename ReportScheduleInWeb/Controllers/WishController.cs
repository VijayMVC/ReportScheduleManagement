using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                DateTime deadlineValue = DateTime.ParseExact(Request.Form["deadlineValue"].ToString(), "yyyy-MM-ddTHH:mm", System.Globalization.CultureInfo.InvariantCulture);
                int attemptsCount = Request.Form["attemptsCount"].ToString() == "null" ? 0 : Convert.ToInt32(Request.Form["attemptsCount"]);
                List<parameters> ParamList = JsonConvert.DeserializeObject<List<parameters>>(Request.Form["parameters"].ToString());
                List<places> PlaceList = JsonConvert.DeserializeObject<List<places>>(Request.Form["places"].ToString());
                //var places = [];
                //var seldep = Request.Form["departments"].Split(',').Select(Int32.Parse).ToList();

                ////Редактирование пользователя
                //if (model.user_id > 0)
                //{
                //    if (!CheckUserLogin(model.user_login, model.user_id))
                //    {
                //        user Use = db.users.SingleOrDefault(x => x.user_id == model.user_id);
                //        Use.user_login = model.user_login;
                //        Use.user_surname = model.user_surname;
                //        Use.user_name = model.user_name;
                //        Use.user_patronymic = model.user_patronymic;
                //        Use.user_isdeleted = model.user_isdeleted;
                //        Use.user_pos_id = model.pos_id;
                //        Use.user_role_id = model.role_id;

                //        //Отделы/ТОСП пользователя ДО редактирования
                //        List<department_users_relation> Dur = new List<department_users_relation>();
                //        foreach (department_users_relation dur in db.department_users_relation.Where(x => x.dur_user_id == model.user_id))
                //        {
                //            Dur.Add(dur);
                //        }

                //        foreach (department d in db.departments)
                //        {
                //            if (seldep.Contains(d.dep_id))
                //            {
                //                if (Dur.Find(x => x.dur_department_id == d.dep_id) == null)
                //                {
                //                    db.department_users_relation.Add(new department_users_relation() { dur_user_id = model.user_id, dur_department_id = d.dep_id });
                //                }
                //            }
                //            else
                //            {
                //                if (Dur.Find(x => x.dur_department_id == d.dep_id) != null)
                //                {
                //                    db.department_users_relation.Remove(Dur.Find(x => x.dur_department_id == d.dep_id));
                //                }
                //            }
                //        }
                //        db.SaveChanges();
                //    }
                //    else
                //    {
                //        result = 1;
                //    }
                //}
                ////Создание нового пользователя
                //else
                //{
                //    if (!CheckUserLogin(model.user_login, 0))
                //    {
                //        user Use = new user();
                //        Use.user_login = model.user_login;
                //        Use.user_password = encryption(model.user_password);
                //        Use.user_surname = model.user_surname;
                //        Use.user_name = model.user_name;
                //        Use.user_patronymic = model.user_patronymic;
                //        Use.user_isdeleted = model.user_isdeleted;
                //        Use.user_pos_id = model.pos_id;
                //        Use.user_role_id = model.role_id;
                //        db.users.Add(Use);
                //        db.SaveChanges();

                //        foreach (department d in db.departments)
                //        {
                //            if (seldep.Contains(d.dep_id))
                //            {
                //                db.department_users_relation.Add(new department_users_relation() { dur_user_id = Use.user_id, dur_department_id = d.dep_id });
                //            }
                //        }

                //        db.SaveChanges();
                //    }
                //    else
                //    {
                //        result = 1;
                //    }
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}