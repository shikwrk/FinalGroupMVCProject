﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace FinalGroupMVCPrj.Models;

public partial class TVenue
{
    public int FVenueId { get; set; }

    public int FVenueProviderId { get; set; }

    public string FVenueCode { get; set; }

    public string FVenueName { get; set; }

    public int FDistrictId { get; set; }

    public string FAddressDetail { get; set; }

    public DateTime? FAddedTime { get; set; }

    public short FMaxPeople { get; set; }

    public decimal FPriceHalfHr { get; set; }

    public bool FOpenStatus { get; set; }

    public bool FReviewStatus { get; set; }

    public string FNote { get; set; }

    public virtual TVenueProviderInfo FVenueProvider { get; set; }
}