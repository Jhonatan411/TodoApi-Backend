using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Data;
using TodoApi.Entities;
using TodoApi.DTOs;

namespace TodoApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PatientsController(AppDbContext context)
        {
            _context = context;
        }

        // POST /api/patients
        [HttpPost]
        public async Task<IActionResult> Create(PatientCreateDto dto)
        {
            // Validar duplicados
            var exists = await _context.Patients
                .AnyAsync(p => p.DocumentType == dto.DocumentType && p.DocumentNumber == dto.DocumentNumber);

            if (exists)
                return Conflict(new { message = "Ya existe un paciente con ese documento" });

            var entity = new Patient
            {
                DocumentType = dto.DocumentType,
                DocumentNumber = dto.DocumentNumber,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                BirthDate = dto.BirthDate,
                PhoneNumber = dto.PhoneNumber,
                Email = dto.Email
            };

            _context.Patients.Add(entity);
            await _context.SaveChangesAsync();

            var result = new PatientDto
            {
                PatientId = entity.PatientId,
                DocumentType = entity.DocumentType,
                DocumentNumber = entity.DocumentNumber,
                FirstName = entity.FirstName,
                LastName = entity.LastName,
                BirthDate = entity.BirthDate,
                PhoneNumber = entity.PhoneNumber,
                Email = entity.Email,
                CreatedAt = entity.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = entity.PatientId }, result);
        }

        // GET /api/patients
        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] int page = 1, [FromQuery] int pageSize = 10,
                                            [FromQuery] string? name = null,
                                            [FromQuery] string? documentNumber = null)
        {
            var query = _context.Patients.AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
                query = query.Where(p => (p.FirstName + " " + p.LastName).Contains(name));

            if (!string.IsNullOrWhiteSpace(documentNumber))
                query = query.Where(p => p.DocumentNumber == documentNumber);

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Transformar a DTO
            var patients = items.Select(p => new PatientDto
            {
                PatientId = p.PatientId,
                DocumentType = p.DocumentType,
                DocumentNumber = p.DocumentNumber,
                FirstName = p.FirstName,
                LastName = p.LastName,
                BirthDate = p.BirthDate,
                PhoneNumber = p.PhoneNumber,
                Email = p.Email,
                CreatedAt = p.CreatedAt
            }).ToList();

            var result = new{
                total,
                page,
                pageSize,
                items = patients  
            };


            return Ok(result);
        }


        // GET /api/patients/created-after?after=2025-01-01
        [HttpGet("filter/created-after")]
        public async Task<IActionResult> GetCreatedAfter([FromQuery] string after){
            if (!DateTime.TryParse(after, out var parsed))
                return BadRequest(new { message = "Formato de fecha inválido. Usa YYYY-MM-DD" });

            var patients = await _context.Patients
                .FromSqlInterpolated($"EXEC dbo.GetPatientsCreatedAfter {parsed}")
                .ToListAsync();

            var result = patients.Select(p => new PatientDto{
                PatientId = p.PatientId,
                DocumentType = p.DocumentType,
                DocumentNumber = p.DocumentNumber,
                FirstName = p.FirstName,
                LastName = p.LastName,
                BirthDate = p.BirthDate,
                PhoneNumber = p.PhoneNumber,
                Email = p.Email,
                CreatedAt = p.CreatedAt
            });

            return Ok(result);
        }

        // GET /api/patients/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            return Ok(new PatientDto
            {
                PatientId = patient.PatientId,
                DocumentType = patient.DocumentType,
                DocumentNumber = patient.DocumentNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                BirthDate = patient.BirthDate,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email,
                CreatedAt = patient.CreatedAt
            });
        }

        // PUT /api/patients/{id} (reemplazo completo)
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, PatientCreateDto dto)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            // Verificar duplicados si cambia doc
            if (patient.DocumentType != dto.DocumentType || patient.DocumentNumber != dto.DocumentNumber)
            {
                var exists = await _context.Patients
                    .AnyAsync(p => p.DocumentType == dto.DocumentType && p.DocumentNumber == dto.DocumentNumber && p.PatientId != id);
                if (exists)
                    return Conflict(new { message = "Otro paciente ya tiene ese documento" });
            }

            patient.DocumentType = dto.DocumentType;
            patient.DocumentNumber = dto.DocumentNumber;
            patient.FirstName = dto.FirstName;
            patient.LastName = dto.LastName;
            patient.BirthDate = dto.BirthDate;
            patient.PhoneNumber = dto.PhoneNumber;
            patient.Email = dto.Email;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/patients/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) return NotFound();

            _context.Patients.Remove(patient);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Patch /api/patients/{id}
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(int id, [FromBody] JsonPatchDocument<PatientUpdateDto> patchDoc){
            
            if(patchDoc == null ) 
                return BadRequest();

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null) 
                return NotFound();

            //Mapear las entidades DTO de actualizacion
            var patientToPatch = new PatientUpdateDto{
                DocumentType = patient.DocumentType,
                DocumentNumber = patient.DocumentNumber,
                FirstName = patient.FirstName,
                LastName = patient.LastName,
                BirthDate = patient.BirthDate,
                PhoneNumber = patient.PhoneNumber,
                Email = patient.Email
            };

            //Aplicar patch
            patchDoc.ApplyTo(patientToPatch, ModelState);

            if(!TryValidateModel(patientToPatch))
                return ValidationProblem(ModelState);

            //Validar duplicados si cambian el documento
            if((patientToPatch.DocumentType != null && patientToPatch.DocumentType != patient.DocumentType)||
                patientToPatch.DocumentNumber != null && patientToPatch.DocumentNumber != patient.DocumentNumber){

                    var newType = patientToPatch.DocumentType ?? patient.DocumentType;
                    var newNumber = patientToPatch.DocumentNumber ?? patient.DocumentNumber;

                    var exists = await _context.Patients.AnyAsync(p => p.DocumentType == newType 
                        && p.DocumentNumber == newNumber && p.PatientId != id);

                    if(exists)
                        return Conflict(new {message = "Otro paciente ya tiene ese documento"});
                }
            
            //Mapear DTO actualizando las entidades
            patient.DocumentType = patientToPatch.DocumentType ?? patient.DocumentType;
            patient.DocumentNumber = patientToPatch.DocumentNumber ?? patient.DocumentNumber;
            patient.FirstName = patientToPatch.FirstName ?? patient.FirstName;
            patient.LastName = patientToPatch.LastName ?? patient.LastName;
            patient.BirthDate = patientToPatch.BirthDate ?? patient.BirthDate;
            patient.PhoneNumber = patientToPatch.PhoneNumber ?? patient.PhoneNumber;
            patient.Email = patientToPatch.Email ?? patient.Email;

            await _context.SaveChangesAsync();

            return NoContent();
        }

    }
}
