﻿using Practise.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Practise.ViewModels
{
    public class CategoryViewModel
    {
        public Category Category { get; set; }

        public List<Category> Categories { get; set; }
    }
}