﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication8.Models
{
    public class LpoDetail
    {
        public int LpoDetailId { get; set; }

        public int MaterialId { get; set; }
        public virtual Material Material { get; set; }

        public float Qty { get; set; }

        public float Price { get; set; }

        public int LpoId { get; set; }
        public virtual Lpo Lpo { get; set; }
    }
}