﻿using Market.Models.Registration;
using Market.Models.Tables;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Market.Models
{
    public interface IOrderRepository
    {
        Task<int> AddOrder(OrderItem[] orderItems, string userId);
        Order[] GetOrdersForUser(string userId);
        Order[] GetAllOrders();
        Object UpdateOrder(Order order);
    }
    public class OrderRepository : IOrderRepository
    {
        private readonly MarketDBContext _context;
        private readonly UserManager<AppUser> _userManager;


        public OrderRepository(MarketDBContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<int> AddOrder(OrderItem[] orderItems, string userId)
        {
            AppUser user = _userManager.Users.SingleOrDefault(u => u.Id == userId);
            if (user.Customer_Id != null && orderItems.Length > 0)
            {
                Order order = new Order()
                {
                    Customer_Id = user.Customer_Id.Value,
                    Order_Date = DateTime.Now.Date,
                    Status = "Новый"
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();
                order = _context.Orders.Last();
                int orderId = order.Id;

                foreach (var orderItem in orderItems)
                {
                    orderItem.Order_Id = orderId;
                    orderItem.Item = null;
                }

                await _context.OrderItems.AddRangeAsync(orderItems);
                return await _context.SaveChangesAsync();
            }
            else return 0;
        }

        public Order[] GetOrdersForUser(string userId)
        {
            var customerId = _userManager.Users.SingleOrDefault(u => u.Id == userId).Customer_Id;
            var arr = _context.Orders.Where(o => o.Customer_Id == customerId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .ToArray();
            return arr;
        }

        public Order[] GetAllOrders()
        {

            var arr = _context.Orders

                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Item)
                .ToArray();
            return arr;
        }

        public Object UpdateOrder(Order order)
        {
            List<OrderItem> newOrderItems = new List<OrderItem>();
            foreach (var orderItem in order.OrderItems)
            {
                if(orderItem.Id == 0)
                {
                    newOrderItems.Append(orderItem);
                    order.OrderItems.Remove(orderItem);
                }                
               
            }
            _context.OrderItems.AddRange(newOrderItems);
            _context.Orders.Update(order);
            _context.SaveChanges();
            var newOrder = _context.Orders.SingleOrDefault(ord => ord.Id == order.Id);           
            return null;
        }

    }
}