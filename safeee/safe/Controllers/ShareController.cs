using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using safe.Models;
using System.Net.Mail;
using System.Net;
using System.Web.Security;

namespace safe.Controllers
{
    public class ShareController : Controller
    {
        // GET: Share
        public ActionResult Index()
        {
            return View();
        }

        public JsonResult getLocation(string  latitude, string longtitude)
        {
            List<object> location = new List<object>();
            location.Add(latitude);
            location.Add(longtitude);
            sendData("",latitude, longtitude, "");
            return Json(location);
        }

        public JsonResult imageCapture(string img)
        {
            string Data = string.Empty;
            sendData("","","",img);
            return Json(Data);
        }

        public void sendData(string emailID,string lon,string lat,string img)
        {
            var verifyUrl = "/User/";
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);
            var fromEmail = new MailAddress("nomansk7744@gmail.com");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "puydcjflwilocbws";

            string subject = "Send data";
            string body = "<br/><br/>we are excited to tell your account" +
                        "successfully created.please click " +"image:"+ img +"location:"+ lat+lon ;

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };
            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
                smtp.Send(message);
        }
    }
    
    }