﻿using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using WebApplication8.Models;

namespace WebApplication8.Controllers
{
    public class CuttingSheetsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: CuttingSheets
        public ActionResult Index(string SearchCode, string SearchProjectCode)
        {
            ViewBag.title1 = "MRF List";

            ViewBag.SearchCode = SearchCode;
            ViewBag.SearchProjectCode = SearchCode;
            

            List<CuttingSheet> cuttingSheets = db.CuttingSheets.Include(c => c.project).ToList();

            if (!String.IsNullOrEmpty(SearchCode))
            {
                cuttingSheets = cuttingSheets.Where(s => s.code.ToString().Contains(SearchCode, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }


            if (!String.IsNullOrEmpty(SearchProjectCode))
            {
                cuttingSheets = cuttingSheets.Where(s => s.project.Code.ToString().Contains(SearchProjectCode, StringComparison.InvariantCultureIgnoreCase)).ToList();
            }

            cuttingSheets = cuttingSheets.OrderByDescending(p => p.StampDate).ToList();
            return View(cuttingSheets.ToList());
        }

        // GET: CuttingSheets/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CuttingSheet cuttingSheet = db.CuttingSheets.Find(id);
            if (cuttingSheet == null)
            {
                return HttpNotFound();
            }
            return View(cuttingSheet);
        }

        public JsonResult CuttingSheetsDetails(int CuttingSheetId)
        {
            var l = db.CuttingSheets.Find(CuttingSheetId);
            var ld = l.CuttingSheetDetails.Select(a => new
            {
                MaterialId = a.MaterialId,
                Name = a.material.Name,
                Unit = a.material.Unit.Name,
                Dimension = a.material.Dimension,

                Qty = a.qty,
                IssueQty = a.IssueQty,

                Price = a.material.AvgPrice,
                qtyApproved = a.qtyApproved,
                Approval = a.Approval,
                Remark = a.Remark,
                 CuttingSheetDetailId = a.CuttingSheetDetailId

            }).ToList();
            return Json(ld, JsonRequestBehavior.AllowGet);
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
            String s1 = "MRHM_CS_";

            string S2 = seq.ToString();

            String s = s1 + S2;
            
            return s;
        }

        // GET: CuttingSheets/Create
        public ActionResult Create()
        {
            ViewBag.title1 = "Create MRF ";
            int Sequense = GetSeq();
            ViewBag.code = generateContractCode(Sequense);

            ViewBag.productlist = new SelectList(db.Materials, "MaterialId", "Name");
            ViewBag.ProjectId = new SelectList(db.Projects.Where(p=>p.AccountApproval==true && p.CuttingSheets.Count==0), "ProjectId", "Code");
            return View();
        }

        // POST: CuttingSheets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "CuttingSheetId,ProjectId,UserCreate,CreateDate,code")] CuttingSheet cuttingSheet,
            String[] FileName)
        {
            if (ModelState.IsValid)
            {
                var proj = db.Projects.Find(cuttingSheet.ProjectId);
                proj.ProjectStatusId = 4;
                db.CuttingSheets.Add(cuttingSheet);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.productlist = new SelectList(db.Materials, "MaterialId", "Name");

            ViewBag.ProjectId = new SelectList(db.Projects, "ProjectId", "Name", cuttingSheet.ProjectId);
            return View(cuttingSheet);
        }


        public JsonResult CreateJson([Bind(Include = "CuttingSheetId,ProjectId,UserCreate,CreateDate,code")] CuttingSheet cuttingSheet)
        {
            if (ModelState.IsValid)
            {
                try
                {

                    var proj = db.Projects.Find(cuttingSheet.ProjectId);
                    proj.ProjectStatusId = 4;
                    cuttingSheet.StampDate = DateTime.Now;
                    cuttingSheet.UserCreate = User.Identity.GetUserName();
                    db.CuttingSheets.Add(cuttingSheet);
                    db.SaveChanges();
                    return Json(cuttingSheet.CuttingSheetId);
                }
                catch (Exception ex)
                {
                    String errors11 = ex.Message;

                    if (ex.InnerException != null)
                    {
                        errors11 = errors11 + ex.InnerException.Message + " \r\n "; ;
                    }
                    return Json(errors11);
                }
            }
            return Json("errors11");
        }

        [HttpPost]
        public JsonResult CreateDetailsJson(List<CuttingSheetDetail> Details)
        {
            string errors11 = "";
            int aaa = 0;
            int test = 0;
            int eid = 0;
            CuttingSheetDetail EP = new CuttingSheetDetail();
            foreach (CuttingSheetDetail d in Details)
            {
                try
                {
                    EP = new CuttingSheetDetail();
                    EP.CuttingSheetId = int.Parse(d.CuttingSheetId.ToString());
                    eid = int.Parse(d.CuttingSheetId.ToString());
                    EP.MaterialId = int.Parse(d.MaterialId.ToString());

                    Material mm = db.Materials.Find(EP.MaterialId);

                    EP.qty = float.Parse(d.qty.ToString());


                    float? AvailableQty = mm.qty - mm.Resevedqty - mm.MinReOrder;
                    if (AvailableQty> EP.qty) {
                        EP.status = statusList.InStock;
                    }else
                    {
                        EP.status = statusList.Purchase;
                    }

                    db.CuttingSheetDetails.Add(EP);
                    db.SaveChanges();


      


                    var StockIssue = db.StockIssues.Find(eid);

                        mm.Resevedqty = mm.Resevedqty + EP.qty;
                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    DeleteConfirmed(eid);
                    test = 1;
                    errors11 = errors11 + ex.Message;
                    if (ex.InnerException != null)
                    {

                        if (ex.InnerException.InnerException != null)
                        {
                            errors11 = errors11 + ex.InnerException.InnerException.Message;
                        }
                    }
                    return Json(errors11);
                }

            }
            if (test == 1)
            {
                return Json(errors11);
            }
            return Json("Cutting Sheet Saved Successfully");

        }

        public ActionResult PrintNewTable(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            CuttingSheet lpo = db.CuttingSheets.Find(id);

            //ViewBag.LpoTerms = db.LpoTerms.ToList();

            return View(lpo);

        }

        // GET: CuttingSheets/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CuttingSheet cuttingSheet = db.CuttingSheets.Find(id);
            if (cuttingSheet == null)
            {
                return HttpNotFound();
            }
            ViewBag.ProjectId = new SelectList(db.Projects, "ProjectId", "Name", cuttingSheet.ProjectId);
            return View(cuttingSheet);
        }

