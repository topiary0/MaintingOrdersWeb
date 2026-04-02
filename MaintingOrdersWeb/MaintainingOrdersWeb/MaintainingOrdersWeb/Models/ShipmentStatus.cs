using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class ShipmentStatus
{
    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
}
