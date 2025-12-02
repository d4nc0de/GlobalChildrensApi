using GlobalChildrensApi.Data;
using GlobalChildrensApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlobalChildrensApi.Controllers
{
    //base
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Usa el JWT de Supabase
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }




        // GET: Obtener todos los usuarios
        [HttpGet("ObtenerUsuariosActivas")]
        public async Task<ActionResult<IEnumerable<AuthUser>>> GetAll()
        {
            try
            {
                var personas = await _db.AuthUsers
                    .Where(u => u.Deleted_At == null)
                    .ToListAsync();

                return Ok(personas);
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

        // GET: Obtener un usuario por ID
        [HttpGet("ObtenerUsuario/{id}")]
        public async Task<ActionResult<AuthUser>> GetById(Guid id)
        {
            try
            {
                var usuario = await _db.AuthUsers
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        error = "USUARIO_NO_ENCONTRADO",
                        message = $"No existe un usuario con el ID: {id}"
                    });
                }

                if (usuario.Deleted_At != null)
                {
                    return BadRequest(new
                    {
                        error = "USUARIO_ELIMINADO",
                        message = "Este usuario ha sido eliminado."
                    });
                }

                return Ok(usuario);
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

        // PUT: Actualizar email de un usuario
        [HttpPut("ActualizarUsuario/{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateEmailRequest request)
        {
            try
            {
                var usuario = await _db.AuthUsers.FindAsync(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        error = "USUARIO_NO_ENCONTRADO",
                        message = $"No existe un usuario con el ID: {id}"
                    });
                }

                if (usuario.Deleted_At != null)
                {
                    return BadRequest(new
                    {
                        error = "USUARIO_ELIMINADO",
                        message = "No se puede actualizar un usuario eliminado."
                    });
                }

                // VALIDACIÓN 1: Email es requerido
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    return BadRequest(new
                    {
                        error = "EMAIL_REQUERIDO",
                        message = "El email es requerido para actualizar."
                    });
                }

                // VALIDACIÓN 2: Email válido
                if (!IsValidEmail(request.Email))
                {
                    return BadRequest(new
                    {
                        error = "EMAIL_INVALIDO",
                        message = "El formato del email no es válido."
                    });
                }

                // VALIDACIÓN 3: Verificar que el email no esté en uso por otro usuario
                var emailExistente = await _db.AuthUsers
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.Id != id);

                if (emailExistente != null)
                {
                    return BadRequest(new
                    {
                        error = "EMAIL_DUPLICADO",
                        message = "Este email ya está siendo utilizado por otro usuario."
                    });
                }

                // Actualizar el email
                usuario.Email = request.Email;
                usuario.Updated_At = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Email actualizado correctamente",
                    user_id = usuario.Id,
                    email = usuario.Email
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

        // DELETE: Eliminar un usuario (soft delete)
        [HttpDelete("EliminarUsuario/{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            try
            {
                var usuario = await _db.AuthUsers.FindAsync(id);

                if (usuario == null)
                {
                    return NotFound(new
                    {
                        error = "USUARIO_NO_ENCONTRADO",
                        message = $"No existe un usuario con el ID: {id}"
                    });
                }

                if (usuario.Deleted_At != null)
                {
                    return BadRequest(new
                    {
                        error = "USUARIO_YA_ELIMINADO",
                        message = "Este usuario ya ha sido eliminado."
                    });
                }

                // Soft delete: marcar como eliminado
                usuario.Deleted_At = DateTimeOffset.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Usuario eliminado correctamente",
                    user_id = usuario.Id,
                    email = usuario.Email
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

        // Método auxiliar para validar email
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    // DTO para actualizar email
    public class UpdateEmailRequest
    {
        public string Email { get; set; } = string.Empty;
    }
}
