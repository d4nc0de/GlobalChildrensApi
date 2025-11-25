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
    public class AulaController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AulaController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ObtenerAulasActivas")]
        public async Task<ActionResult<IEnumerable<Aula>>> GetAll()
        {
            try
            {
                var aulas = await _db.aula
                .Where(a => a.estado == "ACT" && a.activo == true)
                .OrderBy(a => a.aulaid)
                .ToListAsync();

                return Ok(aulas);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpGet("ObtenerAulaPorId/{id:long}")]
        public async Task<ActionResult<Aula>> GetById(long id)
        {
            try
            {
                var aula = await _db.aula
                .FirstOrDefaultAsync(a => a.aulaid == id);

                if (aula == null)
                    return NotFound($"No existe un aula con el id {id}.");

                if (aula.estado != "ACT")
                    return BadRequest($"El aula con id {id} existe pero no está activa (Estado = {aula.estado}).");

                return Ok(aula);
            }
            catch (Exception ex)
            {
                return StatusCode(503, new
                {
                    message = "No fue posible comunicarse con la base de datos. Intenta de nuevo.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("CrearAula")]
        public async Task<ActionResult<Aula>> Create([FromBody] Aula aula)
        {
            try
            {
                // VALIDACIÓN 1: Grado debe ser 4, 5, 9 o 10
                if (aula.grado != 4 && aula.grado != 5 && aula.grado != 9 && aula.grado != 10)
                {
                    return BadRequest(new
                    {
                        error = "GRADO_INVALIDO",
                        message = $"El grado '{aula.grado}' no es válido. Solo se permiten los grados: 4, 5, 9 o 10.",
                        constraint = "chk_grado"
                    });
                }

                // VALIDACIÓN 2: Cupo máximo debe ser mayor a 0
                if (aula.cupo_maximo <= 0)
                {
                    return BadRequest(new
                    {
                        error = "CUPO_INVALIDO",
                        message = $"El cupo máximo debe ser mayor a 0. Valor recibido: {aula.cupo_maximo}",
                        constraint = "chk_cupo_maximo"
                    });
                }

                // VALIDACIÓN 3: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(aula.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del aula es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 4: Verificar que la sede exista y esté activa
                var sede = await _db.sede.FindAsync(aula.sedeid);
                if (sede == null)
                {
                    return BadRequest(new
                    {
                        error = "SEDE_NO_EXISTE",
                        message = $"La sede con ID {aula.sedeid} no existe en el sistema.",
                        constraint = "aula_sedeid_fkey"
                    });
                }
                if (sede.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "SEDE_INACTIVA",
                        message = $"La sede '{sede.nombre}' (ID: {aula.sedeid}) no está activa. Estado actual: {sede.estado}"
                    });
                }

                // VALIDACIÓN 5: Verificar constraint único (sedeid + grado + nombre)
                var aulaExistente = await _db.aula
                    .FirstOrDefaultAsync(a => a.sedeid == aula.sedeid 
                                           && a.grado == aula.grado 
                                           && a.nombre == aula.nombre);

                if (aulaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "AULA_DUPLICADA",
                        message = $"Ya existe un aula con el nombre '{aula.nombre}' para el grado {aula.grado}º en la sede '{sede.nombre}' (ID aula existente: {aulaExistente.aulaid}, Estado: {aulaExistente.estado}).",
                        constraint = "uq_aula_nombre_sede_grado",
                        aula_existente_id = aulaExistente.aulaid,
                        aula_existente_estado = aulaExistente.estado
                    });
                }

                // Establecer valores por defecto si no se proporcionan
                if (string.IsNullOrWhiteSpace(aula.estado))
                    aula.estado = "ACT";

                if (!aula.activo)
                    aula.activo = true;

                _db.aula.Add(aula);
                await _db.SaveChangesAsync();

                // Registrar el tutor inicial en el historial
                var historialTutor = new AulaTutorHistorico
                {
                    aulaid = aula.aulaid,
                    tutorid = aula.tutorid,
                    fecha_inicio = DateTime.UtcNow.Date, // Solo la fecha, en UTC
                    fecha_fin = null, // Aún está activo
                    motivo_cambio = "Asignación inicial de tutor al crear el aula",
                    estado = "ACT"
                };

                _db.aulatutorhistorico.Add(historialTutor);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = aula.aulaid }, aula);
            }
            catch (DbUpdateException dbEx)
            {
                // Capturar errores específicos de la base de datos
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aula_programaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "PROGRAMA_NO_EXISTE",
                        message = $"El programa con ID {aula.programaid} no existe en el sistema.",
                        constraint = "aula_programaid_fkey"
                    });
                }
                else if (innerMessage.Contains("aula_jornadaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_NO_EXISTE",
                        message = $"La jornada con ID {aula.jornadaid} no existe en el sistema.",
                        constraint = "aula_jornadaid_fkey"
                    });
                }
                else if (innerMessage.Contains("aula_tutorid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "TUTOR_NO_EXISTE",
                        message = $"El tutor con ID {aula.tutorid} no existe en el sistema.",
                        constraint = "aula_tutorid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_nombre_sede_grado"))
                {
                    return BadRequest(new
                    {
                        error = "AULA_DUPLICADA",
                        message = $"Ya existe un aula con el nombre '{aula.nombre}' para el grado {aula.grado}º en esta sede.",
                        constraint = "uq_aula_nombre_sede_grado"
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

        [HttpPut("ActualizarAula/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] Aula aula)
        {
            try
            {
                if (id != aula.aulaid)
                    return BadRequest(new
                    {
                        error = "ID_NO_COINCIDE",
                        message = $"El id de la URL ({id}) no coincide con el id del cuerpo ({aula.aulaid})."
                    });

                var existing = await _db.aula.FindAsync(id);
                if (existing == null)
                    return NotFound(new
                    {
                        error = "AULA_NO_ENCONTRADA",
                        message = $"No existe un aula con el id {id}."
                    });

                // VALIDACIÓN 1: Grado debe ser 4, 5, 9 o 10
                if (aula.grado != 4 && aula.grado != 5 && aula.grado != 9 && aula.grado != 10)
                {
                    return BadRequest(new
                    {
                        error = "GRADO_INVALIDO",
                        message = $"El grado '{aula.grado}' no es válido. Solo se permiten los grados: 4, 5, 9 o 10.",
                        constraint = "chk_grado"
                    });
                }

                // VALIDACIÓN 2: Cupo máximo debe ser mayor a 0
                if (aula.cupo_maximo <= 0)
                {
                    return BadRequest(new
                    {
                        error = "CUPO_INVALIDO",
                        message = $"El cupo máximo debe ser mayor a 0. Valor recibido: {aula.cupo_maximo}",
                        constraint = "chk_cupo_maximo"
                    });
                }

                // VALIDACIÓN 3: Nombre no puede estar vacío
                if (string.IsNullOrWhiteSpace(aula.nombre))
                {
                    return BadRequest(new
                    {
                        error = "NOMBRE_REQUERIDO",
                        message = "El nombre del aula es requerido y no puede estar vacío."
                    });
                }

                // VALIDACIÓN 4: Verificar que la sede exista y esté activa
                var sede = await _db.sede.FindAsync(aula.sedeid);
                if (sede == null)
                {
                    return BadRequest(new
                    {
                        error = "SEDE_NO_EXISTE",
                        message = $"La sede con ID {aula.sedeid} no existe en el sistema.",
                        constraint = "aula_sedeid_fkey"
                    });
                }
                if (sede.estado != "ACT")
                {
                    return BadRequest(new
                    {
                        error = "SEDE_INACTIVA",
                        message = $"La sede '{sede.nombre}' (ID: {aula.sedeid}) no está activa. Estado actual: {sede.estado}"
                    });
                }

                // VALIDACIÓN 5: Verificar constraint único (sedeid + grado + nombre)
                // Excepto el aula actual
                var aulaExistente = await _db.aula
                    .FirstOrDefaultAsync(a => a.sedeid == aula.sedeid 
                                           && a.grado == aula.grado 
                                           && a.nombre == aula.nombre
                                           && a.aulaid != id);

                if (aulaExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "AULA_DUPLICADA",
                        message = $"Ya existe otra aula con el nombre '{aula.nombre}' para el grado {aula.grado}º en la sede '{sede.nombre}' (ID aula existente: {aulaExistente.aulaid}, Estado: {aulaExistente.estado}).",
                        constraint = "uq_aula_nombre_sede_grado",
                        aula_existente_id = aulaExistente.aulaid,
                        aula_existente_estado = aulaExistente.estado
                    });
                }

                // REGISTRAR CAMBIO DE TUTOR EN HISTORIAL
                bool cambioTutor = existing.tutorid != aula.tutorid;
                long tutorAnteriorId = existing.tutorid;

                if (cambioTutor)
                {
                    // 1. Cerrar el registro del tutor anterior (poner fecha_fin)
                    var registroActual = await _db.aulatutorhistorico
                        .Where(h => h.aulaid == existing.aulaid 
                                 && h.tutorid == tutorAnteriorId 
                                 && h.fecha_fin == null
                                 && h.estado == "ACT")
                        .FirstOrDefaultAsync();

                    if (registroActual != null)
                    {
                        registroActual.fecha_fin = DateTime.UtcNow.Date; // Cerrar hoy en UTC
                    }

                    // 2. Crear nuevo registro con el nuevo tutor
                    var nuevoHistorial = new AulaTutorHistorico
                    {
                        aulaid = existing.aulaid,
                        tutorid = aula.tutorid,
                        fecha_inicio = DateTime.UtcNow.Date, // En UTC
                        fecha_fin = null, // Aún activo
                        motivo_cambio = "Cambio de tutor", // Puedes hacerlo parametrizable
                        estado = "ACT"
                    };

                    _db.aulatutorhistorico.Add(nuevoHistorial);
                }

                // Actualizamos campos
                existing.grado = aula.grado;
                existing.nombre = aula.nombre;
                existing.cupo_maximo = aula.cupo_maximo;
                existing.activo = aula.activo;
                existing.estado = aula.estado;
                existing.sedeid = aula.sedeid;
                existing.programaid = aula.programaid;
                existing.jornadaid = aula.jornadaid;
                existing.tutorid = aula.tutorid;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Aula actualizada correctamente",
                    aula_id = existing.aulaid,
                    nombre = existing.nombre,
                    grado = existing.grado,
                    cambio_tutor = cambioTutor,
                    tutor_anterior_id = cambioTutor ? tutorAnteriorId : (long?)null,
                    tutor_nuevo_id = cambioTutor ? aula.tutorid : (long?)null
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Capturar errores específicos de la base de datos
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;

                if (innerMessage.Contains("aula_programaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "PROGRAMA_NO_EXISTE",
                        message = $"El programa con ID {aula.programaid} no existe en el sistema.",
                        constraint = "aula_programaid_fkey"
                    });
                }
                else if (innerMessage.Contains("aula_jornadaid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "JORNADA_NO_EXISTE",
                        message = $"La jornada con ID {aula.jornadaid} no existe en el sistema.",
                        constraint = "aula_jornadaid_fkey"
                    });
                }
                else if (innerMessage.Contains("aula_tutorid_fkey"))
                {
                    return BadRequest(new
                    {
                        error = "TUTOR_NO_EXISTE",
                        message = $"El tutor con ID {aula.tutorid} no existe en el sistema.",
                        constraint = "aula_tutorid_fkey"
                    });
                }
                else if (innerMessage.Contains("uq_aula_nombre_sede_grado"))
                {
                    return BadRequest(new
                    {
                        error = "AULA_DUPLICADA",
                        message = $"Ya existe un aula con el nombre '{aula.nombre}' para el grado {aula.grado}º en esta sede.",
                        constraint = "uq_aula_nombre_sede_grado"
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

        // Obtener el historial de tutores de un aula específica
        [HttpGet("ObtenerHistorialTutores/{aulaid:long}")]
        public async Task<ActionResult<IEnumerable<AulaTutorHistorico>>> GetHistorialTutores(long aulaid)
        {
            try
            {
                // Verificar que el aula exista
                var aula = await _db.aula.FindAsync(aulaid);
                if (aula == null)
                {
                    return NotFound(new
                    {
                        error = "AULA_NO_ENCONTRADA",
                        message = $"No existe un aula con el id {aulaid}."
                    });
                }

                // Obtener todos los registros de historial de tutores para esta aula
                var historial = await _db.aulatutorhistorico
                    .Where(h => h.aulaid == aulaid && h.estado == "ACT")
                    .OrderByDescending(h => h.fecha_inicio)
                    .ToListAsync();

                return Ok(new
                {
                    aula_id = aulaid,
                    aula_nombre = aula.nombre,
                    aula_grado = aula.grado,
                    tutor_actual_id = aula.tutorid,
                    total_registros = historial.Count,
                    historial = historial
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

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarAula/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var aula = await _db.aula.FindAsync(id);
                if (aula == null)
                    return NotFound(new
                    {
                        error = "AULA_NO_ENCONTRADA",
                        message = $"No existe un aula con el id {id}."
                    });

                if (aula.estado == "INA" && !aula.activo)
                {
                    return BadRequest(new
                    {
                        error = "AULA_YA_INACTIVA",
                        message = $"El aula '{aula.nombre}' (grado {aula.grado}º) ya está inactiva."
                    });
                }

                aula.estado = "INA";
                aula.activo = false;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Aula inactivada correctamente",
                    aula_id = aula.aulaid,
                    nombre = aula.nombre,
                    grado = aula.grado
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

