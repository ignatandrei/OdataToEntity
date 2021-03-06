﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OdataToEntity.Test.Model
{
    [Table("Categories", Schema = "dbo")]
    public sealed class Category
    {
        public ICollection<Category> Children { get; set; }
        public int Id { get; set; }
        [Required]
        public String Name { get; set; }
        public Category Parent { get; set; }
        public int? ParentId { get; set; }
        public DateTime? DateTime { get; set; }
    }

    public sealed class Customer
    {
        public String Address { get; set; }
        [InverseProperty(nameof(Order.AltCustomer))]
        public ICollection<Order> AltOrders { get; set; }
        [Key, Column(Order = 0), Required]
        public String Country { get; set; }
        [Key, Column(Order = 1)]
        public int Id { get; set; }
        [Required]
        public String Name { get; set; }
        [InverseProperty(nameof(Order.Customer))]
        public ICollection<Order> Orders { get; set; }
        public Sex? Sex { get; set; }
    }

    public abstract class OrderBase
    {
        //[ForeignKey(nameof(AltCustomer)), Column(Order = 0)]
        public String AltCustomerCountry { get; set; }
        //[ForeignKey(nameof(AltCustomer)), Column(Order = 1)]
        public int? AltCustomerId { get; set; }
        [Required]
        public String Name { get; set; }
    }

    public sealed class Order : OrderBase
    {
        [ForeignKey("AltCustomerCountry,AltCustomerId")]
        public Customer AltCustomer { get; set; }
        [ForeignKey("CustomerCountry,CustomerId")]
        public Customer Customer { get; set; }
        //[ForeignKey(nameof(Customer)), Column(Order = 0)]
        [Required]
        public String CustomerCountry { get; set; }
        //[ForeignKey(nameof(Customer)), Column(Order = 1)]
        public int CustomerId { get; set; }

        public DateTimeOffset? Date { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public ICollection<OrderItem> Items { get; set; }
        public OrderStatus Status { get; set; }
    }

    public sealed class OrderItem
    {
        public int? Count { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public Order Order { get; set; }
        public int OrderId { get; set; }
        public Decimal? Price { get; set; }
        [Required]
        public String Product { get; set; }
    }

    public class ManyColumnsBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Column01 { get; set; }
        public int Column02 { get; set; }
        public int Column03 { get; set; }
        public int Column04 { get; set; }
        public int Column05 { get; set; }
        public int Column06 { get; set; }
        public int Column07 { get; set; }
        public int Column08 { get; set; }
        public int Column09 { get; set; }
        public int Column10 { get; set; }
        public int Column11 { get; set; }
        public int Column12 { get; set; }
        public int Column13 { get; set; }
        public int Column14 { get; set; }
        public int Column15 { get; set; }
        public int Column16 { get; set; }
        public int Column17 { get; set; }
        public int Column18 { get; set; }
        public int Column19 { get; set; }
        public int Column20 { get; set; }
        public int Column21 { get; set; }
        public int Column22 { get; set; }
        public int Column23 { get; set; }
        public int Column24 { get; set; }
        public int Column25 { get; set; }
        public int Column26 { get; set; }
        public int Column27 { get; set; }
        public int Column28 { get; set; }
        public int Column29 { get; set; }
        public int Column30 { get; set; }
    }

    public sealed class ManyColumns : ManyColumnsBase
    {
    }

    public sealed class ManyColumnsView : ManyColumnsBase
    {
    }

    public enum OrderStatus
    {
        Unknown,
        Processing,
        Shipped,
        Delivering,
        Cancelled
    }

    public enum Sex
    {
        Male,
        Female
    }
}
