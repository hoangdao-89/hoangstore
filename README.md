# HoangStore

HoangStore is an e-commerce web application built with ASP.NET Core MVC.  
The project supports product browsing, product variants, shopping cart, checkout, COD and VNPAY payments, order management, and administration features.

## Live Demo

http://hoangstore.runasp.net

## Main Features

### Customer
- Register and log in with ASP.NET Core Identity
- Browse, search, filter, and paginate products
- View product variants by size, color, price, image, and inventory
- Add products to cart using AJAX
- Update quantity, remove items, and calculate totals in real time
- Checkout with COD or VNPAY Sandbox
- View and manage personal orders

### Administrator
- Manage products, product variants, categories, and inventory
- Manage customer orders and controlled order-status transitions
- Search, filter, and paginate products and orders
- View revenue information
- Soft-delete products and automatically clean obsolete data

## Payment Integration

The project integrates VNPAY Sandbox with:

- HMAC-SHA512 request signing
- Transaction expiration handling
- Payment callback validation
- Automatic payment-status updates
- Automatic cancellation of expired unpaid VNPAY orders

## Tech Stack

- C#
- ASP.NET Core MVC
- Entity Framework Core
- SQL Server
- ASP.NET Core Identity
- JavaScript
- jQuery
- AJAX
- Bootstrap
- VNPAY Sandbox
- Git and GitHub

## Project Structure

```text
Areas/Admin           Admin controllers and views
Controllers           Customer-facing controllers
Data                  Database context and seeders
Migrations            Entity Framework Core migrations
Models                 Entities, enums, services, and view models
ViewComponents        Reusable view components
Views                  Razor views
wwwroot                CSS, JavaScript, images, and static files