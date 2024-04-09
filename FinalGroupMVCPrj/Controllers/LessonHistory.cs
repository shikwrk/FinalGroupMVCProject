﻿using FinalGroupMVCPrj.Models;
using FinalGroupMVCPrj.Models.DTO;
using FinalGroupMVCPrj.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FinalGroupMVCPrj.Controllers
{
    public class LessonHistory : UserInfoController
    {
        private readonly LifeShareLearnContext _context;
        public LessonHistory(LifeShareLearnContext context)
        {
            _context = context;
        }
        //■ ==========================     子謙作業區      ==========================■
        // GET: LessonHistory/List
        //動作簡述：回傳課程記錄清單的頁面
        [HttpGet]
        public IActionResult LearningRecord()
        {
            var memberId = GetCurrentMemberId(); // 获取当前会员ID
            var successRecord = _context.TOrderDetails
                .Include(lc => lc.FLessonCourse)
                .Where(lr => lr.FOrder.FMemberId == memberId && lr.FOrderValid == true)                
                .ToList();            

            
            var successdict = successRecord.OrderByDescending(lr => lr.FLessonCourse.FLessonDate).ToDictionary(lr => lr.FOrderId, lr => lr.FLessonCourse);           


            var cancelRecord = _context.TOrderDetails
                .Include(lc => lc.FLessonCourse)
                .Where(lr => lr.FOrder.FMemberId == memberId && lr.FOrderValid == false)
                .ToList();  

            
            var canceldict = cancelRecord.OrderByDescending(lr => lr.FLessonCourse.FLessonDate).ToDictionary(lr => lr.FOrderId, lr => lr.FLessonCourse);

            LearningRecordVM learningRecord = new()
            {
                SuccessRecord = successdict,
                CancelRecord = canceldict,

            };
            return View(learningRecord);
        }
        //■ ==========================     Apple 作業區      ==========================■
        public IActionResult Detail(int? id)
        {

            ViewBag.FOrderDetailId = 5;

            if (id == null)
            {
                return NotFound();
            }

            // 取得TOrder、TOrderDetail資料
            var order = _context.TOrderDetails
                .Include(od => od.FOrder)
                .Include(od => od.FLessonCourse)
                .Where(od => od.FOrderId == id)
                .Select(od => new LessonHistoryDetailViewModel
                {
                    FName = od.FLessonCourse.FName,
                    FDescription = od.FLessonCourse.FDescription,
                    FOrderId = od.FOrderId,
                    FOrderNumber = od.FOrder.FOrderNumber,
                    FOrderValid = od.FOrderValid,
                    FOrderDate = od.FOrder.FOrderDate,
                    FLessonDate = od.FLessonCourse.FLessonDate,
                    FPhoto = od.FLessonCourse.FPhoto,
                    FLessonPrice = od.FLessonPrice,
                    FOrderDetailId = od.FOrderDetailId,
                    FLessonCourseId = od.FLessonCourseId,
                    FModificationDescription = od.FModificationDescription,
                }).ToList().FirstOrDefault();
         
            return View("Detail", order);
        }

        [HttpPost]
        public IActionResult CancelOrder(CancelOrderDTO cancelOrderDTO)
        {

            var orderDetail = _context.TOrderDetails.FirstOrDefault(od => od.FOrderId == cancelOrderDTO.id);
            if (orderDetail != null)
            {
                orderDetail.FOrderValid = false; // 更新 FOrderValid 為 false
                orderDetail.FModificationDescription = cancelOrderDTO.reason; //將取消原因寫入資料庫
                _context.SaveChanges();
                return Ok(orderDetail);
            }
            return NotFound();
        }
        [HttpGet]
        public async Task<FileResult> showPicture(int id)
        {
            TLessonCourse? c = await _context.TLessonCourses.FindAsync(id);
            byte[]? Content = c?.FPhoto;
            return File(Content, "image/jpeg");
        }
    }
}
