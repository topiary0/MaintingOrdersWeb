using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class Shipment
{
    public int ShipmentId { get; set; }

    public DateOnly ShipmentDate { get; set; }

    public string? Status { get; set; }

    public int SuppliersId { get; set; }

    public int UserId { get; set; }

    public int StatusshId { get; set; }

    public virtual ShipmentStatus Statussh { get; set; } = null!;

    public virtual Supplier Suppliers { get; set; } = null!;

    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new List<SupplyItem>();

    public virtual User User { get; set; } = null!;
}
