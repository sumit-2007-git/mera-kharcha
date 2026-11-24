# ☀️ MeraKharcha - Smart Split Expense & Debt Settlement Platform

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-MVC-blue?style=for-the-badge)](https://asp.net)
[![SQLite](https://img.shields.io/badge/SQLite-003B57?style=for-the-badge&logo=sqlite&logoColor=white)](https://sqlite.org/)
[![Bootstrap 5.3](https://img.shields.io/badge/Bootstrap-5.3-7952B3?style=for-the-badge&logo=bootstrap&logoColor=white)](https://getbootstrap.com/)
[![SignalR](https://img.shields.io/badge/SignalR-RealTime_WebSockets-orange?style=for-the-badge)](https://dotnet.microsoft.com/apps/aspnet/signalr)
[![Design System](https://img.shields.io/badge/UI_Design-Solcon_Solar_Aesthetic-FF6B00?style=for-the-badge)](https://solcon.co.in/)

> **MeraKharcha** is a modern, high-performance collaborative group expense splitting and debt settlement web application designed specifically for the **Indian context** (Rupees ₹, UPI IDs, flatmates & vacation trips). Built on **ASP.NET Core 8.0 MVC** with a sleek **Solcon-inspired Solar Radiant aesthetic**, it completely replaces and outperforms apps like Splitwise and Tricount.

---

## 🌟 Top 5 Solid Features

### 1. 📸 AI Bill Scanner & Itemized Splitting ("Who Ate What?")
- Upload any restaurant, cafe, or grocery receipt (Image or PDF), or try one-click pre-configured sample bills (*Goa Beach Shack Dinner, CCD Cafe, DMart Groceries, Scuba Adventures*).
- AI automatically extracts vendor name, date, subtotal, GST/taxes, discounts, and itemized line items with quantities and prices.
- **Itemized Split Engine**: Assign individual items to participants with interactive checkboxes. System automatically computes each person's exact share, prorates taxes and discounts, and pre-fills the exact split allocations.

### 2. 📲 Dynamic NPCI UPI QR Codes & 1-Click Pay Deep-linking
- Settle debts instantly via standard NPCI-compliant UPI URI schemes.
- Generates a live, scannable **UPI QR Code** pre-filled with the exact amount and recipient's UPI ID.
- **Direct App Deep-links**: 1-click launcher buttons for **Google Pay, PhonePe, Paytm, and BHIM** directly on mobile browsers.
- Includes a 1-click **Copy UPI ID** action and payment confirmation tracking (*Pending ➔ Paid ➔ Confirmed*).

### 3. 💬 WhatsApp Quick Chat Expense Logger & Polite Debt Reminders
- **Conversational Chat Logger**: Enter natural phrases into the in-app chat widget (e.g., *"Rahul paid 1200 for dinner"* or *"Priya spent 450 on coffee"*). An intelligent Natural Language Parser extracts the payer, amount, and category to log the expense in 1 second!
- **Polite Debt Reminders**: Next to every pending debt, a **"WhatsApp Reminder"** button generates a respectful, automated bilingual message (*Namaste Amit! Goa Trip ka hisaab settle karna hai...*) with direct deep-links to WhatsApp Web and Mobile.

### 4. 🌍 Multi-Currency Support with Real-time Forex Conversion
- Perfect for international group trips (Thailand, Dubai, Europe, US).
- Supports major currencies: **INR (₹), USD ($), EUR (€), AED (د.إ), THB (฿), GBP (£), SGD (S$), JPY (¥)**.
- Live exchange rate converter banner automatically converts foreign amounts to base INR while preserving original currency records.

### 5. 🎯 Smart Budgeting & Overspending Gauge Alerts
- Set group spending targets (e.g. *Goa Vacation Budget: ₹50,000* or *Flatmates Monthly Budget: ₹15,000*).
- Dynamic color-coded progress meter (*Green <75%, Yellow 75–95%, Red >95% Over Budget*).
- **SignalR Real-Time Alerts**: Pushes instant live WebSocket warning notifications to all group members when 80% or 100% of the budget is consumed.

---

## 🧠 Greedy Minimum-Transaction Settlement Engine
When multiple people in a group pay for different expenses, a chaotic web of pairwise debts is formed. MeraKharcha uses a **Minimum Cash Flow Graph Reduction Algorithm**:
- Net balance calculation for all members ($TotalPaid - TotalShare$).
- Separates net debtors and net creditors, then greedily pairs the largest debtor with the largest creditor.
- **Example**: In our demo Goa Trip with 4 members, it simplifies **9 chaotic bilateral debts down to just 3 direct payments**, completely clearing all balances!

---

## 🎨 Solcon-Inspired Solar Design System
- **Palette**: Vibrant Solar Orange (`#FF6B00`), Radiant Sun Gold (`#FFA800`), Warm Amber (`#F59E0B`), and Deep Corporate Slate (`#0F172A`).
- **Typography**: Dual-tone gradient headings rendered in *Plus Jakarta Sans*.
- **Elevation**: Ambient diffuse shadows with subtle golden hover glows.
- **Glassmorphism**: Backdrop blur sticky navigation with radiant sun aura backgrounds.
- **Dark & Light Mode**: Seamless built-in theme toggle.

---

## 🛠️ Tech Stack & Architecture

- **Backend**: C# 12, ASP.NET Core 8.0 (MVC Architecture + Areas)
- **Database**: SQLite (`merakharcha.db` - zero configuration, highly portable) via Entity Framework Core 8
- **Authentication**: ASP.NET Core Identity (Roles: `SystemAdmin`, `GroupAdmin`, `User`)
- **Real-Time Communication**: Microsoft SignalR Hubs (`/hubs/group`)
- **Frontend**: Bootstrap 5.3, Bootstrap Icons, Chart.js 4.4, jQuery Unobtrusive Validation
- **Containerization**: Multi-stage production `Dockerfile` (optimized Linux runtime)

---

## 🚀 Running in Microsoft Visual Studio 2022

1. Open the project solution file:
   ```bash
   MeraKharcha.sln
   ```
   *(Or double-click `OpenInVisualStudio.bat`)*
2. In the top toolbar, ensure the launch profile is set to **`MeraKharcha`**.
3. Press **`F5`** or click the green **`▶ MeraKharcha`** play button.
4. Visual Studio will build the solution and launch your browser at:
   ```text
   http://localhost:5217
   ```

---

## 🔑 Pre-Seeded Demo Test Accounts

The SQLite database is auto-initialized on first run with realistic sample data (*Goa Trip 2026 🌴* & *Apartment 402 Flatmates 🏠*):

| Role | Email | Password | Access / Features |
| :--- | :--- | :--- | :--- |
| **Trip Organizer** | `rahul@merakharcha.com` | `User@123` | Goa Trip & Flatmates member, full balances & settlement rights |
| **Flat Owner** | `priya@merakharcha.com` | `User@123` | Apartment 402 Owner, recurring expenses manager |
| **Member** | `amit@merakharcha.com` | `User@123` | Group participant |
| **Member** | `sneha@merakharcha.com` | `User@123` | Group participant |
| **System Admin** | `admin@merakharcha.com` | `Admin@123` | Full access to `/Admin` system console & audit logs |

---

## 🌐 Deploying to the Cloud

### Option 1: Visual Studio Dev Tunnels (Instant 30-Second Public Link)
1. Next to the green Play button, click the dropdown arrow **(▼)**.
2. Select **`Dev Tunnels` ➔ `Create a Tunnel...`**.
3. Set Access to **`Public`** and click OK.
4. Run the project (`F5`) to get an instant worldwide HTTPS link (*e.g., `https://xxxx-5217.devtunnels.ms`*).

### Option 2: Render.com (24/7 Free Cloud Hosting)
1. Push your repository to GitHub.
2. Sign in to [Render.com](https://render.com) and click **New + ➔ Web Service**.
3. Select your repository `sumit-2007-git/mera-kharcha`.
4. Runtime: **Docker** (Uses the included multi-stage `Dockerfile`).
5. Click **Deploy Web Service** to get a live URL at `https://mera-kharcha.onrender.com`.

---

## 📄 License
This project is licensed under the [MIT License](LICENSE).
