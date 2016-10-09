using System;
using System.Collections.Generic;

namespace Task.UPCDB.Models
{
    public partial class WineJournals
    {
        public string UserId { get; set; }
        public string DeviceId { get; set; }
        public string PlaceId { get; set; }
        public int WineListWineListId { get; set; }
        public DateTime CreatedDate { get; set; }

        public virtual WineList WineListWineList { get; set; }
    }
}
