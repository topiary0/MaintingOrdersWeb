using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class Supplier
{
    public int SuppliersId { get; set; }

    public string SupplierName { get; set; } = null!;

    public string? Address { get; set; }

    public string? ContactInfo { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
