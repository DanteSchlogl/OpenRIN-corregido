# OpenRIN — Comprehensive Alert and Monitoring Platform for Anticoagulated Patients

**Author:** Carlos Santiago Muñiz Cortés
**Professor:** Gastón Matías Weingand
**Institution:** Colegio Leonardo Da Vinci
**Course:** Prácticas Profesionalizantes III · **Year:** 2026

[Español](README.md) · [English] · [中文](README.zh-CN.md)

## Overview

OpenRIN is a desktop platform for hematology practices focused on the follow-up of anticoagulated patients: it centralizes patient and clinical record management, INR (RIN) measurement intake, automatic criticality classification with alert generation, appointment scheduling with smart reassignment, clinical follow-up with a contact-and-escalation protocol, effectiveness statistics reports, the patient portal, and the system administration and security tools.

The solution is built in **C# / .NET 8 (Windows Forms)** on **SQL Server**, with a layered architecture, and covers the practice's full circuit end to end: patient → INR control → alert → appointment → follow-up → report.

## What's new in this version (v1.0)

- **Proactive integrity**: per-row and per-table check digits (DVH/DVV), signed at the single save point and **verified at application startup, before login**.
- **Composite-pattern permissions**: a tree of atomic and composite permissions with unique codes, editable from within the system (check/uncheck groups) and applied to module gating.
- **Dynamic internationalization**: Spanish (Latin America), English and Simplified Chinese, with languages and captions **stored in the database** (no runtime resource sheets); hot language switching via the observer pattern.
- **Change control with restore**: audit history with before/after states and the *Restore previous state* action (recomposes and re-signs).
- **PDF via a third-party library** (QuestPDF, community license) for reports and histories, alongside **Excel** and **JSON** export.
- **Automatic backups**: daily backup of both databases (03:00) with integrity verification of every copy.
- **Complete Project Folder** (deliverable documentation, ~150 pages) under [`Documentacion/`](Documentacion/).

## Repository structure

- `Codigo/` — .NET solution: `Negocio.UI` (Windows Forms), `Negocio.BLL`, `Negocio.DAL`, `Negocio.DomainModel` and `Services` (DomainModel, BLL, DAL, Facade), plus the smoke tests.
- `Documentacion/` — **Project Folder (PDF)**: global description, class and data diagrams, business processes and use cases, and technical aspects.
- `Views/` — analysis and design deliverables (historical).
- `Propuesta Corregida/` — project proposal (TP 1).
- `ops/` — scripts for the daily automatic backup (03:00): SQL, scheduled task and README.

## Architecture

- **Layers**: Presentation (WinForms) → Business Logic (BLL) and Services Facade → Data Access (DAL) → Databases.
- **Two persistence paths**: **Entity Framework Core** for the business domain (context with Fluent configuration and a single save point) and **ADO.NET** for the services database (security, languages, permissions, logs).
- **Databases**: `OpenRIN_Negocio` (13 tables with integrity signatures and change auditing) and `OpenRIN_Services` (security, languages, permissions and logs).
- **Patterns**: Singleton (session), Composite (permissions), Observer (languages), Repository (patients) and Facade (technical services).

## Requirements

- Windows 10/11.
- .NET 8 (Desktop).
- SQL Server 2019 or later (Express edition is enough).

> Credentials and local configurations are **not** versioned in this repository.

## Running

1. Create the `OpenRIN_Negocio` and `OpenRIN_Services` databases and apply the structure and seed scripts (see `Codigo/`).
2. Configure the local connection string (configuration file not versioned).
3. Build and run: `dotnet build`, then the `Negocio.UI` project.

On startup, the application verifies database integrity **before** showing the login screen.

## Full documentation

The **[Project Folder](Documentacion/)** contains the complete deliverable document: global product description, per-layer class diagrams, data model, the five business processes and the **thirteen use cases** (with sequences, affected classes and real system screenshots), and the technical aspects (architecture, session, encryption, profiles, multi-language, activity log, change control, backups and integrity).

## Project status

Deliverable for November 2026. Next steps: installer, user manuals and online help.
