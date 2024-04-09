﻿using System.ComponentModel.DataAnnotations;

namespace FinalGroupMVCPrj.Models.ViewModels
{
    public class SingleEvaluationViewModel
    {
        public int FOrderDetailId { get; set; }
        public int FScore { get; set; }
        public string? FComment { get; set; }
        public DateTime FCommentDate { get; set; }

        public DateTime? FCommentUpdateDate { get; set; }
    }
}
