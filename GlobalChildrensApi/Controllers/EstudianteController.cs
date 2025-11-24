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
    public class EstudianteController : ControllerBase
    {
        private readonly AppDbContext _db;

        public EstudianteController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("ObtenerEstudiantesActivos")]
        public async Task<ActionResult<IEnumerable<Estudiante>>> GetAll()
        {
            try
            {
                var estudiantes = await _db.estudiante
                .Where(e => e.estado == "ACT" && e.activo == true)
                .OrderBy(e => e.estudianteid)
                .ToListAsync();

                return Ok(estudiantes);
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

        [HttpGet("ObtenerEstudiantePorId/{id:long}")]
        public async Task<ActionResult<Estudiante>> GetById(long id)
        {
            try
            {
                var estudiante = await _db.estudiante
                .FirstOrDefaultAsync(e => e.estudianteid == id);

                if (estudiante == null)
                    return NotFound($"No existe un estudiante con el id {id}.");

                if (estudiante.estado != "ACT")
                    return BadRequest($"El estudiante con id {id} existe pero no está activo (Estado = {estudiante.estado}).");

                return Ok(estudiante);
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

        [HttpPost("CrearEstudiante")]
        public async Task<ActionResult<Estudiante>> Create([FromBody] Estudiante estudiante)
        {
            try
            {
                // Validar que el sexo sea válido (M, F, O)
                if (estudiante.sexo != 'M' && estudiante.sexo != 'F' && estudiante.sexo != 'O')
                {
                    return BadRequest("El sexo debe ser 'M' (Masculino), 'F' (Femenino) o 'O' (Otro).");
                }

                // Validar que el estudiante tenga al menos 8 años
                var edad = CalcularEdad(estudiante.fecha_nacimiento);
                if (edad < 8)
                {
                    return BadRequest($"El estudiante debe tener al menos 8 años de edad. Edad actual: {edad} años.");
                }

                // Validar que el número de documento no esté vacío
                if (string.IsNullOrWhiteSpace(estudiante.numero_documento))
                {
                    return BadRequest("El número de documento es requerido.");
                }

                // Validar que nombres y apellidos no estén vacíos
                if (string.IsNullOrWhiteSpace(estudiante.nombres))
                {
                    return BadRequest("Los nombres son requeridos.");
                }

                if (string.IsNullOrWhiteSpace(estudiante.apellidos))
                {
                    return BadRequest("Los apellidos son requeridos.");
                }

                // Verificar que no exista otro estudiante con el mismo tipo de documento y número
                var existeDocumento = await _db.estudiante
                    .AnyAsync(e => e.tipodocumentoid == estudiante.tipodocumentoid 
                                && e.numero_documento == estudiante.numero_documento);

                if (existeDocumento)
                {
                    return BadRequest("Ya existe un estudiante con este tipo y número de documento.");
                }

                // Validar que el aula exista y esté activa
                var aula = await _db.aula.FindAsync(estudiante.aulaid);
                if (aula == null)
                {
                    return BadRequest($"El aula con ID {estudiante.aulaid} no existe en el sistema.");
                }

                if (!aula.activo || aula.estado != "ACT")
                {
                    return BadRequest($"El aula (grado {aula.grado}º - {aula.nombre}) no está activa.");
                }

                // Establecer valores por defecto si no se proporcionan
                if (string.IsNullOrWhiteSpace(estudiante.estado))
                    estudiante.estado = "ACT";

                if (!estudiante.activo)
                    estudiante.activo = true;

                _db.estudiante.Add(estudiante);
                await _db.SaveChangesAsync();

                return CreatedAtAction(nameof(GetById), new { id = estudiante.estudianteid }, estudiante);
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

        [HttpPut("ActualizarEstudiante/{id:long}")]
        public async Task<IActionResult> Update(long id, [FromBody] Estudiante estudiante)
        {
            try
            {
                if (id != estudiante.estudianteid)
                    return BadRequest("El id de la URL no coincide con el id del cuerpo.");

                // Validar que el sexo sea válido (M, F, O)
                if (estudiante.sexo != 'M' && estudiante.sexo != 'F' && estudiante.sexo != 'O')
                {
                    return BadRequest("El sexo debe ser 'M' (Masculino), 'F' (Femenino) o 'O' (Otro).");
                }

                // Validar que el estudiante tenga al menos 8 años
                var edad = CalcularEdad(estudiante.fecha_nacimiento);
                if (edad < 8)
                {
                    return BadRequest($"El estudiante debe tener al menos 8 años de edad. Edad actual: {edad} años.");
                }

                // Validar que el número de documento no esté vacío
                if (string.IsNullOrWhiteSpace(estudiante.numero_documento))
                {
                    return BadRequest("El número de documento es requerido.");
                }

                // Validar que nombres y apellidos no estén vacíos
                if (string.IsNullOrWhiteSpace(estudiante.nombres))
                {
                    return BadRequest("Los nombres son requeridos.");
                }

                if (string.IsNullOrWhiteSpace(estudiante.apellidos))
                {
                    return BadRequest("Los apellidos son requeridos.");
                }

                var existing = await _db.estudiante.FindAsync(id);
                if (existing == null)
                    return NotFound($"No existe un estudiante con el id {id}.");

                // Verificar que no exista otro estudiante con el mismo tipo de documento y número
                // (excepto el estudiante actual)
                var existeDocumento = await _db.estudiante
                    .AnyAsync(e => e.tipodocumentoid == estudiante.tipodocumentoid 
                                && e.numero_documento == estudiante.numero_documento
                                && e.estudianteid != id);

                if (existeDocumento)
                {
                    return BadRequest("Ya existe otro estudiante con este tipo y número de documento.");
                }

                // VALIDACIÓN DE MOVIMIENTO POR NIVEL EDUCATIVO
                // Si está cambiando de aula, validar que sea dentro del mismo nivel (primaria o secundaria)
                if (existing.aulaid != estudiante.aulaid)
                {
                    // Obtener el grado del aula actual del estudiante
                    var aulaActual = await _db.aula.FindAsync(existing.aulaid);
                    if (aulaActual == null)
                    {
                        return BadRequest($"El aula actual (ID: {existing.aulaid}) no existe en el sistema.");
                    }

                    // Obtener el grado del aula nueva
                    var aulaNueva = await _db.aula.FindAsync(estudiante.aulaid);
                    if (aulaNueva == null)
                    {
                        return BadRequest($"El aula destino (ID: {estudiante.aulaid}) no existe en el sistema.");
                    }

                    // Determinar el nivel educativo del aula actual
                    bool aulaActualEsPrimaria = aulaActual.grado == 4 || aulaActual.grado == 5;
                    bool aulaActualEsSecundaria = aulaActual.grado == 9 || aulaActual.grado == 10;

                    // Determinar el nivel educativo del aula nueva
                    bool aulaNuevaEsPrimaria = aulaNueva.grado == 4 || aulaNueva.grado == 5;
                    bool aulaNuevaEsSecundaria = aulaNueva.grado == 9 || aulaNueva.grado == 10;

                    // Validar que ambas aulas estén en el mismo nivel
                    if (aulaActualEsPrimaria && !aulaNuevaEsPrimaria)
                    {
                        return BadRequest($"No se puede mover al estudiante de primaria (grado {aulaActual.grado}º) a secundaria (grado {aulaNueva.grado}º). Solo se permite movimiento dentro del mismo nivel educativo.");
                    }

                    if (aulaActualEsSecundaria && !aulaNuevaEsSecundaria)
                    {
                        return BadRequest($"No se puede mover al estudiante de secundaria (grado {aulaActual.grado}º) a primaria (grado {aulaNueva.grado}º). Solo se permite movimiento dentro del mismo nivel educativo.");
                    }

                    // Validar que el aula nueva esté activa
                    if (!aulaNueva.activo || aulaNueva.estado != "ACT")
                    {
                        return BadRequest($"El aula destino (grado {aulaNueva.grado}º - {aulaNueva.nombre}) no está activa.");
                    }
                }

                // Actualizamos campos
                existing.numero_documento = estudiante.numero_documento;
                existing.nombres = estudiante.nombres;
                existing.apellidos = estudiante.apellidos;
                existing.fecha_nacimiento = estudiante.fecha_nacimiento;
                existing.sexo = estudiante.sexo;
                existing.activo = estudiante.activo;
                existing.estado = estudiante.estado;
                existing.tipodocumentoid = estudiante.tipodocumentoid;
                existing.aulaid = estudiante.aulaid;

                await _db.SaveChangesAsync();

                return Ok("Estudiante actualizado correctamente");
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
        [HttpDelete("InactivarEstudiante/{id:long}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var estudiante = await _db.estudiante.FindAsync(id);
                if (estudiante == null)
                    return NotFound($"No existe un estudiante con el id {id}.");

                estudiante.estado = "INA";
                estudiante.activo = false;
                await _db.SaveChangesAsync();

                return Ok("Estudiante inactivado correctamente");
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

        // Método auxiliar para calcular la edad
        private int CalcularEdad(DateTime fechaNacimiento)
        {
            var hoy = DateTime.Today;
            var edad = hoy.Year - fechaNacimiento.Year;
            
            // Ajustar si aún no ha cumplido años este año
            if (fechaNacimiento.Date > hoy.AddYears(-edad))
                edad--;
            
            return edad;
        }
    }
}

