using System.Collections.Generic;
using GiftOfTheGiversFoundation.Models;

namespace GiftOfTheGiversFoundation.ViewModels
{
    public class AdminViewModel
    {
        public List<User> Users { get; set; }
       
        public List<Incident> Incidents { get; set; }
    }
}
