﻿using FinalGroupMVCPrj.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using FinalGroupMVCPrj.Models.DTO;
using FinalGroupMVCPrj.Models.ViewModels;
using System.Web;
using Azure.Core;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;




namespace FinalGroupMVCPrj.Controllers
{
    [AllowAnonymous]
    public class HomeController : UserInfoController
    {
        private readonly ILogger<HomeController> _logger;
        private readonly LifeShareLearnContext _context;
        string redirect_uri = "https://localhost:7031/Home/LoginDeal";
        string client_id = "2003951855";
        string client_secret = "e7acea74b7f394a0c1735c1bfa5370c0";

        public HomeController(ILogger<HomeController> logger, LifeShareLearnContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var fields = await _context.TCourseFields.Select(u => u.FFieldName).ToListAsync();
            var courseList = await _context.TLessonCourses
            .Where(u => u.FStatus == "開放報名")
            .OrderByDescending(t => t.FLessonDate)
            .Select(course => new LessonCourseVM
            {
                lessonCourse = course,
                // 老師名稱
                teacherName = _context.TTeachers
                    .Where(teacher => teacher.FTeacherId == course.FTeacherId)
                    .Select(teacher => teacher.FTeacherName)
                    .FirstOrDefault() ?? "找不到當前老師",
                // 科目名稱
                subjectName = _context.TCourseSubjects
                    .Where(sub => sub.FSubjectId == course.FSubjectId)
                    .Select(sub => sub.FSubjectName)
                    .FirstOrDefault() ?? "找不到科目名稱",
                // 圖片數據
                imageData = course.FPhoto,
                // 新增老師
                teacher = course.FTeacher, // 將加載的 Teacher 導航屬性賦值給 ViewModel 的 teacher 屬性
                fields = fields,
                fieldName = course.FSubject.FField.FFieldName,
                fieldNumber = course.FSubject.FFieldId,
                TeacherImage = course.FTeacher.FTeacherProfilePic,
            })
            .ToListAsync();
            return View(courseList);

        }
        public IActionResult Test()
        {
            return View();
        }





        //■ ==========================     歐文作業區      ==========================■
        public IActionResult Login()
        {

            return View();
        }

        [HttpPost]
        public IActionResult Login(TMember member)
            {
            var dbMember = _context.TMembers
                .Where(m => m.FEmail == member.FEmail)
                .SingleOrDefault();
            if (dbMember == null)
            {
                return BadRequest("帳號不存在");
            }
            bool pwCheck = false;
            //測試解密驗證
            try
            {
                pwCheck = BCrypt.Net.BCrypt.EnhancedVerify(member.FPassword, dbMember.FPassword);
            }catch (Exception ex)
            {
                return StatusCode(500, "密碼系統異常");
            }


            if (!pwCheck)
            {
                return BadRequest("密碼錯誤");
            }
            else if ((bool)!dbMember.FEmailVerification)
            {
                return BadRequest("信箱未驗證");
            } else if (dbMember.FStatus ==false)
            {
                return BadRequest("帳號停權中，詳情請洽客服");
            }
            else
            {
                var claims = new List<Claim>{
                  new Claim("MemberId", dbMember.FMemberId.ToString()),
                new Claim(ClaimTypes.Name, dbMember.FShowName),
                new Claim(ClaimTypes.Email, dbMember.FEmail)
                };
                if (dbMember.FQualifiedTeacher)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Teacher"));
                }
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                TempData["login"] = "登入成功";
                return Ok();
            }
        }

        //舊版同步登入
        [HttpPost]
        public IActionResult Login2(TMember member)
        {
            var dbMember = _context.TMembers
                .Where(m => m.FEmail == member.FEmail && m.FPassword == member.FPassword)
                .SingleOrDefault();
            if (dbMember == null)
            {
                return Content("帳號密錯誤");
            }
            else if ((bool)!dbMember.FEmailVerification)
            {
                return Content("信箱未驗證");
            }
            else
            {
                var claims = new List<Claim>{
                  new Claim("MemberId", dbMember.FMemberId.ToString()),
                new Claim(ClaimTypes.Name, dbMember.FShowName),
                new Claim(ClaimTypes.Email, dbMember.FEmail)
                };
                if (dbMember.FQualifiedTeacher)
                {
                    claims.Add(new Claim(ClaimTypes.Role, "Teacher"));
                }
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return RedirectToAction("Index");
            }
        }
        
