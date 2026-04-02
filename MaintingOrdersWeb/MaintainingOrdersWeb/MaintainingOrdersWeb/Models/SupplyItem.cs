using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class SupplyItem
{
    public int ShipmentId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal PriceAtShipment { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Shipment Shipment { get; set; } = null!;
}
