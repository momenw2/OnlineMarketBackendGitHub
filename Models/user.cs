using System;
using System.ComponentModel.DataAnnotations;

namespace OnlineMarketApi.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } // GUID-based ID

        public string FullName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime BirthDate { get; set; }
        public string Gender { get; set; }
        public string PhoneNumber { get; set; }

        public User()
        {
            Id = Guid.NewGuid();
        }
    }
}
