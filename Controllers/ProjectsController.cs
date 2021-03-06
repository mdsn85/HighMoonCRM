﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class ProjectsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private string[] rolesArray;

        // GET: Projects
        public ActionResult Index(bool? NotApproved,string CustomerId, string st, string SearchCode, string StatusId,string SearchValue1,string SearchValue2)
        {
            ViewBag.title1 = "Project List";
            ViewBag.SearchCode = SearchCode;

            ViewBag.SearchValue1 = SearchValue1;
            ViewBag.SearchValue2 = SearchValue2;

            ViewBag.StatusId = new SelectList(db.ProjectStatus, "ProjectStatusId", "Name", StatusId);
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name", CustomerId);


            List<Project> projects = db.Projects.Include(p => p.ProjectStatus).Include(p => p.SalesType).Include(p => p.CustomersType).Include(p => p.Customer).Include(p => p.Designer).Include(p => p.ProjectPaymentTerm).Include(p => p.SalesMan).ToList(); ;

            if (NotApproved == true)
            {
                projects = projects.Where(s => s.AccountApproval == false).ToList();
            }
            
            if (!String.IsNullOrEmpty(SearchCode))
            {
                projects = projects.Where(s => s.Code.ToString().Contains(SearchCode, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            if (!String.IsNullOrEmpty(StatusId))
            {
                int qqqq = int.Parse(StatusId);
                projects = projects.Where(s => s.ProjectStatusId != null && s.ProjectStatusId == qqqq).ToList();
            }

            if (!String.IsNullOrEmpty(CustomerId))
            {
                int qqqq = int.Parse(CustomerId);
                projects = projects.Where(s => s.CustomerId != null && s.CustomerId == qqqq).ToList();
            }



            if (!String.IsNullOrEmpty(SearchValue1))
            {
                float v1 = float.Parse(SearchValue1);
                
                if (!String.IsNullOrEmpty(SearchValue2))
                {
                    float v2 = float.Parse(SearchValue2);
                    projects = projects.Where(cb => cb.Value >= v1 && cb.Value < v2).ToList();
                }
                else
                {

                    projects = projects.Where(cb => cb.Value == v1).ToList();
                }
            }


            projects = projects.OrderByDescending(p => p.StampDate).ToList();
            return View(projects.ToList());
        }

        // GET: Projects/Details/5
        public ActionResult Details(int? id,string msg)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            List<Project> projects = db.Projects.Include(p => p.ProjectStatus).Include(p => p.CustomersType).Include(p => p.SalesType).Include(p => p.Customer).Include(p => p.Designer).Include(p => p.ProjectPaymentTerm).Include(p => p.SalesMan).ToList(); ;
            Project project = db.Projects.Find(id);

            float totalPayment = 0;
            foreach(var payment in project.Payments)
            {
                totalPayment += payment.Amount;
            }

            ViewBag.totalPayment = totalPayment;

            ViewBag.AttachedFiles = project.ProjectFiles.OrderBy(c => c.Name); ;
            if (project == null)
            {
                return HttpNotFound();
            }

            ViewBag.Error = msg;
            return View(project);
        }

        [HttpPost]
        public ActionResult Details(bool? AccountApprovalCk, int ProjectId)
        {


            Project proj = db.Projects.Find(ProjectId);

            string msg = "";
            if (AccountApprovalCk != null)
            {
                proj.AccountApproval = AccountApprovalCk ?? false;
            }

            if (proj.Payments != null && proj.Payments.Count() > 0)
            {

                db.SaveChanges();

               //return RedirectToAction("Index");
            }
            else
            {
                msg= "You can not approve , no payment is made";
            }


            List<Project> projects = db.Projects.Include(p => p.ProjectStatus).Include(p => p.CustomersType).Include(p => p.SalesType).Include(p => p.Customer).Include(p => p.Designer).Include(p => p.ProjectPaymentTerm).Include(p => p.SalesMan).ToList(); ;
            Project project = db.Projects.Find(ProjectId);

            float totalPayment = 0;
            foreach (var payment in project.Payments)
            {
                totalPayment += payment.Amount;
            }

            ViewBag.totalPayment = totalPayment;

            ViewBag.AttachedFiles = project.ProjectFiles.OrderBy(c => c.Name); ;
            if (project == null)
            {
                return HttpNotFound();
            }

            return RedirectToAction("Details", new { id = ProjectId ,msg=msg});
            
        }



        public int GetSeq()
        {
            int n = 0;
            try
            {
                n = db.CuttingSheetSequenses.Max(c => c.Sequense);
                if (n < 1000)
                {
                    n = 1001;
                }
                n++;

            }
            catch (Exception)
            {
                n = 1001;
            }

            CuttingSheetSequense ls = new CuttingSheetSequense();
            ls.Sequense = n;
            db.CuttingSheetSequenses.Add(ls);
            db.SaveChanges();
            return n;
        }

        public String generateContractCode(int seq)
        {
            String s1 = "Pro_";

            string S2 = seq.ToString();

            String s = s1 + S2;

            return s;
        }


        public ActionResult AddDeduction(int ProjectId, int? Close)
        {
            ViewBag.close = Close;
            ViewBag.ProjectId = ProjectId;


            Project proj = db.Projects.Find(ProjectId);
            ViewBag.a = proj.Deduction;
            ViewBag.r = proj.DeductionReason;


            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddDeduction(int projectId , float deduction, string reason)
        {
            Project proj = db.Projects.Find(projectId);
            proj.Deduction = deduction;
            proj.DeductionReason = reason;
            db.SaveChanges();


            return RedirectToAction("AddDeduction", new { ProjectId = projectId, Close = 1 });
        }




            // GET: Projects/Create
        public ActionResult Create()
        {
            ViewBag.title1 = "Create Project ";
            int Sequense = GetSeq();
            ViewBag.code = generateContractCode(Sequense);

            ViewBag.SalesDate = DateTime.Now.ToString("dd-MMMM-yyyy", CultureInfo.InvariantCulture); 

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name");
            ViewBag.DesignerId = new SelectList(db.Designers, "DesignerId", "Name");
            ViewBag.ProjectPaymentTermId = new SelectList(db.ProjectPaymentTerms, "ProjectPaymentTermId", "Name");
            ViewBag.SalesManId = new SelectList(db.SalesMen, "SalesManId", "Name");

            ViewBag.CustomersTypeId = new SelectList(db.CustomersTypes, "CustomersTypeId", "Name");

            ViewBag.SalesTypeId = new SelectList(db.SalesTypes, "SalesTypeId", "Name");

            ViewBag.ProjectStatusId = new SelectList(db.ProjectStatus, "ProjectStatusId", "Name");

            ViewBag.ext = FolderPath.allowedExtensions;

            return View();
        }

        // POST: Projects/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ProjectId,Name,CustomersTypeId,SalesTypeId,Code,SalesDate,SalesManId,CustomerId,DesignerId,Value,Discount,Vat,Total,SalePrice,ProjectPaymentTermId" +
            ",Description,DeliveryDate,ActualDeliveryDate,AccountApproval,QuotationRef,QuotationAgreementApprovedby")] Project project,
            String[] FileName)
        {
            ViewBag.SalesDate = project.SalesDate.ToString("dd-MMMM-yyyy", CultureInfo.InvariantCulture); ;
            if (project.DeliveryDate != null)
            {
                ViewBag.DeliveryDate = project.DeliveryDate.ToString("dd-MMMM-yyyy", CultureInfo.InvariantCulture); ;
            }
            ViewBag.ActualDeliveryDate = project.ActualDeliveryDate;
            if (ModelState.IsValid)
            {
                if (FileName!= null)
                {
                    project.Deduction = 0;


                    project.UserCreate = User.Identity.GetUserName();
                    project.CreateDate = DateTime.Now;
                    project.StampDate = DateTime.Now;
                    project.ProjectStatusId = 1;
                    db.Projects.Add(project);
                    db.SaveChanges();


                    //file link
                    //save uploaded files record
                    ProjectFile projectFile = new ProjectFile();
                    if (FileName != null)
                    {
                        foreach (string fn in FileName)
                        {
                            string st = "_Project " + fn;
                            projectFile.ProjectId = project.ProjectId;
                            //String c = db.Customers.Find(contract.CustomerId).CompanyName;

                            projectFile.Name = fn;
                            projectFile.Path = Path.Combine(Server.MapPath(FolderPath.FolderPathCustomerDoc), st);

                            db.projectFiles.Add(projectFile);
                            db.SaveChanges();
                        }
                    }


                    return RedirectToAction("Index");
                }
                else
                {
                    ViewBag.Error = "Please upload Production Sheet";
                }
            }
            ViewBag.title1 = "Create Project ";
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name", project.CustomerId);
            ViewBag.DesignerId = new SelectList(db.Designers, "DesignerId", "Name", project.DesignerId);
            ViewBag.ProjectPaymentTermId = new SelectList(db.ProjectPaymentTerms, "ProjectPaymentTermId", "Name", project.ProjectPaymentTermId);
            ViewBag.SalesManId = new SelectList(db.SalesMen, "SalesManId", "Name", project.SalesManId);



            ViewBag.CustomersTypeId = new SelectList(db.CustomersTypes, "CustomersTypeId", "Name", project.CustomersTypeId);

            ViewBag.SalesTypeId = new SelectList(db.SalesTypes, "SalesTypeId", "Name", project.SalesTypeId);

            ViewBag.code = project.Code;
            return View(project);
        }

        // GET: Projects/Edit/5
        public ActionResult Edit(int? id)
        {
 

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            
            if (project == null)
            {
                return HttpNotFound();
            }
            ViewBag.PaymentTermId1 = project.ProjectPaymentTermId;
            ViewBag.PaymentTermName1 = db.ProjectPaymentTerms.Find(project.ProjectPaymentTermId).Name;

            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name", project.CustomerId);
            ViewBag.DesignerId = new SelectList(db.Designers, "DesignerId", "Name", project.DesignerId);
            ViewBag.ProjectPaymentTermId = new SelectList(db.ProjectPaymentTerms, "ProjectPaymentTermId", "Name", project.ProjectPaymentTermId);
            ViewBag.SalesManId = new SelectList(db.SalesMen, "SalesManId", "Name", project.SalesManId);

            ViewBag.CustomersTypeId = new SelectList(db.CustomersTypes, "CustomersTypeId", "Name", project.CustomersTypeId);

            ViewBag.SalesTypeId = new SelectList(db.SalesTypes, "SalesTypeId", "Name", project.SalesTypeId);

            ViewBag.ProjectStatusId = new SelectList(db.ProjectStatus, "ProjectStatusId", "Name", project.ProjectStatusId);
            project.ProjectFiles = db.Projects.Find(project.ProjectId).ProjectFiles;


            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ProjectId,UserCreate,CreateDate,Deduction,ProjectStatusId,DeductionReason,Name,CustomersTypeId,SalesTypeId,Code,SalesDate,SalesManId,CustomerId,DesignerId,Value,Discount,Vat,Total,SalePrice,ProjectPaymentTermId,Description,DeliveryDate,ActualDeliveryDate,AccountApproval")] Project project
            , String[] FileName)
        {
            if (ModelState.IsValid)
            {
                project.UserLastUpdate = User.Identity.GetUserName();
                project.LastUpdateDate = DateTime.Now;


                project.StampDate = DateTime.Now;

                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();


                List<int> csOld = db.projectFiles.Where(cf => cf.ProjectId == project.ProjectId).Select(c => c.ProjectFileId).ToList();


                ProjectFile projectfile = new ProjectFile();
                if (FileName != null)
                {
                    foreach (string fn in FileName)
                    {
                        projectfile.ProjectId = project.ProjectId;
                        // contractFile.ValidUntil = contract.Valid1;
                        projectfile.Name = fn;
                        projectfile.Path = Path.Combine(FolderPath.FolderPathCustomerDoc, fn);

                        db.projectFiles.Add(projectfile);
                        db.SaveChanges();
                    }
                }
                foreach (int csi in csOld)
                {
                    ProjectFile csd = db.projectFiles.Find(csi);
                    db.projectFiles.Remove(csd);
                    db.SaveChanges();
                }



                return RedirectToAction("Index");
            }
            ViewBag.CustomerId = new SelectList(db.Customers, "CustomerId", "Name", project.CustomerId);
            ViewBag.DesignerId = new SelectList(db.Designers, "DesignerId", "Name", project.DesignerId);
            ViewBag.ProjectPaymentTermId = new SelectList(db.ProjectPaymentTerms, "ProjectPaymentTermId", "Name", project.ProjectPaymentTermId);
            ViewBag.SalesManId = new SelectList(db.SalesMen, "SalesManId", "Name", project.SalesManId);

            ViewBag.CustomersTypeId = new SelectList(db.CustomersTypes, "CustomersTypeId", "Name", project.CustomersTypeId);

            ViewBag.SalesTypeId = new SelectList(db.SalesTypes, "SalesTypeId", "Name", project.SalesTypeId);

            ViewBag.ProjectStatusId = new SelectList(db.ProjectStatus, "ProjectStatusId", "Name", project.ProjectStatusId);
            return View(project);
        }




        // GET: Projects/Edit/5
        public ActionResult AccountApproval(int? id)
        {


            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            List<Project> projects = db.Projects.Include(p => p.CustomersType).Include(p => p.SalesType).Include(p => p.Customer).Include(p => p.Designer).Include(p => p.ProjectPaymentTerm).Include(p => p.SalesMan).ToList(); ;
            Project project = db.Projects.Find(id);
            ViewBag.AttachedFiles = project.ProjectFiles.OrderBy(c => c.Name); ;
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        public ActionResult AccountApproval(bool? AccountApprovalCk, int ProjectId)
        {


                Project proj = db.Projects.Find(ProjectId);

            if (AccountApprovalCk != null)
            {
                proj.AccountApproval = AccountApprovalCk ?? false;
            }

            if (proj.Payments != null && proj.Payments.Count() > 0)
            {

                db.SaveChanges();

                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Error = "You can not approve , no payment is made";
            }

            
            return View(proj);
        }


        public string FileUploadForEdit(HttpPostedFileBase file)
        {

            //upload file check extention
            var allowedExtensions = new[] {
                ".Jpg", ".png", ".jpg", ".jpeg",".pdf",".doc",".docx"
                };
            allowedExtensions = FolderPath.allowedExtensions;
            //*****************

            String FileName = "";
            if (file != null)
            {
                //save file to server
                //var fileName = Path.GetFileName(file.FileName); //getting only file name(ex-ganesh.jpg)  
                //string name = Path.GetFileNameWithoutExtension(fileName); //getting file name without extension  
                var ext = Path.GetExtension(file.FileName); //getting the extension(ex-.jpg)  
                ext = ext.ToLower();
                string OrginalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                if (!allowedExtensions.Contains(ext)) //check what type of extension  
                {
                    ViewBag.message = "Please choose only Image or PDF file";

                    //int Id = session.LegalCaseId;
                    //ViewBag.LegalCaseId = new SelectList(db.LegalCases.Where(l => l.LegalCaseId == Id), "LegalCaseId", "OfficeNum");
                    ////اخر جلسة مرحلة
                    //ViewBag.leatestTransSession = db.LegalCases.Where(l => l.LegalCaseId == Id).FirstOrDefault().leatestTransSession;
                    ////اخر جلسة غير مرحلة اي الجلسة القادمة
                    //ViewBag.NextSession = db.LegalCases.Where(l => l.LegalCaseId == Id).FirstOrDefault().NextSession;
                    //ViewBag.LegalCaseId = new SelectList(db.LegalCases, "LegalCaseId", "Subject", session.LegalCaseId);
                    return "Please choose only Image or PDF file";
                }

                string RandomFileName = Path.GetRandomFileName();
                RandomFileName = RandomFileName.Substring(0, RandomFileName.Length - 4);
                FileName = OrginalFileName + DateTime.Now.ToString("dd MMMM yyyy") + ext;
                var path = Path.Combine(Server.MapPath(FolderPath.FolderPathCustomerDoc), FileName);
                file.SaveAs(path);
                //************************

                ////save file record
                //sessionDecisionFiles.SessionDecisionId = sessionDecisionFiles.SessionDecisionId;
                //sessionDecisionFiles.Name = FileName;
                //sessionDecisionFiles.Path = path;

                //db.SessionDecisionFiles.Add(sessionDecisionFiles);
            }

            //  Send "Success"
            //return "done";

            // return "{\"msg\":\"success\"}";ReportNum

            return "{\"msg\":\"" + FileName + "\"}";
            // return "done";
        }

        // GET: Projects/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            return View(project);
        }

        // POST: Projects/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {

           
            Project project = db.Projects.Find(id);

            foreach(var f in project.ProjectFiles.ToList())
            {
                db.projectFiles.Remove(f);
            }
            db.Projects.Remove(project);
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