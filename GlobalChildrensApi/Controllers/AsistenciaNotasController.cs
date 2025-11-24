using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class AsistenciaNotasController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AsistenciaNotasController(AppDbContext db)
        {
            _db = db;
        }

        //CRUD de AsistenciaEstudiante
        [HttpGet("ObtenerAsistencia/{id:long}")]
        public async Task<ActionResult<AsistenciaEstudiante>> GetAsistenciaById(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante.FindAsync(id);
                if (asistencia == null || asistencia.estado != "ACT")
                {
                    return NotFound($"No existe una asistencia activa con el id {id}.");
                }
                return Ok(asistencia);
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

        [HttpGet("ObtenerAsistenciaSesion/{idsesion:long}")]
        public async Task<ActionResult<IEnumerable<AsistenciaEstudiante>>> GetAsistenciaBySesion(long idsesion)
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                .Where(ae => ae.estado == "ACT" && ae.sesionclaseid == idsesion)
                .OrderBy(ae => ae.estudianteid)
                .ToListAsync();

                return Ok(asistencias);
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

        [HttpGet("ObtenerAsistenciaEstudiante/{idestudiante:long}")]
        public async Task<ActionResult<Aula>> GetAsistenciaByEstudiante(long idestudiante)
        {
            try
            {
                var asistencias = await _db.asistenciaestudiante
                .Where(ae => ae.estado == "ACT" && ae.estudianteid == idestudiante)
                .OrderBy(ae => ae.sesionclaseid)
                .ToListAsync();

                return Ok(asistencias);
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


        [HttpPost("CrearAsistenciaEstudiante")]
        public async Task<ActionResult<AsistenciaEstudiante>> CreateAsistencia([FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                if (asistencia == null)
                {
                    return BadRequest("El objeto de asistencia no puede ser nulo.");
                }

                // Validar que el estudiante exista
                var estudiante = await _db.estudiante.FindAsync(asistencia.estudianteid);
                if (estudiante == null || !estudiante.activo || estudiante.estado != "ACT")
                {
                    return BadRequest($"No existe un estudiante activo con ID {asistencia.estudianteid}");
                }

                // Verificar que no exista ya una asistencia para este estudiante en esta sesión
                var existeAsistencia = await _db.asistenciaestudiante
                    .AnyAsync(a => a.sesionclaseid == asistencia.sesionclaseid
                                && a.estudianteid == asistencia.estudianteid);

                if (existeAsistencia)
                {
                    return BadRequest("Ya existe un registro de asistencia para este estudiante en esta sesión de clase.");
                }

                // Si no asistió y tiene motivo de inasistencia, debe estar justificada
                if (!asistencia.asistio && asistencia.motivoinasistenciaestudianteid.HasValue && !asistencia.justificada)
                {
                    asistencia.justificada = true; // Automáticamente se marca como justificada si tiene motivo
                }
                
                // Si asistió, no puede tener motivo de inasistencia ni estar justificada
                if (asistencia.asistio)
                {
                    asistencia.motivoinasistenciaestudianteid = null;
                    asistencia.justificada = false;
                }
                
                // Establecer valores por defecto
                if (string.IsNullOrWhiteSpace(asistencia.estado))
                    asistencia.estado = "ACT";

                if (string.IsNullOrWhiteSpace(asistencia.fecha_creacion.ToString()))
                    asistencia.fecha_creacion = DateTime.UtcNow;
                    asistencia.fecha_creacion = DateTime.SpecifyKind(asistencia.fecha_creacion, DateTimeKind.Utc);

                _db.asistenciaestudiante.Add(asistencia);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetAsistenciaById), new { id = asistencia.asistenciaestudianteid }, asistencia);
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

        [HttpPut("ActualizarAsistencia/{idasistencia:long}")]
        public async Task<IActionResult> UpdateAsistencia(long idasistencia, [FromBody] AsistenciaEstudiante asistencia)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (idasistencia != asistencia.asistenciaestudianteid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones
                if (asistencia.estado != "ACT" && asistencia.estado != "INA" && asistencia.estado == null)
                {
                    return BadRequest("El estado debe ser 'ACT' o 'INA'.");
                }

                var existing = await _db.asistenciaestudiante.FindAsync(idasistencia);
                if (existing == null)
                    return NotFound($"No existe un asistencia con el id {idasistencia}.");

                // Actualizamos campos
                existing.asistio = asistencia.asistio;
                existing.observacion = asistencia.observacion;
                existing.justificada = asistencia.justificada;
                existing.estado = asistencia.estado;
                existing.sesionclaseid = asistencia.sesionclaseid;
                existing.estudianteid = asistencia.estudianteid;
                existing.motivoinasistenciaestudianteid = asistencia.motivoinasistenciaestudianteid;

                await _db.SaveChangesAsync();

                return Ok("Asistencia de estudiante actualizada correctamente");
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

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarAsistencia/{id:long}")]
        public async Task<IActionResult> InactivateAsistencia(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante.FindAsync(id);
                if (asistencia == null || asistencia.estado == "INA")
                    return NotFound($"No existe una asistencia activa con el id {id}.");

                asistencia.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Asistencia inactivada correctamente");
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

        [HttpDelete("BorrarAsistencia/{id:long}")]
        public async Task<IActionResult> DeleteAsistencia(long id)
        {
            try
            {
                var asistencia = await _db.asistenciaestudiante.FindAsync(id);
                if (asistencia == null)
                    return NotFound($"No existe una asistencia con el id {id}.");

                _db.asistenciaestudiante.Remove(asistencia);
                await _db.SaveChangesAsync();
                return Ok("Asistencia eliminada correctamente");
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

        //CRUD de notas de estudiante
        [HttpGet("ObtenerNotasAula/{idaula:long}")]
        public async Task<ActionResult<IEnumerable<Nota>>> GetNotaByAula(long idaula)
        {
            try
            {
                var aula = await _db.aula.FindAsync(idaula);

                if (aula == null || aula.estado != "ACT" || !aula.activo)
                {
                    return NotFound($"No existe un aula activa con el id {idaula}.");
                }

                var estudiantesEnAula = await _db.estudiante
                    .Where(e => e.aulaid == idaula && e.estado == "ACT" && e.activo)
                    .Select(e => e.estudianteid)
                    .ToListAsync();

                var notas = await _db.nota
                    .Where(n => estudiantesEnAula.Contains(n.estudianteid) && n.estado == "ACT")
                    .OrderBy(n => n.estudianteid)
                    .ToListAsync();

                return Ok(notas);
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

        [HttpGet("ObtenerNota/{idnota:long}")]
        public async Task<ActionResult<IEnumerable<Nota>>> GetNotaById(long idnota)
        {
            try
            {
                var nota = await _db.nota.FindAsync(idnota);

                if (nota == null || nota.estado != "ACT")
                {
                    return NotFound($"No existe una nota activa con el id {idnota}.");
                }

                return Ok(nota);
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

        [HttpGet("ObtenerNotasEstudiante/{idestudiante:long}")]
        public async Task<ActionResult<Nota>> GetNotaByEstudiante(long idestudiante)
        {
            try
            {
                var estudiante = await _db.estudiante.FindAsync(idestudiante);
                if (estudiante == null || estudiante.estado != "ACT" || !estudiante.activo)
                {
                    return NotFound($"No existe un estudiante activo con el id {idestudiante}.");
                }

                var notas = await _db.nota
                .Where(n => n.estado == "ACT" && n.estudianteid == idestudiante)
                .OrderBy(n => n.notaid)
                .ToListAsync();

                return Ok(notas);
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


        [HttpPost("CrearNotaEstudiante")]
        public async Task<ActionResult<Nota>> CreateNota([FromBody] Nota nota)
        {
            try
            {
                // Validar que el estudiante exista y este activo
                var estudiante = await _db.estudiante.FindAsync(nota.estudianteid);
                if (estudiante == null || !estudiante.activo || estudiante.estado != "ACT")
                {
                    return BadRequest($"No existe un estudiante activo con ID {nota.estudianteid}.");
                }

                //validar que el tutor coincidada con el aula del estudiante
                var aula = await _db.aula.FindAsync(estudiante.aulaid);
                if (aula == null || !aula.activo || aula.estado != "ACT")
                {
                    return BadRequest($"El estudiante {estudiante.estudianteid} no pertenece al aula {estudiante.aulaid}.");
                }
                var tutor = await _db.tutor.FindAsync(aula.tutorid);
                if (tutor == null || tutor.estado != "ACT")
                {
                    return BadRequest($"El tutor {aula.tutorid} no pertenece al aula {estudiante.aulaid} donde se encuentra el estudiante {estudiante.estudianteid}.");
                }

                if (string.IsNullOrWhiteSpace(nota.fecha_creacion.ToString()) || string.IsNullOrWhiteSpace(nota.fecha_registro.ToString()))
                    nota.fecha_creacion = DateTime.UtcNow;
                    nota.fecha_registro = DateTime.UtcNow;
                nota.fecha_creacion = DateTime.SpecifyKind(nota.fecha_creacion, DateTimeKind.Utc);
                nota.fecha_registro = DateTime.SpecifyKind(nota.fecha_registro, DateTimeKind.Utc);
                
                _db.nota.Add(nota);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetNotaById), new { idnota = nota.notaid }, nota);
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

        [HttpPut("ActualizarNota/{id:long}")]
        public async Task<IActionResult> UpdateNota(long id, [FromBody] Nota nota)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (id != nota.notaid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones

                var existing = await _db.nota.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe un nota con el id {id}.");

                // Actualizamos campos
                existing.valor = nota.valor;
                existing.fecha_registro = nota.fecha_registro;
                existing.estado = nota.estado;
                existing.estudianteid = nota.estudianteid;
                existing.componentenotaid = nota.componentenotaid;
                existing.periodoevaluacionid = nota.periodoevaluacionid;
                existing.tutorid = nota.tutorid;

                await _db.SaveChangesAsync();

                return Ok("Nota de estudiante actualizada correctamente");
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

        //Cambia el estado de la entidad a INA y activo a false
        [HttpDelete("InactivarNota/{id:long}")]
        public async Task<IActionResult> InactivateNota(long id)
        {
            try
            {
                var nota = await _db.nota.FindAsync(id);
                if (nota == null || nota.estado == "INA")
                    return NotFound($"No existe una nota activa con el id {id}.");

                nota.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Asistencia inactivada correctamente");
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
        
        [HttpDelete("BorrarNota/{id:long}")]
        public async Task<IActionResult> DeleteNota(long id)
        {
            try
            {
                var nota = await _db.nota.FindAsync(id);
                if (nota == null)
                    return NotFound($"No existe una nota con el id {id}.");

                _db.nota.Remove(nota);
                await _db.SaveChangesAsync();

                return Ok("Asistencia eliminada correctamente");
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



    }
}

