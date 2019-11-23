using Newtonsoft.Json;
using ReportScheduleInWeb.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
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
                        result = 0;
                        return Json(result, JsonRequestBehavior.AllowGet);
                    }
                }

                if ((model.user_id > 0) && (model.user_password != null) && (model.user_password.Trim() != ""))
                {
                    Users Use = db.Users.SingleOrDefault(x => x.user_id == model.user_id);
                    Use.user_password = encryption(model.user_password);
                    db.SaveChanges();
                    result = 1;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }

    }
}