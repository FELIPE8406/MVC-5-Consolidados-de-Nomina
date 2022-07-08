using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MEE.Models.ConsolidatePayroll
{
    public class Parameters
    {
        public int Id { get; set; }
        [Display(Name = "Empleado")]
        public string Employee_Id { get; set; }
        public string strConvenio { get; set; }        

        [Display(Name = "Compañia")]
        public string Nit { get; set; }

        [Display(Name = "Sucursal")]
        public string Agreement { get; set; }

        [Display(Name = "Centro Costo")]
        public string Centro_costo { get; set; }

        [Display(Name = "Fecha Inicial Individual")]
        public string FecLiqI { get; set; }

        [Display(Name = "Fecha Final Individual")]
        public string FecLiqF { get; set; }

        [Display(Name = "Fecha Inicial Reporte")]
        public string FecLiqI2 { get; set; }

        [Display(Name = "Fecha Final Reporte")]
        public string FecLiqF2 { get; set; }

        [Display(Name = "Tipo Liquidación")]
        public string Tip_Liq { get; set; }

        [Display(Name = "Tipo de Consolidado")]
        public int Tip_Con{ get; set; }
    }
}



