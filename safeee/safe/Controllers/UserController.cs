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
    public class UserController : Controller
    {
        //Regestration Action
        [HttpGet]
        public ActionResult Registration()
        {
            return View();
        }
        //Regestration Post action
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Registration([Bind(Exclude = "IsEmailVerified,ActivationCode")]User user)
        {
            bool Status = false;
            string message = "";
            //
            // Model Validation
            if (ModelState.IsValid)
            {
                #region//Email is already Exist
                var isExist = IsEmailExist(user.EmailID);
                if(isExist)
                {
                    ModelState.AddModelError("EmailExist", "Email alredy exist");
                    return View(user);
                }
                #endregion

                #region Generate Activation Code
                user.ActivationCode = Guid.NewGuid();
                #endregion
                #region Password Hash
                user.Password = Crypto.Hash(user.Password);
                user.ConfirmPassword = Crypto.Hash(user.ConfirmPassword);//
                #endregion
                user.IsEmailVerified = false;

                #region
                using(MyDatabaseEntities dc = new MyDatabaseEntities())
                {
                    dc.Users.Add(user);
                    dc.SaveChanges();
                    //Send Email to User
                    SendVerificationLinkEmail(user.EmailID, user.ActivationCode.ToString());
                    message = "Registration Successfuly" + "has been sent:" + user.EmailID;
                    Status = true;

                }
                #endregion
            }
            else
            {
                message = "Invalid Request";
            }

            ViewBag.Message = message;
            ViewBag.Status = Status;
            
            return View(user);
        }

        //Verfy Email
        [HttpGet]
         public ActionResult VerifyAccount(string id)
        {
            bool Status = false;
            using(MyDatabaseEntities dc = new MyDatabaseEntities())
            {
                dc.Configuration.ValidateOnSaveEnabled = false;
                var v = dc.Users.Where(a => a.ActivationCode == new Guid(id)).FirstOrDefault();
                if(v != null)
                {
                    v.IsEmailVerified = true;
                    dc.SaveChanges();
                    Status = true;
                }
                else
                {
                    ViewBag.Message = "Invalid Request";
                }
            }
            ViewBag.Status = Status;
            return View();

        }

        //Login
        [HttpGet]
        public ActionResult Login()
        {
            return View();
        }
        //Login POst
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(UserLogin login,string ReturnUrl="")
        {
            string mesage = "";
            using(MyDatabaseEntities dc = new MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID == login.EmailID).FirstOrDefault();
                if(v != null)
                {
                    if(string.Compare(Crypto.Hash(login.Password),v.Password)==0)
                        {
                        int timeout = login.RemeberMe ? 52600 : 20;
                        var ticket = new FormsAuthenticationTicket(login.EmailID, login.RemeberMe, timeout);
                        string encrypted = FormsAuthentication.Encrypt(ticket);
                        var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encrypted);
                        cookie.Expires = DateTime.Now.AddMinutes(timeout);
                        cookie.HttpOnly = true;
                        Response.Cookies.Add(cookie);

                        if(Url.IsLocalUrl(ReturnUrl))
                            {
                            return Redirect(ReturnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }
                    else
                    {
                        mesage = "Invalid credential provided";
                    }

                }
                else
                {
                    mesage = "Invalid credential provided";
                }
            }
            ViewBag.Message = mesage;
            return View();
;        }
        //Logout


        [Authorize]
        [HttpPost]
        public ActionResult Logout()
        {

            FormsAuthentication.SignOut();
            return RedirectToAction("Login", "User");
        }
        public bool IsEmailExist(string emailID)
        {           
            using(MyDatabaseEntities dc = new  MyDatabaseEntities())
            {
                var v = dc.Users.Where(a => a.EmailID == emailID).FirstOrDefault();
                return v != null;
            }

        }
        [NonAction]
        public void SendVerificationLinkEmail(string emailID, string activationCode, string emailFor="VerifyAccount")
        {
            var verifyUrl = "/User/"+emailFor+"/" + activationCode;
            var link = Request.Url.AbsoluteUri.Replace(Request.Url.PathAndQuery, verifyUrl);
            var fromEmail = new MailAddress("mohsinshaikh3421@gmail.com");
            var toEmail = new MailAddress(emailID);
            var fromEmailPassword = "shaikh3421@";

            string subject = "";
            string body = "";

            if(emailFor== "VerifyAccont")
            {
                subject = "your account created";
                 body = "<br/><br/>we are excited to tell your account" +
                    "successfully created.please click " + "<a href='" + link + "'>" + link + "</a>";
            }
           else if(emailFor == "ResetPassword")
            {
                subject = "ResetPassword";
                body = "Hi<br/><br/>We got request for reset account password.Please click on the link to reset password" + "<br/><br/><a href="+link+">Reset Password link</a>";
            }
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
    
         //Part 3- Forgot Password

        public ActionResult ForgotPasswor()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ForgotPasswor(string EmailID)
        {
            //verfy Email Id
            //Generate Reset password link
            //send Email
            string message = "";
            bool status = false;

            using(MyDatabaseEntities dc = new MyDatabaseEntities())
            {
                var account = dc.Users.Where(a => a.EmailID == EmailID).FirstOrDefault();
                if(account != null)
                {
                    //send email for reset password
                    string resetCode = Guid.NewGuid().ToString();
                    SendVerificationLinkEmail(account.EmailID, resetCode, "ResetPassword");
                    account.ResetPasswordCode = resetCode;
                    //confirm password
                    dc.Configuration.ValidateOnSaveEnabled = false;
                    dc.SaveChanges();
                    message = "reset password";

                }
                else
                {
                    message = "Account not fond";

                }
            }
            ViewBag.Message = message;
            return View();
        }

         public ActionResult ResetPassword(string id)
        {
            //veriffy the resetPassword link
            //find account associated
            //redirect to reset password page
            using(MyDatabaseEntities dc = new MyDatabaseEntities())
            {
                var user = dc.Users.Where(a => a.ResetPasswordCode == id).FirstOrDefault();
                if(user != null)
                {
                    ResetPasswordModels model = new ResetPasswordModels();
                    model.ResetCode = id;
                    return View(model);
                }
                else
                {
                    return HttpNotFound();
                }
            }
        }
         
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(ResetPasswordModels model)
        {
            var message = "";
            if(ModelState.IsValid)
            {
                using(MyDatabaseEntities dc = new MyDatabaseEntities())
                    {
                    var user = dc.Users.Where(a => a.ResetPasswordCode == model.ResetCode).FirstOrDefault();
                    if(user != null)
                    {
                        user.Password = Crypto.Hash(model.NewPassword);
                        user.ResetPasswordCode = "";
                        dc.Configuration.ValidateOnSaveEnabled = false;
                        dc.SaveChanges();
                        message = "New password update succesfuly";
                    }
                }
            }
            else
            {
                message = "Something invalid";
            }
            ViewBag.Message = message;
            return View(model);
        }
    }

  
}