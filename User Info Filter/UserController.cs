using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using ScheduleUsers.Models;
using ScheduleUsers.ViewModels;

namespace ScheduleUsers.Areas.Employer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UserController : Controller
    {
        //Generate User's first and last name in navigation bar
        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (User != null)
            {
                var context = new ApplicationDbContext();
                var username = User.Identity.Name;

                if (!string.IsNullOrEmpty(username))
                {
                    var user = context.Users.SingleOrDefault(u => u.UserName == username);
                    string fullName = string.Concat(new string[] { user.FirstName, " ", user.LastName });
                    ViewData.Add("FullName", fullName);
                }
            }
            base.OnActionExecuted(filterContext);
        }

        ApplicationDbContext db = new ApplicationDbContext();
        private ApplicationSignInManager _signInManager;
        private Applicationdbcontext _userManager;

        public UserController()
        {
        }

        public UserController(Applicationdbcontext userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set
            {
                _signInManager = value;
            }
        }

        public Applicationdbcontext UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<Applicationdbcontext>();
            }
            private set
            {
                _userManager = value;
            }
        }
        // GET: Employer/User
        [HttpGet]
        public ActionResult Index()
        {

            var positions = db.Users.Select(y => y.Position).Distinct().ToList();
            var departments = db.Users.Select(z => z.Department).Distinct().ToList();

            var p = positions.Select((r, index) => new SelectListItem { Text = r, Value = index.ToString() });
            var d = departments.Select((r, index) => new SelectListItem { Text = r, Value = index.ToString() });

            ViewBag.Positions = p;
            ViewBag.Departments = d;
            return View();
        }

        public PartialViewResult IndexUserList(string filter)
        {
            if (filter== null||filter=="")
            {
                return PartialView(db.Users.ToList());
            }
            var unfiltered = db.Users.ToList();
            var filtered = new List<ApplicationUser>();
            var clockedIn = new List<ApplicationUser>();
            var clockedOut = new List<ApplicationUser>();
            foreach (var user in unfiltered)
            {
                var status = user.GetStatus();
                if (status == "Clocked In")
                {
                    clockedIn.Add(user);
                }
                else if (status == "Clocked Out")
                {
                    clockedOut.Add(user);
                }
            }
            
            if (filter == "FullTime")
            {
                var fulltime = db.Users.Where(x => x.Fulltime == true).ToList();
                filtered = fulltime;
            }
            else if (filter=="PartTime")
            {
                var parttime = db.Users.Where(x => x.Fulltime == false).ToList();
                filtered = parttime;
            }
            else if (filter=="ClockedIn")
            {
                filtered = clockedIn;
            }
            else if (filter=="ClockedOut")
            {
                filtered = clockedOut;
            }
            else if (filter.Substring(0,3)=="dpt")
            {
                var department = db.Users.Where(x => x.Department == filter.Substring(4)).ToList();
                filtered = department;
            }
            else if (filter.Substring(0,3)=="pos")
            {
                var position = db.Users.Where(x => x.Position == filter.Substring(4)).ToList();
                filtered = position;
            }

            return PartialView("IndexUserList", filtered);
        }

        public ActionResult ViewUserDetails(string id)
        {
            if (id != null)
            {
                try
                {
                    var user = db.Users.Where(w => w.Id == id).First();
                    var userRole = UserManager.GetRoles(id).First();
                    UserDetailsViewModel userDetails = new UserDetailsViewModel(user, userRole);

                    return PartialView("_ViewUserDetails", userDetails);
                }
                catch (Exception e)
                {

                    return Content("No Employee was found. If this is an error, contact your administrator");
                }
            }
            return Content("No Employee was found. If this is an error, contact your administrator");
        }

        //Count of Pending Requests
        public ActionResult RequestCount()
        {

            var count = db.TimeOffEvents.Where(x => x.ApproverId == null).Count();

            if (count == 0)
            {
                ViewBag.Count = "Time Off Requests";
                return PartialView("_RequestCount");
            }
            else if (count >= 0)
            {
                ViewBag.Count = "Time Off Requests | " + count.ToString();
                return PartialView("_RequestCount");
            }
            
            else
            {
                //count should never anything besides a positive integer or 0
                return Content("Sorry there was an error");
            }
        }

        // GET: /Employer/User/Register
        [Authorize(Roles = "Admin")]
        public ActionResult Register()
        {
            ViewBag.Name = new SelectList(db.Roles.ToList(), "", "Name");

            return View();
        }

        // POST: /Employer/User/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model, string submit)
        {
            ViewBag.Name = new SelectList(db.Roles.ToList(), "", "Name");
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    PhoneNumber = model.PhoneNumber,
                    Department = model.Department,
                    Position = model.Position,
                    Address = model.Address,
                    HireDate = model.HireDate,
                    HourlyPayRate = model.HourlyPayRate,
                    Fulltime = model.Fulltime,
                    BirthDate = model.BirthDate
                };
                var result = await UserManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    var addRole = UserManager.AddToRole(user.Id, model.UserRoles);
                    switch (submit)
                    {
                        //if 'Register' button is clicked
                        //load index of users after user is registered.
                        case "Register":
                            return RedirectToAction("Index");
                        // if 'Add User and Create Schedule' is clicked
                        //load schedule create view
                        case "Save User and Add Schedule":
                            return RedirectToAction("Create", "Schedule", new { id = user.Id });
                    }
                }
                AddErrors(result);
            }
            // If we got this far, something failed, redisplay form
            return View();
        }

        [HttpGet]
        public ActionResult Edit(string Id)
        {

            ApplicationUser dataUser = db.Users.Where(x => x.Id == Id).First();

            RegisterViewModel rv = new RegisterViewModel();
            rv.PhoneNumber = dataUser.PhoneNumber;
            rv.FirstName = dataUser.FirstName;
            rv.LastName = dataUser.LastName;
            rv.UserName = dataUser.UserName;
            rv.Department = dataUser.Department;
            rv.Position = dataUser.Position;
            rv.Address = dataUser.Address;
            rv.HourlyPayRate = dataUser.HourlyPayRate;
            rv.HireDate = dataUser.HireDate.Value;
            rv.Email = dataUser.Email;
            rv.Fulltime = dataUser.Fulltime;
            rv.CurrentRole = dataUser.Roles.First().RoleId;

            ViewBag.Roles = new SelectList(db.Roles.ToList(), "Id", "Name");


            if (dataUser == null)
            {
                return HttpNotFound();
            }

            return View("Edit", rv);

        }

        // POST: Update/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost, ActionName("Edit")]
        //[ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public ActionResult EditPost(string Id)
        {
            var userToUpdate = db.Users.Find(Id);
            //We will convert this to a bool on the frontend
            string success;

            if (TryUpdateModel(userToUpdate, "", new string[] { "FirstName", "LastName", "UserName", "PhoneNumber", "Department", "Position", "Address", "HourlyPayRate", "Email", "HireDate", "Fulltime" }))
            {
                try
                {
                    db.SaveChanges();
                    success = "true";
                    return Json(success);
                }
                catch (Exception)
                {
                    //ModelState.AddModelError("", "Unable to save changes.");
                    success = "false";
                    return Json(success);
                }
            }
            return View();
        }

        public ActionResult Delete(string Id)
        {
            ApplicationUser user = db.Users.Find(Id);
            string userRole = UserManager.GetRoles(Id).First();
            ViewBag.userFullname = user.FirstName + " " + user.LastName;
            ViewBag.userId = user.Id;

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ChangeRole(string userId, string currentRole, string roleToChangeTo)
        {
            if(roleToChangeTo == null)
            {
                roleToChangeTo = "User";
            }
            IdentityResult removeResult = await UserManager.RemoveFromRoleAsync(userId, currentRole);
            IdentityResult addResult = await UserManager.AddToRoleAsync(userId, roleToChangeTo);


            if(removeResult.Succeeded && addResult.Succeeded)
            {
                return Content("Success");
            } else
            {
                return Content("Failure");
            }
        }

        //Post /User/Delete
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(LoginViewModel model, string userToDeleteId, string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            //If email contains an @ symbol 
            if (model.Email.IndexOf("@") > -1)
            {
                //Validate email with regex
                string regexString = @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" +
                               @"\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" +
                                  @".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$";
                Regex regex = new Regex(regexString);
                if (!regex.IsMatch(model.Email))
                {
                    ModelState.AddModelError("Email", "Email is not valid!");
                }
            }
            else
            {
                //Validate Username with regex
                string regexString = @"^[a-zA-Z0-9]*$";
                Regex regex = new Regex(regexString);
                if (!regex.IsMatch(model.Email))
                {
                    ModelState.AddModelError("Email", "Username is not valid!");
                }
            }
            
            
            if (ModelState.IsValid)
            {
                ApplicationUser user = await db.Users.Where(u => u.UserName == model.Email).FirstOrDefaultAsync();
                PasswordHasher ph = new PasswordHasher();
                var result = ph.VerifyHashedPassword(user.PasswordHash, model.Password);

                if(result != PasswordVerificationResult.Success)
                {
                    ModelState.AddModelError("", "There was a problem with the password or username, please try again or contact you system administrator if the problem continues.");
                    return Content("There was a problem with the password or username, please try again or contact you system administrator if the problem continues. You can use the back arrow to return to the previous page.");
                }

                var userToDelete = await db.Users.Where(u => u.Id == userToDeleteId)
                            .Include(u => u.Schedules)
                            .Include(u => u.RequestedTimeOff)
                            .Include(u => u.WorkEvents)
                            .Include(u => u.SenderMessages)
                            .Include(u => u.RecipientMessages)
                            .FirstOrDefaultAsync();                

                db.Users.Remove(userToDelete);
                await db.SaveChangesAsync();
                return RedirectToAction("Index");

            }

            //If model is not valid
            return View(model);
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
            }

            base.Dispose(disposing);
        }
    }

        
}