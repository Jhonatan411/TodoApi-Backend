// Entities/Patient.cs

using System;

namespace TodoApi.Entities{
    public class Patient{
        public int PatientId { get; set; }                 // PK autoincremental
        public string DocumentType { get; set; } = null!;  // Tipo de documento (CC, TI, PAS, etc.)
        public string DocumentNumber { get; set; } = null!; // Número de documento (validación de duplicados con Type)
        public string FirstName { get; set; } = null!;     // Nombre
        public string LastName { get; set; } = null!;      // Apellido
        public DateTime BirthDate { get; set; }            // Fecha de nacimiento
        public string? PhoneNumber { get; set; }           // Teléfono opcional
        public string? Email { get; set; }                 // Email opcional
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Fecha de creación (default: ahora)
    }
}