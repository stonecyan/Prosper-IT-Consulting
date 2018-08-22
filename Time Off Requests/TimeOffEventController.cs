using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using ScheduleUsers.Models;
using ScheduleUsers.ViewModels;
using PagedList;
using System.Data.Entity.SqlServer;

namespace ScheduleUsers.Areas.Employer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class TimeOffEventController : Controller
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

        private ApplicationDbContext db = new ApplicationDbContext();

        public ActionResult Index()
        {
            return View("Index");
        }
        // GET: Employer/TimeOffEvent
        public PartialViewResult ProcessedIndex(string sortOrder, string data)
        {
            //Keeping track of current sort
            ViewBag.DateSortParm = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.NameSortParm = sortOrder == "Last Name" ? "name_desc" : "Last Name";
            ViewBag.LengthSortParm = sortOrder == "Length of Event" ? "lengthOfEvent_desc" : "Length of Event";
            ViewBag.SubmittedSortParm = sortOrder == "Submitted" ? "submitted_desc" : "Submitted";

            List<TimeOffViewModel> timeOffList = new List<TimeOffViewModel>();
            if (data == "index")
            {
                var timeOffEvents = db.TimeOffEvents.Where(x => x.ApproverId == null).OrderBy(a => a.Start).ToList();
                for (int i = 0; i < timeOffEvents.Count; i++)
                {
                    TimeOffViewModel timeOff = new TimeOffViewModel(timeOffEvents[i]);
                    timeOffList.Add(timeOff);
                }
            }
            else if (data == "processsed")
            {
                var timeOffEvents = db.TimeOffEvents.Where(x => x.ApproverId != null).OrderBy(a => a.Start).ToList();
                for (int i = 0; i < timeOffEvents.Count; i++)
                {
                    TimeOffViewModel timeOff = new TimeOffViewModel(timeOffEvents[i]);
                    timeOffList.Add(timeOff);
                }
            }

            IEnumerable<TimeOffViewModel> timeOffEventsEnumerable;

            switch (sortOrder)
            {
                case "date_desc":
                    timeOffEventsEnumerable = timeOffList.OrderByDescending(t => t.Start).ToList();
                    break;
                case "Last Name":
                    timeOffEventsEnumerable = timeOffList.OrderBy(t => t.LastName).ToList();
                    break;
                case "name_desc":
                    timeOffEventsEnumerable  = timeOffList.OrderByDescending(t => t.LastName).ToList();
                    break;
                case "Length of Event":
                    timeOffEventsEnumerable = timeOffList.OrderBy(t => t.RequestLength).ToList();
                    break;
                case "lengthOfEvent_desc":
                    timeOffEventsEnumerable = timeOffList.OrderByDescending(t => t.RequestLength).ToList();
                    break;
                case "Submitted":
                    timeOffEventsEnumerable = timeOffList.OrderBy(t => t.Submitted).ToList();
                    break;
                case "submitted_desc":
                    timeOffEventsEnumerable = timeOffList.OrderByDescending(t => t.Submitted).ToList();
                    break;
                default:
                    timeOffEventsEnumerable = timeOffList.OrderBy(t => t.Start).ToList();
                    break;
            }
            return PartialView("_Index", timeOffEventsEnumerable);
        }


        // GET: TimeOffEvent/Create
        public ActionResult Create()
        {
            ViewBag.Id = new SelectList(db.Users, "Id", "FirstName");
            return View();
        }

        // POST: TimeOffEvent/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "EventID,Start,End,ActiveSchedule,Submitted,Id")] TimeOffEvent timeOffEvent, string accountid)
        {
            if (ModelState.IsValid)
            {
                ApplicationUser user = db.Users.Find(accountid);
                timeOffEvent.User = user;
                timeOffEvent.EventID = Guid.NewGuid();
                db.TimeOffEvents.Add(timeOffEvent);
                timeOffEvent.Submitted = DateTime.Now;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.Id = new SelectList(db.Users, "Id", "FirstName", timeOffEvent.Id);
            return PartialView(timeOffEvent);
        }

        // GET: TimeOffEvent/Edit/5
        public ActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TimeOffEvent timeOffEvent = db.TimeOffEvents.Find(id);
            if (timeOffEvent == null)
            {
                return HttpNotFound();
            }
            ViewBag.Id = new SelectList(db.Users, "Id", "FirstName", timeOffEvent.Id);
            return View(timeOffEvent);
        }

        // POST: TimeOffEvent/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "EventID,Start,End,ActiveSchedule,Submitted,Id")] TimeOffEvent timeOffEvent)
        {
            if (ModelState.IsValid)
            {
                db.Entry(timeOffEvent).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.Id = new SelectList(db.Users, "Id", "FirstName", timeOffEvent.Id);
            return View(timeOffEvent);
        }

        // GET: TimeOffEvent/Delete/5
        public ActionResult Delete(Guid? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            TimeOffEvent timeOffEvent = db.TimeOffEvents.Find(id);
            if (timeOffEvent == null)
            {
                return HttpNotFound();
            }
            return View(timeOffEvent);
        }

        // POST: TimeOffEvent/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(Guid id)
        {
            TimeOffEvent timeOffEvent = db.TimeOffEvents.Find(id);
            db.TimeOffEvents.Remove(timeOffEvent);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Approve(Guid id)
        {
            var ApprovedEvent = db.TimeOffEvents.Find(id);
            ApplicationUser user = db.Users.Find(ApprovedEvent.User.Id);
            ApprovedEvent.ActiveSchedule = true;
            var approveID = User.Identity.GetUserId();
            ApprovedEvent.ApproverId = approveID;

            Message m = new Message(ApprovedEvent, approveID, db); // Message constructor

            db.Messages.Add(m);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult Deny(Guid id)
        {
            var DeniedEvent = db.TimeOffEvents.Find(id);
            ApplicationUser user = db.Users.Find(DeniedEvent.User.Id);
            DeniedEvent.ActiveSchedule = false;
            var approveID = User.Identity.GetUserId();
            DeniedEvent.ApproverId = approveID;

            Message m = new Message(DeniedEvent, approveID, db); // Message constructor

            db.Messages.Add(m);
            db.SaveChanges();
            return RedirectToAction("Index");
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