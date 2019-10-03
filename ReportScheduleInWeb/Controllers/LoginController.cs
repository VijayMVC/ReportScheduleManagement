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
    public class LoginController : Controller
    {
        // GET: Login
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

        [HttpPost]
        public ActionResult Authorize(LoginViewModel userModel)
        {
            using (ReportScheduleEntities db = new ReportScheduleEntities())
            {
                if ((userModel.user_password == null) || (userModel.user_password.Trim() == ""))
                {
                    return View("Index", userModel);
                }
                string pass = encryption(userModel.user_password);
                var userDetails = db.Users.Where(x => x.user_login == userModel.user_login && x.user_password == pass).FirstOrDefault();
                if (userDetails != null && userDetails.user_isdeleted == true)
                {
                    userModel.LoginErrorMessage = "Данный пользователь удалён";
                    return View("Index", userModel);
                }
                if (userDetails == null)
                {
                    userModel.LoginErrorMessage = "Неправильные логин или пароль";
                    return View("Index", userModel);
                }
                else
                {
                    Session["userID"] = userDetails.user_id;
                    Session["userSurname"] = userDetails.user_surname;
                    Session["userName"] = userDetails.user_name;

                    return RedirectToAction("Index", "Home");
                }
            }
        }
    }
}