# Arduino-Based Hospital Nurse Call Monitoring System

Final year project — ND Mechatronics Engineering, Kaduna Polytechnic.

## Overview
A low-cost nurse call monitoring system combining an Arduino Uno-based 
firmware with a C# WinForms desktop application. The system detects patient 
call button presses using a hybrid interrupt/polling architecture and 
displays real-time alerts, response times, and event logs on a monitoring 
station.

## Repository Structure
- `Arduino_Firmware/` — Arduino sketch (.ino) controlling call detection, 
  interrupts, and UART serial communication
- `WinForms_App/` — C# WinForms desktop application for real-time alert 
  monitoring, logging, and response time tracking

## Key Features
- Hardware interrupt-driven detection with debounce filtering
- UART serial communication (9600 baud, 8N1)
- Real-time alert dashboard with three-state emergency banner
- Event logging with response time tracking and export

## Requirements
- Arduino Uno (or SMD variant)
- Windows PC with .NET Framework 4.8
- Visual Studio (for building from source)

## Author
Inioluwa — ND Mechatronics Engineering, Kaduna Polytechnic
