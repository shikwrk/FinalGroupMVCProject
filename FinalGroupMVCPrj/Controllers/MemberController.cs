﻿using FinalGroupMVCPrj.Models;
using FinalGroupMVCPrj.Models.DTO;
using FinalGroupMVCPrj.Models.ViewModels;
using FinalGroupMVCPrj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace FinalGroupMVCPrj.Controllers
{
    public class MemberController : UserInfoController
    {
        private readonly LifeShareLearnContext _context;  //資料庫
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;
        public MemberController(LifeShareLearnContext context, IMailService _MailService, IConfiguration configuration)
        {
            _context = context;
            _mailService = _MailService;
            _configuration = configuration;
        }
        // GET: Member/Setting
        //動作簡述：回傳設定會員資訊的頁面
        [HttpGet]
        public IActionResult Setting()
        {
            int memberId = GetCurrentMemberId();
            if (memberId == 0) { return Content("無會員登入中"); };
            var dbMember = _context.TMembers.Include(m=>m.TMemberCitiesLists).Include(m=>m.TMemberWishFields).FirstOrDefault(m => m.FMemberId == memberId);
            if (dbMember == null) { return Content("登入系統異常"); };
            MemberDetailViewModel mdVm = new MemberDetailViewModel
            {
                Member = dbMember,
                Cities = dbMember?.TMemberCitiesLists.Select(c=>c.FCityId),
                WishFields = dbMember?.TMemberWishFields.Select(w => w.FFieldId),
            };
            //會員單選/多選資料比對後給view應該被select的
            //性別(單選)
            IEnumerable<SelectListItem> GenderSelectList = new List<SelectListItem>
                {
                new SelectListItem { Text = "其他", Value = null },
                new SelectListItem { Text = "女", Value = "false" },
     new SelectListItem  { Text = "男", Value = "true"}
                 };
            foreach (var s in GenderSelectList)
            {
                if (s.Value == dbMember.FGender.ToString())
                {
                    s.Selected = true;
                    break;
                }
            }
            ViewBag.GenderSelectList = GenderSelectList;
            //職業類別(單選)
            var JobSelectList = MemberDetailViewModel.GetJobSelectList();
            foreach(var s in JobSelectList)
            {
                if(s.Value == dbMember.FJob)
                {
                    s.Selected = true;
                    break;
                }
            }
            ViewBag.JobSelectList = JobSelectList;
            //最高學歷(單選)
            var EduSelectList = MemberDetailViewModel.GetEduSelectList();
            foreach (var s in EduSelectList)
            {
                if (s.Value == dbMember.FEducation)
                {
                    s.Selected = true;
                    break;
                }
            }
            ViewBag.EduSelectList = EduSelectList;
            //生活縣市(多選)
            var CitySelectList = _context.TCities.Select(c => new SelectListItem { Text = c.FCityName, Value = c.FCityId.ToString() }).ToList();
            if(mdVm.Cities != null)
            {
            foreach (var c in CitySelectList)
            {
                if (mdVm.Cities.Any(mc=>mc.ToString()==c.Value))
                {
                    c.Selected = true;
                }
            }
            }
            ViewBag.CitySelectList = CitySelectList;
            //想學的領域(多選)
            var FieldSelectList = _context.TCourseFields.Select(f => new SelectListItem { Text = f.FFieldName, Value = f.FFieldId.ToString() }).ToList();
            if (mdVm.WishFields != null)
            {
                foreach (var f in FieldSelectList)
                {
                    if (mdVm.WishFields.Any(mf => mf.ToString() == f.Value))
                    {
                        f.Selected = true;
                    }
                }
            }
            ViewBag.FieldSelectList = FieldSelectList;
            return View("OSetting", mdVm);
        }
        // POST: Member/SettingSave
        //動作簡述：儲存會員修改的資訊
        [HttpPost]
        public IActionResult SettingSave([FromBody]MemberDetailViewModel mdVm)
        {
            int memberId = GetCurrentMemberId();
            var dataMember = mdVm?.Member;
            if (mdVm ==null || dataMember == null||mdVm.Cities==null||mdVm.WishFields==null) { return BadRequest("提交會員資料異常"); }
            if (dataMember.FMemberId != memberId) { return BadRequest("不合法的會員提交"); }
            var dbmember = _context.TMembers.SingleOrDefault(m=> m.FMemberId== memberId);
            if(dbmember == null) { return StatusCode(500, "資料庫系統異常"); }
            if(dbmember.FBirthDate == null && dataMember.FBirthDate != null) { dbmember.FBirthDate = dataMember.FBirthDate; }
            dbmember.FShowName = dataMember.FShowName;
            dbmember.FPhone = dataMember.FPhone;
            dbmember.FGender = dataMember.FGender;
            dbmember.FEducation = dataMember.FEducation;
            dbmember.FJob = dataMember.FJob;
            var dbMemCitiesId = _context.TMemberCitiesLists.Where(m => m.FMemberId == memberId).Select(m=>m.FCityId).ToList() ;
            var toDeteteCitiesId = dbMemCitiesId.Except(mdVm.Cities);
            var toAddCitiesId = mdVm.Cities.Except(dbMemCitiesId);
            foreach(int d in toDeteteCitiesId) 
            {
                var toDelete = _context.TMemberCitiesLists.FirstOrDefault(m => m.FCityId == d && m.FMemberId == memberId);
                    _context.TMemberCitiesLists.Remove(toDelete);
             }
            foreach (int a in toAddCitiesId)
            {
                var toAdd = new TMemberCitiesList { FMemberId = memberId, FCityId = a };
                _context.TMemberCitiesLists.Add(toAdd);
            }
            var dbMemFieldsId = _context.TMemberWishFields.Where(m => m.FMemberId == memberId).Select(m => m.FFieldId).ToList();
            var toDeteteFieldsId = dbMemFieldsId.Except(mdVm.WishFields);
            var toAddFieldsId = mdVm.WishFields.Except(dbMemFieldsId);
            foreach (int d in toDeteteFieldsId)
            {
                var toDelete = _context.TMemberWishFields.FirstOrDefault(m => m.FFieldId == d && m.FMemberId == memberId);
                _context.TMemberWishFields.Remove(toDelete);
            }
            foreach (int a in toAddFieldsId)
            {
                var toAdd = new TMemberWishField { FMemberId = memberId, FFieldId = a };
                _context.TMemberWishFields.Add(toAdd);
            }
            try
            {
                _context.TMembers.Update(dbmember);
                _context.SaveChanges();
                return Ok();
            }catch(Exception e)
            {
                return StatusCode(500, "資料庫系統異常："+e);
            }
            
           
        }

        // GET: Member/Fav
        //動作簡述：回傳設定會員資訊的頁面
        [HttpGet]
        public IActionResult Fav()
        {
            return View("Fav");
        }

        // GET: Member/MemberPhoto
        //動作簡述：回傳會員頭像的url
        [HttpGet]
        public async Task<IActionResult> MemberPhoto(int? id)
        {
            string blobDataURL = "";
            TMember? member = await _context.TMembers.FirstOrDefaultAsync(m => m.FMemberId == id);
            byte[]? image = member?.FMemberProfilePic;
            if (image == null || image.Length == 0)
            {
                blobDataURL = "";

            }
            else
            {
                string base64String = Convert.ToBase64String(image);
                blobDataURL = $"data:image/jpeg;base64,{base64String}";
            }
            return Content(blobDataURL);
        }
        // POST: Member/UpdatePhoto
        //動作簡述：更新會員的頭像
        [HttpPost]
        public async Task<IActionResult> UpdatePhoto(IFormFile file)
        {
            if(file==null || file.Length == 0)
            {
                return BadRequest("沒有檔案上傳");
            }
            int memberId = GetCurrentMemberId();
            if(memberId == 0) { return BadRequest("沒有會員登入中"); }
            var dbMember = _context.TMembers.SingleOrDefault(m => m.FMemberId == memberId);
                if (dbMember == null) { return StatusCode(500, "資料庫系統異常"); }
            byte[] fileBytes = null;
            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);
                fileBytes = memoryStream.ToArray();
            }
            if(fileBytes == null) {
                return StatusCode(500, "記憶體轉換異常");}
            dbMember.FMemberProfilePic = fileBytes;
            _context.TMembers.Update(dbMember);
            try
            {
                await _context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception ex)
            {
                return StatusCode(500, "系統異常：" + ex);
            }
        }
        // POST: Member/SaveLoginLog
        //動作簡述：儲存登入資訊
        [HttpPost]
        public async Task<IActionResult> SaveLoginLog(string ip, string geoInfo, string browerNOs)
        {
            try
            {
                int memberId = await Task.Run(() => GetCurrentMemberId());
                if (memberId == 0) { return StatusCode(500, "登入系統異常"); }
                TMemberLoginLog memberLoginLog = new TMemberLoginLog
                {
                    FMemberId = memberId,
                    FLoginDateTime = DateTime.Now,
                    FLoginIp = ip,
                    FLoginGeoInfo = geoInfo,
                    FLoginBrowerNos = browerNOs,
                };
                _context.TMemberLoginLogs.Add(memberLoginLog);
                await _context.SaveChangesAsync();
                return Ok();
            }catch (Exception ex)
            {
                    return StatusCode(500, $"系統異常：{ex}");
             }
        }

        [AllowAnonymous]
        // POST: Member/SignUp
        //動作簡述：儲存登入資訊
        [HttpPost]
        public async Task<IActionResult> SignUp(TMember member)
        {
            try
            {
                var dbMember = _context.TMembers.SingleOrDefault(m=> m.FEmail == member.FEmail);
                if(dbMember != null) { return BadRequest("此信箱已有人使用"); }
                string beforePassword = member.FPassword;
                string afterPassword = BCrypt.Net.BCrypt.EnhancedHashPassword(beforePassword, 13);
                TMember newMember = new TMember
                {
                    FRealName = member.FRealName,
                    FShowName = member.FRealName,
                    FPassword = afterPassword,
                    FRegisterDatetime = DateTime.Now,
                    FEmail = member.FEmail,
                    FEmailVerification = false,
                    FGetCampaignInfo = true,
                    FQualifiedTeacher = false,
                    FStatus = true
                };
                _context.TMembers.Add(newMember);
                await _context.SaveChangesAsync();
                return Ok("註冊成功請驗證信箱");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"系統異常：{ex}");
            }
        }
        [AllowAnonymous]
        [HttpPost]
        public bool SendEmail(MailData mailData)
        {
            return _mailService.SendMail(mailData);
        }
        [AllowAnonymous]
        [HttpPost]
        public IActionResult SendVetifyEmail(string email)
        {
            var dbMember = _context.TMembers.SingleOrDefault(m=> m.FEmail == email);
            if (dbMember == null) { return BadRequest("此信箱尚未註冊"); }
            if (dbMember.FEmailVerification == true) { return BadRequest("此信箱已驗證，請前往登入"); }
            string? emailVetifyURL = GetEmailVetifyURL(email);
            if (string.IsNullOrEmpty(emailVetifyURL)) { return StatusCode(500,"系統異常：產生驗證連結失敗"); }
            MailData mailData = new MailData
            {
                EmailToId = email,
                EmailSubject = "來學樂帳號電子信箱驗證",
                EmailBody = $@"
    <html>
    <head>
        <style>
            .btn {{
                display: inline-block;
                font-weight: 400;
                text-align: center;
                white-space: nowrap;
                vertical-align: middle;
                user-select: none;
                border: 1px solid transparent;
                padding: 0.375rem 0.75rem;
                font-size: 1rem;
                line-height: 1.5;
                border-radius: 0.25rem;
                transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out, border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
            }}
            .btn-primary {{
                color: #fff;
                background-color: #30C2EC;
                border-color: #30C2EC;
            }}
        </style>
    </head>
    <body>
        <h2>請點擊此按鈕驗證您的信箱：</h2>
        <a href='{emailVetifyURL}' class='btn btn-primary'>驗證信箱</a>
<p>*此連結將在30秒後失效</p>
    </body>
    </html>
",
                EmailToName = email
            };
            if (SendEmail(mailData))
            {
                return Ok();
            }
            else
            {
                return StatusCode(500, "系統異常：寄送驗證信失敗");
            }
        }


        [AllowAnonymous]
        public string GetEmailVetifyURL(string email)
        {
            int? memberId = _context.TMembers.SingleOrDefault(m => m.FEmail == email)?.FMemberId;
            if (memberId == null) { return null; }
            //int? memberId = 6;
            if (memberId == null) { return null; }
            var claims = new List<Claim>
{
    new Claim(JwtRegisteredClaimNames.Email, email),
     new Claim("memberId", memberId.ToString())
};

            var expirationTime = DateTime.UtcNow.AddSeconds(30);
            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:KEY"])), SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                claims: claims,
                expires: expirationTime,
                signingCredentials: creds
            );
            var token = new JwtSecurityTokenHandler().WriteToken(jwt);
            //string orignal = Url.Action("VertifyEmail", "Member",new { token = token });
            string orignal = Url.Action(action: "VertifyEmail", controller: "Member", new { token = token }, protocol: "https");
            //string orignal = token;

            return orignal;
        }

        [AllowAnonymous]
        public IActionResult TestGetURL(string email)
        {
            string url = GetEmailVetifyURL(email)??"";
            //JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            //JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(url);
            //string exp = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            //    return Content(jwtToken.ValidTo.ToString()+"   " + DateTime.UtcNow.ToString());
            return Content(url);
        }
        [AllowAnonymous]
        public IActionResult VertifyEmail(string token)
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
            string? email = jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            if (email == null) { return StatusCode(500, "系統異常"); }

            if (jwtToken.ValidTo < DateTime.UtcNow)
            {
                TempData["Error"] = "連結已失效，請登入重新取得驗證信！";
                return RedirectToAction("Login", "Home");
            }
            else
            {
                try
                {
                    var dbMember = _context.TMembers.SingleOrDefault(m => m.FEmail == email);
                    if (dbMember == null) { return StatusCode(500, "資料庫系統異常1" ); }
                    if (dbMember.FEmailVerification == true) {
                        TempData["Error"] = "此會員已驗證信箱，請嘗試登入";
                        return RedirectToAction("Login", "Home");
                    }
                    dbMember.FEmailVerification = true;
                    _context.TMembers.Update(dbMember);
                    _context.SaveChanges();
                        TempData["Success"] = "信箱驗證成功！";
                    return RedirectToAction("Login", "Home");

                }
                catch (Exception ex)
                {
                    return StatusCode(500, "資料庫系統異常2"+ex);
                }
            }
            //return Content(jwtToken.ValidTo.ToString() + "   " + DateTime.UtcNow.ToString());
        }
        //植入假資料用
        [AllowAnonymous]
        [HttpGet]
        public IActionResult courseFakeData(int id)
        {
            var course = _context.TLessonCourses.Include(l=>l.FTeacher).FirstOrDefault(l => l.FLessonCourseId == id);
            if(course == null) { return Content("沒有這堂課"); }
            var teacherMemberId = course.FTeacher.FMemberId;
            var MemberIdList = _context.TMembers.Select(m=>m.FMemberId).ToList();
            MemberIdList.Remove(teacherMemberId);
            int orderNumber = new Random().Next((int)course.FMinPeople, ((int)course.FMaxPeople + 1));
            for(int i = 0; i < orderNumber; i++)
            {
                TOrder order = new TOrder
                {
                    FMemberId = MemberIdList[(new Random().Next(0, MemberIdList.Count))],
                    FOrderDate = ((DateTime)course.FRegDeadline).AddDays(-1),
                    FPaymentMethod = "CreditCard",
                };
                _context.TOrders.Add(order);
                _context.SaveChanges();
                TOrder o = _context.TOrders.OrderBy(o=>o.FOrderId).Last();
                TOrderDetail orderdetail = new TOrderDetail
                {
                    FOrderId = o.FOrderId,
                    FLessonCourseId = id,
                    FDiscount = 0,
                    FLessonPrice = (decimal)course.FPrice,
                    FModificationDescription = null,
                    FOrderValid = true
                };
                _context.TOrderDetails.Add(orderdetail);
                _context.SaveChanges();
                TOrderDetail od = _context.TOrderDetails.OrderBy(o => o.FOrderDetailId).Last();
                TLessonEvaluation lessonEvaluation = new TLessonEvaluation
                {
                    FMemberId = o.FMemberId,
                    FComment = " ",
                    FCommentDate = ((DateTime)course.FLessonDate).AddDays(1),
                    FDisplayStatus = true,
                    FOrderDetailId = od.FOrderDetailId,
                    FScore = new Random().Next(3,6)
                };
                _context.TLessonEvaluations.Add(lessonEvaluation);
                _context.SaveChanges();
                MemberIdList.Remove(o.FMemberId);
            }
            return Content("假資料成功");
        }
        //重開課程用
        [AllowAnonymous]
        [HttpGet]
        public IActionResult ReOcourse()
        {
            var historycourses = _context.TLessonCourses.ToList();
            foreach (var c in historycourses)
            {
                var reC = new TLessonCourse {
                    FName = c.FName,
                    FSubjectId = c.FSubjectId,
                    FCode = c.FCode,
                    FTeacherId = c.FTeacherId,
                    FDescription = c.FDescription,
                    FRequirement = c.FRequirement,
                    FPrice = c.FPrice,
                    FHomeworkDescription = c.FHomeworkDescription,
                    FMaxPeople = c.FMaxPeople,
                    FMinPeople = c.FMinPeople,
                    FLessonDate = GetRandomDate(new DateTime(2024, 3, 30), new DateTime(2024, 4, 12)),
                    FStartTime = c.FStartTime,
                    FEndTime = c.FEndTime,
                    FVenueType = c.FVenueType,
                    FVenueName = c.FVenueName,
                    FOnlineLink = c.FOnlineLink,
                    FAddressDetail = c.FAddressDetail,
                    FDistrictId = c.FDistrictId,
                    FStatus = "開放報名"
                };
                reC.FRegDeadline = ((DateTime)reC.FLessonDate).AddDays(-1);
                _context.TLessonCourses.Add(reC);
            }
            _context.SaveChanges();
            return Content("複製舊課成功");
        }
        //重開課程報名用
        [AllowAnonymous]
        [HttpGet]
        public IActionResult REGC()
        {
            var ReOcourse = _context.TLessonCourses.Include(l=>l.FTeacher).Where(l => l.FStatus == "開放報名").ToList();
            foreach (var course in ReOcourse)
            {
                var teacherMemberId = course.FTeacher.FMemberId;
                var MemberIdList = _context.TMembers.Select(m => m.FMemberId).ToList();
                MemberIdList.Remove(teacherMemberId);
                int orderNumber = new Random().Next(2, ((int)course.FMinPeople + 2));
                for (int i = 0; i < orderNumber; i++)
                {
                    TOrder order = new TOrder
                    {
                        FMemberId = MemberIdList[(new Random().Next(0, MemberIdList.Count))],
                        FOrderDate = GetRandomDate(new DateTime(2024, 3, 18), new DateTime(2024, 3, 28)),
                        FPaymentMethod = "CreditCard",
                    };
                    _context.TOrders.Add(order);
                    _context.SaveChanges();
                    TOrder o = _context.TOrders.OrderBy(o => o.FOrderId).Last();
                    TOrderDetail orderdetail = new TOrderDetail
                    {
                        FOrderId = o.FOrderId,
                        FLessonCourseId =course.FLessonCourseId,
                        FDiscount = 0,
                        FLessonPrice = (decimal)course.FPrice,
                        FModificationDescription = null,
                        FOrderValid = true
                    };
                    _context.TOrderDetails.Add(orderdetail);
                    _context.SaveChanges();
                    MemberIdList.Remove(o.FMemberId);
                }
            }
            return Content("假資料報名成功");
        }
        //訂單補訂單編號
        [AllowAnonymous]
        [HttpGet]
        public IActionResult AddOrderNumber()
        {
            var orders = _context.TOrders.OrderBy(o=>o.FOrderDate).ToList();
            foreach (var o in orders)
            {
                o.FOrderNumber = GetOrderNumber(o.FOrderDate);
                _context.TOrders.Update(o);
                _context.SaveChanges();
            }
            
            return Ok("成功！");
        }

        [AllowAnonymous]
            static DateTime GetRandomDate(DateTime startDate, DateTime endDate)
            {
                Random random = new Random();
                int range = (endDate - startDate).Days; // 計算日期範圍

                // 生成隨機日期
                DateTime randomDate = startDate.AddDays(random.Next(range));

                return randomDate;
            }



        [AllowAnonymous]
        // 獲取OrderNumber
        private string GetOrderNumber(DateTime date)
        {
            DateTime start = date.Date;
            DateTime end = start.AddDays(1);
            int no = _context.TOrders.Where(o => o.FOrderDate < end && o.FOrderDate >= start &&o.FOrderNumber!=null).Count() + 1;
            string orderNumber = "LSL" + start.ToString("yyMMdd") + $"{no:D4}";
            return orderNumber;
        }
    }
}
