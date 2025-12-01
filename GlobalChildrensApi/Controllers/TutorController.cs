using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class TutorController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TutorController(AppDbContext db)
        {
            _db = db;
        }

        // Obtener todos los tutores activos (SOLICITADO)
        [HttpGet("GetAllTutores")]
        public async Task<ActionResult<IEnumerable<Tutor>>> GetAllTutores()
        {
            try
            {
                var tutores = await _db.tutor
                    .Where(t => t.estado == "ACT")
                    .OrderBy(t => t.tutorid)
                    .ToListAsync();

                return Ok(new
                {
                    total = tutores.Count,
                    tutores = tutores
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        // Obtener tutor por ID
        [HttpGet("GetById/{id:long}")]
        public async Task<ActionResult<Tutor>> GetById(long id)
        {
            try
            {
                var tutor = await _db.tutor.FirstOrDefaultAsync(t => t.tutorid == id);

                if (tutor == null)
                    return NotFound(new
                    {
                        error = "TUTOR_NO_ENCONTRADO",
                        message = $"No existe un tutor con el id {id}."
                    });

                if (tutor.estado != "ACT")
                    return BadRequest(new
                    {
                        error = "TUTOR_INACTIVO",
                        message = $"El tutor con id {id} existe pero no está activo (Estado = {tutor.estado})."
                    });

                return Ok(tutor);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        // Obtener tutor por persona ID
        [HttpGet("GetByPersona/{personaid:long}")]
        public async Task<ActionResult<Tutor>> GetByPersona(long personaid)
        {
            try
            {
                var tutor = await _db.tutor
                    .FirstOrDefaultAsync(t => t.personaid == personaid && t.estado == "ACT");

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        error = "TUTOR_NO_ENCONTRADO",
                        message = $"No existe un tutor asociado a la persona con id {personaid}."
                    });
                }

                return Ok(tutor);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        // Crear tutor (SOLICITADO)
        [HttpPost("CreateTutor")]
        public async Task<ActionResult<Tutor>> CreateTutor([FromBody] Tutor tutor)
        {
            try
            {
                // VALIDACIÓN 1: Verificar que la persona exista
                var persona = await _db.persona.FindAsync(tutor.personaid);
                if (persona == null)
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_NO_EXISTE",
                        message = $"La persona con ID {tutor.personaid} no existe en el sistema.",
                        constraint = "tutor_personaid_fkey"
                    });
                }

                // VALIDACIÓN 2: Verificar que la persona no esté ya registrada como tutor
                var tutorExistente = await _db.tutor
                    .FirstOrDefaultAsync(t => t.personaid == tutor.personaid);

                if (tutorExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_YA_ES_TUTOR",
                        message = $"La persona con ID {tutor.personaid} ya está registrada como tutor (Tutor ID: {tutorExistente.tutorid}, Estado: {tutorExistente.estado}).",
                        constraint = "uq_tutor_persona",
                        tutor_existente_id = tutorExistente.tutorid,
                        tutor_existente_estado = tutorExistente.estado
                    });
                }

                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(tutor.estado))
                    tutor.estado = "ACT";

                _db.tutor.Add(tutor);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = tutor.tutorid }, tutor);
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("tutor_personaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_NO_EXISTE",
                        message = $"La persona con ID {tutor.personaid} no existe en el sistema.",
                        constraint = "tutor_personaid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_tutor_persona"))
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_YA_ES_TUTOR",
                        message = $"La persona con ID {tutor.personaid} ya está registrada como tutor.",
                        constraint = "uq_tutor_persona"
                    });
                }

                return StatusCode(503, new
                {
                    error = "ERROR_BASE_DATOS",
                    message = "Error al guardar en la base de datos.",
                    details = innerMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        // Actualizar tutor (SOLICITADO)
        [HttpPut("UpdateTutor/{id:long}")]
        public async Task<IActionResult> UpdateTutor(long id, [FromBody] Tutor tutor)
        {
            try
            {
                if (id != tutor.tutorid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({tutor.tutorid})."
                    });

                var existing = await _db.tutor.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "TUTOR_NO_ENCONTRADO",
                        message = $"No existe un tutor con el id {id}."
                    });

                // VALIDACIÓN 1: Verificar que la persona exista
                var persona = await _db.persona.FindAsync(tutor.personaid);
                if (persona == null)
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_NO_EXISTE",
                        message = $"La persona con ID {tutor.personaid} no existe en el sistema.",
                        constraint = "tutor_personaid_fkey"
                    });
                }

                // VALIDACIÓN 2: Verificar que la persona no esté ya registrada como otro tutor
                var tutorExistente = await _db.tutor
                    .FirstOrDefaultAsync(t => t.personaid == tutor.personaid && t.tutorid != id);

                if (tutorExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_YA_ES_TUTOR",
                        message = $"La persona con ID {tutor.personaid} ya está registrada como otro tutor (Tutor ID: {tutorExistente.tutorid}, Estado: {tutorExistente.estado}).",
                        constraint = "uq_tutor_persona",
                        tutor_existente_id = tutorExistente.tutorid,
                        tutor_existente_estado = tutorExistente.estado
                    });
                }

                // Actualizamos campos
                existing.estado = tutor.estado;
                existing.personaid = tutor.personaid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Tutor actualizado correctamente",
                    tutor_id = existing.tutorid,
                    persona_id = existing.personaid,
                    estado = existing.estado
                });
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("tutor_personaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_NO_EXISTE",
                        message = $"La persona con ID {tutor.personaid} no existe en el sistema.",
                        constraint = "tutor_personaid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_tutor_persona"))
                {
                    return BadRequest(new
                    {
                        error = "PERSONA_YA_ES_TUTOR",
                        message = $"La persona con ID {tutor.personaid} ya está registrada como tutor.",
                        constraint = "uq_tutor_persona"
                    });
                }

                return StatusCode(503, new
                {
                    error = "ERROR_BASE_DATOS",
                    message = "Error al guardar en la base de datos.",
                    details = innerMessage
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }

        // Inactivar tutor (SOLICITADO)
        [HttpDelete("InactivateTutor/{id:long}")]
        public async Task<IActionResult> InactivateTutor(long id)
        {
            try
            {
                var tutor = await _db.tutor.FindAsync(id);
                if (tutor == null)
                    return NotFound(new
                    {
                        error = "TUTOR_NO_ENCONTRADO",
                        message = $"No existe un tutor con el id {id}."
                    });

                if (tutor.estado == "INA")
                {
                    return BadRequest(new
                    {
                        error = "TUTOR_YA_INACTIVO",
                        message = $"El tutor con id {id} ya está inactivo."
                    });
                }

                // VALIDACIÓN: Verificar si el tutor tiene aulas asignadas actualmente
                var aulasAsignadas = await _db.aula
                    .Where(a => a.tutorid == id && a.activo == true && a.estado == "ACT")
                    .ToListAsync();

                if (aulasAsignadas.Any())
                {
                    var nombresAulas = string.Join(", ", aulasAsignadas.Select(a => $"'{a.nombre}' (Grado {a.grado}º)"));
                    
                    return BadRequest(new
                    {
                        error = "TUTOR_TIENE_AULAS_ASIGNADAS",
                        message = $"No se puede inactivar el tutor porque tiene {aulasAsignadas.Count} aula(s) activa(s) asignada(s): {nombresAulas}. Debe reasignar o inactivar las aulas primero.",
                        aulas_asignadas = aulasAsignadas.Select(a => new
                        {
                            aula_id = a.aulaid,
                            nombre = a.nombre,
                            grado = a.grado
                        })
                    });
                }

                tutor.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Tutor inactivado correctamente",
                    tutor_id = tutor.tutorid,
                    persona_id = tutor.personaid
                });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    error = "ERROR_SERVIDOR",
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    details = ex.Message
                });
            }
        }
    }
}