        //登出
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }
        public IActionResult testAPI()
        {
            MemberInfoDTO member = new MemberInfoDTO { Email = "ssdasdad@sdasdas", MemberId = 5, ShowName = "2222", RealName = "5555" };
            return Json(member);
        }

        // POST: Home/teacherApply
        //動作簡述：老師申請資料異動結果
        [HttpPost]
        public IActionResult TeacherApply([FromBody] TTeacherApplyLog data)
        {
            string message = "";
            try
            {
            if (data==null) { return BadRequest(new { Message = "操作失敗", Data = data }); }
                var checkExist = _context.TTeacherApplyLogs.FirstOrDefault(r=>r.FMemberId==data.FMemberId);
            if (checkExist ==null)
            {
                data.FApplyDatetime = DateTime.Now;
                    data.FProgressStatus = "待審核";
                _context.TTeacherApplyLogs.Add(data);
                _context.SaveChanges();
                message = "新增成功";
            }
            else {
                    if (data.FProgressStatus != checkExist.FProgressStatus) { return BadRequest("審核狀態不相符，不當操作"); }
                    checkExist.FProgressStatus = "待審核";
                    checkExist.FReviewDatetime = DateTime.Now;
                    if ("請補件請修正內容".Contains(data.FProgressStatus))
                    {
                        checkExist.FPdfLink = data.FPdfLink;
                        checkExist.FTeacherPlanLink = data.FTeacherPlanLink;
                        checkExist.FNote = "已補件：" + data.FNote;
                    }
                    if(data.FProgressStatus == "請修正內容")
                    {
                        checkExist.FRealName = data.FRealName;
                        checkExist.FTeacherName = data.FTeacherName;
                        checkExist.FIntroduction = data.FIntroduction;
                        checkExist.FReason = data.FReason;
                        checkExist.FNote = "已提交修正內容：" + data.FNote;
                    }
                    _context.Update(checkExist);
                _context.SaveChanges();
                message = "更新成功";
            }
            return Ok(new { Message = message, Data = data });
            } catch (Exception ex)
            {
                return StatusCode(500, "伺服器錯誤：" + ex.Message);
            }
        }
        // GET: Home/TApplyRecord?memberId=
        //動作簡述：回傳設定會員申請老師資料
        [HttpGet]
        public IActionResult TApplyRecord(int memberId)
        {
            try
            {
                if (memberId ==0) { return BadRequest("會員ID為0"); }
                TTeacherApplyLog? record = _context.TTeacherApplyLogs.FirstOrDefault(r => r.FMemberId == memberId);
                return Ok(record);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "伺服器錯誤：" + ex.Message);
            }
        }
        /// 產生新的LineLoginUrl
        public IActionResult GetLineLoginUrl()
        {
            //state使用隨機字串比較安全
            //每次Ajax Request都產生不同的state字串，避免駭客拿固定的state字串將網址掛載自己的釣魚網站獲取用戶的Line個資授權(CSRF攻擊)
            string state = Guid.NewGuid().ToString();
            TempData["state"] = state;//利用TempData被取出資料後即消失的特性，來防禦CSRF攻擊
            //如果是ASP.net Form，就改成放入Session或Cookie，之後取出資料時再把Session或Cookie設為null刪除資料
            string LineLoginUrl =
             $@"https://access.line.me/oauth2/v2.1/authorize?response_type=code&client_id={client_id}&redirect_uri={redirect_uri}&state={state}&scope=openid%20profile&ui_locales=zh-TW";
            return Content(LineLoginUrl);
        }
        public IActionResult LoginDeal(string state, string code)
        {
            if (TempData["state"] == null)
            {//可能使用者停留Line登入頁面太久
                return Content("頁面逾期");
            }

            if (Convert.ToString(TempData["state"]) != state)
            {//使用者原先Request QueryString的TempData["state"]和Line導頁回來夾帶的state Querystring不一樣，可能是parameter tampering或CSRF攻擊
                return Content("state驗證失敗");
            }

            if (Convert.ToString(TempData["state"]) == state)
            {//state字串驗證通過

                //取得id_token和access_token:https://developers.line.biz/en/docs/line-login/web/integrate-line-login/#spy-getting-an-access-token
                string issue_token_url = "https://api.line.me/oauth2/v2.1/token";
                var request = new HttpRequestMessage(HttpMethod.Post, issue_token_url);

                var postParams = new Dictionary<string, string>
    {
        {"grant_type", "authorization_code"},
        {"code", code},
        {"redirect_uri", this.redirect_uri},
        {"client_id", this.client_id},
        {"client_secret", this.client_secret}
    };

                request.Content = new FormUrlEncodedContent(postParams);
                using (var client = new HttpClient())
                {
                    var response = client.SendAsync(request).Result;

                    if (response.IsSuccessStatusCode)
                    {
                        var responseStr = response.Content.ReadAsStringAsync().Result;

                        // 在這裡處理 API 回傳的字串 (responseStr)
                        LineLoginToken tokenObj = JsonConvert.DeserializeObject<LineLoginToken>(responseStr);
                        string id_token = tokenObj.id_token;
                        var jst = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(id_token);
                        var userId = jst.Payload.Sub;
                       
                        var dbMember = _context.TMembers.
                            SingleOrDefault(m => m.FNote == userId);
                        int currentMemberId = GetCurrentMemberId();
                        //沒有綁定的人要登入
                        if (dbMember == null && currentMemberId == 0) 
                        {
                            TempData["Error"] = "尚未綁定的Line帳號，請登入後前往綁定";
                            return RedirectToAction("Login", "Home");
                        }
                        //已登入，在帳號設定頁面請求綁定
                        else if (dbMember == null && currentMemberId != 0)
                        {
                            var toAddLineMember = _context.TMembers.SingleOrDefault(m => m.FMemberId == currentMemberId);
                            if (toAddLineMember == null) 
                            {
                                TempData["Error"] = "資料庫系統異常，找不到已登入會員";
                                return RedirectToAction("Login", "Home");
                            }
                            toAddLineMember.FNote = userId;
                            try
                            {
                                _context.TMembers.Update(toAddLineMember);
                                _context.SaveChanges();
                                TempData["Success"] = "Line帳號綁定成功！";
                                return RedirectToAction("Setting", "Member");
                            }
                            catch (Exception e)
                            {
                                TempData["Error"] = "資料庫系統異常，無法綁定" + e;
                                return RedirectToAction("Login", "Home");
                            }
                        }
                        else if (dbMember != null && currentMemberId != 0)
                        {
                            TempData["Error"] = "此Line帳號已有其他帳號綁定！";
                            return RedirectToAction("Setting", "Member");
                        } else if (dbMember.FStatus == false)
                    {
                        return BadRequest("帳號停權中，詳情請洽客服");
                    }
                    var claims = new List<Claim>{
                  new Claim("MemberId", dbMember.FMemberId.ToString()),
                new Claim(ClaimTypes.Name, dbMember.FShowName),
                new Claim(ClaimTypes.Email, dbMember.FEmail)
                };
                        if (dbMember.FQualifiedTeacher)
                        {
                            claims.Add(new Claim(ClaimTypes.Role, "Teacher"));
                        }
                        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                        HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                        TempData["login"] = "登入成功";
                        return RedirectToAction("Index");
                    }
                    else
                    {
                        // 處理錯誤狀態碼
                        var statusCode = response.StatusCode;
                        // ...

                        return StatusCode((int)statusCode);
                    }
                }
            }//end if 
            return StatusCode(500,"系統異常");
        }
        [HttpPost]
        public IActionResult LineLinkCancel( int id)
        {
            int currentMemberId = GetCurrentMemberId();
            if (id != currentMemberId) { return BadRequest("非當前登入會員"); }
            if (currentMemberId == 0) { return BadRequest("目前沒有會員登入"); }
                var toDeleteLineMember = _context.TMembers.SingleOrDefault(m => m.FMemberId == currentMemberId);
                if (toDeleteLineMember == null) { return BadRequest("資料庫系統異常，找不到已登入會員"); }
            toDeleteLineMember.FNote = null;
            try
            {
                _context.TMembers.Update(toDeleteLineMember);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest("資料庫系統異常，無法取消連結：" + e);
            }
        }

        [HttpGet]
        public IActionResult VertifyEmail(string email)
        {

            ViewBag.ToVertifyEmail = email;
            return View();
        }
    }
    public class LineLoginToken
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string id_token { get; set; }
        public string refresh_token { get; set; }
        public string scope { get; set; }
        public string token_type { get; set; }
    }
}