        // POST: CuttingSheets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "CuttingSheetId,ProjectId,UserCreate,CreateDate")] CuttingSheet cuttingSheet)
        {
            if (ModelState.IsValid)
            {
                db.Entry(cuttingSheet).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.ProjectId = new SelectList(db.Projects, "ProjectId", "Code", cuttingSheet.ProjectId);
            return View(cuttingSheet);
        }



        public JsonResult EditJson([Bind(Include = "CuttingSheetId,ProjectId,UserCreate,CreateDate,code")] CuttingSheet cuttingSheet)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //cuttingSheet.UserCreate = User.Identity.GetUserName();
                    db.Entry(cuttingSheet).State = EntityState.Modified;
                    db.SaveChanges();
                    return Json(cuttingSheet.CuttingSheetId);
                }
                catch (Exception ex)
                {
                    String errors11 = ex.Message;

                    if (ex.InnerException != null)
                    {
                        errors11 = errors11 + ex.InnerException.Message + " \r\n "; ;
                    }
                    return Json(errors11);
                }
            }
            return Json("errors11");
        }

        [HttpPost]
        public JsonResult EditDetailsJson(List<CuttingSheetDetail> Details,int id)
        {
            string errors11 = "";
            int aaa = 0;
            int test = 0;
            int eid = 0;

            //delete exist
            var cs = db.CuttingSheets.Find(id);
            List<CuttingSheetDetail> OldDetails = cs.CuttingSheetDetails.ToList();
            foreach (CuttingSheetDetail d in OldDetails)
            {
                db.CuttingSheetDetails.Remove(d);
                
            }
            db.SaveChanges();



            CuttingSheetDetail EP = new CuttingSheetDetail();
            foreach (CuttingSheetDetail d in Details)
            {
                try
                {
                    EP = new CuttingSheetDetail();
                    EP.CuttingSheetId = int.Parse(d.CuttingSheetId.ToString());
                    eid = int.Parse(d.CuttingSheetId.ToString());
                    EP.MaterialId = int.Parse(d.MaterialId.ToString());

                    Material mm = db.Materials.Find(EP.MaterialId);

                    EP.qty = float.Parse(d.qty.ToString());


                    float? AvailableQty = mm.qty - mm.Resevedqty - mm.MinReOrder;
                    if (AvailableQty > EP.qty)
                    {
                        EP.status = statusList.InStock;
                    }
                    else
                    {
                        EP.status = statusList.Purchase;
                    }

                    db.CuttingSheetDetails.Add(EP);
                    db.SaveChanges();





                    var StockIssue = db.StockIssues.Find(eid);

                    mm.Resevedqty = mm.Resevedqty + EP.qty;
                    db.SaveChanges();

                }
                catch (Exception ex)
                {
                    DeleteConfirmed(eid);
                    test = 1;
                    errors11 = errors11 + ex.Message;
                    if (ex.InnerException != null)
                    {

                        if (ex.InnerException.InnerException != null)
                        {
                            errors11 = errors11 + ex.InnerException.InnerException.Message;
                        }
                    }
                    return Json(errors11);
                }

            }
            if (test == 1)
            {
                return Json(errors11);
            }
            return Json("Cutting Sheet Saved Successfully");

        }




        // GET: CuttingSheets/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            CuttingSheet cuttingSheet = db.CuttingSheets.Find(id);
            if (cuttingSheet == null)
            {
                return HttpNotFound();
            }
            return View(cuttingSheet);
        }

        // POST: CuttingSheets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            CuttingSheet cuttingSheet = db.CuttingSheets.Find(id);
            db.CuttingSheets.Remove(cuttingSheet);
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
