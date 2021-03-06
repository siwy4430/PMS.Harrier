﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using PMS.Harrier.BusinessLogicLayer.Abstract;
using PMS.Harrier.DataAccessLayer.Concrete;
using PMS.Harrier.DataAccessLayer.Models;
using PMS.Harrier.WebUI.ViewModels;

namespace PMS.Harrier.WebUI.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly IProjectLogic _projectLogic;
        private readonly IDeveloperLogic _developerLogic;

        public ProjectController(IProjectLogic projectLogic, IDeveloperLogic developerLogic)
        {
            _projectLogic = projectLogic;
            _developerLogic = developerLogic;
            AutoMapper.Mapper.CreateMap<Project, ProjectViewModel>();
            AutoMapper.Mapper.CreateMap<AddDeveloperViewModel, ProjectDeveloper>();
            AutoMapper.Mapper.CreateMap<ProjectViewModel, Project>();
            AutoMapper.Mapper.CreateMap<Developer, AddDeveloperViewModel>()
                .ForMember(dest => dest.FirstName, opts => opts.MapFrom(src => src.Account.FirstName))
                .ForMember(dest => dest.LastName, opts => opts.MapFrom(src => src.Account.LastName))
                .ForMember(dest => dest.DeveloperId, opts => opts.MapFrom(src => src.DeveloperId));
            AutoMapper.Mapper.CreateMap<Developer, DeveloperViewModel>()
                .ForMember(dest => dest.FirstName, opts => opts.MapFrom(src => src.Account.FirstName))
                .ForMember(dest => dest.LastName, opts => opts.MapFrom(src => src.Account.LastName))
                .ForMember(dest => dest.Email, opts => opts.MapFrom(src => src.Account.Email));


        }

        // GET: Project
        public ActionResult Index()
        {
            var projects = _projectLogic.GetAllProjects();
            return View(AutoMapper.Mapper.Map<List<Project>, List<ProjectViewModel>>(projects));

        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Create(Project project)
        {
            if (!ModelState.IsValid)
                return View(project);

            project.StartDate = DateTime.Now;
            _projectLogic.AddNewProject(project);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var project = _projectLogic.GetProject(id.Value);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(AutoMapper.Mapper.Map<Project, ProjectViewModel>(project));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ProjectViewModel project)
        {
            if (ModelState.IsValid)
            {
                var entity = AutoMapper.Mapper.Map<ProjectViewModel, Project>(project);
                _projectLogic.EditProject(entity);
                return RedirectToAction("Index");
            }
            return View(project);
        }

        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var project = _projectLogic.GetProject(id.Value);
            if (project == null)
            {
                return HttpNotFound();
            }

            return View(AutoMapper.Mapper.Map<Project, ProjectViewModel>(project));
        }

        public ActionResult MyProjects()
        {
            var loggedUser =
                System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>()
                    .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            if (loggedUser == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var userProjects = _projectLogic.GetProjectsByDeveloperId(loggedUser.Developer.DeveloperId);
            return View(AutoMapper.Mapper.Map<List<Project>, List<ProjectViewModel>>(userProjects));
        }

        public JsonResult ValidateProjectName(string projectName)
        {
            var result = _projectLogic.IsProjectNameAvailable(projectName);
            return result
                ? Json(true, JsonRequestBehavior.AllowGet)
                : Json("Projekt o takiej nazwie już istnieje", JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddDeveloperToProject(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var project = _projectLogic.GetProject(id.Value);
            var result =
                AutoMapper.Mapper.Map<List<Developer>, List<AddDeveloperViewModel>>(_developerLogic.GetAllDevelopers());
            result.ForEach(n => n.ProjectId = project.ProjectId);

            return View(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDeveloperToProject(
            [Bind(Include = "DeveloperId,ProjectId, IsSelected, FirstName, LastName")] List<AddDeveloperViewModel> selectedDevelopers)
        {
            if (!ModelState.IsValid)
                return View(selectedDevelopers);
            try
            {
                var result =
                    AutoMapper.Mapper.Map<List<AddDeveloperViewModel>, List<ProjectDeveloper>>(
                        selectedDevelopers.Where(n => n.IsSelected).ToList());
                _projectLogic.AddDevelopersToProject(result);
                return RedirectToAction("Index");
            }
            catch (DbUpdateException exception)
            {
                ModelState.AddModelError(string.Empty, "Wybrane osoby są przypisane do wybranego projektu");

                foreach (var exceptionEntry in exception.Entries)
                {
                    var entity = (ProjectDeveloper) exceptionEntry.Entity;
                    ModelState.AddModelError(entity.DeveloperId.ToString(), "Ten programista jest już przypisany do tego projektu");
                }

                return View(selectedDevelopers);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("Developer.Id", "Wystąpił błąd");
                return View();
            }
            
        }

        public ActionResult ProjectTeam(int? id)
        {

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var developers = _developerLogic.GetAllDevelopersByProjectId(id.Value); 
            if (developers == null)
            {
                return HttpNotFound();
            }

            return View(AutoMapper.Mapper.Map<List<Developer>, List<DeveloperViewModel>>(developers));
        }

        [HttpGet]
        public ActionResult CreateIssue(int? id)
        {
            if (!id.HasValue)
            {
                return HttpNotFound();
            }
            var loggedUser =
                System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>()
                    .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            var model = new Issue
            {
                ProjectId = id.Value
            };
            return View(model);
        }

        [HttpPost]
        public ActionResult CreateIssue(Issue issue)
        {
            if (!ModelState.IsValid)
            {
                return View(issue);
            }
            var loggedUser =
                System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>()
                    .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());

            issue.Reporter = loggedUser.Developer.DeveloperId;
            using (var ctx = new EfDbContext())
            {
                ctx.Issues.Add(issue);
                ctx.SaveChanges();

            }

            return RedirectToAction("Index");
        }

        public ActionResult AvailableIssues(int? id)
        {
            if (!id.HasValue)
            {
                return HttpNotFound();
            }

            var result = new List<Issue>();
            using (var ctx = new EfDbContext())
            {
                var firstOrDefault = ctx.Projects.FirstOrDefault(p => p.ProjectId == id.Value);
                if (firstOrDefault != null)
                    result = firstOrDefault.Issues.ToList();
                else
                    return RedirectToAction("Index");
            }
            return View(result);
        }
        

        public ActionResult AssignToMe(int id, int projectId)
        {
            var loggedUser =
                System.Web.HttpContext.Current.GetOwinContext()
                    .GetUserManager<ApplicationUserManager>()
                    .FindById(System.Web.HttpContext.Current.User.Identity.GetUserId());
            using (var ctx = new EfDbContext())
            {
                var dev = ctx.Developers.FirstOrDefault(x => x.DeveloperId == loggedUser.Developer.DeveloperId);
                ctx.Issues.FirstOrDefault(x => x.IssueId == id).DeveloperId = dev.DeveloperId;
                ctx.SaveChanges();
            }

            ViewBag.TheResult = true;
            return RedirectToAction("AvailableIssues", new {id =  projectId});
        }

        public ActionResult IssueDetails(int id)
        {
            Issue issue;
            using (var ctx = new EfDbContext())
            {
                issue = ctx.Issues.FirstOrDefault(x => x.IssueId == id);
            }
                return View(issue);
        }

        public ActionResult LogTime(int id)
        {
            Issue issue;
            using (var ctx = new EfDbContext())
            {
                issue = ctx.Issues.Find(id);
            }
                return View(issue);
        }
        [HttpPost]
        public ActionResult LogTime(Issue issue)
        {
            if (!ModelState.IsValid)
            {
                return View(issue);
            }
            using (var ctx = new EfDbContext())
            {
                var result = ctx.Issues.Find(issue.IssueId);
                result.LoggedHours += issue.LoggedHours;
                ctx.Entry(result).State = EntityState.Modified;
                ctx.SaveChanges();
            }
            return RedirectToAction("IssueDetails", new {id =  issue.IssueId});

        }

        public ActionResult FinishIssue(int id)
        {
            return View();
        }
        
        [ChildActionOnly]

        public ActionResult GetDeveloperDetails(int? id)
        {
            Developer developer;
            DeveloperViewModel model;
            using (var ctx = new EfDbContext())
            {
                developer = ctx.Developers.FirstOrDefault(x => x.DeveloperId == id);
                model = new DeveloperViewModel()
                {
                    DeveloperId = id.Value,
                    FirstName = developer.Account.FirstName,
                    LastName = developer.Account.LastName
                };
            }

            return PartialView("_reporerPartial", model);
        }
    }
}