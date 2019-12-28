using Newtonsoft.Json;
using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace ReportScheduleInWeb.Controllers
{
    public class UserController : Controller
    {
        private ReportScheduleEntities db = new ReportScheduleEntities();

        // GET: User
        public ActionResult Index()
        {
            return View();
        }

        public string encryption(String password)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] originalBytes = ASCIIEncoding.Default.GetBytes(password);
            byte[] encodedBytes = md5.ComputeHash(originalBytes);

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < encodedBytes.Length; i++)
                sb.Append(encodedBytes[i].ToString("x2").ToLower());

            return sb.ToString();
        }

        public JsonResult GetUserList()
        {
            List<UserViewModel> UserList = db.Users.Select(x => new UserViewModel
            {
                user_id = x.user_id,
                user_login = x.user_login,
                user_surname = x.user_surname,
                user_name = x.user_name,
                user_patronymic = x.user_patronymic,
                user_isdeleted = x.user_isdeleted,
                user_email = x.user_email,
                user_FIO = (!String.IsNullOrEmpty(x.user_surname) ? x.user_surname : "") + " " + (!String.IsNullOrEmpty(x.user_name) ? x.user_name : "") + " " + (!String.IsNullOrEmpty(x.user_patronymic) ? x.user_patronymic : ""),
                changeable = 1
            }).OrderBy(x => x.user_surname).ThenBy(x => x.user_name).ThenBy(x => x.user_patronymic).ToList();

            foreach (var user in UserList)
            {
                List<string> roles = (from r in db.Roles
                                      join ur in db.User_roles on r.role_id equals ur.userrole_role_id
                                      where ur.userrole_user_id == user.user_id
                                      orderby r.role_id
                                      select r.role_name).ToList<string>();
                user.user_roles = String.Join(", ", roles.ToArray());
            }

            return Json(UserList, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetUserById(int UserId)
        {
            var Usermodel = new UserViewModel();
            if (UserId == 0)
            {
                UserId = Convert.ToInt32(Session["userID"]);
            }
            Users model = db.Users.Where(x => x.user_id == UserId).SingleOrDefault();

            Usermodel.user_id = model.user_id;
            Usermodel.user_login = model.user_login;
            Usermodel.user_password = model.user_password;
            Usermodel.user_isdeleted = model.user_isdeleted;
            Usermodel.user_surname = model.user_surname;
            Usermodel.user_name = model.user_name;
            Usermodel.user_patronymic = model.user_patronymic;
            Usermodel.user_email = model.user_email;

            Usermodel.roles = (from r in db.Roles
                               join ur in db.User_roles on r.role_id equals ur.userrole_role_id
                               where ur.userrole_user_id == UserId
                               orderby r.role_id
                               select r.role_id).ToList<int>();

            Usermodel.report_types = (from rt in db.Report_types
                                      join rur in db.Report_user_relation on rt.report_type_id equals rur.rur_report_type_id
                                      where rur.rur_user_id == UserId
                                      orderby rt.report_type_name
                                      select rt.report_type_id).ToList<int>();

            string value = string.Empty;
            value = JsonConvert.SerializeObject(Usermodel, Formatting.Indented, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            return Json(value, JsonRequestBehavior.AllowGet);
        }

        public JsonResult ChangePassword(UserViewModel model)
        {
            var result = 0;
            try
            {
                //Пользователь меняет свой пароль - проверяем ввод старого пароля
                if (model.user_id == Convert.ToInt32(Session["userID"]))
                {
                    Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                    if (Use.user_password != encryption(model.user_password_old))
                    {
                        result = 1;
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                }

                if ((model.user_id > 0) && (model.user_password != null) && (model.user_password.Trim() != ""))
                {
                    Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                    Use.user_password = encryption(model.user_password);
                    db.SaveChanges();
                }
                else
                {
                    result = 2;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}