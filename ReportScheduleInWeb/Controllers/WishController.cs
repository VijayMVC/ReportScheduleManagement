using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
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
        public JsonResult GetParameterList()
        {
            List<WishViewModel> WishList = db.Wishes.Select(x => new WishViewModel
            {
                wish_id

                user_id = x.user_id,
                user_login = x.user_login,
                user_password = x.user_password,
                user_isdeleted = x.user_isdeleted,
                user_surname = x.user_surname,
                user_name = x.user_name,
                user_patronymic = x.user_patronymic,
                user_role_id = x.user_role_id,
                user_fil_id = x.user_fil_id,
                filial = x.user_fil_id == null ? "" : db.filials.FirstOrDefault(f => f.filial_id == x.user_fil_id).filial_name
            }).OrderBy(x => x.user_surname).ThenBy(x => x.user_name).ThenBy(x => x.user_patronymic).ToList();

            return Json(WishList, JsonRequestBehavior.AllowGet);
        }

    }
}