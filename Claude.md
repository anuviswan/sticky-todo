# Sticky TODO

The purpose of Sticky TODO is to combine the functionality of TODO apps and Sticky notes. It provides user to create TODOs, which can be viewed as sticky notes on a Windows 11. User can create TODOS via Windows 11 Sticky notes or using the Android App.

## Project Architecture

The solution would consist of

- Backend API
- Windows Widget Desktop App
- Android App

The solution would be an offline-first app, which would allow users to use app even if there is no internet. The synchornization would happen in the background whenever the connectivity is possible.

Core
├── Domain
├── Sync Engine
├── API Client

Windows (WPF)
├── Main App
├── Desktop Widgets
├── System Tray
├── Global Hotkeys

Android (MAUI)
├── Mobile UI
├── Notifications
├── Home Screen Widgets

ASP.NET Core Backend
├── Authentication
├── Sync
├── Push Notifications

---

## Tech Stack

### Backend

- ASP.Net Core
- Azure Tables
- .Net 10 or higher
- C# 14

### Mobile

- .Net MAUI
- SQL Lite

### Windows Widget

- WPF
- SQL Lite

---
