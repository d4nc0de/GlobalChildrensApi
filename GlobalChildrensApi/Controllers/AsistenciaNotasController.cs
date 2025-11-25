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
        //======================================================================================
        //CRUD de AsistenciaEstudiante
        //======================================================================================
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
        public async Task<ActionResult<AsistenciaEstudiante>> GetAsistenciaByEstudiante(long idestudiante)
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

        //======================================================================================
        //CRUD de notas de estudiante
        //======================================================================================
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

        [HttpGet("ObtenerNotaByComponente/{idcomponente:long}")]
        public async Task<ActionResult<IEnumerable<Nota>>> GetNotaByComponente(long idcomponente)
        {
            try
            {
                // Validar si el componente de nota existe
                var componente = await _db.componentenota.FindAsync(idcomponente);
                if (componente == null || componente.estado != "ACT" || !componente.activo)
                {
                    return NotFound($"No existe un componente de nota activo con el id {idcomponente}.");
                }

                // Obtener las notas asociadas al componente de nota
                var notas = await _db.nota
                    .Where(n => n.componentenotaid == idcomponente && n.estado == "ACT")
                    .OrderBy(n => n.notaid)
                    .ToListAsync();

                if (!notas.Any())
                {
                    return NotFound($"No existen notas activas con el componente de nota id {idcomponente}.");
                }

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

        [HttpGet("ObtenerNotaByPeriodo/{idperiodo:long}")]
        public async Task<ActionResult<IEnumerable<Nota>>> GetNotaByPeriodo(long idperiodo)
        {
            try
            {
                // Validar si el componente de nota existe
                var periodo = await _db.periodoevaluacion.FindAsync(idperiodo);
                if (periodo == null || periodo.estado != "ACT")
                {
                    return NotFound($"No existe un periodo activo con el id {idperiodo}.");
                }

                // Obtener las notas asociadas al periodo
                var notas = await _db.nota
                    .Where(n => n.periodoevaluacionid == idperiodo && n.estado == "ACT")
                    .OrderBy(n => n.notaid)
                    .ToListAsync();

                if (!notas.Any())
                {
                    return NotFound($"No existen notas activas con para el periodo id {idperiodo}.");
                }

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

        [HttpGet("ObtenerPonderacionNotasEstudiante/{idestudiante:long}")]
        public async Task<ActionResult<Nota>> GetPonderacionNotasByEstudiante(long idestudiante)
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

                // Calcular la ponderación total como la multipliación del valor de la nota por el porcentaje del componente de nota
                var ponderacionNotas = notas.Select(n => new
                {
                    Nota = n,
                    ComponenteNota = _db.componentenota.Find(n.componentenotaid),
                    Ponderacion = n.valor * (_db.componentenota.Find(n.componentenotaid)?.porcentaje ?? 0) / 100
                }).ToList();

                return Ok(ponderacionNotas);
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

        //======================================================================================
        //CRUD de componente notas de estudiante
        //======================================================================================
        [HttpGet("ObtenerComponenteNota/{id:long}")]
        public async Task<ActionResult<IEnumerable<ComponenteNota>>> GetComponenteNotaById(long id)
        {
            try
            {
                // Obtener el componente nota por su ID
                var componente = await _db.componentenota.FindAsync(id);

                if (componente == null || componente.estado != "ACT" || !componente.activo)
                {
                    return NotFound($"No existe un componente de nota activo con el id {id}.");
                }

                return Ok(componente);
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

        [HttpPost("CrearComponenteNota")]
        public async Task<ActionResult<ComponenteNota>> CreateComponenteNota([FromBody] ComponenteNota componente)
        {
            try
            {
                //Validaciones
                if (string.IsNullOrWhiteSpace(componente.nombre))
                {
                    return BadRequest("El nombre del componente de nota no puede estar vacío.");
                }
                if (componente.porcentaje < 0 || componente.porcentaje > 100)
                {
                    return BadRequest("El porcentaje del componente de nota debe estar entre 0 y 100.");
                }
                if (string.IsNullOrWhiteSpace(componente.estado))
                    componente.estado = "ACT";
                if (string.IsNullOrWhiteSpace(componente.fecha_creacion.ToString()))
                    componente.fecha_creacion = DateTime.UtcNow;
                componente.fecha_creacion = DateTime.SpecifyKind(componente.fecha_creacion, DateTimeKind.Utc);

                _db.componentenota.Add(componente);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetComponenteNotaById), new { id = componente.componentenotaid }, componente);
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

        [HttpPut("ActualizarComponenteNota/{id:long}")]
        public async Task<IActionResult> UpdateComponenteNota(long id, [FromBody] ComponenteNota componente)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (id != componente.componentenotaid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones

                var existing = await _db.componentenota.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe un componente de nota con el id {id}.");

                // Actualizamos campos
                existing.nombre = componente.nombre;
                existing.porcentaje = componente.porcentaje;
                existing.activo = componente.activo;
                existing.estado = componente.estado;

                await _db.SaveChangesAsync();

                return Ok("Componeten de nota actualizado correctamente");
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
        [HttpDelete("InactivarCompoenteNota/{id:long}")]
        public async Task<IActionResult> InactivateComponenteNota(long id)
        {
            try
            {
                var componente = await _db.componentenota.FindAsync(id);
                if (componente == null || componente.estado == "INA")
                    return NotFound($"No existe un componente de nota activo con el id {id}.");

                componente.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Componente de nota inactivado correctamente");
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
        
        [HttpDelete("BorrarComponenteNota/{id:long}")]
        public async Task<IActionResult> DeleteComponenteNota(long id)
        {
            try
            {
                var componente = await _db.componentenota.FindAsync(id);
                if (componente == null)
                    return NotFound($"No existe un componente de nota con el id {id}.");

                _db.componentenota.Remove(componente);
                await _db.SaveChangesAsync();

                return Ok("Componente de nota eliminada correctamente");
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

        //======================================================================================
        //CRUD de periodos de evaluacion
        //======================================================================================
        [HttpGet("ObtenerPeriodoEvaluacion/{id:long}")]
        public async Task<ActionResult<IEnumerable<ComponenteNota>>> GetPeriodoEvaluacionById(long id)
        {
            try
            {
                // Obtener el periodo de evaluacion por su ID
                var periodo = await _db.periodoevaluacion.FindAsync(id);

                if (periodo == null || periodo.estado != "ACT")
                {
                    return NotFound($"No existe un periodo de evaluacion activo con el id {id}.");
                }

                return Ok(periodo);
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

        [HttpPost("CrearPeriodoEvaluacion")]
        public async Task<ActionResult<PeriodoEvaluacion>> CreatePeriodoEvaluacion([FromBody] PeriodoEvaluacion periodo)
        {
            try
            {
                //Validaciones
                if (string.IsNullOrWhiteSpace(periodo.nombre))
                {
                    return BadRequest("El nombre del periodo de evaluacion no puede estar vacío.");
                }
                if (string.IsNullOrWhiteSpace(periodo.fecha_inicio.ToString()))
                {
                    return BadRequest("La fecha de inicio del periodo de evaluacion no puede estar vacía.");
                }
                periodo.fecha_inicio = DateTime.SpecifyKind(periodo.fecha_inicio, DateTimeKind.Utc);
                if (string.IsNullOrWhiteSpace(periodo.fecha_fin.ToString()))
                {
                    return BadRequest("La fecha de fin del periodo de evaluacion no puede estar vacía.");
                }
                periodo.fecha_fin = DateTime.SpecifyKind(periodo.fecha_fin, DateTimeKind.Utc);
                if (periodo.fecha_fin < periodo.fecha_inicio)
                {
                    return BadRequest("La fecha de fin del periodo de evaluacion no puede ser anterior a la fecha de inicio.");
                }
                if (string.IsNullOrWhiteSpace(periodo.estado))
                    periodo.estado = "ACT";
                if (string.IsNullOrWhiteSpace(periodo.fecha_creacion.ToString()))
                    periodo.fecha_creacion = DateTime.UtcNow;
                periodo.fecha_creacion = DateTime.SpecifyKind(periodo.fecha_creacion, DateTimeKind.Utc);


                _db.periodoevaluacion.Add(periodo);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPeriodoEvaluacionById), new { id = periodo.periodoevaluacionid }, periodo);
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

        [HttpPut("ActualizarPeriodoEvaluacion/{id:long}")]
        public async Task<IActionResult> UpdatePeriodoEvaluacion(long id, [FromBody] PeriodoEvaluacion periodo)
        {
            try
            {
                // Validar que el id en la URL coincida con el id en el cuerpo
                if (id != periodo.periodoevaluacionid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validaciones
                var existing = await _db.periodoevaluacion.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe un periodo de evaluacion con el id {id}.");
                if (string.IsNullOrWhiteSpace(periodo.fecha_inicio.ToString()))
                {
                    return BadRequest("La fecha de inicio del periodo de evaluacion no puede estar vacía.");
                }
                periodo.fecha_inicio = DateTime.SpecifyKind(periodo.fecha_inicio, DateTimeKind.Utc);
                if (string.IsNullOrWhiteSpace(periodo.fecha_fin.ToString()))
                {
                    return BadRequest("La fecha de fin del periodo de evaluacion no puede estar vacía.");
                }
                periodo.fecha_fin = DateTime.SpecifyKind(periodo.fecha_fin, DateTimeKind.Utc);
                if (periodo.fecha_fin < periodo.fecha_inicio)
                {
                    return BadRequest("La fecha de fin del periodo de evaluacion no puede ser anterior a la fecha de inicio.");
                }

                // Actualizamos campos
                existing.nombre = periodo.nombre;
                existing.fecha_inicio = periodo.fecha_inicio;
                existing.fecha_fin = periodo.fecha_fin;
                existing.orden = periodo.orden;
                existing.estado = periodo.estado;

                await _db.SaveChangesAsync();

                return Ok("Periodo de evaluacion actualizado correctamente");
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
        [HttpDelete("InactivarPeriodoEvaluacion/{id:long}")]
        public async Task<IActionResult> InactivatePeriodoEvaluacion(long id)
        {
            try
            {
                var periodo = await _db.periodoevaluacion.FindAsync(id);
                if (periodo == null || periodo.estado == "INA")
                    return NotFound($"No existe un periodo de evaluacion activo con el id {id}.");

                periodo.estado = "INA";
                await _db.SaveChangesAsync();

                return Ok("Periodo de evaluacion inactivado correctamente");
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
        
        [HttpDelete("BorrarPeriodoEvaluacion/{id:long}")]
        public async Task<IActionResult> DeletePeriodoEvaluacion(long id)
        {
            try
            {
                var periodo = await _db.periodoevaluacion.FindAsync(id);
                if (periodo == null)
                    return NotFound($"No existe un periodo de evaluacion con el id {id}.");

                _db.periodoevaluacion.Remove(periodo);
                await _db.SaveChangesAsync();

                return Ok("periodo de evaluacion eliminado correctamente");
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

