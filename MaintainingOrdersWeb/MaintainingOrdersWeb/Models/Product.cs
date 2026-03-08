using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Article { get; set; } = null!;

    public string Name { get; set; } = null!;

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? PurchasePrice { get; set; }

    public int? Remains { get; set; }

    public decimal? Weight { get; set; }

    public string? Dimensions { get; set; }

    public int SuppliersId { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Supplier Suppliers { get; set; } = null!;

    public virtual ICollection<SupplyItem> SupplyItems { get; set; } = new List<SupplyItem>();
}
