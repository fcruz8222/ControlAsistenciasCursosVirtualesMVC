using ControlAsistenciasCursosVirtuales.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Web.Mvc;

namespace ControlAsistenciasCursosVirtuales.Controllers
{
    public class AlumnosController : Controller
    {
        private readonly string cadena = ConfigurationManager.ConnectionStrings["cnBD"].ConnectionString;

        // Lista para dropdown muestre ACTIVO / INACTIVO
        private List<SelectListItem> ObtenerEstados()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Text = "ACTIVO", Value = "1" },
                new SelectListItem { Text = "INACTIVO", Value = "0" }
            };
        }


        // Mostrar lista de alumnos
        public ActionResult Index(string buscar)
        {
            var model = new AlumnosIndexViewModel();
            model.Buscar = buscar;
            model.Alumnos = new List<AlumnoListaViewModel>();

            using (SqlConnection con = new SqlConnection(cadena))
            {
                string query = @"
                    SELECT IdEstudiante, Nombres, Apellidos, Codigo, Email, Telefono, Activo, CodigoUsuario
                    FROM Estudiantes
                    WHERE (@Buscar IS NULL OR Nombres LIKE '%' + @Buscar + '%' OR Apellidos LIKE '%' + @Buscar + '%')
                    ORDER BY IdEstudiante DESC";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Buscar", string.IsNullOrWhiteSpace(buscar) ? (object)DBNull.Value : buscar);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    model.Alumnos.Add(new AlumnoListaViewModel
                    {
                        IdEstudiante = Convert.ToInt32(dr["IdEstudiante"]),
                        Nombres = dr["Nombres"].ToString(),
                        Apellidos = dr["Apellidos"].ToString(),
                        Codigo = dr["Codigo"].ToString(),
                        Email = dr["Email"].ToString(),
                        Telefono = dr["Telefono"].ToString(),
                        CodigoUsuario = dr["CodigoUsuario"].ToString(),
                        Activo = Convert.ToBoolean(dr["Activo"]) ? "ACTIVO" : "INACTIVO"
                    });
                }
            }

            return View(model);
        }

        // Mostrar formulario crear
        public ActionResult Create()
        {
            var model = new AlumnoViewModel
            {
                EstadoList = ObtenerEstados(),
                Activo = 1
            };

            return View(model);
        }

        // Guardar alumno nuevo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(AlumnoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.EstadoList = ObtenerEstados();
                return View(model);
            }

            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();

                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    // 1. Guardar usuario
                    string queryUsuario = @"
                        INSERT INTO Usuario (CodigoUsuario, NombreUsuario, Rol, Activo, [Contraseña])
                        VALUES (@CodigoUsuario, @NombreUsuario, @Rol, @Activo, @Contrasena)";

                    SqlCommand cmdUsuario = new SqlCommand(queryUsuario, con, trans);
                    cmdUsuario.Parameters.AddWithValue("@CodigoUsuario", model.CodigoUsuario);
                    cmdUsuario.Parameters.AddWithValue("@NombreUsuario", model.Nombres + " " + model.Apellidos);
                    cmdUsuario.Parameters.AddWithValue("@Rol", "Estudiante");
                    cmdUsuario.Parameters.AddWithValue("@Activo", model.Activo);
                    cmdUsuario.Parameters.AddWithValue("@Contrasena", model.Contrasena);
                    cmdUsuario.ExecuteNonQuery();

                    // 2. Guardar estudiante
                    string queryEstudiante = @"
                        INSERT INTO Estudiantes (Nombres, Apellidos, Codigo, Email, Telefono, Activo, CodigoUsuario)
                        VALUES (@Nombres, @Apellidos, @Codigo, @Email, @Telefono, @Activo, @CodigoUsuario)";

                    SqlCommand cmdEstudiante = new SqlCommand(queryEstudiante, con, trans);
                    cmdEstudiante.Parameters.AddWithValue("@Nombres", model.Nombres);
                    cmdEstudiante.Parameters.AddWithValue("@Apellidos", model.Apellidos);
                    cmdEstudiante.Parameters.AddWithValue("@Codigo", model.Codigo);
                    cmdEstudiante.Parameters.AddWithValue("@Email", model.Email);
                    cmdEstudiante.Parameters.AddWithValue("@Telefono", model.Telefono);
                    cmdEstudiante.Parameters.AddWithValue("@Activo", model.Activo);
                    cmdEstudiante.Parameters.AddWithValue("@CodigoUsuario", model.CodigoUsuario);
                    cmdEstudiante.ExecuteNonQuery();

                    trans.Commit();

                    return RedirectToAction("Index");
                }
                catch
                {
                    trans.Rollback();
                    ModelState.AddModelError("", "Error al guardar el alumno. Verifique que el CódigoUsuario no exista.");
                    model.EstadoList = ObtenerEstados();
                    return View(model);
                }
            }
        }

        // Mostrar formulario editar
        public ActionResult Edit(int? id)
        {
            
                if (id == null)
            {
                return RedirectToAction("Index");
            }
            int idEstudiante = id.Value;
           
            AlumnoViewModel model = null;

            using (SqlConnection con = new SqlConnection(cadena))
            {
                string query = @"
                    SELECT IdEstudiante, Nombres, Apellidos, Codigo, Email, Telefono, Activo, CodigoUsuario
                    FROM Estudiantes
                    WHERE IdEstudiante = @IdEstudiante";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@IdEstudiante", id.Value);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new AlumnoViewModel
                    {
                        IdEstudiante = Convert.ToInt32(dr["IdEstudiante"]),
                        Nombres = dr["Nombres"].ToString(),
                        Apellidos = dr["Apellidos"].ToString(),
                        Codigo = dr["Codigo"].ToString(),
                        Email = dr["Email"].ToString(),
                        Telefono = dr["Telefono"].ToString(),
                        CodigoUsuario = dr["CodigoUsuario"].ToString(),
                        Activo = Convert.ToBoolean(dr["Activo"]) ? 1: 0,
                        EstadoList = ObtenerEstados()
                    };
                }
            }

            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        // Actualizar alumno y sincronizar Usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(AlumnoViewModel model)
        {
            model.EstadoList = ObtenerEstados();
            if (!ModelState.IsValid)
            {
             
                return View(model);
            }


            int activo = model.Activo ;

            using (SqlConnection con = new SqlConnection(cadena))
            {
                string query = @"
            UPDATE Estudiantes
            SET Nombres = @Nombres,
                Apellidos = @Apellidos,
                Codigo = @Codigo,
                Email = @Email,
                Telefono = @Telefono,
                Activo = @Activo
                
            WHERE IdEstudiante = @IdEstudiante";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@Nombres", model.Nombres);
                cmd.Parameters.AddWithValue("@Apellidos", model.Apellidos);
                cmd.Parameters.AddWithValue("@Codigo", model.Codigo);
                cmd.Parameters.AddWithValue("@Email", model.Email);
                cmd.Parameters.AddWithValue("@Telefono", model.Telefono);
                cmd.Parameters.AddWithValue("@Activo", activo);
                cmd.Parameters.AddWithValue("@IdEstudiante", model.IdEstudiante);

                con.Open();
                int filas = cmd.ExecuteNonQuery();

                TempData["Success"] = filas > 0
                    ? "Alumno actualizado correctamente."
                    : "No se actualizó ningún registro.";

                // se agrogo este update para actualizar tambien el usuario del alumno desde el formulario actualizar alumno
                string queryUsuario = @"
                UPDATE Usuario 
                SET NombreUsuario = @NombreUsuario,
                Activo = @Activo
                WHERE CodigoUsuario = @CodigoUsuario";

                SqlCommand cmdUsuario = new SqlCommand(queryUsuario, con);
                cmdUsuario.Parameters.AddWithValue("@NombreUsuario", model.Nombres);
                cmdUsuario.Parameters.AddWithValue("@Activo", activo);
                cmdUsuario.Parameters.AddWithValue("@CodigoUsuario", model.CodigoUsuario);

                
                cmdUsuario.ExecuteNonQuery();

            }

            return RedirectToAction("Index");
        }

        // Mostrar confirmación eliminar
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");

            }
            int idEstudiante = id.Value;

            AlumnoViewModel model = null;

            using (SqlConnection con = new SqlConnection(cadena))
            {
                string query = @"
                    SELECT IdEstudiante, Nombres, Apellidos, Codigo, Email, Telefono, Activo, CodigoUsuario
                    FROM Estudiantes
                    WHERE IdEstudiante = @IdEstudiante";

                SqlCommand cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@IdEstudiante", idEstudiante);

                con.Open();
                SqlDataReader dr = cmd.ExecuteReader();

                if (dr.Read())
                {
                    model = new AlumnoViewModel
                    {
                        IdEstudiante = Convert.ToInt32(dr["IdEstudiante"]),
                        Nombres = dr["Nombres"].ToString(),
                        Apellidos = dr["Apellidos"].ToString(),
                        Codigo = dr["Codigo"].ToString(),
                        Email = dr["Email"].ToString(),
                        Telefono = dr["Telefono"].ToString(),
                        CodigoUsuario = dr["CodigoUsuario"].ToString(),
                        Activo = Convert.ToBoolean(dr["Activo"]) ? 1 : 0,
                        EstadoList = ObtenerEstados()

                    };
                }
            }

            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        // Eliminar alumno y usuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(AlumnoViewModel model)
        {
            using (SqlConnection con = new SqlConnection(cadena))
            {
                con.Open();
                SqlTransaction trans = con.BeginTransaction();

                try
                {
                    string codigoUsuario = "";

                    // Primero obtener CodigoUsuario
                    string queryBuscar = @"
                        SELECT CodigoUsuario
                        FROM Estudiantes
                        WHERE IdEstudiante = @IdEstudiante";

                    SqlCommand cmdBuscar = new SqlCommand(queryBuscar, con, trans);
                    cmdBuscar.Parameters.AddWithValue("@IdEstudiante", model.IdEstudiante);

                    object result = cmdBuscar.ExecuteScalar();

                    if (result != null)
                    {
                        codigoUsuario = result.ToString();
                    }

                    // Eliminar estudiante
                    string queryEstudiante = @"
                        DELETE FROM Estudiantes
                        WHERE IdEstudiante = @IdEstudiante";

                    SqlCommand cmdEstudiante = new SqlCommand(queryEstudiante, con, trans);
                    cmdEstudiante.Parameters.AddWithValue("@IdEstudiante", model.IdEstudiante);
                    cmdEstudiante.ExecuteNonQuery();

                    // Eliminar usuario relacionado
                    string queryUsuario = @"
                        DELETE FROM Usuario
                        WHERE CodigoUsuario = @CodigoUsuario";

                    SqlCommand cmdUsuario = new SqlCommand(queryUsuario, con, trans);
                    cmdUsuario.Parameters.AddWithValue("@CodigoUsuario", codigoUsuario);
                    cmdUsuario.ExecuteNonQuery();

                    trans.Commit();

                    return RedirectToAction("Index");
                }
                catch
                {
                    trans.Rollback();
                    return RedirectToAction("Index");
                }
            }
        }
    }
}