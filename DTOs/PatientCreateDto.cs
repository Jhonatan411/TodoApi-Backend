using System.ComponentModel.DataAnnotations;

namespace TodoApi.DTOs{
    
    public class PatientCreateDto{

        [Required, MaxLength(10)]
        public string DocumentType { get; set; } = null!;

        [Required, MaxLength(20)]
        public string DocumentNumber { get; set; } = null!;

        [Required, MaxLength(80)]       
        public string FirstName { get; set; } = null!;

        [Required, MaxLength(80)]
        public string LastName { get; set; } = null!;

        [Required]
        public DateTime BirthDate { get; set;}

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress, MaxLength(120)]
        public string? Email { get; set; }
    }
}