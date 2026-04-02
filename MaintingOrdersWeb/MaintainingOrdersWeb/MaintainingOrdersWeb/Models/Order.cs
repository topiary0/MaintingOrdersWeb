using System;
using System.Collections.Generic;

namespace MaintainingOrdersWeb.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public DateOnly OrderDate { get; set; }

    public int StatusId { get; set; }

    public decimal TotalPrice { get; set; }

    public string? DeliveryAddress { get; set; }

    public int ClientId { get; set; }

    public int UserId { get; set; }

    public int MethodId { get; set; }

    public virtual Client Client { get; set; } = null!;

    public virtual DeliveryMethod Method { get; set; } = null!;

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual OrderStatus Status { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
