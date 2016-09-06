using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineTastingGuides
    {
        public int WineTastingGuideId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string BackgroundImagePath { get; set; }
        public bool IsActive { get; set; }
    }
}
