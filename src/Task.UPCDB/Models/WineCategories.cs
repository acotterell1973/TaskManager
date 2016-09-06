using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineCategories
    {
        public int WineCategoryId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string RegionMapPath { get; set; }
        public string ImagePath { get; set; }
        public string Region { get; set; }
        public string ShortDescription { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }
}
