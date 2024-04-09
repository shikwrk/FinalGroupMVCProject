﻿using System.ComponentModel.DataAnnotations;

namespace FinalGroupMVCPrj.Models.Metadatas
{
    public class TTeacherMatadata
    {
        //[Required(ErrorMessage = "老師名稱為必填")]
        [Display(Name = "老師名稱")]
        public string? FTeacherName { get; set; }
        [Display(Name = "老師頭像")]
        public byte[]? FTeacherProfilePic { get; set; }
        [Display(Name = "自我介紹")]
        public string? FIntroduction { get; set; }
        [Display(Name = "公開聯絡方式")]
        public string? FContactInfo { get; set; }
        [Display(Name = "備註")]
        public string? FNote { get; set; }
    }
}
