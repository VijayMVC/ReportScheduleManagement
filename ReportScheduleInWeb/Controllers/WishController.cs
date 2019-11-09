using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Serialization;
using EncryptStringSample;
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

    }
}