using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace ReportScheduleInWeb.Controllers
{
    public class ReportController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: Report
        public ActionResult Index()
        {
            var users = db.Users.Select(x => new { Fullname = ((x.user_surname ?? "") + " " + (x.user_name ?? "") + " " + (x.user_patronymic ?? "")).Trim(), User_id = x.user_id }).ToList();

            ViewBag.Authors = new SelectList(users, "User_id", "Fullname");
            return View();
        }

        public JsonResult GetPlacesByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Places.Select(x => x.place_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> PlaceList = db.Report_place_relation.Where(x => x.rpr_report_type_id == ReportTypeId).Select(x => x.rpr_place_id).ToList();
                    return Json(PlaceList, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetGroupsByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Report_groups.Select(x => x.report_group_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> GroupList = db.Report_group_relation.Where(x => x.rgr_report_id == ReportTypeId).Select(x => x.rgr_report_group_id).ToList();
                    return Json(GroupList, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetUsersByReportTypeId(int ReportTypeId)
        {
            switch (ReportTypeId)
            {
                case -1:
                    List<int> EmptyList = new List<int>();
                    return Json(EmptyList, JsonRequestBehavior.AllowGet);
                case 0:
                    List<int> AllList = db.Users.Select(x => x.user_id).ToList();
                    return Json(AllList, JsonRequestBehavior.AllowGet);
                default:
                    List<int> UserList = db.Report_user_relation.Where(x => x.rur_report_type_id == ReportTypeId).Select(x => x.rur_user_id).ToList();
                    return Json(UserList, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult SaveReport()
        {
            var result = 0;
            try
            {
                //Вытаскиваем данные из запроса
                int? report_type_id = Convert.ToInt32(Request.Form["search_report_type_id"]);
                List<int> GroupList = Request.Form["search_groups"].Split(',').Select(Int32.Parse).ToList();
                List<int> PlaceList = Request.Form["search_places"].Split(',').Select(Int32.Parse).ToList();
                List<int> UserList = Request.Form["search_users"].Split(',').Select(Int32.Parse).ToList();

                if ((report_type_id == null) || (report_type_id == 0))
                {
                    result = 1;
                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                //Отчет-группы ДО редактирования
                List<Report_group_relation> Rgr = new List<Report_group_relation>();
                foreach (Report_group_relation rgr in db.Report_group_relation.Where(x => x.rgr_report_id == report_type_id))
                {
                    Rgr.Add(rgr);
                }

                foreach (Report_groups rg in db.Report_groups)
                {
                    if (GroupList.Contains(rg.report_group_id))
                    {
                        if (Rgr.Find(x => x.rgr_report_group_id == rg.report_group_id) == null)
                        {
                            db.Report_group_relation.Add(new Report_group_relation() { rgr_report_id = (int)report_type_id, rgr_report_group_id = rg.report_group_id });
                        }
                    }
                    else
                    {
                        if (Rgr.Find(x => x.rgr_report_group_id == rg.report_group_id) != null)
                        {
                            db.Report_group_relation.Remove(Rgr.Find(x => x.rgr_report_group_id == rg.report_group_id));
                        }
                    }
                }

                //Отчет-места ДО редактирования
                List<Report_place_relation> Rpr = new List<Report_place_relation>();
                foreach (Report_place_relation rpr in db.Report_place_relation.Where(x => x.rpr_report_type_id == report_type_id))
                {
                    Rpr.Add(rpr);
                }

                foreach (Places p in db.Places)
                {
                    if (PlaceList.Contains(p.place_id))
                    {
                        if (Rpr.Find(x => x.rpr_place_id == p.place_id) == null)
                        {
                            db.Report_place_relation.Add(new Report_place_relation() { rpr_report_type_id = (int)report_type_id, rpr_place_id = p.place_id });
                        }
                    }
                    else
                    {
                        if (Rpr.Find(x => x.rpr_place_id == p.place_id) != null)
                        {
                            db.Report_place_relation.Remove(Rpr.Find(x => x.rpr_place_id == p.place_id));
                        }
                    }
                }

                //Отчет-места ДО редактирования
                List<Report_user_relation> Rur = new List<Report_user_relation>();
                foreach (Report_user_relation rur in db.Report_user_relation.Where(x => x.rur_report_type_id == report_type_id))
                {
                    Rur.Add(rur);
                }

                foreach (Users u in db.Users)
                {
                    if (UserList.Contains(u.user_id))
                    {
                        if (Rur.Find(x => x.rur_user_id == u.user_id) == null)
                        {
                            db.Report_user_relation.Add(new Report_user_relation() { rur_report_type_id = (int)report_type_id, rur_user_id = u.user_id });
                        }
                    }
                    else
                    {
                        if (Rur.Find(x => x.rur_user_id == u.user_id) != null)
                        {
                            db.Report_user_relation.Remove(Rur.Find(x => x.rur_user_id == u.user_id));
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

    }
}