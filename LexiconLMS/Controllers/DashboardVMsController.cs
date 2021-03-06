﻿using LexiconLMS.Models;
using LexiconLMS.ViewModels;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;

namespace LexiconLMS.Controllers
{
    [Authorize(Roles = "Student")]
    public class DashboardVMsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        /// <summary>
        /// ShowDashboard is the default action for a student. It collects the information 
        /// that is relevant for the logged in student for the current date.
        /// </summary>
        /// <returns></returns>
        public ActionResult ShowDashboard(int? assignmentDocId)
        {
            ApplicationUser currentUser = db.Users
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();

            DashboardVM dashboard = new DashboardVM();

            dashboard.FilterHandIns = false;

            string activityType = " ";

            var courseId = currentUser.CourseId;

            var course = db.Courses.Find(courseId);

            var currentDate = DateTime.Now;

            dashboard.CourseName = course.Name;
            dashboard.StudentName = currentUser.FirstName + " " + currentUser.LastName;
            dashboard.TodaysDate = currentDate;
            dashboard.ModuleExists = true;
            dashboard.ActivityExists = true;

            List<Activity> activities = new List<Activity>();

            var module = db.Modules.Where(c => c.CourseId == courseId)
                                            .Where(s => s.StartDate <= currentDate)
                                            .Where(e => e.EndDate >= currentDate)
                                            .FirstOrDefault();

            if (module != null)
            {
                dashboard.ModuleName = module.Name;

                var moduleId = module.Id;

                activities = db.Activities.Where(m => m.ModuleId == moduleId)
                                                        .ToList();

                List<Activity> dashboardActivities = new List<Activity>();

                //Filter for activities valid for the current date only
                if (activities != null)
                {
                    foreach (var item in activities)
                    {
                        if (item.StartDate.ToString("yyyy-MM-dd") == currentDate.ToString("yyyy-MM-dd"))
                        {
                            dashboardActivities.Add(item);
                        }
                    }
                }

                if (dashboardActivities.Count > 0)
                {
                    List<string> typenames = new List<string>();
                    foreach (var item in dashboardActivities)
                    {
                        //The following is only needed if activities are allowed to span more than one day. Not yet implemented.
                        //Check if the start date of the activity is less than the current date. If it is the set start time = 8:30.
                        //Check if the end date of the activity is greater than the current date. If it is the set end time = 17:00.

                        //Read activity type names into a List of strings
                        activityType = db.ActivityTypes.Find(item.ActivityTypeId).TypeName;
                        typenames.Add(activityType);
                    }
                    dashboard.ActivitiesForTodayList = dashboardActivities;
                    dashboard.ActivityTypesForTodayList = typenames;
                }
                else
                {
                    dashboard.ActivityExists = false;
                }
            }
            else
            {
                dashboard.ModuleExists = false;
                dashboard.ActivityExists = false;
            }

            //The code below populates the viewmodel properties related to documents
            //Check if the course has any documents

            var otherDocuments = new List<Document>();
            var assignmentDescriptionsAndRowEmphasis = new List<AssignmentAndRowEmphasis>();
            var handIns = new List<Document>();
            var feedbacksList = new List<FeedbackObject>();

            if (course.Documents != null)
            {
                otherDocuments = course.Documents;
            }

            //Find current or past modules with documents
            var modulesUpToThisDate = db.Modules.Where(c => c.CourseId == courseId)
                                            .Where(s => s.StartDate <= currentDate)
                                            .ToList();

            foreach (var item in modulesUpToThisDate)
            {
                if (item.Documents != null)
                {
                    foreach (var document in item.Documents)
                    {
                        otherDocuments.Add(document);

                    }
                }
            }

            //Find curent or past activities with documents
            //First find all current and passed modules
            var modules = db.Modules.Where(c => c.CourseId == courseId)
                                            .Where(s => s.StartDate <= currentDate)
                                            .ToList();

            //Then, among the found modules, find all activities uo to this date with documents
            if (modules != null)
            {
                foreach (var item in modules)
                {
                    if (item.Activities != null)
                    {
                        foreach (var activity in item.Activities)
                        {
                            if (activity.Documents != null)
                            {
                                foreach (var document in activity.Documents)
                                {
                                    //Activity Descriptions
                                    if (document.PurposeId == 4)
                                    {
                                        otherDocuments.Add(document);
                                    }
                                    //Assignment Descriptions
                                    if (document.PurposeId == 5)
                                    {
                                        var assignmentAndEmphasisObject = new AssignmentAndRowEmphasis();
                                        assignmentAndEmphasisObject.AssignmentDescription = document;

                                        //Add logic here to decide the type of emphasis, for now everything will be "danger"
                                        if (DateTime.Now.Date.AddDays(2) >= document.DeadLine)
                                        {
                                            assignmentAndEmphasisObject.RowEmphasis = "warning";
                                        }
                                        else if (DateTime.Now.Date > document.DeadLine)
                                        {
                                            assignmentAndEmphasisObject.RowEmphasis = "danger";
                                        }

                                        assignmentDescriptionsAndRowEmphasis.Add(assignmentAndEmphasisObject);
                                    }
                                    //Assignment Descriptions
                                    if (document.PurposeId == 6)
                                    {
                                        otherDocuments.Add(document);
                                    }
                                    //Hand-ins
                                    if (document.PurposeId == 7 && document.Owner == currentUser)
                                    {
                                        if (assignmentDocId != null && assignmentDocId != 0)
                                        {
                                            dashboard.FilterHandIns = true;
                                            //Filtered on Assignment Description
                                            if (document.AssignmentDocId == assignmentDocId)
                                            {
                                                handIns.Add(document);
                                                dashboard.AssignmentDescriptionFilename = db.Documents.Where(d => d.Id == assignmentDocId).FirstOrDefault().Filename;
                                            }
                                        }
                                        else
                                        {
                                            handIns.Add(document);
                                        }
                                        //Look for any feedback to this document
                                        var feedback = db.FeedBacks.Where(d => d.DocumentId == document.Id).FirstOrDefault();
                                        FeedbackObject feedbackObject = new FeedbackObject();
                                        if (feedback != null)
                                        {
                                            feedbackObject.FeedbackExists = true;
                                            feedbackObject.FeedbackId = feedback.Id;
                                        }
                                        else
                                        {
                                            feedbackObject.FeedbackExists = false;
                                        }
                                        feedbacksList.Add(feedbackObject);
                                    }
                                }
                            }

                        }
                    }

                }
            }
            if (otherDocuments != null)
            {
                dashboard.OtherDocuments = otherDocuments;
            }
            else
            {
                dashboard.OtherDocuments = null;
            }

            if (assignmentDescriptionsAndRowEmphasis != null)
            {
                dashboard.AssignmentDescriptionAndEmphasis = assignmentDescriptionsAndRowEmphasis;
            }
            else
            {
                dashboard.AssignmentDescriptionAndEmphasis = null;
            }

            if (handIns != null)
            {
                dashboard.HandIns = handIns;
            }
            else
            {
                dashboard.HandIns = null;
            }
            if (feedbacksList != null)
            {
                dashboard.FeedbackList = feedbacksList;
            }
            else
            {
                dashboard.FeedbackList = null;
            }

            return View("Dashboard", dashboard);
        }

