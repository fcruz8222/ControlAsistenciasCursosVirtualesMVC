using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;


namespace ControlAsistenciasCursosVirtuales.Helpers
{
    public static class Db
    {
        private static string ConnString
        {
            get
            {
                return ConfigurationManager.ConnectionStrings["cnBD"].ConnectionString;
            }
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(ConnString);
        }

        public static DataTable Query(string sql, params SqlParameter[] parameters)
        {
            using (var cn = GetConnection())
            using (var cmd = new SqlCommand(sql, cn))
            using (var da = new SqlDataAdapter(cmd))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                var dt = new DataTable();
                cn.Open();
                da.Fill(dt);
                return dt;
            }
        }

        public static int Execute(string sql, params SqlParameter[] parameters)
        {
            using (var cn = GetConnection())
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        public static object Scalar(string sql, params SqlParameter[] parameters)
        {
            using (var cn = GetConnection())
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (parameters != null && parameters.Length > 0)
                    cmd.Parameters.AddRange(parameters);

                cn.Open();
                return cmd.ExecuteScalar();
            }
        }
    }
}