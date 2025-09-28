using System.ComponentModel.DataAnnotations;

namespace TodoApi.DTOs
{
    public class PatientUpdateDto
    {
        [MaxLength(10)]
        public string? DocumentType { get; set; }

        [MaxLength(20)]
        public string? DocumentNumber { get; set; }

        [MaxLength(80)]
        public string? FirstName { get; set; }

        [MaxLength(80)]
        public string? LastName { get; set; }

        public DateTime? BirthDate { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [EmailAddress, MaxLength(120)]
        public string? Email { get; set; }
    }
}