        public ActionResult ShowModules()
        {
            ApplicationUser currentUser = db.Users
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();

            ModulesForStudentVM modulesForStudent = new ModulesForStudentVM();

            var courseId = currentUser.CourseId;

            var course = db.Courses.Find(courseId);

            modulesForStudent.CourseName = course.Name;

            modulesForStudent.Modules = db.Modules.Where(c => c.CourseId == courseId).ToList();

            return View("CourseModulesForStudent", modulesForStudent);
        }

        public ActionResult ShowActivities(int id)
        {
            ApplicationUser currentUser = db.Users
                .Where(u => u.UserName == User.Identity.Name)
                .FirstOrDefault();

            ActivitiesForStudentVM activitiesForStudent = new ActivitiesForStudentVM();

            string activityType = " ";

            var moduleId = id;

            var module = db.Modules.Find(moduleId);

            var course = db.Courses.Find(module.CourseId);

            activitiesForStudent.CourseName = course.Name;
            activitiesForStudent.ModuleName = module.Name;

            activitiesForStudent.Activities = db.Activities.Where(m => m.ModuleId == moduleId).ToList();

            if (activitiesForStudent.Activities.Count > 0)
            {
                List<string> typenames = new List<string>();
                foreach (var item in activitiesForStudent.Activities)
                {
                    //The following is only needed if activities are allowed to span more than one day. Not yet implemented.
                    //Check if the start date of the activity is less than the current date. If it is the set start time = 8:30.
                    //Check if the end date of the activity is greater than the current date. If it is the set end time = 17:00.

                    //Read activity type names into a List of strings
                    activityType = db.ActivityTypes.Find(item.ActivityTypeId).TypeName;
                    typenames.Add(activityType);
                }
                activitiesForStudent.ActivityTypes = typenames;
            }

            return View("ActivitiesForStudent", activitiesForStudent);
        }

        [HttpGet]
        [Authorize(Roles = "Student")]
        public ActionResult ShowClassmates()
        {
            var me = db.Users.Find(User.Identity.GetUserId());
            var courseId = me.CourseId;
            var mates = db.Users
                .Where(u => u.CourseId == courseId)
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .ToList();
            return View(mates);
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
