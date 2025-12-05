# Green Resource Monitor 💻🌱

A Windows-Based Energy and Environmental Impact Estimation Tool

## 📖 Overview

Modern software systems consume significant amounts of electrical energy, yet this consumption remains largely invisible to the average user. While tools exist for hardware diagnostics (like Task Manager or HWMonitor), few provide insight into the environmental cost of specific software applications.
Green Resource Monitor bridges this gap. It is a WPF desktop application that tracks per-process resource usage and translates technical metrics (CPU/RAM) into tangible environmental indicators:

- Energy Consumption (Wh)
- Carbon Footprint (gCO₂)
- Financial Cost (€)

By making the invisible cost of computing visible, this tool empowers developers and users to make more sustainable digital choices.

## ✨ Key Features

- 🚀 Real-Time Process Monitoring: Continuously scans active processes to calculate CPU utilization and Memory (Working Set) usage.
- ⚡ Energy Estimation Model: Uses a software-based algorithm derived from the processor's Thermal Design Power (TDP) to estimate Watt-hour consumption without requiring specialized hardware sensors.
- 🌱 Carbon & Cost Conversion: Automatically converts energy usage into Carbon Dioxide emissions and Euro cost based on regional grid data.
- 🌎 Regional Customization: Integrates with the [Electricity Maps API](https://app.electricitymaps.com/developer-hub/api/) to fetch real-time carbon intensity for your specific country (e.g., Bulgaria, Germany, USA), ensuring accurate local estimates.
- 📊 Historical Visualization: Built-in graphing tools (powered by ScottPlot) allow you to analyze trends over time, filtering by process or metric.
- 💾 Dual Storage System: Saves monitoring data to both CSV files (for portability) and SQL Server LocalDB (for high-performance historical analysis).
- 🔄 Robust Error Handling: Operates safely in User Mode without requiring Administrator privileges, gracefully handling system-protected processes.

## Screenshot
### The Dashboard
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 125455.png"/></p>

### 📊 Graph Window
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 130202.png"/></p>

### 💾 Data Export
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 130213.png"/></p>

### Settings Configuration
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 130301.png"/></p>

### 🌎 Country Selection
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 130314.png"/></p>

### 👨‍💼 About Page
<p align="center"><img src="./SCREENSHOTS/Screenshot 2025-12-05 130322.png"/></p>

<br><br/>
## 🚀 Getting Started

Prerequisites
- Operating System: Windows 10 or Windows 11 (x64)
- Runtime: .NET Framework 4.8 (Usually pre-installed on Windows 10/11)
- Database: SQL Server LocalDB (Optional, but recommended for historical features)


## Installation
1. Clone the repository:
```bash
git clone https://github.com/Dzhano/GreenResourceMonitor.git
```
2. Open the solution GreenResourceMonitor.sln in Visual Studio 2019/2022/2026.
3. If NuGet packages are not downloaded or doesn't load, restore them via NuGet Package Manager Console.
4. Build and Run (Ctrl + F5).

<br><br/>
## Contact
Dzhano Mihaylov - Developer<br/>
[LinkedIn Profile](https://www.linkedin.com/in/dzhano-mihaylov/)<br/>
Project Link: [https://github.com/Dzhano/GreenResourceMonitor](https://github.com/Dzhano/GreenResourceMonitor)<br/>
For more information, you can open my [Google Drive](https://drive.google.com/drive/folders/1favtV06oPGoXDmHvWQJEnq4VJPZdNmot?usp=sharing) where you can learn more about my project.