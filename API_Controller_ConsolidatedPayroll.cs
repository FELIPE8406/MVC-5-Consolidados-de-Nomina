using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using WebApi.Models;   
using WebApi.Models.ConsolidatePayroll; 
using WebApi.Models.Employee;
using WebApi.Models.Paysheet;
using WebApi.Models.ProfilePosition;
using WebApi.Models.SystemResponses;

namespace WebApi.Controllers 
{
    [Route("api/[controller]")]
    [ApiController]
    //  [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ConsolidatePayrollController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ConsolidatePayrollController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Create Templates No programers
        /// </summary>
        /// <remarks>
        /// Create Templates No programers
        /// </remarks>
        [Authorize(Roles = "Admin, ANomina")]
        [HttpPost("[action]")]
        public bool CreationTemplates(NoveltyData data)
        {
            try
            {
                bool response = false;
                SqlConnection conn = (SqlConnection)_context.Database.GetDbConnection();
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = "NovasoftPruebas.dbo.mi_sp_crea_programacion_plantilla";
                cmd.Parameters.Add("@strConvenio", System.Data.SqlDbType.VarChar, 10).Value = data.Agreement.Trim();
                cmd.Parameters.Add("@dtmCorte", System.Data.SqlDbType.DateTime).Value = data.Date.Trim();
                cmd.Parameters.Add("@TipoLiq", System.Data.SqlDbType.VarChar).Value = data.ConsolidateType;
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response = rdr.GetBoolean(0);
                }
                conn.Close();
                return response;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Consultar consolido nomina
        /// </summary>
        /// <remarks>
        /// Consultar consolidado nomina
        /// </remarks>
        //     [Authorize(Roles = "Admin, AContratacion")]
        [HttpPost]
        [Route("ConsolidadoNomina")]
        public Array ConsolidadoNomina(Parameters Parameters)
        {
            var response = new List<Consolidate>();
            var sql = "Select * " +
                "From( " +
                "    Select a.cod_emp, cod_cont, val_liq, b.*, d.sal_bas, Case c.ind_pdias when 1 then 365 else 360 end Dias, a.fec_cte, a.tip_liq, c.cod_conv, c.nom_conv " +
                "    From NovasoftPruebas.dbo.rhh_liqhis a(Nolock) " +
                "    Inner join(Select cod_con, nom_con, mod_liq, ind_prs, ind_ces, ind_vadf, a.cod_bas, b.nom_bas " +
                "    From NovasoftPruebas.dbo.V_rhh_concep a Inner join  NovasoftPruebas.dbo.rhh_basesliq b (Nolock) On a.cod_bas= b.cod_bas " +
                "    Where (ind_prs= 1 or ind_ces = 1 or ind_vadf = 1) " +
                "    union all " +
                "    Select cod_con, nom_con, mod_liq, ind_prs, ind_ces, ind_vadf, a.cod_bas, b.nom_bas " +
                "    From NovasoftPruebas.dbo.V_rhh_concep a inner join NovasoftPruebas.dbo.rhh_basesliq b(Nolock) On a.cod_bas = b.cod_bas " +
                "    where cod_con = '000009' " +
                "	) as b On a.cod_con = b.cod_con and a.mod_liq = b.mod_liq " +
                "    Inner join NovasoftPruebas.dbo.rhh_convenio c(Nolock) On a.cod_conv = c.cod_conv Inner join NovasoftPruebas.dbo.rhh_emplea d(Nolock) On a.cod_emp = d.cod_emp " +
                "    Where  fec_cte between '" + Parameters.FecLiqI + "' and '" + Parameters.FecLiqF + "') Indiv " +
                "  Inner join( " +
                         "      Select a.cod_emp Empleado, NovasoftPruebas.[dbo].[fn_rhh_ContratoFch](a.cod_emp, Retiro, 1) Contrato " +
                "          From( " +
                "                  Select *, NovasoftPruebas.dbo.fn_rhh_ConvEmp(a.cod_emp, a.fec_con) Convenio " +
                "                  From( " +
                "                      Select a.cod_emp, a.fec_con, isnull(max(b.fec_ret), '20991231') retiro " +
                "                      From NovasoftPruebas.dbo.GTH_Contratos a Inner join NovasoftPruebas.dbo.rhh_hislab b " +
                "                      on a.cod_emp = b.cod_emp and a.cod_con = b.cod_con " +
                "                      Group by a.cod_emp, a.fec_con " +
                "                     ) as a " +
                "                  Where getdate() between a.fec_con and retiro " +
                "             ) as a " +
                "          ) EmpActCont " +
                "  On Indiv.cod_emp = EmpActCont.Empleado and Indiv.cod_cont = EmpActCont.Contrato ";

            //"    Where a.cod_conv = '" + Parameters.Agreement + "' and fec_cte between '" + Parameters.FecLiqI + "' and '" + Parameters.FecLiqF + "') Indiv " +


            SqlConnection conn = (SqlConnection)_context.Database.GetDbConnection();
            SqlCommand cmd = conn.CreateCommand();
            conn.Open();
            cmd.CommandText = sql;
            cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                response.Add(new Consolidate()
                {
                    cod_emp = rdr["cod_emp"].ToString(),
                    cod_cont = rdr["cod_cont"].ToString(),
                    val_liq = Convert.ToDouble(rdr["val_liq"]),
                    cod_con = (string)rdr["cod_con"],
                    nom_con = rdr["nom_con"].ToString(),
                    mod_liq = rdr["mod_liq"].ToString(),
                    ind_prs = (bool)rdr["ind_prs"],
                    ind_ces = (bool)rdr["ind_ces"],
                    ind_vadf = (bool)rdr["ind_vadf"],
                    cod_bas = rdr["cod_bas"].ToString(),
                    nom_bas = rdr["nom_bas"].ToString(),
                    Dias = rdr["Dias"].ToString(),
                    fec_cte = rdr["fec_cte"].ToString(),
                    tip_liq = rdr["tip_liq"].ToString(),
                    sal_bas = (decimal)rdr["sal_bas"],
                    cod_conv = rdr["cod_conv"].ToString(),
                    nom_conv = rdr["nom_conv"].ToString(),

                });
            }
            conn.Close();
            return response.ToArray();     }


