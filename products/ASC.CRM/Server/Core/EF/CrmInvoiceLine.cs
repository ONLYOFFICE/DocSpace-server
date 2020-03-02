﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ASC.CRM.Core.EF
{
    [Table("crm_invoice_line")]
    public partial class CrmInvoiceLine
    {
        [Key]
        [Column("id", TypeName = "int(11)")]
        public int Id { get; set; }
        [Column("invoice_id", TypeName = "int(11)")]
        public int InvoiceId { get; set; }
        [Column("invoice_item_id", TypeName = "int(11)")]
        public int InvoiceItemId { get; set; }
        [Column("invoice_tax1_id", TypeName = "int(11)")]
        public int InvoiceTax1Id { get; set; }
        [Column("invoice_tax2_id", TypeName = "int(11)")]
        public int InvoiceTax2Id { get; set; }
        [Required]
        [Column("description", TypeName = "text")]
        public string Description { get; set; }
        [Column("quantity", TypeName = "decimal(10,2)")]
        public decimal Quantity { get; set; }
        [Column("price", TypeName = "decimal(10,2)")]
        public decimal Price { get; set; }
        [Column("discount", TypeName = "decimal(10,2)")]
        public decimal Discount { get; set; }
        [Column("sort_order", TypeName = "int(11)")]
        public int SortOrder { get; set; }
        [Column("tenant_id", TypeName = "int(11)")]
        public int TenantId { get; set; }
    }
}