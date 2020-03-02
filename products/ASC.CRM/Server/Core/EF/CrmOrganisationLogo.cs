﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.CRM.Core.EF
{
    [Table("crm_organisation_logo")]
    public partial class CrmOrganisationLogo
    {
        [Key]
        [Column("id", TypeName = "int(11)")]
        public int Id { get; set; }
        [Required]
        [Column("content", TypeName = "mediumtext")]
        public string Content { get; set; }
        [Required]
        [Column("create_by", TypeName = "char(38)")]
        public string CreateBy { get; set; }
        [Column("create_on", TypeName = "datetime")]
        public DateTime CreateOn { get; set; }
        [Column("tenant_id", TypeName = "int(11)")]
        public int TenantId { get; set; }
    }
}