        /// <summary>
        /// Consultar reporte nomina208 (Consolidados)
        /// </summary>
        /// <remarks>
        /// Consultar reporte nomina208 (Consolidados)
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom208(Parameters Parameters)
        {
            try
            {
                var response = new List<nom208>();
                var sql = "NovasoftPruebas.dbo.sp_rhh_RepNom208";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;
                //cmd.Parameters.AddWithValue("@cod_cia", "%");
                //cmd.Parameters.Add("@CodSuc", System.Data.SqlDbType.VarChar, 10).Value = "%";
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 20).Value = "%";
                //cmd.Parameters.Add("@cod_cla1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                cmd.Parameters.Add("@Codemp", System.Data.SqlDbType.VarChar, 12).Value = Parameters.Employee_Id;
                //cmd.Parameters.Add("@Codcon", System.Data.SqlDbType.VarChar, 6).Value = "000000";
                //cmd.Parameters.Add("@CodconFin", System.Data.SqlDbType.VarChar, 6).Value = "999999";
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2022-01-10");
                //cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2022-01-30");
                cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = parameters.FecLiqI;
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = parameters.FecLiqF;
                ////////cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = "2022-04-01";
                ////////cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = "2022-04-26";
                cmd.Parameters.Add("@TipLiq", System.Data.SqlDbType.VarChar, 2).Value = "14";
                //cmd.Parameters.Add("@Origen", System.Data.SqlDbType.VarChar, 1).Value = "h";
                //cmd.Parameters.Add("@CodConv", System.Data.SqlDbType.VarChar, 15).Value = "%";
                //cmd.Parameters.Add("@IndFec", System.Data.SqlDbType.SmallInt).Value = 0;
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new nom208()
                    {
                        Cod_cia = rdr["Cod_cia"].ToString(),
                        nom_cia = rdr["nom_cia"].ToString(),
                        Cod_emp = rdr["Cod_emp"].ToString(),
                        ap1_emp = rdr["ap1_emp"].ToString(),
                        ap2_emp = rdr["ap2_emp"].ToString(),
                        nom_emp = rdr["nom_emp"].ToString(),
                        Nat_liq = rdr["Nat_liq"].ToString(),
                        Cod_con = rdr["Cod_con"].ToString(),
                        nom_con = rdr["nom_con"].ToString(),
                        val_liq = rdr["val_liq"].ToString(),
                        nom_liq = rdr["nom_liq"].ToString(),
                        Fec_liq = rdr["Fec_liq"].ToString(),
                        Mod_liq = rdr["Mod_liq"].ToString(),
                        can_liq = rdr["can_liq"].ToString(),
                        cod_conv = rdr["cod_conv"].ToString(),
                        nom_conv = rdr["nom_conv"].ToString(),
                        fec_cte = rdr["fec_cte"].ToString(),
                        Retroac = rdr["Retroac"].ToString()
                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }



        /// <summary>
        /// Consultar reporte nomina1011 (Prima)
        /// </summary>
        /// <remarks>
        /// Consultar reporte nomina1011 (Prima)
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom1011(Parameters Parameters)
        {
            try
            {
                var response = new List<RepNom1011>();
                var sql = "NovasoftPruebas.dbo.rs_rhh_RepNom1011";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;
                //cmd.Parameters.AddWithValue("@cod_cia", "%");
                //cmd.Parameters.Add("@CodSuc", System.Data.SqlDbType.VarChar, 10).Value = "%";
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 20).Value = "%";
                //cmd.Parameters.Add("@cod_cla1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                // cmd.Parameters.Add("@Codemp", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codcon", System.Data.SqlDbType.VarChar, 6).Value = "000000";
                //cmd.Parameters.Add("@CodconFin", System.Data.SqlDbType.VarChar, 6).Value = "999999";}
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqI);
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqF);
                ////cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2022-01-10");
                ////cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2022-01-30");
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI2;
                cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF2;
                cmd.Parameters.Add("@tipliq", System.Data.SqlDbType.VarChar, 2).Value = "14";
                //cmd.Parameters.Add("@Origen", System.Data.SqlDbType.VarChar, 1).Value = "h";
                //cmd.Parameters.Add("@cod_cco", System.Data.SqlDbType.VarChar, 15).Value = Parameters.Agreement;
                //cmd.Parameters.Add("@IndFec", System.Data.SqlDbType.SmallInt).Value = 0;
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new RepNom1011()
                    {

                        cod_emp = rdr["cod_emp"].ToString(),
                        //cod_con = rdr["cod_con"].ToString(),
                        //cod_cia = rdr["cod_cia"].ToString(),
                        //cod_suc = rdr["cod_suc"].ToString(),
                        //cod_cco = rdr["cod_cco"].ToString(),
                        //cod_cl1 = rdr["cod_cl1"].ToString(),
                        //cod_cl2 = rdr["cod_cl2"].ToString(),
                        //cod_cl3 = rdr["cod_cl3"].ToString(),
                        //cod_cl4 = rdr["cod_cl4"].ToString(),
                        //cod_cl5 = rdr["cod_cl5"].ToString(),
                        //cod_cl6 = rdr["cod_cl6"].ToString(),
                        //cod_cl7 = rdr["cod_cl7"].ToString(),
                        fec_liq = rdr["fec_liq"].ToString(),
                        fec_corte = rdr["fec_corte"].ToString(),
                        //tip_liq = rdr["tip_liq"].ToString(),
                        mes_cau = rdr["mes_cau"].ToString(),
                        //mes_lic = rdr["mes_lic"].ToString(),
                        //  val_liq = Convert.ToDecimal(rdr["val_liq"].ToString().Replace(".",",")),

                        val_liq = decimal.Parse(rdr["val_liq"].ToString().Replace(",", "."), System.Globalization.NumberStyles.Currency, CultureInfo.InvariantCulture),
                        val_provi = (decimal)rdr["val_provi"],
                        //val_dife = rdr["val_dife"].ToString(),
                        //b01_liq = rdr["b01_liq"].ToString(),
                        //b02_liq = rdr["b02_liq"].ToString(),
                        //b03_liq = rdr["b03_liq"].ToString(),
                        //b04_liq = rdr["b04_liq"].ToString(),
                        //b05_liq = rdr["b05_liq"].ToString(),
                        //b06_liq = rdr["b06_liq"].ToString(),
                        //b07_liq = rdr["b07_liq"].ToString(),
                        //b08_liq = rdr["b08_liq"].ToString(),
                        //b09_liq = rdr["b09_liq"].ToString(),
                        //b10_liq = rdr["b10_liq"].ToString(),
                        //b11_liq = rdr["b11_liq"].ToString(),
                        //b12_liq = rdr["b12_liq"].ToString(),
                        //b13_liq = rdr["b13_liq"].ToString(),
                        //b14_liq = rdr["b14_liq"].ToString(),
                        //b15_liq = rdr["b15_liq"].ToString(),
                        //b16_liq = rdr["b16_liq"].ToString(),
                        //b17_liq = rdr["b17_liq"].ToString(),
                        //b18_liq = rdr["b18_liq"].ToString(),
                        //b19_liq = rdr["b19_liq"].ToString(),
                        //b20_liq = rdr["b20_liq"].ToString(),
                        //cod_cont = rdr["cod_cont"].ToString(),
                        //Cons_MesAnt = rdr["Cons_MesAnt"].ToString(),
                        //Cons_PagosAnt = rdr["Cons_PagosAnt"].ToString(),
                        //Cons_PagosMes = rdr["Cons_PagosMes"].ToString(),
                        //ValProvMes = rdr["ValProvMes"].ToString(),
                        //IDL_Num = rdr["IDL_Num"].ToString(),
                        NombreEmp = rdr["NombreEmp"].ToString(),
                        //nom_suc = rdr["nom_suc"].ToString()

                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// Consultar reporte nomina1012 (Cesantías e intereses )
        /// </summary>
        /// <remarks>
        /// Consultar reporte nomina1012 (Cesantías e intereses )
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom1012(Parameters Parameters)
        {
            try
            {
                var response = new List<RepNom1012>();
                var sql = "NovasoftPruebas.dbo.rs_rhh_nom1012";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;
                //cmd.Parameters.AddWithValue("@cod_cia", "%");
                cmd.Parameters.Add("@Cod_emp", System.Data.SqlDbType.VarChar, 12).Value = Parameters.Employee_Id;
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 20).Value = "%";
                //cmd.Parameters.Add("@cod_cla1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codemp", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codcon", System.Data.SqlDbType.VarChar, 6).Value = "000000";
                //cmd.Parameters.Add("@CodconFin", System.Data.SqlDbType.VarChar, 6).Value = "999999";
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqI);
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqF);
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI2;
                cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF2;
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2021-12-04");
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2021-12-30");
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@TipLiq", System.Data.SqlDbType.VarChar, 2).Value = "14";
                //cmd.Parameters.Add("@Origen", System.Data.SqlDbType.VarChar, 1).Value = "h";
                //cmd.Parameters.Add("@CodConv", System.Data.SqlDbType.VarChar, 15).Value = "%";
                //cmd.Parameters.Add("@IndFec", System.Data.SqlDbType.SmallInt).Value = 0;
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new RepNom1012()
                    {
                        cod_emp = rdr["cod_emp"].ToString(),
                        //cod_con = rdr["cod_con"].ToString(),
                        //cod_cia = rdr["cod_cia"].ToString(),
                        //cod_suc = rdr["cod_suc"].ToString(),
                        //cod_cco = rdr["cod_cco"].ToString(),
                        //cod_cl1 = rdr["cod_cl1"].ToString(),
                        //cod_cl2 = rdr["cod_cl2"].ToString(),
                        //cod_cl3 = rdr["cod_cl3"].ToString(),
                        //cod_cl4 = rdr["cod_cl4"].ToString(),
                        //cod_cl5 = rdr["cod_cl5"].ToString(),
                        //cod_cl6 = rdr["cod_cl6"].ToString(),
                        //cod_cl7 = rdr["cod_cl7"].ToString(),
                        //cod_ano = rdr["cod_ano"].ToString(),
                        //cod_per = rdr["cod_per"].ToString(),
                        fec_liq = rdr["fec_liq"].ToString(),
                        fec_cte = rdr["fec_cte"].ToString(),
                        //tip_liq = rdr["tip_liq"].ToString(),
                        dia_cau = rdr["dia_cau"].ToString(),
                        dia_lic = rdr["dia_lic"].ToString(),
                        val_liq = (decimal)rdr["val_liq"],
                        int_ces = (decimal)rdr["int_ces"],
                        //reg_sal = rdr["reg_sal"].ToString(),
                        val_ant = (decimal)rdr["val_ant"],
                        //b01_liq = rdr["b01_liq"].ToString(),
                        //b02_liq = rdr["b02_liq"].ToString(),
                        //b03_liq = rdr["b03_liq"].ToString(),
                        //b04_liq = rdr["b04_liq"].ToString(),
                        //b05_liq = rdr["b05_liq"].ToString(),
                        //b06_liq = rdr["b06_liq"].ToString(),
                        //b07_liq = rdr["b07_liq"].ToString(),
                        //b08_liq = rdr["b08_liq"].ToString(),
                        //b09_liq = rdr["b09_liq"].ToString(),
                        //b10_liq = rdr["b10_liq"].ToString(),
                        //b11_liq = rdr["b11_liq"].ToString(),
                        //b12_liq = rdr["b12_liq"].ToString(),
                        //b13_liq = rdr["b13_liq"].ToString(),
                        //b14_liq = rdr["b14_liq"].ToString(),
                        //b15_liq = rdr["b15_liq"].ToString(),
                        //b16_liq = rdr["b16_liq"].ToString(),
                        //b17_liq = rdr["b17_liq"].ToString(),
                        //b18_liq = rdr["b18_liq"].ToString(),
                        //b19_liq = rdr["b19_liq"].ToString(),
                        //b20_liq = rdr["b20_liq"].ToString(),
                        val_prov_ces = (decimal)rdr["val_prov_ces"],
                        //val_prov_int = rdr["val_prov_int"].ToString(),
                        val_dife_ces = (decimal)rdr["val_dife_ces"],
                        //val_dife_int = rdr["val_dife_int"].ToString(),
                        //Int_Pag = rdr["Int_Pag"].ToString(),
                        //cod_cont = rdr["cod_cont"].ToString(),
                        //Cons_MesAnt = rdr["Cons_MesAnt"].ToString(),
                        //Cons_PagosAnt = rdr["Cons_PagosAnt"].ToString(),
                        //Cons_PagosMes = rdr["Cons_PagosMes"].ToString(),
                        //ValProvMes = rdr["ValProvMes"].ToString(),
                        //Cons_MesAntInt = rdr["Cons_MesAntInt"].ToString(),
                        //Cons_PagosAntInt = rdr["Cons_PagosAntInt"].ToString(),
                        //Cons_PagosMesInt = rdr["Cons_PagosMesInt"].ToString(),
                        //ValProvMesInt = rdr["ValProvMesInt"].ToString(),
                        //IDL_Num = rdr["IDL_Num"].ToString(),
                        NombreEmp = rdr["NombreEmp"].ToString(),
                        fec_ing = rdr["fec_ing"].ToString(),
                        sal_bas = (decimal)rdr["sal_bas"],
                        //nom_suc = rdr["nom_suc"].ToString(),
                        //nom_cco = rdr["nom_cco"].ToString(),
                        BASE = rdr["BASE"].ToString(),

                        //num_ide = rdr["num_ide"].ToString(),
                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// Consultar reporte nomina1013 (Vacaciones)
        /// </summary>
        /// <remarks>
        /// Consultar reporte nomina1013 (Vacaciones)
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom1013(Parameters Parameters)
        {
            try
            {
                var response = new List<RepNom1013>();
                var sql = "NovasoftPruebas.dbo.rs_rhh_RepNom1013";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;

                //cmd.Parameters.Add("@Cod_emp", System.Data.SqlDbType.VarChar, 12).Value = "1128475864";
                //cmd.Parameters.AddWithValue("@cod_cia", "%");
                //cmd.Parameters.Add("@CodSuc", System.Data.SqlDbType.VarChar, 10).Value = "%";
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 20).Value = "%";
                //cmd.Parameters.Add("@cod_cla1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codemp", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codcon", System.Data.SqlDbType.VarChar, 6).Value = "000000";
                //cmd.Parameters.Add("@CodconFin", System.Data.SqlDbType.VarChar, 6).Value = "999999";
                //  cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2021-12-04");
                // cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2022-04-22");
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI2;
                cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF2;
                cmd.Parameters.Add("@tipliq", System.Data.SqlDbType.VarChar, 2).Value = "14";
                cmd.Parameters.Add("@IndFec ", System.Data.SqlDbType.VarChar, 2).Value = "1";
                //cmd.Parameters.Add("@Origen", System.Data.SqlDbType.VarChar, 1).Value = "h";
                //cmd.Parameters.Add("@CodConv", System.Data.SqlDbType.VarChar, 15).Value = "%";
                //cmd.Parameters.Add("@IndFec", System.Data.SqlDbType.SmallInt).Value = 0;
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new RepNom1013()
                    {
                        cod_emp = rdr["cod_emp"].ToString(),
                        //cod_con = rdr["cod_con"].ToString(),
                        //cod_cia = rdr["cod_cia"].ToString(),
                        //cod_suc = rdr["cod_suc"].ToString(),
                        //cod_cco = rdr["cod_cco"].ToString(),
                        //cod_cl1 = rdr["cod_cl1"].ToString(),
                        //cod_cl2 = rdr["cod_cl2"].ToString(),
                        //cod_cl3 = rdr["cod_cl3"].ToString(),
                        //cod_cl4 = rdr["cod_cl4"].ToString(),
                        //cod_cl5 = rdr["cod_cl5"].ToString(),
                        //cod_cl6 = rdr["cod_cl6"].ToString(),
                        //cod_cl7 = rdr["cod_cl7"].ToString(),
                        fec_liq = rdr["fec_liq"].ToString(),
                        tip_liq = rdr["tip_liq"].ToString(),
                        //fec_ing = rdr["fec_ing"].ToString(),
                        //fec_Ini_Cau = rdr["fec_Ini_Cau"].ToString(),
                        //fec_fin_cau = rdr["fec_fin_cau"].ToString(),
                        Dia_cau = rdr["Dia_cau"].ToString(),
                        Dia_per_cau = rdr["Dia_per_cau"].ToString(),
                        val_vac = (decimal)rdr["val_vac"],
                        sal_bas = (decimal)rdr["sal_bas"],
                        //sal_prom = rdr["sal_prom"].ToString(),
                        val_acu = (decimal)rdr["val_acu"],
                        //dia_per_acum = rdr["dia_per_acum"].ToString(),
                        val_provi = (decimal)rdr["val_provi"],
                        val_dife = (decimal)rdr["val_dife"],
                        fec_cte = rdr["fec_cte"].ToString(),
                        Val_VacPag = (decimal)rdr["Val_VacPag"], 
                        //Dia_tot_vac = rdr["Dia_tot_vac"].ToString(),
                        //b01_liq = rdr["b01_liq"].ToString(),
                        //b02_liq = rdr["b02_liq"].ToString(),
                        //b03_liq = rdr["b03_liq"].ToString(),
                        //b04_liq = rdr["b04_liq"].ToString(),
                        //b05_liq = rdr["b05_liq"].ToString(),
                        //b06_liq = rdr["b06_liq"].ToString(),
                        //b07_liq = rdr["b07_liq"].ToString(),
                        //b08_liq = rdr["b08_liq"].ToString(),
                        //b09_liq = rdr["b09_liq"].ToString(),
                        //b10_liq = rdr["b10_liq"].ToString(),
                        //b11_liq = rdr["b11_liq"].ToString(),
                        //b12_liq = rdr["b12_liq"].ToString(),
                        //b13_liq = rdr["b13_liq"].ToString(),
                        //b14_liq = rdr["b14_liq"].ToString(),
                        //b15_liq = rdr["b15_liq"].ToString(),
                        //b16_liq = rdr["b16_liq"].ToString(),
                        //b17_liq = rdr["b17_liq"].ToString(),
                        //b18_liq = rdr["b18_liq"].ToString(),
                        //b19_liq = rdr["b19_liq"].ToString(),
                        //b20_liq = rdr["b20_liq"].ToString(),
                        cod_cont = rdr["cod_cont"].ToString(),
                        //Cons_AnoAnt = rdr["Cons_AnoAnt"].ToString(),
                        //Cons_MesAnt = rdr["Cons_MesAnt"].ToString(),
                        //Cons_PagosAnt = rdr["Cons_PagosAnt"].ToString(),
                        //Cons_PagosMes = rdr["Cons_PagosMes"].ToString(),
                        //ValProvMes = rdr["ValProvMes"].ToString(),
                        //IDL_Num = rdr["IDL_Num"].ToString(),
                        NombreEmp = rdr["NombreEmp"].ToString(),
                        //nom_cia = rdr["nom_cia"].ToString(),
                        //nom_cco = rdr["nom_cco"].ToString(),
                        //nom_suc = rdr["nom_suc"].ToString(),
                        //nom_cl1 = rdr["nom_cl1"].ToString(),
                        //nom_cl2 = rdr["nom_cl2"].ToString(),
                        //nom_cl3 = rdr["nom_cl3"].ToString(),
                        //num_ide = rdr["num_ide"].ToString(),

                        //Tot_ces = rdr["Tot_ces"].ToString(),

                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// Consultar reporte Detalle_Prima708 (Detalle_Prima) 
        /// </summary>
        /// <remarks>
        /// Consultar reporte Detalle_Prima708  (Detalle_Prima) 
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom708(Parameters Parameters)
        {
            try
            {
                var response = new List<RepNom708>();
                var sql = "NovasoftPruebas.dbo.RS_rhh_Repnom708";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;

                cmd.Parameters.Add("@ind_fec ", System.Data.SqlDbType.VarChar, 12).Value = "1";
                //cmd.Parameters.Add("@Codcia", System.Data.SqlDbType.VarChar, 3).Value = "%";
                //cmd.Parameters.Add("@CodSuc", System.Data.SqlDbType.VarChar, 3).Value = "%";
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 6).Value = "%";
                //cmd.Parameters.Add("@Cod_cl1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cl2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cl3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codconv", System.Data.SqlDbType.VarChar, 15).Value = "%";
                //cmd.Parameters.Add("@@ind_fec", System.Data.SqlDbType.DateTime).Value = "0";
                cmd.Parameters.Add("@fec_ini", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI2;
                cmd.Parameters.Add("@fec_fin", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF2;
                cmd.Parameters.Add("@tip_liq", System.Data.SqlDbType.VarChar, 2).Value = "02";
                //cmd.Parameters.Add("@@Idl", System.Data.SqlDbType.VarChar, 100).Value = "%";
                //cmd.Parameters.Add("@@Cod_Usuario_Rep", System.Data.SqlDbType.VarChar, 12).Value = "USER";
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new RepNom708()
                    {

                        Textbox18 = rdr["IDL_Num"].ToString(),
                        fec_liq = rdr["fec_liq"].ToString(),
                        fec_cte = rdr["fec_cte"].ToString(),
                        cod_conv = rdr["cod_conv"].ToString(),
                        textbox45 = "Sueldo Base",
                        textbox50 = "Valor Prima",
                        cod_emp = rdr["cod_emp"].ToString(),
                        NOMEMP = rdr["NOMEMP"].ToString(),
                        base_1 = rdr["base"].ToString(),
                        mes_cau = rdr["mes_cau"].ToString(),
                        mes_lic = rdr["mes_lic"].ToString(),
                        Sal_bas = rdr["Sal_bas"].ToString(),
                        Subtrans = rdr["Subtrans"].ToString(),
                        HorExtr = rdr["HorExtr"].ToString(),
                        //  HorExtr2 = rdr["HorExtr2"].ToString(),
                        OTROS = rdr["OTROS"].ToString(),
                        val_liq = rdr["val_liq"].ToString(),
                        b01_liq = rdr["b01_liq"].ToString(),
                        b02_liq = rdr["b02_liq"].ToString(),
                        b03_liq = rdr["b03_liq"].ToString(),

                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }


        /// <summary>
        /// Consultar reporte nomina703 (Cesantías e intereses )
        /// </summary>
        /// <remarks>
        /// Consultar reporte nomina703 (Cesantías e intereses )
        /// </remarks>
        [HttpPost("[action]")]
        public Array ConsultarNom703(Parameters Parameters)
        {
            try
            {
                var tip_ces =0;
                if (Parameters.Tip_Liq == "2")
                {
                     tip_ces = 04;
                }
                else
                {
                     tip_ces = 07;
                }

                var response = new List<RepNom703>();
                var sql = "NovasoftPruebas.dbo.RS_rhh_Repnom703";
                SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                SqlCommand cmd = conn.CreateCommand();
                conn.Open();
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.CommandText = sql;
                //cmd.Parameters.AddWithValue("@cod_cia", "%");
                //cmd.Parameters.Add("@Cod_emp", System.Data.SqlDbType.VarChar, 12).Value = Parameters.Employee_Id;
                //cmd.Parameters.Add("@CodCco", System.Data.SqlDbType.VarChar, 20).Value = "%";
                //cmd.Parameters.Add("@cod_cla1", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla2", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@cod_cla3", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codemp", System.Data.SqlDbType.VarChar, 12).Value = "%";
                //cmd.Parameters.Add("@Codcon", System.Data.SqlDbType.VarChar, 6).Value = "000000";
                //cmd.Parameters.Add("@CodconFin", System.Data.SqlDbType.VarChar, 6).Value = "999999";
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqI);
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime(parameters.FecLiqF);
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@FecIni", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI2;
                cmd.Parameters.Add("@FecFin", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF2;
                //cmd.Parameters.Add("@Fechai", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2021-12-04");
                //cmd.Parameters.Add("@Fechaf", System.Data.SqlDbType.DateTime).Value = Convert.ToDateTime("2021-12-30");
                //cmd.Parameters.Add("@FecLiqI", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqI;
                //cmd.Parameters.Add("@FecLiqF", System.Data.SqlDbType.DateTime).Value = Parameters.FecLiqF;
                cmd.Parameters.Add("@TipLiq", System.Data.SqlDbType.VarChar, 2).Value = tip_ces;
                //cmd.Parameters.Add("@Origen", System.Data.SqlDbType.VarChar, 1).Value = "h";
                //cmd.Parameters.Add("@CodConv", System.Data.SqlDbType.VarChar, 15).Value = "%";
                //cmd.Parameters.Add("@IndFec", System.Data.SqlDbType.SmallInt).Value = 0;
                cmd.CommandTimeout = _configuration.GetValue<Int32>("TiempoEspera");
                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    response.Add(new RepNom703()
                    {

                        fec_liq = rdr["fec_liq"].ToString(),
                        fec_cte = rdr["fec_cte"].ToString(),
                        cod_conv = rdr["cod_conv"].ToString(),
                        textbox45 = "Sueldo Base",
                        textbox50 = "Cesantias",
                        fdo_ces = rdr["fdo_ces"].ToString(),
                        nom_fdo = rdr["nom_fdo"].ToString(),
                        cod_emp = rdr["cod_emp"].ToString(),
                        NOMEMP = rdr["NOMEMP"].ToString(),
                        base1 = (decimal)rdr["base"],
                        Sal_bas = (decimal)rdr["Sal_bas"],
                        dia_cau = rdr["dia_cau"].ToString(),
                        dia_lic = rdr["dia_lic"].ToString(),
                        Tot_ces = (decimal)rdr["Tot_ces"],

                    });
                }
                conn.Close();

                return response.ToArray();
            }
            catch (Exception ex)
            {

                throw ex;
            }
        }





        /// <summary>
        /// Consultar conceptos nomina
        /// </summary>
        /// <remarks>
        /// Consultar conceptos nomina
        /// </remarks>
        //     [Authorize(Roles = "Admin, AContratacion")]
        [HttpGet]
        [Route("ConceptosNomina")]
        public Array ConceptosNomina()
        {
            var response = new List<Concepts>();
            var sql = "select * from NovasoftPruebas.dbo.rhh_DefConcep";

            SqlConnection conn = (SqlConnection)_context.Database.GetDbConnection();
            SqlCommand cmd = conn.CreateCommand();
            conn.Open();
            cmd.CommandText = sql;
            SqlDataReader rdr = cmd.ExecuteReader();
            while (rdr.Read())
            {
                response.Add(new Concepts()
                {
                    cod_con = rdr["cod_con"].ToString(),
                    nom_con = rdr["nom_con"].ToString(),
                    mod_liq = rdr["mod_liq"].ToString(),
                    emp_apl = (string)rdr["emp_apl"],
                    apl_con = rdr["apl_con"].ToString(),
                    nom_pro = rdr["nom_pro"].ToString(),
                    cod_for = rdr["cod_for"].ToString(),
                    ind_sal = (bool)rdr["ind_sal"],
                    ind_pen = (bool)rdr["ind_pen"],
                    ind_rpr = (bool)rdr["ind_rpr"],
                    ind_ret = (bool)rdr["ind_ret"],
                    ind_nded = (bool)rdr["ind_nded"],
                    ind_bon = (bool)rdr["ind_bon"],
                    ind_prn = (bool)rdr["ind_prn"],
                    ind_prs = (bool)rdr["ind_prs"],
                    ind_prv = (bool)rdr["ind_prv"],
                    ind_ppa = (bool)rdr["ind_ppa"],
                    ind_ces = (bool)rdr["ind_ces"],
                    ind_caj = (bool)rdr["ind_caj"],
                    ind_icb = (bool)rdr["ind_icb"],
                    ind_sen = (bool)rdr["ind_sen"],
                    ind_ind = (bool)rdr["ind_ind"],
                    ind_vadf = (bool)rdr["ind_vadf"],
                    ind_vad = (bool)rdr["ind_vad"],
                    ind_valc = (bool)rdr["ind_valc"],
                    ind_vst = (bool)rdr["ind_vst"],
                    cta_deb = rdr["cta_deb"].ToString(),
                    cta_cre = rdr["cta_cre"].ToString(),
                    cco_deb = rdr["cco_deb"].ToString(),
                    cco_cre = rdr["cco_cre"].ToString(),
                    ter_deb = rdr["ter_deb"].ToString(),
                    ter_cre = rdr["ter_cre"].ToString(),
                    cod_rub = rdr["cod_rub"].ToString(),
                    ind_tes = rdr["ind_tes"].ToString(),
                    ben_tes = rdr["ben_tes"].ToString(),
                    cta_tes = rdr["cta_tes"].ToString(),
                    cod_bas = rdr["cod_bas"].ToString(),
                    dis_cco = (bool)rdr["dis_cco"],
                    ind_terc = rdr["ind_terc"].ToString(),
                    ind_pre = rdr["ind_pre"].ToString(),
                    ind_stras = (bool)rdr["ind_stras"],
                    ind_proy = (bool)rdr["ind_proy"],
                    cod_prov = rdr["cod_prov"].ToString(),
                    tipo_docxp = rdr["tipo_docxp"].ToString(),
                    ind_BonAl = (bool)rdr["ind_BonAl"],
                    ind_PagIndir = (bool)rdr["ind_PagIndir"],
                    int_cnt = (bool)rdr["int_cnt"],
                    ind_valnov = (bool)rdr["ind_valnov"],
                    ind_SoloConv = (bool)rdr["ind_SoloConv"],
                    ind_PagNoSal = (bool)rdr["ind_PagNoSal"],
                    No_Facturable = (bool)rdr["No_Facturable"],
                    orden_liq = rdr["orden_liq"].ToString(),
                    Ind_sueldo = (bool)rdr["Ind_sueldo"],
                    Desc_DIAN = rdr["Desc_DIAN"].ToString(),
                    cod_clabono = rdr["cod_clabono"].ToString(),
                    con_nif = rdr["con_nif"].ToString(),
                    Ind_HExtra = (bool)rdr["Ind_HExtra"],
                    Cod_def_concep = rdr["Cod_def_concep"].ToString(),
                    ind_antc_EC = (bool)rdr["ind_antc_EC"],
                    ind_antc50_ec = (bool)rdr["ind_antc50_ec"],
                    ind_EgrAnt_ec = (bool)rdr["ind_EgrAnt_ec"],
                    conded_ec = rdr["conded_ec"].ToString(),
                    condev_ec = rdr["condev_ec"].ToString(),
                    ind_pagindir_ec = (bool)rdr["ind_pagindir_ec"],
                    ind_decter_ec = (bool)rdr["ind_decter_ec"],
                    ind_deccta_ec = (bool)rdr["ind_deccta_ec"],
                    ind_prod_ec = (bool)rdr["ind_prod_ec"],
                    int_niif = (bool)rdr["int_niif"],
                    niif_deb = rdr["niif_deb"].ToString(),
                    niif_cre = rdr["niif_cre"].ToString(),
                    ind_discconiif = (bool)rdr["ind_discconiif"],
                    ind_tercniif = rdr["ind_tercniif"].ToString(),
                    niif_tercdeb = rdr["niif_tercdeb"].ToString(),
                    niif_tercre = rdr["niif_tercre"].ToString(),
                    Ind_ValNeto = (bool)rdr["Ind_ValNeto"],
                    ind_NetoTodosTipLiq = (bool)rdr["ind_NetoTodosTipLiq"],
                    ind_ctt = (bool)rdr["ind_ctt"],
                    ind_Aux01 = (bool)rdr["ind_Aux01"],
                    ind_Aux02 = (bool)rdr["ind_Aux02"],
                    ind_Aux03 = (bool)rdr["ind_Aux03"],
                    ind_Aux04 = (bool)rdr["ind_Aux04"],
                    ind_Aux05 = (bool)rdr["ind_Aux05"],
                    ind_Aux06 = (bool)rdr["ind_Aux06"],
                    ind_Aux07 = (bool)rdr["ind_Aux07"],
                    ind_Aux08 = (bool)rdr["ind_Aux08"],
                    ind_Aux09 = (bool)rdr["ind_Aux09"],
                    ind_Aux10 = (bool)rdr["ind_Aux10"],
                    ind_Aux11 = (bool)rdr["ind_Aux11"],
                    ind_Aux12 = (bool)rdr["ind_Aux12"],
                    ind_Aux13 = (bool)rdr["ind_Aux13"],
                    ind_Aux14 = (bool)rdr["ind_Aux14"],
                    ind_Aux15 = (bool)rdr["ind_Aux15"],

                });
            }
            conn.Close();
            return response.ToArray();
        }
    }
}
