# Darbak E-Commerce

Darbak is a responsive automotive parts and accessories e-commerce web application built with **ASP.NET Core MVC**.

The platform provides a complete customer storefront and an administration area for managing products, categories, inventory, orders, users, reviews, testimonials, and product images.

## Live Website

**Darbak:**
https://darbak.runasp.net

---

## Main Features

### Customer

* Browse automotive products and categories
* Search by product or category name
* Partial-text search
* Category filtering and sorting
* Product details with multiple images
* Product ratings and reviews
* Shopping cart
* Guest cart using Session
* Quantity controls
* Wishlist
* Checkout
* Current and previous orders
* Order details
* Printable invoices
* Customer testimonials with ratings
* User profile management
* Responsive interface for desktop, tablet, and mobile

### Admin

* Admin dashboard
* Product CRUD
* Category CRUD
* Multiple product image management
* Category image upload
* Product stock management
* Product activation/deactivation
* Order management
* Order and payment status updates
* Review approval/rejection
* Testimonial approval/rejection
* User management
* User role management
* Advanced filtering across administration pages

---

## Technology Stack

* **.NET 10**
* **ASP.NET Core MVC**
* **ASP.NET Core Identity**
* **Entity Framework Core**
* **SQL Server**
* **Razor Views**
* **Bootstrap**
* **HTML5**
* **CSS3**
* **JavaScript**
* **Session**

---

## Authentication & Authorization

Authentication is implemented using **ASP.NET Core Identity**.

The application uses two roles:

* `Admin`
* `User`

Admin pages are protected using role-based authorization.

Users can register with:

* Full Name
* Email
* Password

Checkout requires authentication.

---

## Product Images

Products support multiple images.

The Admin can:

* Upload multiple images
* Choose a main image
* Add images while creating a product
* Add more images later
* Delete images
* Change the main image

Supported formats:

* JPG
* JPEG
* PNG
* WebP

Uploaded files are validated before being stored.

Product images are stored in:

```text
wwwroot/images/products
```

Category images are stored in:

```text
wwwroot/images/categories
```

---

## Shopping Cart

The shopping cart uses Session and works for guests.

Features include:

* Add products without login
* Increase/decrease quantity
* Prevent quantity above available stock
* Remove individual items
* Clear cart
* Dynamic total calculation
* Continue shopping after adding an item
* Login required before checkout

---

## Reviews & Testimonials

### Product Reviews

Users can submit:

* A written review
* A star rating

Reviews require Admin approval before being displayed publicly.

### Testimonials

Users can submit:

* Store feedback
* A `1–5` star rating

Testimonials also require Admin approval.

Approved testimonials are displayed in the storefront using a horizontal carousel.

---

## Orders & Invoices

Users can:

* Complete checkout
* View current orders
* View previous orders
* View order details
* View and print invoices

Invoices contain:

* Order number
* Order date
* Customer details
* Shipping information
* Products
* Quantities
* Unit prices
* Line totals
* Grand total
* Order status
* Payment status

Prices are displayed in **JD (Jordanian Dinar)**.

---

## Local Development

### Requirements

Install:

* .NET 10 SDK
* SQL Server / LocalDB
* Visual Studio 2022 or another compatible IDE

### Restore Packages

```bash
dotnet restore
```

### Configure Database

The development connection string is configured in:

```text
appsettings.json
```

Do not store production credentials in source control.

### Apply Migrations

```bash
dotnet ef database update
```

### Build

```bash
dotnet build
```

### Run

```bash
dotnet run
```

---

## Admin Account

Admin credentials should be configured using **User Secrets** or environment variables.

Example:

```bash
dotnet user-secrets set "AdminAccount:Email" "admin@example.com"
dotnet user-secrets set "AdminAccount:Password" "YOUR_SECURE_PASSWORD"
```

Do not commit real passwords to Git.

---

## Production Configuration

Sensitive production settings should be stored using environment variables.

Examples:

```text
ConnectionStrings__DefaultConnection
AdminAccount__Email
AdminAccount__Password
```

Production database passwords and other secrets must not be committed to the repository.

---

## Publishing

Recommended deployment workflow:

```text
Save All
→ Rebuild Solution
→ Publish to Folder
→ Compress published files
→ Upload to hosting
→ Extract with overwrite enabled
→ Restart application
→ Smoke Test
```

When deploying a new version, do **not** delete uploaded product or category image directories.

---

## Post-Deployment Check

After publishing, verify:

* Home page
* HTTPS
* Navigation
* Product catalog
* Product details
* Product images
* Search and filters
* Cart
* Wishlist
* Login/Register
* Checkout
* Orders
* Invoice
* Testimonials
* Admin dashboard
* Admin image upload
* Desktop and mobile layouts

---

## Source Control

Do not commit generated or local development files such as:

```text
.vs/
bin/
obj/
*.user
*.suo
*.pubxml.user
*.log
```

Before committing:

```bash
git status
git diff
```

---

## Security Notes

The project uses:

* ASP.NET Core Identity
* Role-based authorization
* Anti-forgery protection
* Server-side stock validation
* Server-side price validation
* Secure image validation
* Safe generated image filenames
* Ownership checks for protected resources
* HTTPS in production
* Environment variables / User Secrets for sensitive configuration

---

## Project

**Darbak — Precision Parts. Built for the Road.**
